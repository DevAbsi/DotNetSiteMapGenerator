using System;
using System.Collections.Generic;
using System.Text;

namespace SiteMapGenerator
{
    public class SitemapIndex
    {
        public string Filename { get; set; }
        public List<SitemapFile> Sitemaps { get; set; }
    }
}