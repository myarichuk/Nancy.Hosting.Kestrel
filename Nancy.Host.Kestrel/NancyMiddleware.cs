using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Nancy.Bootstrapper;
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

        public NancyMiddleware(
            NancyOptions options, 
            ILogger<NancyMiddleware> logger, 
            RequestDelegate next)
        {
            _options = options;
            var bootstrapper = _options.Bootstrapper ?? new DefaultNancyBootstrapper();//new DefaultKestrelBootstrapper();
            
            bootstrapper.Initialise();
            _engine = bootstrapper.GetEngine();
            
            _logger = logger; //TODO: add proper logging
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = CreateNancyRequest(context);

            using var nancyContext = await _engine.HandleRequest(request).ConfigureAwait(false);

            SetNancyResponseToHttpResponse(context, nancyContext.Response);
            if(_options.PerformPassThrough(nancyContext))
                await _next(context);
        }

        private Request CreateNancyRequest(HttpContext context)
        {
            var expectedRequestLength = context.Request.ContentLength ?? 0;

            //TODO: instead of allocating Url each time, implement object pool
            var nancyUrl = new Url
                               {
                                   Scheme = context.Request.Scheme,
                                   HostName = context.Request.Host.Host,
                                   Port = context.Request.Host.Port,
                                   BasePath = context.Request.PathBase,
                                   Path = context.Request.Path,
                                   Query = context.Request.QueryString.Value,
                               };

            RequestStream body = null;
            
            if (expectedRequestLength != 0 || HasChunkedEncoding(context.Request.Headers))
                body = RequestStream.FromStream(context.Request.Body, expectedRequestLength,
                    StaticConfiguration.DisableRequestStreamSwitching ?? true);

            var headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());

            //TODO: instead of allocating Request each time, implement object pool
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
            if (incomingHeaders == null || !incomingHeaders.TryGetValue("Transfer-Encoding", out var transferEncodingValue))
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

            foreach(var cookie in response.Cookies)
                context.Response.Headers.Add("Set-Cookie", cookie.ToString());
        }
    }
}
