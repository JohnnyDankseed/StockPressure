using Newtonsoft.Json;
using System;
using System.IO;

namespace GetShares
{
    /// <summary>
    /// I can only hit this account SO MANY times before I get shut down for a month. As such, I want to
    /// cache as MUCH of this information as possible.
    /// </summary>
    public class YFCache
    {
        public static bool Has(string symbol)
        {
            if (!Directory.Exists("Cache"))
            {
                Directory.CreateDirectory("Cache");
            }
            
            string fn = $@"Cache\{symbol}.txt";

            if (File.Exists(fn))
            {
                string data = File.ReadAllText(fn);
                YFCache cache = JsonConvert.DeserializeObject<YFCache>(data);
                if (cache.Expires < DateTime.Now)
                {
                    // It has expired. Delete it.
                    File.Delete(fn);
                    return false;
                }
                return true;
            }

            return false;
        }

        public static YahooFinanceStatistics Load(string symbol)
        {
            string fn = $@"Cache\{symbol}.txt";

            string data = File.ReadAllText(fn);
            YFCache cache = JsonConvert.DeserializeObject<YFCache>(data);

            return cache.Statistics;
        }

        public DateTime Expires { get; set; }
        public YahooFinanceStatistics Statistics { get; set; }

        public static void Save(string symbol, YahooFinanceStatistics obj)
        {
            string fn = $@"Cache\{symbol}.txt";
            YFCache cache = new YFCache();

            cache.Expires = DateTime.Now.AddDays(7);
            cache.Statistics = obj;
            string json = JsonConvert.SerializeObject(cache);
            
            File.WriteAllText(fn, json);
        }
    }
}