using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HttpProxyAndAudit.WebApi
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(RequestDelegate next, IClientSessionsClient clientSessionsClient, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _clientSessionsClient = clientSessionsClient;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = GetToken(context);

            var clientId = "none";

            if (!string.IsNullOrEmpty(Program.SessionClientUrl))
            {
                try
                {
                    var session = await _clientSessionsClient.GetAsync(token);
                    clientId = session.ClientId;
                }
                catch (Exception)
                {
                }
            }



            var sw = new Stopwatch();
            sw.Start();

            var path = context.Request?.Path;
            var method = context.Request?.Method;
            var protocol = context.Request?.Protocol;

            await _next(context);
            sw.Stop();
            _logger.LogInformation("{message} {Protocol}, {Method}, {Path}, {StatusCode}, {TimeMs}, {TokenHash}, {clientId}",
                "Http audit",
                protocol,
                method,
                path,
                context.Response?.StatusCode,
                sw.ElapsedMilliseconds,
                token.GetHashCode(),
                clientId);
        }

        public string GetToken(HttpContext context)
        {

            var header = context.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(header))
                return null;

            var values = header.Split(' ');

            if (values.Length != 2)
                return null;

            if (values[0] != "Bearer")
                return null;

            return values[1];
        }
    }
}
