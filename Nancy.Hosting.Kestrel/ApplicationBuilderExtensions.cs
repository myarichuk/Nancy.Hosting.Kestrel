using System;
using Microsoft.AspNetCore.Builder;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

namespace Nancy.Hosting.Kestrel
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseNancy(
            this IApplicationBuilder builder,
            Action<NancyOptions> configure = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof (builder));

            var opts = new NancyOptions();
            configure?.Invoke(opts);

            return builder.UseMiddleware<NancyMiddleware>(opts);
        }

    }
}
