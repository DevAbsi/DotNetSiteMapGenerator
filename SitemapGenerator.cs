using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace DotNetSiteMapGenerator
{
    /// <summary>
    /// sitemap file name structure :
    ///  sitemap_category_number_dd-mm-yyyy_hh-mm-ss.xml
    /// </summary>
    public class SitemapGenerator : ISitemapGenerator
    {
        private List<SitemapFile> sitemapsFiles = new List<SitemapFile>();
        private const string signature = "Generated using DotNetSitemapGenerator *** https://github.com/DevAbsi/DotNetSiteMapGenerator ";
        private const string googlePingUrl = "https://www.google.com/webmasters/tools/ping?sitemap=";
        private const string bingPingUrl = "https://www.bing.com/ping?sitemap=";
        private const string yandexPingUrl = "http://webmaster.yandex.com/site/map.xml?host=";
        private string domain = string.Empty;
        private string baseFilename = "sitemap";
        private string sitemapIndexFilename = "sitemap-index";
        private int maximumThreads = 2;
        private int maximumAllowedEntries = 50000;
        private string subdirectory = "Sitemaps";
        private string workingPath;
        private SearchEngines[] autoPingEngines;

        /// <summary>
        ///
        /// </summary>
        /// <param name="baseUrl">Home Url</param>
        public SitemapGenerator(string baseUrl)
        {
            Build();
        }

        public SitemapGenerator()
        {
            Build();
        }

        public SitemapGenerator WithFilename(string filename)
        {
            this.baseFilename = filename;
            return this;
        }

        public SitemapGenerator WithSitemapIndex(string sitemapIndexFileName)
        {
            this.sitemapIndexFilename = sitemapIndexFileName;
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

        public SitemapGenerator WithOutputSubDirectory(string subDirectory)
        {
            this.subdirectory = subDirectory;
            return this;
        }

        public SitemapGenerator WithDomainName(string domainnameUrl)
        {
            this.domain = domainnameUrl;
            return this;
        }

        public SitemapGenerator WithAutoPing(params SearchEngines[] autoPingEngines)
        {
            this.autoPingEngines = autoPingEngines;
            return this;
        }

        public SitemapGenerator Build()
        {
            this.workingPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", subdirectory);
            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);
            // check if this class has been initiated without the builder pattern to prevent double loading of created sitemaps
            if (sitemapsFiles.Count == 0)
                LoadCreatedSitemaps();
            return this;
        }

        private void LoadCreatedSitemaps()
        {
            var files = Directory.GetFiles(workingPath, this.baseFilename + FilenameSeparators.BlockSeparator + "*.xml");

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

        public void AddUrlEntry(string RelativeUrl, string category, ChangeFrequency changeFrequency, DateTime lastModification)
        {
            UrlEntry urlEntry = new UrlEntry
            {
                Category = category,
                ChangeFrequency = changeFrequency,
                LastModification = lastModification,
                URL = new Uri(new Uri(this.domain), RelativeUrl).ToString()
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
            // TODO create a new sitemap index that will containes all the sitemaps generated in case that a new sitemap file has been changed
            await Task.Run(() =>
            {
                if (sitemapsFiles.Any(w => w.NeedReWrite == true))
                {
                    XmlDocument document = createDefaultXmlDocument();
                    var root = document.CreateElement("sitemapindex");

                    foreach (var sitemapfile in sitemapsFiles)
                    {
                        // create all the needed tags
                        var sitemapTag = document.CreateElement("sitemap");
                        var locTag = document.CreateElement("loc");
                        var lastModificationTag = document.CreateElement("lastmod");

                        // join url paths
                        Uri uri = new Uri(new Uri(domain), subdirectory);
                        uri = new Uri(uri, sitemapfile.Filename);
                        locTag.InnerText = uri.ToString();

                        // for last modification date, have to find the latest urlEntry that need to be written
                        DateTime latest = sitemapfile.Entires.Where(w => w.Written == false)
                            .OrderByDescending(w => w.LastModification)
                            .First().LastModification;
                        lastModificationTag.InnerText = latest.ToString();
                        sitemapTag.AppendChild(locTag);
                        sitemapTag.AppendChild(lastModificationTag);
                        root.AppendChild(sitemapTag);
                    }
                    document.AppendChild(root);
                    document.Save(Path.Combine(workingPath, $"{this.sitemapIndexFilename}.xml"));
                }

                foreach (var sitemapFile in sitemapsFiles.Where(w => w.NeedReWrite == true))
                {
                    XmlDocument document = createDefaultXmlDocument();
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

                    sitemapFile.NeedReWrite = false;

                    document.Save(Path.Combine(workingPath, sitemapFile.Filename));
                }
            });

            if (this.autoPingEngines != null)
                await PingSearchEngines(this.autoPingEngines);
        }

        private XmlDocument createDefaultXmlDocument()
        {
            XmlDocument document = new XmlDocument();
            var declaration = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            var comment = document.CreateComment(signature);
            document.AppendChild(declaration);
            document.AppendChild(comment);
            return document;
        }

        private string GenerateFilename(string category, int sitemapNumber)
        {
            string dateTimeFormat = "dd" + FilenameSeparators.DateSeparator + "MM" + FilenameSeparators.DateSeparator +
                "yyyy" + FilenameSeparators.BlockSeparator + "HH" + FilenameSeparators.DateSeparator + "mm";
            return baseFilename + FilenameSeparators.BlockSeparator + category + FilenameSeparators.BlockSeparator + sitemapNumber.ToString() +
                               FilenameSeparators.BlockSeparator + DateTime.Now.ToString(dateTimeFormat) + ".xml";
        }

        /// <summary>
        /// submit sitemap Index to search engines
        /// </summary>
        /// <param name="searchengines"></param>
        /// <returns></returns>
        public async Task PingSearchEngines(params SearchEngines[] searchengines)
        {
            await Task.Run(() =>
            {
                var options = new ParallelOptions();
                options.MaxDegreeOfParallelism = maximumThreads;
                var uri = new Uri(new Uri(this.domain), workingPath);
                var indexUrl = Uri.EscapeUriString(new Uri(uri, this.sitemapIndexFilename).ToString());
                // escape IndexUrl
                Parallel.ForEach(searchengines, options, async searchEngine =>
                {
                    HttpClient client = new HttpClient();
                    switch (searchEngine)
                    {
                        case SearchEngines.Google:
                            await client.GetAsync(googlePingUrl.Trim() + indexUrl);
                            Debug.WriteLine(Uri.EscapeUriString(indexUrl));
                            break;

                        case SearchEngines.Bing:
                            await client.GetAsync(bingPingUrl.Trim() + indexUrl);
                            Debug.WriteLine(Uri.EscapeUriString(indexUrl));
                            break;

                        case SearchEngines.Yandex:
                            await client.GetAsync(yandexPingUrl.Trim() + indexUrl);
                            Debug.WriteLine(Uri.EscapeUriString(indexUrl));
                            break;
                    }
                });
            });
        }

        public void RemoveUrl(string url)
        {
            foreach (var sitemapFile in sitemapsFiles)
            {
                var urlEntry = sitemapFile.Entires.FirstOrDefault(w => w.URL == url);
                if (urlEntry != null)
                {
                    sitemapFile.Entires.Remove(urlEntry);
                    sitemapFile.NeedReWrite = true;
                    break;
                }
            }
        }
    }
}