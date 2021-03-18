namespace GetShares
{
    public class YahooQuoteKeyStatistics
    {
        public YahooInteger sharesOutstanding { get; set; }
        
        public YahooNumber heldPercentInsiders { get; set; }

        public YahooNumber heldPercentInstitutions { get; set; }

        public YahooInteger floatShares { get; set; }

        public YahooNumber shortPercentOfFloat { get; set; }

        public YahooDate sharesShortPreviousMonthDate { get; set; }


        public YahooDate dateShortInterest { get; set; }

        public YahooInteger sharesShort { get; set; }

        public YahooNumber shortRatio { get; set; }

        public YahooInteger sharesShortPriorMonth { get; set; }

    }
}