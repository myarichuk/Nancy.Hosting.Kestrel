using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Nancy.IO;
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Nancy.Host.Kestrel
{
    public class NancyMiddleware
    {
        private readonly NancyOptions _options;
        private readonly ILogger<NancyMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly INancyEngine _engine;

        private readonly ConcurrentQueue<Url> _urlPool = new ConcurrentQueue<Url>();

        public NancyMiddleware(
            NancyOptions options,
            ILogger<NancyMiddleware> logger,
            RequestDelegate next)
        {
            _options = options;
            var bootstrapper =
                _options.Bootstrapper ?? new DefaultNancyBootstrapper(); //new DefaultKestrelBootstrapper();

            bootstrapper.Initialise();
            _engine = bootstrapper.GetEngine();

            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = CreateNancyRequest(context);

            try
            {
                if(_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation($"Nancy: received request {request.Method}: {request.Url}");

                using var nancyContext = await _engine.HandleRequest(request).ConfigureAwait(false);
                
                if(_logger.IsEnabled(LogLevel.Information) && nancyContext.Response.StatusCode == HttpStatusCode.OK)
                    _logger.LogInformation($"Nancy response: OK, content-type: {nancyContext.Response.ContentType}");
                else if(_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError($"Nancy response: {nancyContext.Response.StatusCode}, content-type: {nancyContext.Response.ContentType}, error: {nancyContext.Response.ReasonPhrase}");

                SetNancyResponseToHttpResponse(context, nancyContext.Response);
                if (_options.PerformPassThrough(nancyContext))
                    await _next(context);
            }
            finally
            {
                _urlPool.Enqueue(request.Url);
            }
        }

        private Request CreateNancyRequest(HttpContext context)
        {
            var expectedRequestLength = context.Request.ContentLength ?? 0;

            if(!_urlPool.TryDequeue(out var nancyUrl))
                nancyUrl = new Url();

            nancyUrl.Scheme = context.Request.Scheme;
            nancyUrl.HostName = context.Request.Host.Host;
            nancyUrl.Port = context.Request.Host.Port;
            nancyUrl.BasePath = context.Request.PathBase;
            nancyUrl.Path = context.Request.Path;
            nancyUrl.Query = context.Request.QueryString.Value;

            RequestStream body = null;

            if (expectedRequestLength != 0 || HasChunkedEncoding(context.Request.Headers))
                body = RequestStream.FromStream(context.Request.Body, expectedRequestLength,
                    StaticConfiguration.DisableRequestStreamSwitching ?? true);

            var headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());

            return new Request(context.Request.Method.ToUpperInvariant(),
                nancyUrl,
                body,
                headers,
                context.Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                context.Request.HttpContext.Connection.ClientCertificate,
                context.Request.Protocol);
        }

        private static bool HasChunkedEncoding(IHeaderDictionary incomingHeaders)
        {
            if (incomingHeaders == null ||
                !incomingHeaders.TryGetValue("Transfer-Encoding", out var transferEncodingValue))
                return false;

            var transferEncodingString = transferEncodingValue.SingleOrDefault() ?? string.Empty;
            return transferEncodingString.Equals("chunked", StringComparison.OrdinalIgnoreCase);
        }

        public static void SetNancyResponseToHttpResponse(HttpContext context, Response response)
        {
            SetHttpResponseHeaders(context, response);

            if (response.ContentType != null)
                context.Response.ContentType = response.ContentType;

            context.Response.StatusCode = (int) response.StatusCode;

            if (response.ReasonPhrase != null)
                context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = response.ReasonPhrase;

            response.Contents.Invoke(context.Response.Body);
        }

        private static void SetHttpResponseHeaders(HttpContext context, Response response)
        {
            foreach (var header in response.Headers)
                context.Response.Headers.Add(header.Key, header.Value);

            foreach (var cookie in response.Cookies)
                context.Response.Headers.Add("Set-Cookie", cookie.ToString());
        }
    }
}
