using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace DotNetSiteMapGenerator
{
    /// <summary>
    /// sitemap file name structure :
    ///  sitemap_category?_number?_02-5-2019_13-52-14.xml
    /// </summary>
    public class SitemapGenerator : ISitemapGenerator
    {
        private List<UrlEntry> entriesPool = new List<UrlEntry>();
        private List<SitemapFile> sitemapsFiles = new List<SitemapFile>();
        private const string signature = "Generated using DotNetSitemapGenerator *** http://github.com/DevAbsi/ ";

        private string baseFilename = "sitemap";
        private int maximumThreads = 2;
        private int maximumAllowedEntries = 50000;

        public SitemapGenerator()
        {
        }

        public SitemapGenerator WithFilename(string filename)
        {
            this.baseFilename = filename;
            return this;
        }

        public SitemapGenerator WithMaximumAllowedEntries(int maximumEntries)
        {
            this.maximumAllowedEntries = maximumEntries;
            return this;
        }

        public SitemapGenerator WithMaximumThreads(int maxthreads)
        {
            this.maximumAllowedEntries = maximumThreads;
            return this;
        }

        public SitemapGenerator Build()
        {
            LoadCreatedSitemaps();
            return this;
        }

        private void LoadCreatedSitemaps()
        {
            var files = Directory.GetFiles(Environment.CurrentDirectory, "*.xml");

            foreach (var file in files)
                LoadUrlsFromFile(file);
        }

        private void LoadUrlsFromFile(string filePath)
        {
            string filename = Path.GetFileName(filePath).Replace(Path.GetExtension(filePath), "").Trim();
            string[] splitData = filename.Split(new char[] { FilenameSeparators.BlockSeparator });
            string category = splitData[1].Trim();
            int number;
            int.TryParse(splitData[2], out number);
            SitemapFile sitemapFile = new SitemapFile()
            {
                Filename = filename,
                Number = number,
                Category = category
            };
            XmlDocument document = new XmlDocument();
            document.Load(filePath);
            foreach (XmlNode x in document.SelectNodes("urlset/url"))
            {
                var entry = new UrlEntry
                {
                    URL = x.SelectSingleNode("loc").InnerText,
                    ChangeFrequency = Enum.Parse<ChangeFrequency>(x.SelectSingleNode("changefreq").InnerText, true),
                    Category = category,
                    Written = true
                };
                sitemapFile.Entires.Add(entry);
                // AddUrlEntry(entry);
            }
            sitemapsFiles.Add(sitemapFile);
        }

        public void AddUrlEntry(string url, string category, ChangeFrequency changeFrequency, DateTime lastModification)
        {
            UrlEntry urlEntry = new UrlEntry
            {
                Category = category,
                ChangeFrequency = changeFrequency,
                LastModification = lastModification,
                URL = url
            };
            // check if the url is already exists in any of the sitemaps
            foreach (var sitemapfile in sitemapsFiles)
                if (sitemapfile.Entires.Any(w => w.URL == urlEntry.URL))
                    return;

            // first lets check if any sitemaps has the same category
            var sitemapWithSameCategory = sitemapsFiles.Where(w => w.Category.ToLower().Trim() == urlEntry.Category.ToLower().Trim());
            if (sitemapWithSameCategory.Count() == 0)
            {
                // create new sitemap
                SitemapFile sitemapFile = new SitemapFile()
                {
                    Category = urlEntry.Category,
                    Number = 1,
                    Filename = GenerateFilename(urlEntry.Category, 1),
                    NeedReWrite = true
                };
                sitemapFile.Entires.Add(urlEntry);
                sitemapsFiles.Add(sitemapFile);
            }
            else
            {
                // Get the latest sitemap within the same category and has less than the maximum allowed entries
                int num = sitemapWithSameCategory.Max(w => w.Number);
                var latestSitemap = sitemapWithSameCategory.FirstOrDefault(w => w.Entires.Count < maximumAllowedEntries);
                if (latestSitemap == null)
                {
                    // all sitemaps are full, so we need a new sitemap file
                    SitemapFile sitemapFile = new SitemapFile()
                    {
                        Category = urlEntry.Category,
                        Number = num + 1,
                        Filename = GenerateFilename(urlEntry.Category, num + 1),
                        NeedReWrite = true
                    };
                    sitemapFile.Entires.Add(urlEntry);
                    sitemapsFiles.Add(sitemapFile);
                }
                else
                {
                    // append urlEntry to this sitemaps file
                    latestSitemap.NeedReWrite = true;
                    latestSitemap.Entires.Add(urlEntry);
                }
            }
        }

        public async Task Save()
        {
            // TODO create a new sitemap index that will containes all the sitemaps generated
            await Task.Run(() =>
             {
                 foreach (var sitemapFile in sitemapsFiles.Where(w => w.NeedReWrite == true))
                 {
                     XmlDocument document = new XmlDocument();
                     var declaration = document.CreateXmlDeclaration("1.0", "UTF-8", null);
                     var comment = document.CreateComment(signature);
                     document.AppendChild(declaration);
                     document.AppendChild(comment);
                     var root = document.CreateElement("urlset");
                     document.AppendChild(root);
                     ParallelOptions options = new ParallelOptions()
                     {
                         MaxDegreeOfParallelism = maximumThreads
                     };
                     Parallel.ForEach(sitemapFile.Entires, options, urlEntry =>
                     {
                         XmlElement urlTag = document.CreateElement("url");
                         var loc = document.CreateElement("loc");
                         loc.InnerText = urlEntry.URL;
                         var lastmod = document.CreateElement("lastmod");
                         lastmod.InnerText = urlEntry.LastModification.ToString();
                         var changeFrequency = document.CreateElement("changefreq");
                         changeFrequency.InnerText = Enum.GetName(typeof(ChangeFrequency), urlEntry.ChangeFrequency).ToString();
                         urlTag.AppendChild(loc);
                         urlTag.AppendChild(lastmod);
                         urlTag.AppendChild(changeFrequency);
                         root.AppendChild(urlTag);
                     });

                     document.Save(sitemapFile.Filename);
                 }
             });
        }

        private string GenerateFilename(string category, int sitemapNumber)
        {
            string dateTimeFormat = "dd" + FilenameSeparators.DateSeparator + "MM" + FilenameSeparators.DateSeparator +
                "yyyy" + FilenameSeparators.BlockSeparator + "HH" + FilenameSeparators.DateSeparator + "mm";
            return baseFilename + FilenameSeparators.BlockSeparator + category + FilenameSeparators.BlockSeparator + sitemapNumber.ToString() +
                               FilenameSeparators.BlockSeparator + DateTime.Now.ToString(dateTimeFormat) + ".xml";
        }

        public async Task PingSearchEngines(params SearchEngines[] searchengines)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void RemoveUrl(string url)
        {
            //TODO
            throw new NotImplementedException();
        }
    }
}