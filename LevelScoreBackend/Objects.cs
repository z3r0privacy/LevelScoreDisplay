using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend
{
    public class Level
    {
        public int LevelID { get; set; }
        public int PointsRequired { get; set; }
    }

    public class Team
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int CurrentPoints { get; set; }
    }
}
