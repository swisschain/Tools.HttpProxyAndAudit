
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using HttpProxyAndAudit.WebApi;
using Lykke.Service.Session.Client;
using Lykke.Service.Session.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Swagger;
using Swisschain.Sdk.Server.WebApi.ExceptionsHandling;

namespace HttpProxyAndAudit
{
    public class Startup2
    {
        public Startup2(IConfiguration configRoot)
        {
            ConfigRoot = configRoot;
            Config = ConfigRoot.Get<AppConfig>();
        }

        public IConfiguration ConfigRoot { get; }

        public AppConfig Config { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Config);
            services.AddOcelot();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<AuditMiddleware>();
            app.UseOcelot().Wait();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            if (!string.IsNullOrEmpty(Program.SessionClientUrl))
            {
                builder.RegisterClientSessionClient(Program.SessionClientUrl, Common.Log.EmptyLog.Instance);
            }
            else
            {
                builder.RegisterType<SessionClientFake>()
                    .As<IClientSessionsClient>()
                    .SingleInstance();
            }
        }
    }

    public class AppConfig
    {
    }

    public class SessionClientFake: IClientSessionsClient
    {
        public Task<int> GetActiveUsersCount()
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetActiveClientIdsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IClientSession> GetAsync(string sessionToken)
        {
            throw new NotImplementedException();
        }

        public Task<IClientSession> Authenticate(string clientId,
            string clientInfo,
            string partnetId = null,
            string application = null)
        {
            throw new NotImplementedException();
        }

        public Task SetTag(string sessionToken, string tag)
        {
            throw new NotImplementedException();
        }

        public Task RefreshSessionAsync(string sessionToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSessionIfExistsAsync(string sessionToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool?> ConfirmTradingSession(string clientId, string authId)
        {
            throw new NotImplementedException();
        }

        public Task ExtendTradingSession(string sessionToken, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public Task<TradingSessionModel> GetTradingSession(string sessionToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateTradingSession(string sessionToken, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsConfirmationsRequired(string clientId)
        {
            throw new NotImplementedException();
        }

        public Task CancelUnconfirmedSessions(string clientId)
        {
            throw new NotImplementedException();
        }
    }
}
