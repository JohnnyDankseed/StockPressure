namespace GetShares
{
    public class YahooQuoteKeyStatistics
    {
        public YahooInteger sharesOutstanding { get; set; }
        
        public YahooNumber heldPercentInsiders { get; set; }

        public YahooInteger floatShares { get; set; }

        public YahooDate sharesShortPreviousMonthDate { get; set; }

        public YahooInteger sharesShort { get; set; }

        public YahooInteger sharesShortPriorMonth { get; set; }
    }
}