using BBP.CORE.API.Utilities;
using BMSCommon;
using BMSCommon.Model;
using Newtonsoft.Json;
using Npgsql;
using System.Numerics;
using static BMSCommon.BBPCharting;
using static BMSCommon.Common;

namespace BBPAPI
{
    public static class PricingService
    {
        private static string msTickers = "BTC/USD,DASH/BTC,DOGE/BTC,LTC/BTC,ETH/BTC,XRP/BTC,XLM/BTC,BBP/BTC,ZEC/BTC,BCH/BTC";
        private static string msWeights = "1,185,130000,185,15,35000,125000,45000000,210,50";

        private static double PricingBigIntToDouble(BigInteger bi, int nDecimals)
        {
            BigInteger nDivisor = 1;
            if (nDecimals == 18)
            {
                nDivisor = 1000000000000000000;
            }
            else if (nDecimals == 10)
            {
                nDivisor = 10000000000;
            }
            else if (nDecimals == 8)
            {
                nDivisor = 100000000;
            }
            else
            {
                throw new Exception("Divisor unknown");
            }

            BigInteger divided = BigInteger.Divide(bi, nDivisor);
            if (divided < 1000)
            {
                decimal nNew = (decimal)bi;
                double nOut = (double)(nNew / (decimal)GetDouble(nDivisor.ToString()));
                return nOut;
            }
            else
            {
                double nBal = GetDouble(divided.ToString());
                return nBal;
            }
        }

        public static string GetChartOfIndex()
        {
            BBPChart b = new BBPChart
            {
                Name = "BiblePay Weighted CryptoCurrency Index",
                Type = "date"
            };

            string[] vTickers = BBPAPI.PricingService.msTickers.Split(",");
            string[] vWeights = PricingService.msWeights.Split(",");
           
            var dPrices = new Dictionary<DateTime, double>();

            //Index
            ChartSeries sIndex = new ChartSeries
            {
                Name = b.Name,
                BorderColor = "lime",
                BackgroundColor = "green"
            };
            b.CollectionSeries.Add(sIndex);
            double nPrice2 = 0;

            //Convert to opensource version: https://www.w3schools.com/ai/ai_chartjs.asp
            int iStep = 1;
            DateTime dtStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            for (int i = 180; i > 1; i = i - iStep)
            {
                DateTime dt = dtStart.AddDays(-1 * i);

                long nTimestamp = DateToUnixTimestamp(dt);

                b.XAxis.Add(nTimestamp);

                bool fGot = dPrices.TryGetValue(dt, out nPrice2);
                if (nPrice2 == 0)
                {
                    // This is a base level for the cryptocurrency homogenized index.  This doesn't get hit after chart is 60 days old.
                    nPrice2 = 15000;
                }

                b.CollectionSeries[0].DataPoint.Add(nPrice2);

            }

            string html = GenerateJavascriptChart(b);
            return html;
        }


        public static async Task<double> ConvertUSDToBiblePay(double nUSD)
        {
            price1 nBTCPrice = await GetCryptoPrice("BTC/USD");
            price1 nBBPPrice = await GetCryptoPrice("BBP/BTC");
            double nUSDBBP = nBTCPrice.AmountUSD * nBBPPrice.Amount;
            double nOut = nUSD / (nUSDBBP + .000000001);
            return nOut;
        }

        private static price1 _nBTCPrice = GetCryptoPrice("BTC/USD").Result;
        private static price1 _nBBPPrice = GetCryptoPrice("BBP/BTC").Result;

        public static double ConvertUSDToBiblePayWithCache(double nUSD)
        {
            double nUSDBBP = _nBTCPrice.AmountUSD * _nBBPPrice.Amount;
            double nOut = nUSD / (nUSDBBP + .000000001);
            return nOut;
        }


