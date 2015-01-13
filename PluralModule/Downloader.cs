using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace PluralModule
{
    public class Downloader
    {
        private readonly TraceListener _listener;

        public Downloader(TraceListener listener)
        {
            Trace.Listeners.Add(listener);
            _listener = listener;
        }

        public void Download(IEnumerable<string> moduleNames)
        {
            foreach (var moduleName in moduleNames)
            {
                Download(moduleName);   
            }
        }

        public void Download(string moduleName)
        {
            Download(moduleName, "c:\\");
        }

        public void Download(string moduleName, string path)
        {
            HttpWebResponse response;
            _listener.WriteLine("Downloading... " + moduleName);

            string url = "http://www.pluralsight.com/data/course/content/" + moduleName;

            if (Request_www_pluralsight_com(out response, url))
            {
                var modules = (List<Module>)JsonConvert.DeserializeObject(ReadResponse(response), typeof(List<Module>));
                response.Close();
                int id = 1;
                foreach (var module in modules)
                {
                    foreach (var clip in module.clips)
                    {
                        var par = HttpUtility.ParseQueryString(clip.playerParameters);
                        var dir = path + par["course"] + "\\" + id + "-" + module.title + "\\";
                        var invalidChars = Path.GetInvalidFileNameChars();
                        var title = RemoveInvalidFilePathCharacters(clip.title, "");

                        string filename = dir + clip.name + " - " + title + ".mp4";

                        if (File.Exists(filename))
                        {
                            _listener.WriteLine("File already downloaded");
                            continue;
                        }

                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        _listener.WriteLine("Using dir: " + dir);
                        Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);

                        if (Request_www_pluralsight_Video(out response, BuildPostBody(par, clip.clipIndex)))
                        {
                            string resp = ReadResponse(response);
                            response.Close();

                            _listener.WriteLine("Downloading video from: " + resp);

                            var client = new WebClient();
                            client.DownloadFile(new Uri(resp), filename);

                            _listener.WriteLine("Created file: " + filename);
                        }
                    }
                    id++;
                }
            }

            _listener.Flush();
        }

        public string RemoveInvalidFilePathCharacters(string filename, string replaceChar)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename, replaceChar);
        }
        
        private string ReadResponse(HttpWebResponse response)
        {
            using (var responseStream = response.GetResponseStream())
            {
                var streamToRead = responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                {
                    streamToRead = new GZipStream(streamToRead, CompressionMode.Decompress);
                }
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                {
                    streamToRead = new DeflateStream(streamToRead, CompressionMode.Decompress);
                }

                using (var streamReader = new StreamReader(streamToRead, Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        private bool Request_www_pluralsight_com(out HttpWebResponse response, string url)
        {
            response = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);

                request.UserAgent = "Fiddler";

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private string BuildPostBody(NameValueCollection clipParams, int clipNumber)
        {
            return "{a:\"" + clipParams["author"] + "\", m:\"" + clipParams["name"] + "\", course:\"" + clipParams["course"] + "\", cn:"+ clipNumber +", mt:\"mp4\"}";
        }

        private bool Request_www_pluralsight_Video(out HttpWebResponse response, string body)
        {
            response = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("http://www.pluralsight.com/training/Player/ViewClip");

                request.KeepAlive = true;
                request.Accept = "application/json, text/plain, */*";
                //request.Headers.Add("X-NewRelic-ID", @"VwUGVl5VGwIJVFVXAAc=");
                request.Headers.Add("Origin", @"http://www.pluralsight.com");
                request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.95 Safari/537.36";
                request.ContentType = "application/json;charset=UTF-8";
                //request.Referer = "http://www.pluralsight.com/training/player?" + clipParams;
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-GB,en;q=0.8,en-US;q=0.6,pt;q=0.4,pt-PT;q=0.2");
                request.Headers.Set(HttpRequestHeader.Cookie, @"optimizelyEndUserId=oeu1416418471489r0.5932143593672663; mf_23f90557-90d2-4682-9764-9e0e1aa7dc97=-1; __uvt=; __utma=195666797.1071572145.1416418472.1418391652.1418391652.1; __utmc=195666797; __utmz=195666797.1418391652.1.1.utmcsr=google|utmccn=(organic)|utmcmd=organic|utmctr=(not%20provided); newSiteVisited=true; _uslk_visits=1; _uslk_referrer=https%3A%2F%2Fwww.google.co.uk%2F; _uslk_bootstrapped=1; _uslk_ct=0; _uslk_co=0; _uslk_active=0; _uslk_page_impressions=4; _uslk_app_state=Idle%3B0; __RequestVerificationToken_L2E1=KvSnrAwdch34X0YXpW6QmO_dUi-f53f4-0Sl6xZo3S3Yse02rU2oxBpWZCNWRT2G4epu2FTH31i21GbP4kMR_wf7CaY1; _dc_gtm_UA-5643470-2=1; _gat_UA-5643470-2=1; PSM=79C440B1FD6441CFA6BDC4858DADC6B7DE120226402315ABD37A7E4D5CFC2FB6B8D30600CD70FEC6875F7D3E95D4980724F7F743C71AD53CBF0D3ABE52D997EFEECEFB943BC30986B7CAFCB40DC97EF43C4314C51D70191F506DB3D4289D6B867B110CF7A65C205CDC7736B975C33F0BE76DA7C20506F4F76D41DBDF0BBED7CA3EAAEC4750C88A107A9BA1958F8A73358561170B1E0B20FF84E7DB11AFFC719E670D4122454373B6762EE06B90E225E192FCBC5B; optimizelySegments=%7B%221227392893%22%3A%22search%22%2C%221248401246%22%3A%22gc%22%2C%221258181237%22%3A%22false%22%7D; optimizelyBuckets=%7B%222312010859%22%3A%222321470446%22%7D; _ga=GA1.2.1071572145.1416418472; __ar_v4=ALUY7XVFIBAENNNGZSQOT6%3A20150106%3A3%7CPHHEMWCT7RGRDFUHGCNCJV%3A20150106%3A4%7CBFLWHRV7W5FLTIZIQ4OSO6%3A20150106%3A24%7CNPTOMQSYYZABNNUIQDRAKL%3A20150106%3A24%7C4YCMENXFKFBQLNQCLOV3GS%3A20150106%3A8%7CDOMT5ESRMRH2PDG3LFNDCP%3A20150106%3A7%7CG2LTJRL5ORA4PICF7WRTYP%3A20150108%3A2; _we_wk_ss_lsf_=true; _gali=mfa92; optimizelyPendingLogEvents=%5B%5D; uvts=1dJohRWautxxAsDN; visitor_id36882=62000172; psPlayer=%7B%22videoScaling%22%3A%22Scaled%22%2C%22videoQuality%22%3A%22High%22%7D");

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }
    }
}
