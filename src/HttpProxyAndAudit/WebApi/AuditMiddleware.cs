using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            try
            {
                var token = GetToken(context);

                var clientId = await GetClientId(token);

                var path = context.Request?.Path;
                var method = context.Request?.Method;
                var protocol = context.Request?.Protocol;

                var sw = new Stopwatch();
                sw.Start();

                await _next(context);
                sw.Stop();

                var code = context.Response?.StatusCode;

                _logger.LogInformation(
                    "{message} {Protocol}, {Method}, {Path}, {StatusCode}, {TimeMs}, {TokenHash}, {clientId}",
                    "Http audit",
                    protocol,
                    method,
                    path,
                    code,
                    sw.ElapsedMilliseconds,
                    token?.GetHashCode(),
                    clientId);
            }
            catch (Exception ex)
            {
            }
        }

        private ConcurrentDictionary<string, string> _clientChache = new ConcurrentDictionary<string, string>();

        private async Task<string> GetClientId(string token)
        {
            if (!string.IsNullOrEmpty(Program.SessionClientUrl))
            {
                try
                {
                    if (_clientChache.TryGetValue(token, out var clientId))
                    {
                        return clientId;
                    }

                    var session = await _clientSessionsClient.GetAsync(token);
                    clientId = session?.ClientId ?? "none";
                    _clientChache.TryAdd(token, clientId);
                    return clientId;
                }
                catch (Exception)
                {
                    _clientChache.TryAdd(token, "none");
                }
            }

            return "none";
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
