using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MyLib 
{
    public partial class Util {
        public static void Log(string msg) {
            Console.WriteLine(msg);
        }


        public class Pair
        {
            public int moduleId;
            public int messageId;

            public Pair (int a, int b)
            {
                moduleId = a;
                messageId = b;
            }
        }



        public static Pair GetMsgID (string name)
        {
            return SaveGame.saveGame.GetMsgID (name);
        }

        public static Pair GetMsgID (string moduleName, string name)
        {
            Debug.Log ("moduleName " + moduleName + " " + name);
            var mId = SaveGame.saveGame.msgNameIdMap [moduleName] ["id"].AsInt;
            var pId = SaveGame.saveGame.msgNameIdMap [moduleName] [name].AsInt;
            return new Pair (mId, pId);
        }

        public static string FlattenException(Exception exception){
            var stringBuilder = new StringBuilder();
            while(exception != null) {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);
                exception = exception.InnerException;
            }
            return stringBuilder.ToString();
        }

        public static void PrintStackTrace() {
            var st = new StackTrace(true);
            for(var i = 0; i < st.FrameCount; i++){
                var sf = st.GetFrame(i);
                Debug.Log(sf.ToString());
            }
        }
    }

    public class Debug{
        public static void Log(string msg) {
            Console.WriteLine(msg);
        }
        public static void LogError(string msg) {
            Console.WriteLine("Error:"+msg);
            Util.PrintStackTrace();
        }
        public static void LogWarning(string msg){
            Console.WriteLine(msg);
        
        }
    }
}

