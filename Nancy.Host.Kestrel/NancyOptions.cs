using System;
using Nancy.Bootstrapper;

namespace Nancy.Host.Kestrel
{


    /// <summary>
    /// Options for hosting Nancy with Kestrel.
    /// </summary>
    public class NancyOptions
    {
        private Func<NancyContext, bool> _performPassThrough;

        /// <summary>
        /// Gets or sets the bootstrapper. If none is set, DefaultKestrelBootstrapper will be used.
        /// </summary>
        public INancyBootstrapper Bootstrapper { get; set; }

        /// <summary>
        /// Gets or sets the delegate that determines if NancyMiddleware performs pass through.
        /// </summary>
        public Func<NancyContext, bool> PerformPassThrough
        {
            get => _performPassThrough ?? (context => false);
            set => _performPassThrough = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to request a client certificate or not.
        /// Defaults to false.
        /// </summary>
        public bool EnableClientCertificates { get; set; }
    }
}
