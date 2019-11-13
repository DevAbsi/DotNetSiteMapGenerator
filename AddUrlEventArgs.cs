using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetSiteMapGenerator
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