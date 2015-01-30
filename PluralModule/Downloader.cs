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
        private string _cookie = @"optimizelyEndUserId=oeu1422617105857r0.47923015779815614; newSiteVisited=true; _dc_gtm_UA-5643470-2=1; mf_23f90557-90d2-4682-9764-9e0e1aa7dc97=-1; _gat_UA-5643470-2=1; csAnnouncmentShown=true; __uvt=; __RequestVerificationToken_L2E1=OZQ71yn3-CIakcRU_kxvl8yp-VV2VZQdvDFWgDuTiVuBQ1tA4OmjIqarSr2BL5-znVKnGXacDk8Hg-Yd9V6MT5QLi941; PSM=858FBDAF2AFC5F2DD185E2BA54A9AFE44EC461384C845F3BA2F9ADED78F38F395B8711D804878C912668C48B7862BA6DB8A585C62BF595D17A18346DF58DC0583A8FD77BCD4D985CA33002F64FB277223F308092B9FA6FD7DD84982CEE86B743BAD615C107B50D91AE1CC0C6C9BFC5D88D6C8CC2A81C80A4F6EDAC45BB683E0738E0F19E58A15BB72B03082CD55226C99D38DE4B5624C8CBF82C5F073F73ADDDB578E7F97D94B722810F647D07813A86E7C2D1D7; _uslk_visits=1; _uslk_referrer=; _uslk_bootstrapped=1; _uslk_ct=0; _uslk_co=0; _uslk_active=0; _uslk_page_impressions=2; _uslk_app_state=Idle%3B0; visitor_id36882=74752644; _gali=mfa80; optimizelySegments=%7B%221227392893%22%3A%22direct%22%2C%221248401246%22%3A%22gc%22%2C%221258181237%22%3A%22false%22%7D; optimizelyBuckets=%7B%222312010859%22%3A%222321470446%22%7D; optimizelyPendingLogEvents=%5B%5D; _ga=GA1.2.1821668503.1422617107; uvts=2daAwFusdIRtUS1V; _we_wk_ss_lsf_=true; __ar_v4=BFLWHRV7W5FLTIZIQ4OSO6%3A20150201%3A5%7CNPTOMQSYYZABNNUIQDRAKL%3A20150201%3A5%7C4YCMENXFKFBQLNQCLOV3GS%3A20150201%3A4%7CDOMT5ESRMRH2PDG3LFNDCP%3A20150201%3A1";

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

        public void Download(string moduleName, string path, string cookie)
        {
            _cookie = cookie;
            Download(moduleName, path);
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
                bool second = false;
                foreach (var module in modules)
                {
                    foreach (var clip in module.clips)
                    {
                        var par = HttpUtility.ParseQueryString(clip.playerParameters);
                        var dir = path + "\\" + par["course"] + "\\" + id + "-" + RemoveInvalidFilePathCharacters(module.title, "") + "\\";
                        var invalidChars = Path.GetInvalidFileNameChars();
                        var title = RemoveInvalidFilePathCharacters(clip.title, "");
                        var name = RemoveInvalidFilePathCharacters(clip.name, "");

                        string filename = dir + name + " - " + title + ".mp4";

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
                        if (second)
                        {
                            Thread.Sleep((int) TimeSpan.FromMinutes(1).TotalMilliseconds);
                        }

                        second = true;

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
                request.Headers.Set(HttpRequestHeader.Cookie, _cookie);

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
