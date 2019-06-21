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
using LevelScoreBackend.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public static IServiceProvider ServiceProvider { get; private set; }

        internal static string AdminPassword { get; private set; }

        public static int Main(string[] args)
        {
            var cmd = new CommandLineApplication();
            var useSslArg = cmd.Option("-s | --use-ssl <value>", "Use SSL", CommandOptionType.NoValue);
            var certArg = cmd.Option("-c | --certificate <value>", "Path to SSL certificate (pfx)", CommandOptionType.SingleValue);
            var passArg = cmd.Option("-p | --password <value>", "Password for SSL certificate", CommandOptionType.SingleValue);
            var subjectArg = cmd.Option("-n | --subject-name <value>", "Subject name of certificate from certificate store", CommandOptionType.SingleValue);
            var adminPasswordArg = cmd.Option("-a | --admin-password <value>", "Password needed to enter admin section", CommandOptionType.SingleValue);
            cmd.HelpOption("-? | -h | --help");

            cmd.OnExecute(() => {
                if (!adminPasswordArg.HasValue() || string.IsNullOrEmpty(adminPasswordArg.Value()))
                {
                    Console.WriteLine("No password for admin section defined");
                    return -4;
                }
                AdminPassword = adminPasswordArg.Value();

                if (!useSslArg.HasValue())
                {
                    Console.WriteLine("Starting without SSL");
                    return ParsedMain(false, false, null, null);
                }

                if (!subjectArg.HasValue() && !certArg.HasValue())
                {
                    Console.WriteLine("Certificate not specified");
                    return -3;
                }

                if (certArg.HasValue() && !File.Exists(certArg.Value())) {
                    Console.WriteLine("Certificate file not found");
                    return -1;
                }

                if (!passArg.HasValue() && certArg.HasValue())
                {
                    Console.WriteLine("Password not specified");
                    return -2;
                }

                return ParsedMain(true, subjectArg.HasValue(), subjectArg.HasValue() ? subjectArg.Value() : certArg.Value(), passArg.Value());
            });

            return cmd.Execute(args);
        }

        private static int ParsedMain(bool useSsl, bool useCertStore, string certID, string pass)
        {
            RWLockLevels = new ReaderWriterLockSlim();
            RWLockLevels.AddTag("RWLS_Levels");
            RWLockTeams = new ReaderWriterLockSlim();
            RWLockTeams.AddTag("RWLS_Teams");

            Levels = new List<Level>();
            Teams = new List<Team>();

            DataLogger = new Datalogger(Path.Combine(AppPath, "DataLogs", DateTime.Now.ToString("yyyyMMdd_HHmmss")));

            var port = 80;
            X509Certificate2 cert = null;

            if (useSsl)
            {
                if (useCertStore)
                {
                    cert = GetCertificateFromStore(certID);
                }
                else
                {
                    cert = new X509Certificate2(certID, pass);
                } 
                port = 443;
            }

            try
            {
                using (var host = CreateWebHostBuilder(IPAddress.Any, port, useSsl, cert).Build()) //.Run();
                {
                    ServiceProvider = host.Services;

                    var task = host.RunAsync();

                    var url = (useSsl ? "https" : "http") + "://localhost/";
                    var ps1 = new ProcessStartInfo(url)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    //Process.Start(ps1);
                    var ps2 = new ProcessStartInfo(url + "admin/")
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    //Process.Start(ps2);

                    task.Wait();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not use specified configuration. Using fallback at localhost:5000");
                Console.WriteLine(e.ToString());
                CreateWebHostBuilder(IPAddress.Loopback, 5000, false, null).Build().Run();
            }

            return 0;
        }

        private static IWebHostBuilder CreateWebHostBuilder(IPAddress bindAddr, int port, bool useSsl, X509Certificate2 certificate = null) =>
            WebHost.CreateDefaultBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(bindAddr, port, listenOptions =>
                    {
                        if (useSsl)
                        {
                            listenOptions.UseHttps(certificate);
                        }
                    });
                    if (useSsl)
                    {
                        options.Listen(bindAddr, 80);
                    }
                })
                .UseStartup<Startup>();

        private static X509Certificate2 GetCertificateFromStore(string subject)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindBySubjectDistinguishedName, "CN="+subject, false);
                return currentCerts.Count == 0 ? null : currentCerts[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
