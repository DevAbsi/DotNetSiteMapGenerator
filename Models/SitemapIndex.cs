using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetSiteMapGenerator
{
    internal class SitemapIndex
    {
        public string Filename { get; set; }
        public List<SitemapFile> Sitemaps { get; set; }
    }
}