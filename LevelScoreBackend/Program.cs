using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LevelScoreBackend
{
    public class Program
    {

        public static List<Level> Levels { get; private set; }
        public static List<Team> Teams { get; private set; }
        public static ReaderWriterLockSlim RWLockLevels { get; private set; }
        public static ReaderWriterLockSlim RWLockTeams { get; private set; }

        public static void Main(string[] args)
        {
            RWLockLevels = new ReaderWriterLockSlim();
            RWLockTeams = new ReaderWriterLockSlim();
            Levels = new List<Level>();
            Teams = new List<Team>();

            Task.Run(() =>
            {
                Thread.Sleep(1000);
                var ps = new ProcessStartInfo("http://localhost/")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            });
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                var ps = new ProcessStartInfo("http://localhost/admin")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            });

            try
            {
                CreateWebHostBuilder(args, "http://*:80").Build().Run();
            }
            catch (Exception)
            {
                CreateWebHostBuilder(args, "http://localhost:5000").Build().Run();
            }
            
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args, string url) => 
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(url)
                .UseStartup<Startup>();
    }
}
