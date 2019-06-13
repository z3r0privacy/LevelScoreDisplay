using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace LevelScoreBackend
{
    public class Datalogger
    {
        private int _teamUpdateCounter = 0;
        private int _levelUpdateCounter = 0;

        private readonly string _logPathTeams;
        private readonly string _logPathLevels;
        
        private static readonly object lockTeams = new object();
        private static readonly object lockLevels = new object();
        
        public Datalogger(string logFolder)
        {
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            _logPathTeams = Path.Combine(logFolder, "Teams");
            _logPathLevels = Path.Combine(logFolder, "Levels");

            if (!Directory.Exists(_logPathTeams))
            {
                Directory.CreateDirectory(_logPathTeams);
            }

            if (!Directory.Exists(_logPathLevels))
            {
                Directory.CreateDirectory(_logPathLevels);
            }
        }

        public void TeamsChanged()
        {
            lock (lockTeams)
            {
                Action final = () => { };
                
                try
                {
                    if (!(Program.RWLockTeams.IsReadLockHeld || Program.RWLockTeams.IsWriteLockHeld))
                    {
                        Program.RWLockTeams.EnterReadLock();
                        final = () => { Program.RWLockTeams.ExitReadLock(); };
                    }
                    
                    ++_teamUpdateCounter;
                    File.WriteAllText(Path.Combine(_logPathTeams, $"{_teamUpdateCounter}.json"), 
                        JsonConvert.SerializeObject(Program.Teams));
                }
                finally
                {
                    final();
                }
            }
        }
        
        public void LevelsChanged()
        {
            lock (lockLevels)
            {
                Action final = () => { };
                
                try
                {
                    if (!(Program.RWLockLevels.IsReadLockHeld || Program.RWLockLevels.IsWriteLockHeld))
                    {
                        Program.RWLockLevels.EnterReadLock();
                        final = () => { Program.RWLockLevels.ExitReadLock(); };
                    }
                    
                    ++_levelUpdateCounter;
                    File.WriteAllText(Path.Combine(_logPathLevels, $"{_levelUpdateCounter}.json"), 
                        JsonConvert.SerializeObject(Program.Levels));
                }
                finally
                {
                    final();
                }
            }
        }
    }
}