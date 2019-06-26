using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LevelScoreBackend.SignalR;
using LevelScoreBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LevelScoreBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private IHubContext<LevelScoreHub> _hubCtx;

        public TeamController(IHubContext<LevelScoreHub> hubCtx)
        {
            _hubCtx = hubCtx;
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> AddTeam([FromBody]string teamName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrWhiteSpace(teamName))
            {
                return BadRequest("Could not get teamname or teamname is empty");
            }

            using(new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Write))
            {
                if (Program.Teams.Exists(t => t.Name.Equals(teamName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return Conflict("A team with this name already exists");
                }

                var team = new Team { CurrentPoints = 0, Name = teamName, ID = Program.Teams.Max(t => t.ID, 0)+1 };
                Program.Teams.Add(team);

                var notifyTask = LevelScoreHub.UpdateAllClientsNewTeam(_hubCtx, team);
                Program.DataLogger.TeamsChanged();
                await notifyTask;
            }

            return Ok();
        }

        [HttpDelete("{id:min(1)}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> RemoveTeam(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            using(new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Write))
            {
                var team = Program.Teams.FirstOrDefault(t => t.ID == id);
                if (team == null)
                {
                    return NotFound();
                }
                Program.Teams.Remove(team);
                team.CurrentPoints = 0;
                
                var notifyTask = LevelScoreHub.UpdateAllClientsRemoveTeam(_hubCtx, team);
                Program.DataLogger.TeamsChanged();
                await notifyTask;
            }
            return Ok();
        }
    }
}
