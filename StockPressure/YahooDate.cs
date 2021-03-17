using System;

namespace GetShares
{
    public class YahooDate
    {
        public string raw { get; set; }
        public string fmt { get; set; }

        public DateTime value 
        { 
            get
            {
                DateTime result = DateTime.MinValue;
                DateTime.TryParse(fmt, out result);
                return result;
            } 
        }
    }
}