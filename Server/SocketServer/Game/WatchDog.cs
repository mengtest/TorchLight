using System;

namespace MyLib 
{
    public class WatchDog : Actor
    {
        protected override async System.Threading.Tasks.Task ReceiveMsg(ActorMsg msg)
        {
            if(!string.IsNullOrEmpty(msg.msg)) {
                var cmds = msg.msg.Split(' ');
                if(cmds[0] == "open"){
                    var agentId = System.Convert.ToUInt32(cmds[1]);
                    var act = new PlayerActor(agentId);
                    Debug.Log("CreateActor "+agentId);
                    ActorManager.Instance.AddActor(act);
                }else if(cmds[0] == "close"){
                    var agentId = System.Convert.ToInt32(cmds[1]);

                }
            }
        }

    }
}

