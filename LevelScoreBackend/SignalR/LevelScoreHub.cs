using LevelScoreBackend.Utils;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend.SignalR
{
    public class LevelScoreHub : Hub
    {
        public static async Task UpdateAllClientsNewTeam(IHubContext<LevelScoreHub> instance, Team team)
        {
            using (new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            {
                await instance.Clients.All.SendAsync("add_one", ViewUpdate.Create(team));
            }
        }

        public static async Task UpdateAllClientsRemoveTeam(IHubContext<LevelScoreHub> instance, Team team)
        {
            using (new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            {
                await instance.Clients.All.SendAsync("remove_one", ViewUpdate.Create(team));
            }
        }

        public static async Task UpdateAllClientsTeamChanged(IHubContext<LevelScoreHub> instance, Team team)
        {
            using (new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            {
                await instance.Clients.All.SendAsync("update_one", ViewUpdate.Create(team));
            }
        }

        public static async Task UpdateAllClientsFull(IHubContext<LevelScoreHub> instance)
        {
            using(new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            {
                var allVU = Program.Teams.Select(t => ViewUpdate.Create(t)).ToList();
                await instance.Clients.All.SendAsync("update_all", allVU);
            }
        }

        [HubMethodName("request_all")]
        public async Task RequestAllData()
        {
            using (new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            {
                var allVU = Program.Teams.Select(t => ViewUpdate.Create(t)).ToList();
                await Clients.Caller.SendAsync("update_all", allVU);
            }
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client {Clients.Caller} connected");
            await Clients.Caller.SendAsync("update_title", "Technorama Reisespiel");

            //using (new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            //{
            //    var allList = Program.Teams.Select(t => ViewUpdate.Create(t)).ToList();
            //    var t2 = Clients.Caller.SendAsync("update_all", allList);
            //    await t2;
            //}
            //await t1;
        }
    }
}
