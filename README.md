# Nancy.Hosting.Kestrel
Nancyfx is an extremely useful and fun to use library, which unfortunately (at the moment of writing this) has only one way of hosting it in ASP.Net Core: through an OWIN middleware.  
Using Nancy as OWIN middleware works fine, but since OWIN is a middle man that doesn't HAVE to be there I decided to implement a Kestrel middleware to use Nancy directly without Owin. And of course I wanted an excuse to actually implement Kestrel middleware :)  

Nancy.Hosting.Kestrel can be used as follows:

```cs
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
```
