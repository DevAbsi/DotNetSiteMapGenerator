using System;
using System.Collections.Generic;
using System.Text;

namespace SiteMapGenerator
{
    internal class AddUrlEventArgs : EventArgs
    {
        private UrlEntry urlEntry;

        public AddUrlEventArgs(UrlEntry urlEntry)
        {
            this.urlEntry = urlEntry;
        }

        public UrlEntry UrlEntry
        {
            get => urlEntry;
        }
    }
}