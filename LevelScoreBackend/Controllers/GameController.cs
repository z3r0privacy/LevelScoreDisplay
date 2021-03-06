﻿using LevelScoreBackend.SignalR;
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
    public class GameController : ControllerBase
    {
        private IHubContext<LevelScoreHub> _hubCtx;

        public GameController(IHubContext<LevelScoreHub> hubCtx)
        {
            _hubCtx = hubCtx;
        }

        [HttpGet("title")]
        public ActionResult GetTitle()
        {
            return Ok(Program.GameTitle);
        }

        [HttpPut("title")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> UpdateTitle([FromBody]string title)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest("Title cannot be empty");
            }
            Program.GameTitle = title;
            await LevelScoreHub.SetNewTitle(_hubCtx);
            return Ok();
        }

        [HttpGet("levels")]
        public ActionResult GetLevels()
        {
            using (new RWLockHelper(Program.RWLockLevels, RWLockHelper.LockMode.Read))
            {
                if (Program.Levels.Count == 0)
                {
                    return Ok("");
                }
                var levels = Program.Levels.Select(l => l.PointsRequired.ToString()).Aggregate((a, b) => $"{a},{b}");
                return Ok(levels);
            }
        }

        [HttpPut("levels")]
        [Authorize(Policy = "Admin")]
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

            using (new RWLockHelper(Program.RWLockLevels, RWLockHelper.LockMode.Write))
            {
                Program.Levels.Clear();
                var l = 1;
                Program.Levels.AddRange(nums.Select(n => new Level { LevelID = l++, PointsRequired = n }));
                
                var notifyTask = LevelScoreHub.UpdateAllClientsFull(_hubCtx);
                Program.DataLogger.LevelsChanged();
                await notifyTask;
            }

            return Ok();
        }
    }
}
