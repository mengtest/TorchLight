using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyLib 
{
    /// <summary>
    /// 交换的数据都是需要不可修改的Message 而内部状态是可修改的
    /// 线程安全数据 
    /// http://www.codeproject.com/Articles/535635/Async-Await-and-the-Generated-StateMachine
    /// </summary>
    public class PlayerActor : Actor
    {
		private uint agentId;
        Agent agent;
        //当前时刻的状态
        private AvatarInfo avatarInfo;
        //上一帧更新的Avatar数据
        private AvatarInfo lastAvatarInfo = null;

        WorldActor world;
        public PlayerActor(uint id)
        {
            agentId = id;
            var server = ActorManager.Instance.GetActor<SocketServer>();
            agent = server.GetAgent(agentId);
            agent.actor = this;
            Debug.Log("PlayerActor "+agentId);
            world = ActorManager.Instance.GetActor<WorldActor>();
        }

		public async Task<AvatarInfo> GetAvatarInfo() {
            await _messageQueue;

            if(lastAvatarInfo == null){
                var na = AvatarInfo.CreateBuilder(avatarInfo);
                return na.Build();
            }
			var na1 = AvatarInfo.CreateBuilder(lastAvatarInfo);
			return na1.Build();
		}

        /// <summary>
        /// 得到玩家属性的diff
        /// public 的Async方法需要 和Actor自身同步 
		/// delta 数据压缩
		/// 	repeat的Delta数据压缩
        /// </summary>
        public async Task<AvatarInfo> GetAvatarInfoDiff() {
            await _messageQueue;
            if(lastAvatarInfo == null){
                var na = AvatarInfo.CreateBuilder(avatarInfo);
                lastAvatarInfo = avatarInfo;
                return na.Build();
            }
			var na1 = AvatarInfo.CreateBuilder();
			na1.Id = avatarInfo.Id;
			na1.X = avatarInfo.X;
			na1.Y = avatarInfo.Y;
			na1.Z = avatarInfo.Z;
			na1.Dir = avatarInfo.Dir;
			na1.HP = avatarInfo.HP;

            lastAvatarInfo = avatarInfo;
            return na1.Build();
        }
        //所有Async方法都应该自动补全这个await 函数 函数修饰器
        public async void SendCmd(GCPlayerCmd.Builder cmd) {
            await _messageQueue;

            agent.SendPacket(cmd, 0, 0); 
        }

        //在锁区域不要调用有福作用的函数
        /// <summary>
        /// 该函数已经在当前的Context下面执行了
        /// 如果一个Task await到 Time.Delay 那么Task就会从执行流里面被扔出去 
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="msg">Message.</param>
        protected override async System.Threading.Tasks.Task ReceiveMsg(ActorMsg msg)
        {
            if(!string.IsNullOrEmpty(msg.msg)) {
                var cmds = msg.msg.Split(' ');
                if(cmds[0] == "close"){
                    ActorManager.Instance.RemoveActor(Id);
                    Debug.Log("CloseActor "+Id);
                    world.RemovePlayer(this, AvatarInfo.CreateBuilder(avatarInfo).Build());
                }
            }else {
                Debug.Log("ReceivePacket "+Id+" p "+msg.packet.protoBody.ToString());
                if(msg.packet.protoBody.GetType() == typeof(CGPlayerCmd)){
                    var cmd = msg.packet.protoBody as CGPlayerCmd;
                    var cmds = cmd.Cmd.Split(' ');
					if (cmds [0] == "Login") {
						var ret = GCPlayerCmd.CreateBuilder ();
						ret.Result = string.Format ("Login {0}", Id);
						agent.SendPacket (ret, msg.packet.flowId, 0);
					} else if (cmds [0] == "InitData") {
						avatarInfo = cmd.AvatarInfo;
						avatarInfo.Id = Id;
						lastAvatarInfo = avatarInfo;
						var ainfo2 = AvatarInfo.CreateBuilder (avatarInfo);
						world.AddPlayer (this, ainfo2.Build ());
					} else if (cmds [0] == "UpdateData") {
						avatarInfo.X = cmd.AvatarInfo.X;
						avatarInfo.Y = cmd.AvatarInfo.Y;
						avatarInfo.Z = cmd.AvatarInfo.Z;
						avatarInfo.Dir = cmd.AvatarInfo.Dir;
						avatarInfo.HP = cmd.AvatarInfo.HP;
					} else if (cmds [0] == "Damage") {
						var gc = GCPlayerCmd.CreateBuilder ();
						gc.DamageInfo = cmd.DamageInfo;
						gc.Result = cmd.Cmd;
						world.AddCmd (gc);
					} else if (cmds [0] == "Skill") {
						var gc = GCPlayerCmd.CreateBuilder ();
						gc.SkillAction = cmd.SkillAction;
						gc.Result = cmd.Cmd;
						world.AddCmd (gc);
					} else if (cmds [0] == "Move") {//快速移动
						avatarInfo.X = cmd.AvatarInfo.X;
						avatarInfo.Y = cmd.AvatarInfo.Y;
						avatarInfo.Z = cmd.AvatarInfo.Z;
						avatarInfo.Dir = cmd.AvatarInfo.Dir;

						var gc = GCPlayerCmd.CreateBuilder ();
						gc.AvatarInfo = cmd.AvatarInfo;
						gc.AvatarInfo.Id = Id; 
						gc.Result = "Update";
						world.AddCmd (gc);
					} else if (cmds [0] == "Buff") {
						var gc = GCPlayerCmd.CreateBuilder ();
						gc.BuffInfo = cmd.BuffInfo;
						gc.Result = cmd.Cmd;
						world.AddCmd (gc);
					}
                }
            }
        }
    }
}

