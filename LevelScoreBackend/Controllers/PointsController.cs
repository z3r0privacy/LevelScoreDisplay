using LevelScoreBackend.AAA;
using LevelScoreBackend.SignalR;
using LevelScoreBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend.Controllers
{
    [Authorize]
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
            
            if (points < 0 && !User.HasClaim(c => c.Type == PasswordBasedLoginProvider.IS_ADMIN_CLAIM_TYPE && c.Value == "yes"))
            {
                return Forbid();
            }

            using(new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Write))
            {
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

                var notifyTask = LevelScoreHub.UpdateAllClientsTeamChanged(_hubCtx, team);
                Program.DataLogger.TeamsChanged();
                await notifyTask;
            }
            return Ok();
        }
    }
}
