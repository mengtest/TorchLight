
/*
Author: liyonghelpme
Email: 233242872@qq.com
*/

/*
Author: liyonghelpme
Email: 233242872@qq.com
*/
using Google.ProtocolBuffers;

namespace KBEngine
{
	using System; 
	using System.Collections; 
	using System.Collections.Generic;
	
	using MessageID = System.UInt16;
	using MyLib;
	
    public class Message 
    {
    	public MessageID id = 0;
		public string name;
		public Int16 msglen = -1;
		public System.Reflection.MethodInfo handler = null;
		public System.Reflection.MethodInfo[] argtypes = null;
		public sbyte argsType = 0;
			
		public static Dictionary<MessageID, Message> loginappMessages = new Dictionary<MessageID, Message>();
		public static Dictionary<MessageID, Message> baseappMessages = new Dictionary<MessageID, Message>();
		public static Dictionary<MessageID, Message> clientMessages = new Dictionary<MessageID, Message>();
		
		public static Dictionary<string, Message> messages = new Dictionary<string, Message>();

		public static void reset()
		{
			loginappMessages.Clear();
			baseappMessages.Clear();
			clientMessages.Clear();
			messages.Clear();
		}

		public Message() {
			id = 0;
			name = null;
			msglen = 0;
			handler = null;
			argsType = 0;
			argtypes = null;
		}

		
		
		public object[] createFromStream(MemoryStream msgstream)
		{
			if(argtypes.Length <= 0)
				return new object[]{msgstream};
			
			object[] result = new object[argtypes.Length];
			
			for(int i=0; i<argtypes.Length; i++)
			{
				result[i] = argtypes[i].Invoke(msgstream, new object[0]);
			}
			
			return result;
		}
		

		public IMessageLite handlePB(byte moduleId, System.UInt16 msgId, MemoryStream msgstream) {
			//var buf = msgstream.getbuffer ();
			IMessageLite msg =  Util.GetMsg (moduleId, msgId, msgstream.getBytString());
			return msg;
		}
    }
} 
