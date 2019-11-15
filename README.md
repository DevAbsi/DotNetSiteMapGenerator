# DotNetSiteMapGenerator

A .core library to generate sitemap for any site, it supports sitemap index with nested sitemaps, and sitemap categories.

### H2 How to implement?
inside the Startup.cs you have two main options:

### H3 Option 1: Zero configuration implementation
```c#
 public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
                 // have to initilise it with baseUrl of website eg: site home URL
            services.AddSingleton<ISitemapGenerator>(new SitemapGenerator("https://localhost:44399/"));
        }
```
### H3Option 2: With Customization
```c#
  public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            //services.AddSingleton<ISitemapGenerator, SitemapGenerator>();
            services.AddSingleton<ISitemapGenerator>(new SitemapGenerator()
                        .WithDomainName("https://localhost:44399")
                        .WithFilename("sitemap")
                        .WithOutputSubDirectory("Sitemaps")
                        .WithSitemapIndex("sitemap-index")
                        .WithMaximumAllowedEntries(50000)
                        .WithMaximumThreads(2)
                        .WithAutoPing(SearchEngines.Google, SearchEngines.Bing, SearchEngines.Yandex));
        }
```

### H3 How to add urls?

```c#
public class HomeController : Controller
    {
        private readonly ISitemapGenerator sitemapGenerator;

        public HomeController(ISitemapGenerator sitemapGenerator)
        {
            this.sitemapGenerator = sitemapGenerator;
        }

        public async Task<IActionResult> Index()
        {
            sitemapGenerator.AddUrlEntry(
                "http://domain.com/ ",
                "Blog",
                ChangeFrequency.Hourly,
                DateTime.Now);
            await sitemapGenerator.Save();
            return View();
        }
    }
```
 
