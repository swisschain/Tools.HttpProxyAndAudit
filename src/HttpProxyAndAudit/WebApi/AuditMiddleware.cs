using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Lykke.Common.Extensions;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
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
            var ip = "";
            var userAgent = "";
            var walletId = "";


            try
            {
                try
                {
                    token = GetToken(context);

                    clientId = await GetClientId(token);

                    path = context.Request?.Path;
                    method = context.Request?.Method;
                    protocol = context.Request?.Protocol;
                    ip = context.GetIp();
                    userAgent = Helper.GetHeaderValueAs<string>(context, "User-Agent");
                    

                    (clientId, walletId) = ParceHFTToken(token, clientId);
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
                        "{message} {Protocol}, {Method}, {Path}, {StatusCode}, {TimeMs}, {TokenHash}, {clientId}, {walletId}, {ip} {userAgent}",
                        "Http audit",
                        protocol,
                        method,
                        path,
                        code,
                        sw.ElapsedMilliseconds,
                        token?.GetHashCode(),
                        clientId,
                        walletId,
                        ip,
                        userAgent);
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

        private static ConcurrentDictionary<int, (string, string)> _cache = new ConcurrentDictionary<int, (string, string)>(); 

        private static (string, string) ParceHFTToken(string token, string clientId)
        {
            try
            {
                while (_cache.Count > 1000)
                {
                    _cache.Remove(_cache.FirstOrDefault().Key, out _);
                }

                string walletId = "";

                if (_cache.TryGetValue(token.GetHashCode(), out var item))
                    return (item.Item1, item.Item2);

                var handler = new JwtSecurityTokenHandler();
                var tdata = handler.ReadJwtToken(token);

                clientId = tdata.Claims.FirstOrDefault(c => c.Type == "client-id")?.Value;
                walletId = tdata.Claims.FirstOrDefault(c => c.Type == "wallet-id")?.Value;
                _cache[token.GetHashCode()] = (clientId, walletId);
                return (clientId, walletId);

            } catch(Exception)
            { }

            _cache[token.GetHashCode()] = (clientId, string.Empty);

            return (clientId, string.Empty);

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

            if (values.Length == 2 && values[0] == "Bearer")
                return values[1];

            return header;
        }
    }

    public static class Helper
    {
        public static string GetIp(this HttpContext ctx)
        {
            string ip = string.Empty;

            // http://stackoverflow.com/a/43554000/538763
            var xForwardedForVal = GetHeaderValueAs<string>(ctx, "X-Forwarded-For").SplitCsv().FirstOrDefault();

            if (!string.IsNullOrEmpty(xForwardedForVal))
            {
                ip = xForwardedForVal.Split(':')[0];
            }

            // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
            if (string.IsNullOrWhiteSpace(ip) && ctx?.Connection?.RemoteIpAddress != null)
                ip = ctx.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrWhiteSpace(ip))
                ip = GetHeaderValueAs<string>(ctx, "REMOTE_ADDR");

            return ip;
        }

        public static T GetHeaderValueAs<T>(HttpContext httpContext, string headerName)
        {
            StringValues values;

            if (httpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!string.IsNullOrEmpty(rawValues))
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
            return default(T);
        }

        private static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false)
        {
            if (string.IsNullOrWhiteSpace(csvList))
                return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

            return csvList
                .TrimEnd(',')
                .Split(',')
                .AsEnumerable<string>()
                .Select(s => s.Trim())
                .ToList();
        }
    }
}
