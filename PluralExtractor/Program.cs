using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using PluralModule;

namespace PluralExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("At least one module name is required");
                Console.Read();
                return;
            }

            var myWriter = new ConsoleTraceListener();

            var downloader = new Downloader(myWriter);
            
            
            foreach (var s in args)
            {
                downloader.Download(s, ConfigurationManager.AppSettings["folder"]);
            }

            Console.WriteLine("DONE!!");
            Console.Read();
        }
    }
}
