using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointsController : ControllerBase
    {
        private IHubContext<LevelScoreHub> _hubCtx;

        public PointsController(IHubContext<LevelScoreHub> hubCtx)
        {
            _hubCtx = hubCtx;
        }

        [HttpPost("{teamId:min(1)}")]
        public async Task<ActionResult> AddPoints(int teamId, [FromBody]int points)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (points == 0)
            {
                return BadRequest("Enter a positive number to add points or a negative number to remove points");
            }
            
            try
            {
                Program.RWLockTeams.EnterWriteLock();
                var team = Program.Teams.FirstOrDefault(t => t.ID == teamId);
                if (team == null)
                {
                    return NotFound();
                }
                var newPoints = team.CurrentPoints + points;
                if (newPoints < 0)
                {
                    return BadRequest("cannot remove points; the teams total points would be negative");
                }
                team.CurrentPoints = newPoints;

                var vu = ViewUpdate.Create(team);
                var notifyTask = _hubCtx.Clients.All.SendAsync("update_one", vu);
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
