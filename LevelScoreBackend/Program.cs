using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LevelScoreBackend
{
    public class Program
    {
        public static string AppPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        public static List<Level> Levels { get; private set; }
        public static List<Team> Teams { get; private set; }
        public static ReaderWriterLockSlim RWLockLevels { get; private set; }
        public static ReaderWriterLockSlim RWLockTeams { get; private set; }
        
        public static Datalogger DataLogger { get; private set; }

        public static int Main(string[] args)
        {
            var cmd = new CommandLineApplication();
            var useSslArg = cmd.Option("-s | --use-ssl <value>", "Use SSL", CommandOptionType.NoValue);
            var certArg = cmd.Option("-c | --certificate <value>", "Path to SSL certificate (pfx)", CommandOptionType.SingleValue);
            var passArg = cmd.Option("-p | --password <value>", "Password for SSL certificate", CommandOptionType.SingleValue);
            cmd.HelpOption("-? | -h | --help");

            cmd.OnExecute(() => {
                if (!useSslArg.HasValue())
                {
                    Console.WriteLine("Starting without SSL");
                    return ParsedMain(false, null, null);
                }

                if (!certArg.HasValue() || !File.Exists(certArg.Value())) {
                    Console.WriteLine("Certificate file not defined or file not found");
                    return -1;
                }

                if (!passArg.HasValue())
                {
                    Console.WriteLine("Password not specified");
                    return -2;
                }

                return ParsedMain(true, certArg.Value(), passArg.Value());
            });

            return cmd.Execute(args);
        }

        private static int ParsedMain(bool useSsl, string certPath, string pass)
        {
            RWLockLevels = new ReaderWriterLockSlim();
            RWLockTeams = new ReaderWriterLockSlim();
            Levels = new List<Level>();
            Teams = new List<Team>();

            DataLogger = new Datalogger(Path.Combine(AppPath, "DataLogs", DateTime.Now.ToString("yyyyMMdd_HHmmss")));

            var port = 80;
            X509Certificate2 cert = null;

            if (useSsl)
            {
                cert = new X509Certificate2(certPath, pass);
                port = 443;
            }

            try
            {
                using (var test = CreateWebHostBuilder(IPAddress.Any, port, cert).Build()) //.Run();
                {
                    var task = test.RunAsync();

                    var url = (useSsl ? "https" : "http") + "://localhost/";
                    var ps1 = new ProcessStartInfo(url)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps1);
                    var ps2 = new ProcessStartInfo(url + "admin/")
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps2);

                    task.Wait();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not use specified configuration. Using fallback at localhost:5000");
                Console.WriteLine(e.ToString());
                CreateWebHostBuilder(IPAddress.Loopback, 5000, null).Build().Run();
            }

            return 0;
        }

        private static IWebHostBuilder CreateWebHostBuilder(IPAddress bindAddr, int port, X509Certificate2 certificate) =>
            WebHost.CreateDefaultBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(bindAddr, port, listenOptions =>
                    {
                        if (certificate != null)
                        {
                            listenOptions.UseHttps(certificate);
                        }
                    });
                })
                .UseStartup<Startup>();
    }
}
