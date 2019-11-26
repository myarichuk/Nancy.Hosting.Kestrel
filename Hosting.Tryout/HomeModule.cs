using Nancy;
// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable UnusedMember.Global

namespace Hosting.Tryout
{
    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            
            Get("/", args => "Hello World from Kestrel!");
            Get("/{Bar}", args => Response.AsJson(new { Foo = args.Bar }));
        }
    }
}
