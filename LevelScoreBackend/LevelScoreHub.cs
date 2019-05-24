using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend
{
    public class LevelScoreHub : Hub
    {
        public async Task SendSingleUpdate(ViewUpdate vu)
        {
            await Clients.All.SendAsync("update_one", vu);
        }

        public static async Task UpdateAllClientsFull(IHubContext<LevelScoreHub> instance)
        {
            Action finalize = () => { };
            try
            {
                // aqcuire lock if necessary and set finalize-method
                if (!Program.RWLockTeams.IsReadLockHeld && !Program.RWLockTeams.IsWriteLockHeld)
                {
                    Program.RWLockTeams.EnterReadLock();
                    finalize = () => Program.RWLockTeams.ExitReadLock();
                }

                var allVU = Program.Teams.Select(t => ViewUpdate.Create(t)).ToList();
                await instance.Clients.All.SendAsync("update_all", allVU);
            }
            finally
            {
                finalize();
            }
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client {Clients.Caller} connected");
            try
            {
                var t1 = Clients.Caller.SendAsync("update_title", "Technorama Reisespiel");
                Program.RWLockTeams.EnterReadLock();
                var allList = Program.Teams.Select(t => ViewUpdate.Create(t)).ToList();
                var t2 = Clients.Caller.SendAsync("update_all", allList);
                await t1;
                await t2;
            }
            finally
            {
                Program.RWLockTeams.ExitReadLock();
            }
        }
    }
}