        internal static string TickerToName(string sTicker)
        {
            if (sTicker == "DOGE")
            {
                return "dogecoin";
            }
            else if (sTicker == "BTC")
            {
                return "bitcoin";
            }
            else if (sTicker == "DASH")
            {
                return "dash";
            }
            else if (sTicker == "LTC")
            {
                return "litecoin";
            }
            else if (sTicker == "XRP")
            {
                return "ripple";
            }
            else if (sTicker == "XLM")
            {
                return "stellar";
            }
            else if (sTicker == "BCH")
            {
                return "bitcoin-cash";
            }
            else if (sTicker == "ZEC")
            {
                return "zcash";
            }
            else if (sTicker == "ETH")
            {
                return "ethereum";
            }
            Log("Ticker mapping missing " + sTicker);
            return sTicker;
        }
        public async static Task<BMSCommon.Model.price1> GetCryptoPrice(string sTicker)
        {
            price1 p = new price1();


            p.Amount = await GetPriceQuote(sTicker);
            double dUSDCryptoPrice = await GetPriceQuote("BTC/USD");
            p.AmountUSD = dUSDCryptoPrice * p.Amount;
            p.Ticker = sTicker.ToUpper();
            if (sTicker.ToUpper() == "BTC" || sTicker == "BTC/BTC" || sTicker == "BTC/USD" || sTicker == "BTC/USDT")
            {
                p.AmountUSD = dUSDCryptoPrice;
            }
            return p;
        }


        internal static void StorePriceHistory(string sTicker, double sUSDValue, double sBTCValue, DateTime theDate)
        {
            string added = theDate.ToString("yyyy-MM-dd");
            string sql = "Delete from bbp.quotehistory where ticker=@ticker and added='" + added + "';";
            NpgsqlCommand cmd1 = new NpgsqlCommand(sql);
            cmd1.Parameters.AddWithValue("@ticker", sTicker);
            sql = "Insert Into bbp.quotehistory (id,added,ticker,usd,btc) values (cast(gen_random_uuid() as varchar(256)),'"
                + added + "',@ticker,'" + sUSDValue.ToString() + "','" + sBTCValue.ToString() + "');";
            NpgsqlCommand cmd2 = new NpgsqlCommand(sql);
            cmd2.Parameters.AddWithValue("@ticker", sTicker);
            if (sUSDValue < .01 && sTicker != "BBP")
            {
                Log("Low quote " + sTicker + sUSDValue.ToString() + "," + sBTCValue.ToString());
            }
        }

