using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Logging;

namespace HttpProxyAndAudit
{
    public class Program
    {
        private sealed class RemoteSettingsConfig
        {
            public IReadOnlyCollection<string> RemoteSettingsUrls { get; set; }
        }

        public static string SessionClientUrl { get; set; }

        public static void Main(string[] args)
        {
            Console.Title = "Tools HttpProxyAndAudit";

            SessionClientUrl = Environment.GetEnvironmentVariable("SESSION_SERVICE_URL");

            var remoteSettingsConfig = ApplicationEnvironment.Config.Get<RemoteSettingsConfig>();

            using var loggerFactory = LogConfigurator.Configure("Tools", remoteSettingsConfig.RemoteSettingsUrls ?? Array.Empty<string>());
            
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("Application is being started");

                CreateHostBuilder(loggerFactory, remoteSettingsConfig).Build().Run();

                logger.LogInformation("Application has been stopped");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application has been terminated unexpectedly");
            }
        }

        private static IHostBuilder CreateHostBuilder(ILoggerFactory loggerFactory, RemoteSettingsConfig remoteSettingsConfig) =>
            new HostBuilder()
                .SwisschainService<Startup2>(options =>
                {
                    options.UseLoggerFactory(loggerFactory);
                    options.AddWebJsonConfigurationSources(remoteSettingsConfig.RemoteSettingsUrls ?? Array.Empty<string>());
                })
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureAppConfiguration((context, configurationBuilder) =>
                    {
                        ApplySettings(context.HostingEnvironment.ContentRootPath);
                        configurationBuilder.AddOcelot(context.HostingEnvironment);
                    });
                    builder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 5005, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
                    });
                });

        private static void ApplySettings(string root)
        {
            var downstreamScheme = Environment.GetEnvironmentVariable("DownstreamScheme");
            var downstreamHost = Environment.GetEnvironmentVariable("DownstreamHost");
            var downstreamPort = Environment.GetEnvironmentVariable("DownstreamPort");

            Console.WriteLine($"DownstreamScheme: {downstreamScheme}");
            Console.WriteLine($"downstreamHost: {downstreamHost}");
            Console.WriteLine($"downstreamPort: {downstreamPort}");

            if (string.IsNullOrEmpty(downstreamScheme))
            {
                throw new Exception("Environment variable DownstreamScheme is empty");
            }

            if (string.IsNullOrEmpty(downstreamHost))
            {
                throw new Exception("Environment variable DownstreamProtocol is empty");
            }

            if (string.IsNullOrEmpty(downstreamPort))
            {
                if (downstreamScheme == "http")
                    downstreamPort = "80";
                else if (downstreamScheme == "https")
                    downstreamPort = "443";
                else
                    throw new Exception("Environment variable DownstreamPort is empty");
            }

            var reader = new StreamReader(Path.Combine(root, "config.json"));
            var config = reader.ReadToEnd();
            reader.Dispose();
            config = config.Replace("$DownstreamScheme$", downstreamScheme).Replace("$DownstreamHost$", downstreamHost).Replace("$DownstreamPort$", downstreamPort);
            
            //Console.WriteLine("Config:");
            //Console.WriteLine(config);
            Console.WriteLine("Options - HttpPort (1 and 2): 5005");
            
            var writer = new StreamWriter(Path.Combine(root, "ocelot.config.json"));
            writer.Write(config);
            writer.Flush();
            writer.Dispose();
        }
    }
}
