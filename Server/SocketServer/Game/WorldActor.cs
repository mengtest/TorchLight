using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyLib
{
	/// <summary>
	/// 全局唯一的世界管理器 
	/// Actor 之间通信通过启动Actor都是通过ActorManager进行的
	/// 简化实现：
	///     避免锁区域添加死锁
	///     孩子调用Parent方法
	///     Parent不能直接调用孩子方法 需要通过Message方式中转一下
	/// 
	/// 启动定时的世界刷新机制Timer和Coroutine协程来执行
	/// 协作程序都在DispatchMessage中分发执行
	/// </summary>
	public class WorldActor : Actor
	{
		List<PlayerActor> players = new List<PlayerActor> ();
		List<AvatarInfo> newPlayer = new List<AvatarInfo> ();
		List<AvatarInfo> removePlayer = new List<AvatarInfo> ();

		public WorldActor ()
		{
		}

		//Update Task Task.Sleep() Timer Message 注入
		//必须要拿到一份Avatar的复制属性
		//切换当前TaskSyncContext
		//在当前Context下面运行
		//await 都会导致代码重新运行回到 Context线程下面
		/// <summary>
		/// Await 会将当前函数转化为多个函数片段状态机
		/// 每次将一个Action Post 给MessageQueue这个Context去执行 
		/// 接着进入等待状态
		/// </summary>
		private async Task UpdateWorld ()
		{
			while (!isStop) {
				await Task.Delay (1000);

				foreach (var p in newPlayer) {
					var gc = GCPlayerCmd.CreateBuilder ();
					gc.Result = "Add";
					gc.AvatarInfo = p;
					BroadcastToAll (gc); 
					var rp = GetPlayer (p.Id);
					CurrentPlayerToNew (rp);
				}

				newPlayer.Clear ();
				foreach (var p in removePlayer) {
					var gc = GCPlayerCmd.CreateBuilder ();
					gc.Result = "Remove";
					gc.AvatarInfo = p;
					BroadcastToAll (gc);
				}
				removePlayer.Clear ();

				//CollectP.Avatar Update Push ToClient
				//Monitor P Property Change
				foreach (var p in players) {
					var diff = await p.GetAvatarInfoDiff ();
					var gc = GCPlayerCmd.CreateBuilder ();
					gc.Result = "Update";
					gc.AvatarInfo = diff;
					BroadcastToAll (gc);
				}
			}
		}

		private PlayerActor GetPlayer (int id)
		{
			foreach (var p in players) {
				if (p.Id == id) {
					return p;
				}
			}
			return null;
		}

		/// <summary>
		/// 将当前服务器上的所有玩家同步给新玩家 
		/// </summary>
		/// <param name="player">Player.</param>
		private async void CurrentPlayerToNew (PlayerActor player)
		{
			foreach (var p in players) {
				if (p.Id != player.Id) {
					var info = await p.GetAvatarInfo ();
					var gc = GCPlayerCmd.CreateBuilder ();
					gc.Result = "Add";
					gc.AvatarInfo = info;
					player.SendCmd (gc);
				}
			}
		}



		void BroadcastToAll (GCPlayerCmd.Builder cmd)
		{
			foreach (var p in players) {
				p.SendCmd (cmd);
			}
		}


		void BroadcastToExclusiveMe (GCPlayerCmd.Builder cmd)
		{
			foreach (var p in players) {
            
			}    
		}


		protected override async System.Threading.Tasks.Task ReceiveMsg (ActorMsg msg)
		{
		}

		public override void Init ()
		{
			base.Init ();
			RunTask (UpdateWorld);
		}

		public async Task AddPlayer (PlayerActor player, AvatarInfo ainfo)
		{
			await _messageQueue;
			players.Add (player);
			newPlayer.Add (ainfo);
		}

		//伤害直接广播不用等待
		public async Task AddCmd(GCPlayerCmd.Builder cmd) {
			await _messageQueue;
			BroadcastToAll (cmd);
		}



		public async Task RemovePlayer (PlayerActor player, AvatarInfo ainfo)
		{
			await _messageQueue;
			players.Remove (player);
			removePlayer.Add (ainfo);
		}

	}
}

