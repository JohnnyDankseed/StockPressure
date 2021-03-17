using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using Newtonsoft.Json;

namespace GetShares
{
    /// <summary>
    /// TO USE THIS PROGRAM, YOU MUST GET YOUR OWN ACCOUNT WITH https://rapidapi.com/apidojo/api/yahoo-finance1
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string symbol = "GME";

            if (AppConfig.ApiKey == null || AppConfig.ApiHost == null)
            {
                Console.WriteLine("To use this program, you must have an API account with https://rapidapi.com/apidojo/api/yahoo-finance1");
                Console.WriteLine("You should only have to do this once.");
                Console.Write("Enter your API Key here :");
                string apiKey = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(apiKey)) AppConfig.ApiKey = apiKey.Trim();

                Console.Write("Write your API Host here :");
                string apiHost = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(apiKey)) AppConfig.ApiHost = apiHost.Trim();
            }

            while (true)
            {
                Console.Write($"Symbol ({symbol}): ");
                symbol = GetString(symbol);
                Console.WriteLine("------------------------------------------------------------------------------");

                YahooFinanceStatistics yfData = GetYahooFinanceForSymbol(symbol);

                if (yfData?.defaultKeyStatistics?.sharesOutstanding == null)
                {
                    Console.WriteLine($"ERROR: Unable to find data for {symbol}.\r\n\r\n");
                    continue;
                }

                Int64 sharesHeldByInsiders = (Int64)(
                    yfData.defaultKeyStatistics.sharesOutstanding.value * 
                    yfData.defaultKeyStatistics.heldPercentInsiders.value
                );


                // I MAY remove this chunk of expository once I know the calculations are correct and just let the numbers be the numbers.
                // I kinda like this section.
                // ------------------------------------------------------------------------------------------------------------------------
                Console.WriteLine("");
                Console.WriteLine($"According to Yahoo Finance, on " +
                    $"{yfData.defaultKeyStatistics.sharesShortPreviousMonthDate.value:yyyy-MM-dd} " +
                    $"the outstanding shorts for {symbol} were approx. " +
                    $"***{yfData.defaultKeyStatistics.sharesShortPriorMonth.value:###,###,###,##0}***.");
                Console.WriteLine("");
                Console.WriteLine($"It also gives the total outstanding shares for ***{symbol}*** to be approx. " +
                    $"***{yfData.defaultKeyStatistics.sharesOutstanding.value:###,###,###,##0}*** ");
                Console.WriteLine($"and says that " +
                    $"***{yfData.defaultKeyStatistics.heldPercentInsiders.value * 100.0m:0.000}%*** " +
                    $"are held by insiders, so I'll take ***{sharesHeldByInsiders:###,###,###,##0}*** shares out, ");
                Console.WriteLine($"making the total available shares for purchase right around " +
                    $"***{yfData.defaultKeyStatistics.sharesOutstanding.value - sharesHeldByInsiders:###,###,###,##0}***.");
                Console.WriteLine("This is the number I'll use to determine shorted percent... the percent of ");
                Console.WriteLine("shares available in the 'open' market.");
                Console.WriteLine("");

                if (yfData.defaultKeyStatistics.sharesOutstanding.value > 0)
                {
                    GetShortDataSinceDate(
                        yfData.defaultKeyStatistics.sharesShortPriorMonth.value, 
                        yfData.defaultKeyStatistics.sharesShortPreviousMonthDate.value, 
                        symbol,
                        yfData.defaultKeyStatistics.sharesOutstanding.value - sharesHeldByInsiders
                    );
                }

                Console.WriteLine("------------------------------------------------------------------------------");
            }
        }

        private static YahooFinanceStatistics GetYahooFinanceForSymbol(string symbol)
        {
            YahooFinanceStatistics obj = null;
            string apiKey = ConfigurationManager.AppSettings["ApiKey"];
            string apiHost = ConfigurationManager.AppSettings["ApiHost"];
            if (YFCache.Has(symbol))
            {
                obj = YFCache.Load(symbol);
            }
            else
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{apiHost}/stock/v2/get-statistics?symbol={symbol}&region=US"),
                    Headers =
                {
                    { "x-rapidapi-key", apiKey },
                    { "x-rapidapi-host", apiHost },
                },
                };

                using (var response = client.SendAsync(request).Result)
                {
                    response.EnsureSuccessStatusCode();
                    var body = response.Content.ReadAsStringAsync().Result;

                    obj = JsonConvert.DeserializeObject<YahooFinanceStatistics>(body);
                }

                YFCache.Save(symbol, obj);
            }
            return obj;
        }

        private static string GetString(string start)
        {
            string tmpStr = Console.ReadLine();
            if (tmpStr.Trim() == "") return start;

            return tmpStr;
        }

        private static void GetShortDataSinceDate(Int64 startingShort, DateTime startingDate, string symbol, Int64 sharesOutstanding)
        {
            Int64 cumulativeShort = startingShort;

            DateTime curr = startingDate;

            // I'll remove this chunk of expository once I know the calculations are correct and just let the numbers be the numbers.
            // ------------------------------------------------------------------------------------------------------------------------
            Console.WriteLine("By my calculation of published FINRA data, (I know... self-reported... but look at what they reported. " +
                "It may reveal more than you think!) the math reveals that their short position MAY be increasing " +
                "or decreasing. I then try to use this same data to guess at how MUCH it possibly increased or decreased. " +
                "This is a stat I'm calling Squeeze Factor, where the math on even the REPORTED numbers shows that it is " +
                "very likely over-shorted, and a wild stab at quantifying HOW MUCH it is over-shorted." +
                "\r\n\r\n " +
                "***Keep an eye on the second column, SqF.*** That's the key factor here. The bigger that number gets, the better " +
                "off our position becomes.");
            Console.WriteLine("");

            // Create Markdown Table            
            Console.Write("| Date  | Squeeze Factor (SqF) | Pressure |");
            if (AppConfig.ShowVolumes) { Console.Write(" Volume | Short Volume | Daily guess at short volume change |"); }
            Console.WriteLine("");

            Console.Write("|:--:| --:|:--:|");
            if (AppConfig.ShowVolumes)
            {
                Console.Write("--:| --:| --:|");
            }
            Console.WriteLine("");

            bool todaysFile = AppConfig.CheckForToday ? 
                    GetFileDataForDate(DateTime.Now.Date, symbol) != null :
                    true;

            if (todaysFile)
            {
                decimal pct = 0;
                decimal prevSqueezeFactor = 0;
                int decimals = 5;

                while (curr < DateTime.Now.Date)
                {
                    curr = curr.AddDays(1);

                    if (curr.DayOfWeek == DayOfWeek.Sunday || curr.DayOfWeek == DayOfWeek.Saturday) continue;

                    FinraData data = GetFileDataForDate(curr, symbol);

                    // Keep a cumulative count of short volumes. This is not EXACT, but we'll use this 
                    // to determine how much PRESSURE we think the hedge funds might be under. This assumes
                    // that ALL non-short volumes go to cover the short volumes.
                    if (data != null && sharesOutstanding != 0)
                    {
                        cumulativeShort = cumulativeShort + (2 * data.ShortVolume) - data.TotalVolume;

                        Int64 newShorts = cumulativeShort - startingShort;

                        // How does this cumulative Shortage compare to the previous shortage (by percent)
                        if (cumulativeShort != 0)
                        {
                            pct = ((decimal)newShorts / (decimal)startingShort) * 100m;
                        }

                        // If they've already possibly cleared their position, set it to 0.
                        if (cumulativeShort < 0) { cumulativeShort = 0; }

                        // It really skyrockets when the squeeze is above 50% of all outstanding shares. Anything under this percentage, 
                        // and hedge funds are likely to wiggle out before a squeeze can take effect.
                        decimal osp = 50.0M;
                        decimal shortPercent = Math.Round((decimal)cumulativeShort / (decimal)sharesOutstanding, decimals);
                        // The Squeeze Factor is the cumulative short percent (integer form) MINUS the outstanding share percent wiggle room defined above.
                        decimal squeezeFactor = (shortPercent * 100.0M) - osp;
                        // If that is not above 50*, then leave it simply the assumed short percentage in decimal form.
                        if (squeezeFactor < 0) squeezeFactor = shortPercent;

                        string incdec = " ";
                        if (squeezeFactor > prevSqueezeFactor) incdec = "+";
                        if (squeezeFactor < prevSqueezeFactor) incdec = "-";
                        prevSqueezeFactor = squeezeFactor;

                        // SO.... shorted cumulative from 0-50% will be a Squeeze factor from 0.0 to 0.9999. At 50% and up, it starts climbing. 
                        // A Squeeze Factor of 50 would then imply that 100% of the available float MAY have been accounted for. Since we don't
                        // really know for sure, we don't want to state that the Shorted Float is 100%!! It may not be. But what we DO know is 
                        // that the Hedge funds should be feeling quite a bit of pressure, which could drive the price up.

                        Console.Write($"| {curr.ToString("yyyy-MM-dd")} | {squeezeFactor:0.000} | {incdec} ");
                        if (AppConfig.ShowVolumes)
                        {
                            Console.Write($"| {data.TotalVolume.ToString("###,###,###,##0")} | {data.ShortVolume.ToString("###,###,###,##0")} | {pct:0.0000} % |");
                        }
                        Console.WriteLine("");
                    }
                    else
                    {
                        Console.WriteLine($"| {curr.Date.ToString("yyyy-MM-dd")} | has no data.");
                    }
                }
            }
            else
            {
                Console.WriteLine("| No data for today, yet.");
            }
        }

        private static FinraData GetFileDataForDate(DateTime startingDate, string symbol)
        {
            string dateFile = @"http://regsho.finra.org/" + $"CNMSshvol{startingDate.ToString("yyyyMMdd")}.txt";

            var webRequest = WebRequest.Create(dateFile);

            FinraData data = null;
            try
            {
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    var strContent = reader.ReadToEnd();

                    foreach (string l in strContent.Replace("\r\n", "\n").Split("\n".ToCharArray()))
                    {
                        string[] strArr = l.Split("|".ToCharArray()).ToArray();
                        if (strArr.Length > 4 && strArr[1] == symbol)
                        {
                            data = new FinraData();

                            data.Symbol = strArr[1];
                            data.ShortVolume = int.Parse(strArr[2]);
                            data.ShortExempt = int.Parse(strArr[3]);
                            data.TotalVolume = int.Parse(strArr[4]);

                            break;
                        }
                    }
                }
            } catch (Exception ex)
            {
            }

            return data;
        }
    }
}
