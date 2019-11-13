using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetSiteMapGenerator
{
    public class SitemapIndex
    {
        public string Filename { get; set; }
        public List<SitemapFile> Sitemaps { get; set; }
    }
}