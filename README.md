# DotNetSiteMapGenerator

A .core library to generate sitemap for any site, it supports sitemap index with nested sitemaps, and sitemap categories.

How to implement?
inside the Startup.cs:
```c#
 public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSingleton<ISitemapGenerator>(
                new SitemapGenerator().WithFilename("sitemap")
                .WithMaximumAllowedEntries(50000)
                .WithMaximumThreads(2)
                .Build());
        }
```

How to add urls?

```c#
public class HomeController : Controller
    {
        private readonly ISitemapGenerator sitemapGenerator;

        public HomeController(ISitemapGenerator sitemapGenerator)
        {
            this.sitemapGenerator = sitemapGenerator;
        }

        public IActionResult Index()
        {
            sitemapGenerator.AddUrlEntry(
                "http://domain.com/ " + i,
                "Blog",
                ChangeFrequency.Hourly,
                DateTime.Now);
            sitemapGenerator.Save();
            return View();
        }
    }
```
 
