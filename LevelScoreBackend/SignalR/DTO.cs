using LevelScoreBackend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend.SignalR
{
    public class ViewUpdate
    {
        public int ID { get; set; }
        public int Level { get; set; }
        public int  Score { get; set; }
        public int Target { get; set; }
        public string Name { get; set; }

        public static ViewUpdate Create(Team t)
        {
            using (new RWLockHelper(Program.RWLockTeams, RWLockHelper.LockMode.Read))
            {
                using (new RWLockHelper(Program.RWLockLevels, RWLockHelper.LockMode.Read))
                {
                    var currLevel = 0;
                    var pointsNextLvl = t.CurrentPoints;
                    while (currLevel < Program.Levels.Count && pointsNextLvl >= Program.Levels[currLevel].PointsRequired)
                    {
                        pointsNextLvl -= Program.Levels[currLevel].PointsRequired;
                        currLevel++;
                    }

                    var vu = new ViewUpdate
                    {
                        ID = t.ID,
                        Name = t.Name,
                        Level = currLevel + 1,
                        Score = pointsNextLvl
                    };

                    if (currLevel == Program.Levels.Count)
                    {
                        vu.Target = 200;
                    }
                    else
                    {
                        vu.Target = Program.Levels[currLevel].PointsRequired;
                    }

                    return vu;
                }
            }
        }
    }
}
