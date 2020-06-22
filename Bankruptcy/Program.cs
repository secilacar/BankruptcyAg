using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using HtmlAgilityPack;

namespace Bankruptcy
{
    class Program
    {
        static void Main(string[] args)
        {
            int pageSize = 100;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var url = new Uri(@"https://www.insolvenzbekanntmachungen.de/cgi-bin/bl_suche.pl");
            var client = new WebClient();
            client.Encoding = Encoding.GetEncoding(1252);
            string content = GetResults(client, url, 1, pageSize);

            Regex rx = new Regex(@"<b><li>\s*<a .*?([/]cgi-bin[/].*?.htm)'\)"">(\d{4}-\d{1,2}-\d{1,2})\s*<ul>(.*?)<[/]ul>\s*<p>\s*<[/]a><[/]li><[/]b>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex rxTotal = new Regex(@"Es wurden (\d+) Treffer gefunden", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            decimal count = int.Parse(rxTotal.Match(content).Result("$1"));
            decimal pageCount = Math.Ceiling(count / pageSize);
            using StringWriter w = new StringWriter();
            for (int pageNr = 1; pageNr <= Math.Min(pageCount, 100); pageNr++)
            {
                if (pageNr > 1)
                {
                    content = GetResults(client, url, pageNr, pageSize);
                }
                foreach (Match m in rx.Matches(content))
                {
                    var detailUrl = m.Result("$1");
                    var date = m.Result("$2");
                    var name = m.Result("$3");
                    var city = m.Result("$4");
                    var caseId = m.Result("$5");
                    //(.*),\s*([^,]+),\s*([\s\w]*(IN|IK|IE)\s+\d+[\/]\d+[^,\r\n]*)

                    //Console.WriteLine($"{date}, {caseId}, {name}, {city}");
                    w.WriteLine(name);
                    /*
                    Uri detailUri = new Uri(url, detailUrl);
                    var detail = client.DownloadString(detailUri);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(detail);
                    var result = doc.DocumentNode.SelectSingleNode("//body").InnerText;
                    result = HttpUtility.HtmlDecode(result);
                    var lines = Regex.Split(result, @"[\r\n]+")
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    result = string.Join(Environment.NewLine, lines);
                    Console.WriteLine(result);
                    Console.WriteLine(new string('-', 40));
                    */
                }
            }
            var matchList = w.ToString();
            var matchCount = Regex.Matches(matchList, @"(.*),\s*([^,]+),\s*([\s\wÜ]*(IN|IK|IE)\s+\d+[\/]\d+[^,\r\n]*)").Count();
            Console.WriteLine(matchCount);
            Console.ReadLine();
        }

        private static string GetResults(WebClient client, Uri url, int pageNr, int pageSize = 100)
        {
            var data = new NameValueCollection();
            data.Add("MIME Type", "application/x-www-form-urlencoded");
            data.Add("Suchfunktion", "uneingeschr");
            //data.Add("Absenden", "Suche starten");
            //data.Add("Bundesland", "--+Alle+Bundesländer+--");
            //data.Add("Gericht", "-- Alle Insolvenzgerichte --");
            data.Add("Datum1", DateTime.Today.AddMonths(-6).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
            //data.Add("Datum2", null);
            //data.Add("Name", null);
            //data.Add("Sitz", null);
            //data.Add("Abteilungsnr", null);
            //data.Add("Registerzeichen", "--");
            //data.Add("Lfdnr", null);
            //data.Add("Jahreszahl", "--");
            //data.Add("Registerart", "-- keine Angabe --");
            //data.Add("select_registergericht", null);
            //data.Add("Registergericht", "-- keine Angabe --");
            //data.Add("Registernummer", null);
            //data.Add("Gegenstand", "-- Alle Bekanntmachungen innerhalb des Verfahrens --");
            data.Add("matchesperpage", pageSize.ToString());
            data.Add("page", pageNr.ToString());
            data.Add("sortedby", "Datum");
            var buffer = client.UploadValues(url, data);
            var content = client.Encoding.GetString(buffer);
            return content;
        }
    }
}
