using System;
using System.Collections.Generic;
using SimpleJSON;

namespace MyLib 
{
    public class SaveGame
    {
        public static SaveGame saveGame;
        public JSONClass msgNameIdMap;

        public SaveGame()
        {
            saveGame = this;
            var file = System.IO.File.ReadAllText("nameMap.json");
            msgNameIdMap = JSON.Parse(file).AsObject;
        }

        public Util.Pair GetMsgID(string msgName) {
            foreach (KeyValuePair<string, JSONNode> m in msgNameIdMap) {
                if(m.Value[msgName] != null) {
                    int a = m.Value["id"].AsInt;
                    int b = m.Value[msgName].AsInt;
                    return new Util.Pair(a, b);     
                }
            }
            return null;
        }

        public string getModuleName(int moduleId) {
            Debug.Log ("find Module Name is "+moduleId);
            foreach (KeyValuePair<string, JSONNode> m in msgNameIdMap) {
                var job = m.Value.AsObject;
                if(job["id"].AsInt == moduleId) {
                    return m.Key;
                }
            }
            Debug.Log ("name map file not found  ");
            return null;
        }

        public string getMethodName(string module, int msgId) {
            var msgs = msgNameIdMap[module].AsObject;
            foreach (KeyValuePair<string, JSONNode> m in msgs) {
                if(m.Key != "id") {
                    if(m.Value.AsInt == msgId) {
                        return m.Key;
                    }
                }
            }
            return null;
        }

    }
}