        public static async Task<double> GetPriceQuote(string ticker, int nAssessmentType = 0)
        {
            // main area
            string sData1 = String.Empty;
            double dCachedQuote = 0;
            if (ticker == "BTC/USD")
            {
                ticker = "BTC/USDT";//SX has moved from USD to USDT
            }


			if (ticker == "ARB/USD")
			{
				// price from chainlink
				double nPrice = await Ethereum.GetChainlinkPrice("ARB/USD");
                return nPrice;
			}

			// Since BBP is at Bololex now, if they ask for BBP/BTC, we get the BTC price first, and the BBP/USDT first, then we calculate BBP/BTC here:
			if (ticker=="BBP/BTC")
            {
                double nBTCUSD = await GetPriceQuote("BTC/USDT");
                double nBBPUSD = await GetPriceQuote("BBP/USDT");
                double nMyCalculator = nBBPUSD / (nBTCUSD + .01);
                return nMyCalculator;
            }

			ticker = ticker.ToUpper();
			ticker = ticker.Replace("/", "-");


			try
			{
                if (ticker == "BTC/BTC")
                {
                    return 0;
                }
                dCachedQuote = NVRAM.GetNVRamValue(ticker).ToDouble();
                if (dCachedQuote > 0)
                    return dCachedQuote;

                string[] vTicker = ticker.Split("/");
                string LeftTicker = "";
                if (vTicker.Length == 2)
                {
                    LeftTicker = vTicker[0];
                }
                if (LeftTicker == "XRP" || LeftTicker == "XLM" || LeftTicker == "BCH" || LeftTicker == "ZEC")
                {
                    string sCoinName = TickerToName(LeftTicker);
                    string sKey = Encryption.DecryptAES256("ZtEciCL5O3gSru+1VvKpzppMuAflYzPkE4pZ8dz+F41U52tSupSEG8ldJKgRI/rw", "");

                    string sURL1 = "https://api.blockchair.com/" + sCoinName + "/stats?key=" + sKey;
                    sData1 = BMSCommon.Functions.ExecuteMVCCommand(sURL1);
                    dynamic oJson = JsonConvert.DeserializeObject<dynamic>(sData1);
                    if (oJson != null)
                    {
                        double nMyValue = oJson["data"]["market_price_btc"].Value ?? 0;
                        if (nMyValue > 0)
                        {
                           NVRAM.SetNVRamValue(ticker, nMyValue.ToString("0." + new string('#', 339)));
                        }
                        if (nMyValue == 0)
                        {
                            Log("For some reason my quote is very low for " + LeftTicker + ", " + sData1 + ": " + nMyValue.ToString());
                        }
                        return nMyValue;
                    }
                    else
                    {
                        Log("For some reason my quote is very low for " + LeftTicker + ", " + sData1 + ": ");
                        return 0;

                    }
                }

                
                string sURL = "https://api.bololex.com/api/prices/" + ticker;
                string sData = "";

                sData = BMSCommon.Functions.ExecuteMVCCommand(sURL);

				string bid = ExtractXML(sData, "bid\":", ",").ToString();
                bid = bid.Replace("\"", "");
                
                string ask = ExtractXML(sData, "ask\":", ",").ToString();
                ask = ask.Replace("\"", "");

                double dbid = GetDouble(bid);
                double dask = GetDouble(ask);
                double dTotal = dbid + dask;
                double dmid = dTotal / 2;
                if (nAssessmentType == 1)
                    dmid = dbid;
                if (dmid > 0)
                {
                    NVRAM.SetNVRamValue(ticker, dmid.ToString("0." + new string('#', 339)));
                }


                Price2 pr2 = new Price2();
                pr2.Added = DateTime.Now;
                pr2.Symbol = ticker;
                pr2.Price = dmid;
                pr2.id = pr2.Symbol;
                return dmid;
            }
            catch (Exception ex)
            {
                Log("Bad Pricing error [2] " + ticker + " - " + ex.Message + " " + sData1);
                return dCachedQuote;
            }
        }



        // Once per day we will store the historical quotes, to build the cryptocurrency index chart
        internal static async Task<bool> StorePriceQuotes(int offset)
        {
            try
            {
                DateTime theDate = DateTime.Now;
                if (offset < 0)
                {
                    theDate = DateTime.Now.Subtract(TimeSpan.FromDays(offset * -1));
                }
                string[] vTickers = msTickers.Split(",");
                string[] vWeights = msWeights.Split(",");
                double dTotalIndex = 0;
                double nBTCUSD = NVRAM.GetNVRamValue("BTC/USD").ToDouble();

                for (int i = 0; i < vTickers.Length; i++)
                {
                    double nQuote = await GetPriceQuote(vTickers[i]);

                    double nUSDValue = 0;
                    if (vTickers[i] != "BTC/USD")
                    {
                        nUSDValue = nBTCUSD * nQuote;
                    }
                    else
                    {
                        nUSDValue = nQuote;
                    }
                    double dWeight = GetDouble(BMSCommon.Functions.GE(vWeights[i], ",", 0));
                    dTotalIndex += dWeight * nUSDValue;
                    string sTicker = BMSCommon.Functions.GE(vTickers[i], "/", 0);
                    StorePriceHistory(sTicker, nUSDValue, nQuote, theDate);
                }
                double dIndexValue = dTotalIndex / vTickers.Length;
                StorePriceHistory("IndexValue", dIndexValue, dIndexValue, theDate);
            }
            catch (Exception ex)
            {
                Log("Store Quote History:" + ex.Message);
            }
            return true;
        }
    }
}

