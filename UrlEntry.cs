using System;
using System.Collections.Generic;
using System.Text;

namespace SiteMapGenerator
{
    public class UrlEntry
    {
        public string URL { get; set; }
        public string Category { get; set; }
        public ChangeFrequency ChangeFrequency { get; set; }
        public DateTime LastModification { get; set; }
        public bool Written { get; set; } = false;
    }
}