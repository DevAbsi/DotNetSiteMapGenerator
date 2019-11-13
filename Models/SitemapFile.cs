using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetSiteMapGenerator
{
    public class SitemapFile
    {
        public string Filename { get; set; }
        public string Category { get; set; }
        public int Number { get; set; }
        public bool NeedReWrite { get; set; } = false;
        public List<UrlEntry> Entires { get; set; }

        public SitemapFile()
        {
            Entires = new List<UrlEntry>();
        }
    }
}