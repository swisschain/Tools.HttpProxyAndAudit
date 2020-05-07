
using System;
using System.Threading.Tasks;
using Autofac;
using HttpProxyAndAudit.WebApi;
using Lykke.Service.Session.Client;
using Lykke.Service.Session.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Swisschain.Sdk.Server.Common;

namespace HttpProxyAndAudit
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            services.AddOcelot();
        }

        protected override void ConfigureExt(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<AuditMiddleware>();
            app.UseOcelot().Wait();

        }

        protected override void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {
            base.RegisterEndpoints(endpoints);
        }

        protected override void ConfigureContainerExt(ContainerBuilder builder)
        {
            base.ConfigureContainerExt(builder);

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
