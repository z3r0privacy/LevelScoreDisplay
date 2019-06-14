using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            try
            {
                Program.RWLockTeams.EnterWriteLock();
                if (Program.Teams.Exists(t => t.Name.Equals(teamName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return Conflict("A team with this name already exists");
                }
                var team = new Team { CurrentPoints = 0, Name = teamName, ID = Program.Teams.Max(t => t.ID, 0)+1 };
                Program.Teams.Add(team);
                
                var notifyTask = LevelScoreHub.UpdateAllClientsFull(_hubCtx);
                Program.DataLogger.TeamsChanged();
                await notifyTask;
            }
            finally
            {
                Program.RWLockTeams.ExitWriteLock();
            }
            return Ok();
        }

        [HttpDelete("{id:min(1)}")]
        public async Task<ActionResult> RemoveTeam(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                Program.RWLockTeams.EnterWriteLock();
                var team = Program.Teams.FirstOrDefault(t => t.ID == id);
                if (team == null)
                {
                    return NotFound();
                }
                Program.Teams.Remove(team);
                
                
                var notifyTask = LevelScoreHub.UpdateAllClientsFull(_hubCtx);
                Program.DataLogger.TeamsChanged();
                await notifyTask;
            }
            finally
            {
                Program.RWLockTeams.ExitWriteLock();
            }
            return Ok();
        }
    }
}
