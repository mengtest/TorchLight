using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using Google.ProtocolBuffers;
using System.Diagnostics;

namespace MyLib  
{
    public class MsgBuffer {
        public int position = 0;
        public System.Byte[] buffer ;
        public int Size {
            get {
                return buffer.Length-position;
            }
        }
    }

    /// <summary>
    /// Gate--->Forward OpenAgent To Somebody 
    /// 单线程程序
    /// </summary>
    public class Agent{
        private static uint maxId = 0;
        public uint id;

        Socket mSocket;
        ServerMsgReader msgReader;
        bool isClose = false;
        public Actor actor;
        public WatchDog watchDog;
        SocketServer server;
        List<MsgBuffer>msgBuffer = new List<MsgBuffer>();
        EndPoint ep;
        private byte[] mTemp = new byte[0x2000];

        public Agent(Socket socket){
            id = ++maxId;
            mSocket = socket;
            ep = mSocket.RemoteEndPoint;
            msgReader = new ServerMsgReader();
            msgReader.msgHandle = handleMsg;
            Debug.Log("AgentCreate "+id);
        }
        public void StartReceiving() {
            Debug.Log("StartReceiving "+id);
            if(mSocket != null && mSocket.Connected) {
                try{
                    if(watchDog != null) {
                        watchDog.SendMsg(string.Format("open {0}", id));
                    }
                    mSocket.BeginReceive(mTemp, 0, mTemp.Length, SocketFlags.None, OnReceive, mSocket);
                }catch(Exception exception) {
                    Util.Log(exception.Message);
                    Close(false);
                    Debug.LogError(exception.Message+" stack "+exception.StackTrace);
                }
            }
        }

        void handleMsg(KBEngine.Packet packet) {
            if(actor != null) {
                actor.SendMsg(packet);
            }
        }

        void OnReceive(IAsyncResult result) {
            int bytes = 0;
            try{
                bytes = mSocket.EndReceive(result);

            }catch(Exception exception) {
                Debug.LogError(exception.Message);
                Close(true);
            }
            if(bytes <= 0){
                Debug.LogError("bytes "+bytes);
                Close(true);
            }else {
                //MessageReader
                //BeginReceive
                uint num = (uint)bytes;
                msgReader.process(mTemp, num );
                try{
                    mSocket.BeginReceive(mTemp, 0, mTemp.Length, SocketFlags.None, OnReceive, mSocket);
                }catch(Exception exception2) {
                    Util.Log(exception2.Message);
                    Close(false);
                }
            }
        }

        void Close(bool b ) {
            if(isClose) {
                return;
            }
            if(mSocket != null  && mSocket.Connected) {
                Debug.LogError("CloseSocket");
                try {
                    mSocket.Shutdown(SocketShutdown.Both);
                    mSocket.Close();
                }catch(Exception exception) {
                    Debug.LogError(Util.FlattenException(exception));
                    //Util.PrintStackTrace();
                }
            }
            mSocket = null;
            isClose = true;

            if(actor != null) {
                actor.SendMsg(string.Format("close"));
            }   
            if(watchDog != null) {
                watchDog.SendMsg(string.Format("close {0}", id));
            }
            if(server != null) {
                server.RemoveAgent(this);
            }
        }


        /// <summary>
        /// 内部Actor将Agent要发送的消息推送给客户端 
        /// SendPacket 应该以SendBuff行驶发送
        /// 同一个Socket的Write Read只能加入一次 epoll 
        /// Read在初始化的时候加入
        /// Write在每次要写入的时候加入
        /// </summary>
        public void SendPacket(IBuilderLite retpb, uint flowId, int errorCode) {
            if(isClose) {
                return;
            }
            var bytes = ServerBundle.sendImmediateError(retpb, flowId, errorCode);
            Debug.Log("SendBytes: "+bytes.Length);
            lock(msgBuffer) {
                var mb = new MsgBuffer(){position=0, buffer = bytes};
                msgBuffer.Add(mb);
                if(msgBuffer.Count == 1){
                    try{
                        mSocket.BeginSend(mb.buffer, mb.position, mb.Size, SocketFlags.None, OnSend, null);
                    }catch(Exception exception){
                        Debug.LogError(exception.Message);
                        Close(false);
                    }
                }
            }
        }

