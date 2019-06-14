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
    public class LevelController : ControllerBase
    {
        private IHubContext<LevelScoreHub> _hubCtx;

        public LevelController(IHubContext<LevelScoreHub> hubCtx)
        {
            _hubCtx = hubCtx;
        }

        [HttpGet]
        public ActionResult GetLevels()
        {
            try
            {
                Program.RWLockLevels.EnterReadLock();
                if (Program.Levels.Count == 0)
                {
                    return Ok("");
                }
                var levels = Program.Levels.Select(l => l.PointsRequired.ToString()).Aggregate((a, b) => $"{a},{b}");
                return Ok(levels);
            }
            finally
            {
                Program.RWLockLevels.ExitReadLock();
            }
        }

        [HttpPut]
        public async Task<ActionResult> SetLevel([FromBody]string levels)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrWhiteSpace(levels))
            {
                return BadRequest("Could either not read level or required points is less than 1");
            }
            var nums = new List<int>();
            foreach (var p in levels.Split(','))
            {
                if (int.TryParse(p, out var i))
                {
                    nums.Add(i);
                }
                else
                {
                    return BadRequest("Malformed data. Provide comma seperated int-values");
                }
            }

            try
            {
                Program.RWLockLevels.EnterWriteLock();
                Program.Levels.Clear();
                var l = 1;
                Program.Levels.AddRange(nums.Select(n => new Level { LevelID = l++, PointsRequired = n }));
                
                var notifyTask = LevelScoreHub.UpdateAllClientsFull(_hubCtx);
                Program.DataLogger.LevelsChanged();
                await notifyTask;
            }
            finally
            {
                Program.RWLockLevels.ExitWriteLock();
            }

            return Ok();
        }
    }
}
