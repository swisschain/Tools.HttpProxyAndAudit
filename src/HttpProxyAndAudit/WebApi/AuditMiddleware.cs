using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

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
            var clientId = "";

            var path = "";
            var method = "";
            var protocol = "";
            var token = "";

            try
            {
                try
                {
                    token = GetToken(context);

                    clientId = await GetClientId(token);

                    path = context.Request?.Path;
                    method = context.Request?.Method;
                    protocol = context.Request?.Protocol;


                    //if (path.ToString().Contains("elasticsearch") && method == "POST")
                    //{
                    //    if (!path.ToString().StartsWith("/elasticsearch/logs*/_search"))
                    //        throw new NotImplementedException();

                    //    var request = (new StreamReader(context.Request.Body)).ReadToEnd();

                    //    var phace =
                    //        "{ \"match_phrase\": { \"fields.SourceContext\": { \"query\": \"MassTransit\" } } }";
                    //    var jo = JObject.Parse(request);
                    //    var p = JObject.Parse(phace);
                    //    var filter = jo["query"]["bool"]["filter"];
                    //    var arr = filter as JArray;
                    //    arr?.Add(p);
                    //    request = jo.ToString();


                    //    Console.WriteLine(request);

                    //    var mem = new MemoryStream();
                    //    var writer = new StreamWriter(mem);
                    //    writer.WriteLine(request);
                    //    writer.Flush();
                    //    mem.Seek(0, SeekOrigin.Begin);
                    //    context.Request.Body = mem;
                    //}
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERROR ON PRE_INVOKE: {path}", path);
                }


                var sw = new Stopwatch();
                sw.Start();

                await _next(context);
                sw.Stop();

                try
                {
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
                    _logger.LogError(ex, "ERROR ON POST_INVOKE: {path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR ON INVOKE: {path}", path);
            }
        }

        private ConcurrentDictionary<string, string> _clientChache = new ConcurrentDictionary<string, string>();

        private async Task<string> GetClientId(string token)
        {
            if (string.IsNullOrEmpty(token))
                return "none";

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
