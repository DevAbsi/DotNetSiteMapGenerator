using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetSiteMapGenerator
{
    public interface ISitemapGenerator
    {
        public void AddUrlEntry(UrlEntry urlEntry);

        public void RemoveUrl(string url);

        public Task Save();

        public Task PingSearchEngines(params SearchEngines[] searchengines);
    }
}