        void OnSend(IAsyncResult result) {
            int num = 0;
            try {
                num = mSocket.EndSend(result);
            }catch(Exception exception){
                num = 0;
                Close(true);
                Debug.LogError(exception.Message);
                return;
            }

            lock(msgBuffer) {
                if(mSocket != null && mSocket.Connected) {
                    var mb = msgBuffer[0];
                    MsgBuffer nextBuffer = null;
                    if(mb.Size == num){
                        msgBuffer.RemoveAt(0);
                        if(msgBuffer.Count > 0){
                            nextBuffer = msgBuffer[0];
                        }
                    }else if(mb.Size > num) {
                        mb.position += num;
                        nextBuffer = msgBuffer[0];
                    }else {
                        //SendError
                        msgBuffer.RemoveAt(0);
                        if(msgBuffer.Count > 0){
                            nextBuffer = msgBuffer[0];
                        }
                    }
                    if(nextBuffer != null) {
                        try{
                            mSocket.BeginSendTo(nextBuffer.buffer, nextBuffer.position, nextBuffer.Size, SocketFlags.None, ep, new AsyncCallback(OnSend), null);
                        }catch(Exception exception) {
                            Debug.LogError(exception.Message);
                            Close(false);
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// Socket服务器
    /// EventLoop 启动
    /// 分发 确保在SocketServer所在的线程安全么？
    /// 避免线程全部使用Message投递机制 Actor的Message
    /// 
    /// Actor 要比较简单的调用另外一个Actor的方法
    /// 将方法调用转化为Message发送
    ///     隐藏：PushMessage和HandlerMsg的代码
    /// 
    /// HandlerMsg可以在类初始化的时候构建Method到Msg映射
    /// 通过Attribute调用方法的时候自动调用SendMsg方法 最后再调用实际的方法
    /// </summary>
    public class SocketServer : Actor
    {
        TcpListener mListener;
        int mListenerPort;

        public Thread mThread;
        Dictionary<uint, Agent> agents = new Dictionary<uint, Agent>();
        WatchDog dog;

        public bool  Start(int tcpPort)
        {
            try
            {
                mListenerPort = tcpPort;
                mListener = new TcpListener(IPAddress.Any, tcpPort);
                mListener.Start(50);
            } catch (Exception exception)
            {
                Util.Log(exception.Message);
                return false;
            }
            dog = ActorManager.Instance.GetActor<WatchDog>();
            Debug.Log("GetWatchDog "+dog);
            mThread = new Thread(new ThreadStart(this.ThreadFunction));
            mThread.Start();
            return true;
        }

        void AddAgent(Socket socket) {
            var item = new Agent(socket);
            item.watchDog = dog;
            lock(agents) {
                agents.Add(item.id, item);
            }
            item.StartReceiving();
        }
        public void RemoveAgent(Agent agent) {
            lock(agents) {
                agents.Remove(agent.id);
            }
        }

        public Agent GetAgent(uint agentId) {
            Agent agent = null;
            lock(agents) {
                var ok = agents.TryGetValue(agentId, out agent);
            }
            return agent;
        }

        void ThreadFunction() {
            while(!isStop) {
                if(this.mListener != null && mListener.Pending()  ) {
                    var socket = mListener.AcceptSocket();
                    AddAgent(socket);
                }
                Thread.Sleep(1);
            }
        }

    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("StartServer");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            var sg = new SaveGame();
            var am = new ActorManager();
            var dog = new WatchDog();
            am.AddActor(dog, true);
            var wd = new WorldActor();
            am.AddActor(wd, true);

            var ss = new SocketServer();
            am.AddActor(ss, true);

            ss.Start(10001);
            ss.mThread.Join();
            Console.WriteLine("EndServer");
        }
        static void myHandler(object sender, ConsoleCancelEventArgs args) {
            Debug.Log("ServerStop");
            ActorManager.Instance.Stop();
        }
    }
}
