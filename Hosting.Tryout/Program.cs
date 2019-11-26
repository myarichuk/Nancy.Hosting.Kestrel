using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Nancy.Hosting.Kestrel;

namespace Hosting.Tryout
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = WebHost
                .CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }

    internal class Startup
    {
        public void Configure(IApplicationBuilder app) => app.UseNancy();
    }
}
