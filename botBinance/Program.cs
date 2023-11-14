using Binance.Net.Clients;
using Binance.Net.Clients.SpotApi;
using Binance.Net.Enums;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CryptoExchange.Net.Interfaces;
using NLog.Config;
using NLog.Targets;
using NLog;
using NLog.Conditions;
using TANet.Core;
using System;
using Newtonsoft.Json.Linq;
using Binance.Net.Interfaces.Clients;
using System.Runtime.CompilerServices;
using CryptoExchange.Net.CommonObjects;
using System.Collections.Generic;
using TicTacTec.TA.Library;
using TANet.Contracts.OperationResults.Indicators;
using System.Runtime.InteropServices;
using TANet.Contracts.Models;
using static botBinance.Program;
using System.Runtime.Intrinsics.X86;

namespace botBinance
{
    class Program
    {

        public static int POINTS_TO_ENTER = 2;
        static int checkPointTimeOut = 10000;
        
        //Supertrend
        static int STPeriod = 21;
        static double STFactor = 2.62;
        //Stoch
        static int fastK1m = 7;
        static int slowD1m = 1;
        static int smooth1m = 1;
        //Bolliger
        static int period = 20;
        static double deviation = 2.45;
        //RSI Period
        static int RSI_Period = 14;

        static double currentPrice = 1;
        static double threshold = 0.00025; // 50% deviation from the price
        static double profit_precent = 1.00301;
        static double profit_precentLimitMarket = 1.0000;
        static string API_KEY = "";
        static string API_SECRET = "";

        static string BUYside = "BUY";
        static string SELLside = "SELL";
        static string type = "MARKET";

        static decimal quantityDec = 24m;
        public static double quantity = 24;
        public static double quantityMartin = quantity * 2;

        static string baseQ = "XRP";
        static string quoteQ = "USDT";
        static string symbol = baseQ + quoteQ;
        static string url = "https://api.binance.com/api/v3/ticker/price?symbol=XRPUSDT";

        static double priceJSONglobal;

        static bool isBuy = false;
        static bool isLong = false;
        static bool isShort = false;
        public static bool isSell = false;
        public static double sellPrice = 0;

        static double qtyJSONglobalBase;
        static double cummulativeJSONglobalBase = 0;

        static double summaryProfit;
        static double percentageProfitSummary;
        static double profit_summary;
        static double percent_profitSummary;
        static double percent_profitSummaryOneOrder;
        static decimal lastOrderId;

        private static Timer _timer;
        private static int _counter = 0;
        private static readonly object _lock = new object();

        public static Logger Logger = LogManager.GetCurrentClassLogger();
        public enum TradeDirection
        {
            Long,
            Short
        }


        static void Main(string[] args)
        {

            #region NLog Initializator
            var config = new NLog.Config.LoggingConfiguration();
            LogManager.Configuration = new LoggingConfiguration();
            const string LayoutFile = @"[${date:format=yyyy-MM-dd HH\:mm\:ss}] [${logger}/${uppercase: ${level}}] [THREAD: ${threadid}] >> ${message} ${exception: format=ToString}";
            var consoleTarget = new ColoredConsoleTarget("Console Target")
            {
                Layout = @"${counter}|[${date:format=yyyy-MM-dd HH\:mm\:ss}] [${logger}/${uppercase: ${level}}] [THREAD: ${threadid}] >> ${message} ${exception: format=ToString}"
            };

            var logfile = new FileTarget();

            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);

            logfile.CreateDirs = true;
            logfile.FileName = $"logs{Path.DirectorySeparatorChar}lastlog.log";
            logfile.AutoFlush = true;
            logfile.LineEnding = LineEndingMode.CRLF;
            logfile.Layout = LayoutFile;
            logfile.FileNameKind = FilePathKind.Absolute;
            logfile.ConcurrentWrites = false;
            logfile.KeepFileOpen = true;

            #region NLog Colors

            var Trace = new ConsoleRowHighlightingRule();
            Trace.Condition = ConditionParser.ParseExpression("level == LogLevel.Trace");
            Trace.ForegroundColor = ConsoleOutputColor.Yellow;
            consoleTarget.RowHighlightingRules.Add(Trace);
            var Debug = new ConsoleRowHighlightingRule();
            Debug.Condition = ConditionParser.ParseExpression("level == LogLevel.Debug");
            Debug.ForegroundColor = ConsoleOutputColor.DarkCyan;
            consoleTarget.RowHighlightingRules.Add(Debug);
            var Info = new ConsoleRowHighlightingRule();
            Info.Condition = ConditionParser.ParseExpression("level == LogLevel.Info");
            Info.ForegroundColor = ConsoleOutputColor.Green;
            consoleTarget.RowHighlightingRules.Add(Info);
            var Warn = new ConsoleRowHighlightingRule();
            Warn.Condition = ConditionParser.ParseExpression("level == LogLevel.Warn");
            Warn.ForegroundColor = ConsoleOutputColor.DarkYellow;
            consoleTarget.RowHighlightingRules.Add(Warn);
            var Error = new ConsoleRowHighlightingRule();
            Error.Condition = ConditionParser.ParseExpression("level == LogLevel.Error");
            Error.ForegroundColor = ConsoleOutputColor.DarkRed;
            consoleTarget.RowHighlightingRules.Add(Error);
            var Fatal = new ConsoleRowHighlightingRule();
            Fatal.Condition = ConditionParser.ParseExpression("level == LogLevel.Fatal");
            Fatal.ForegroundColor = ConsoleOutputColor.Black;
            Fatal.BackgroundColor = ConsoleOutputColor.DarkRed;
            consoleTarget.RowHighlightingRules.Add(Fatal);

            #endregion NLog Colors

            // Apply config
            NLog.LogManager.Configuration = config;

            #endregion NLog Initializator
            /*
                        Logger.Trace("Hello from Gamania!");
                        Logger.Debug("Hello from Gamania!");
                        Logger.Info("Hello from Gamania!");
                        Logger.Warn("Hello from Gamania!");
                        Logger.Error("Hello from Gamania!");
                        Logger.Fatal("Hello from Gamania!");*/

            Console.WriteLine("Старт бот");
            Thread.Sleep(500);
            /*CheckPointsSUPER();*/
            Start();

        }

        public static void Start()
        {
            isSell = false;
            /*            priceJSONglobal = 0;
                        currentPrice = 0;
                        qtyJSONglobalBase = 0;
                        cummulativeJSONglobalBase = 0;*/
            /*GetPrice();*/
            Thread.Sleep(1000);
            CheckIndicatorsStart();
        }


        static void GetPrice()
        {

            var client23 = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });

            var usdFuturesTradeHistoryData = client23.UsdFuturesApi.ExchangeData.GetTradeHistoryAsync(symbol, limit: 1).Result;
            List<double> priceArray = new List<double>();
            foreach (var item in usdFuturesTradeHistoryData.Data)
            {
                priceArray.Add(Convert.ToDouble(item.Price));
            }
            currentPrice = priceArray[0];


        }

        static void LimitOrderAsync()
        {
            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });

            var percentStopLoss = 0.0002;
            var percentProfit = 0.0006;
            var currentDirection = TradeDirection.Long;


            dynamic lowPrices1m = new List<decimal>();

            // Получение минимальной цены текущей свечи
            var candles = client.SpotApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute, limit: 1).Result;
            if (candles.Success)
            {
                foreach (var candle in candles.Data)
                {
                    lowPrices1m.Add(candle.LowPrice);

                }
            }
            else
            {
                /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
            }

            double[] arrLow1m2 = ConvertToDouble(lowPrices1m);

            var currentCandle = arrLow1m2[0];
            var minPrice = currentCandle;
            var dev = minPrice * percentStopLoss;
            var proffit = percentProfit * priceJSONglobal;

            // Вычисление стоп-лосса и take-profit
            var stopLoss = currentDirection == TradeDirection.Long ? minPrice : currentPrice + (currentPrice - minPrice);
            stopLoss = stopLoss - dev;
            var stopLossFormat = Math.Round(stopLoss, 2);
            var takeProfit = priceJSONglobal + proffit;
            var takeProfFormat = Math.Round(takeProfit, 2);


            // Размещение ордера на продажу по take-profit
            var orderQuantity = quantityDec;
            var sellOrder = client.SpotApi.Trading.PlaceOrderAsync(
                symbol,
                OrderSide.Sell,
                SpotOrderType.StopLossLimit,
                orderQuantity,
                stopPrice: Convert.ToDecimal(stopLossFormat),
                price: Convert.ToDecimal(takeProfFormat),
                timeInForce: TimeInForce.GoodTillCanceled);

        }


        static void MarketOrderLong()
        {
            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });



            BinanceClient clientCheckBinance = new BinanceClient();
            dynamic lowPrices1m = new List<decimal>();

            // Получение минимальной цены текущей свечи
            var candles = client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute, limit: 1).Result;
            if (candles.Success)
            {
                foreach (var candle in candles.Data)
                {
                    lowPrices1m.Add(candle.LowPrice);

                }
            }
            else
            {
                /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
            }

            double[] arrLow1m2 = ConvertToDouble(lowPrices1m);

            var currentCandle = arrLow1m2[0];



            while (true)
            {
                var percentStopLoss = 0.0002;
                var percentTakeProfit = 0.0006;

                var minPrice = currentCandle;
                var dev = minPrice * percentStopLoss;
                var proffit = percentTakeProfit * priceJSONglobal;

                // Вычисление стоп-лосса и take-profit
                var stopLoss = priceJSONglobal;
                stopLoss = stopLoss - dev;
                var stopLossFormat = Math.Round(stopLoss, 2);
                var takeProfit = priceJSONglobal + proffit;
                var takeProfFormat = Math.Round(takeProfit, 2);

                var latestPrice4 = clientCheckBinance.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result;
                var resss = latestPrice4.Data.Price;
                double btcPrice = Convert.ToDouble(resss);
                Console.WriteLine("CurrentPrice: " + btcPrice + " TakeProfit: " + takeProfFormat + " StopLoss: " + stopLossFormat);

                // Размещение ордера на продажу по take-profit
                if (btcPrice >= takeProfFormat)
                {

                    Console.WriteLine("Продажа по рынку\n");
                    SellOrderFururesLong();

                    Thread.Sleep(1000);
                    CheckPoints();
                    break;
                }

                if (btcPrice <= stopLossFormat)
                {
                    Console.WriteLine("Сработал стоп-лосс, создан ордер на продажу\n");
                    SellOrderFururesLong();

                    Thread.Sleep(1000);
                    CheckPoints();
                    break;

                }

            }


        }

        static void MarketOrderShort()
        {
            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });

            var percentStopLoss = 0.0002;
            var percentTakeProfit = 0.0006;
            /*var currentDirection = TradeDirection.Long;*/

            BinanceClient clientCheckBinance = new BinanceClient();
            dynamic highPrices1m = new List<decimal>();

            // Получение минимальной цены текущей свечи
            var candles = client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute, limit: 1).Result;
            if (candles.Success)
            {
                foreach (var candle in candles.Data)
                {
                    highPrices1m.Add(candle.HighPrice);

                }
            }
            else
            {
                /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
            }

            double[] arrHigh1m2 = ConvertToDouble(highPrices1m);

            var currentCandle = arrHigh1m2[0];

            while (true)
            {

                var maxPrice = currentCandle;
                var dev = maxPrice * percentStopLoss;
                var proffit = percentTakeProfit * priceJSONglobal;

                // Вычисление стоп-лосса и take-profit
                var stopLoss = priceJSONglobal;
                stopLoss = stopLoss + dev;
                var stopLossFormat = Math.Round(stopLoss, 2);
                var takeProfit = priceJSONglobal - proffit;
                var takeProfFormat = Math.Round(takeProfit, 2);

                var latestPrice4 = clientCheckBinance.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result;
                var resss = latestPrice4.Data.Price;
                double btcPrice = Convert.ToDouble(resss);
                Console.WriteLine("CurrentPrice: " + btcPrice + " TakeProfit: " + takeProfFormat + " StopLoss: " + stopLossFormat);

                // Размещение ордера на продажу по take-profit
                if (btcPrice <= takeProfFormat)
                {

                    Console.WriteLine("Продажа Short-позиции по рынку\n");
                    SellOrderFururesShort();

                    Thread.Sleep(1000);
                    CheckPointsShort();
                    break;
                }

                if (btcPrice >= stopLossFormat)
                {
                    Console.WriteLine("Сработал стоп-лосс, создан ордер на продажу\n");
                    SellOrderFururesShort();

                    Thread.Sleep(1000);
                    CheckPointsShort();
                    break;

                }

            }


        }

        //check price for sell/buy Futures order
        static void LongMarketTakeProfit()
        {

            Console.WriteLine("ShortMarketTakeProfit");
            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();

            var client1m = new BinanceClient();

            while (true)
            {
                int enter_points = 0;
                Console.WriteLine(enter_points);
                var result1m = client1m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m.Success)
                {
                    foreach (var candle in result1m.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m.Error.Message}");*/
                }
                double[] arrCloses1m = ConvertToDouble(closePrices1m);
                double[] arrHigh1m = ConvertToDouble(highPrices1m);
                double[] arrLow1m = ConvertToDouble(lowPrices1m);


                /*                //GetPrice*********************************************
                                var resultPrice = client1m.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result;
                                var price5m = Math.Round(resultPrice.Data.Price, 2);
                                var priceToDouble = Convert.ToDouble(price5m);*/

                //SMA10*30*********************************************
                /* var sma9 = CalculateSMA(arrCloses1m, 9);
                 var sma9_val = sma9[sma9.Length - 1];
                 var sma9_valFormat = Math.Round(sma9_val, 2);

                 var sma30 = CalculateSMA(arrCloses1m, 30);
                 var sma30_val = sma30[sma30.Length - 1];
                 var sma30_valFormat = Math.Round(sma30_val, 2);

                 if (sma9_valFormat < sma30_valFormat)
                 {
                     enter_points += 1;
                 }*/

                //SUPERTREND**********************************************
                /*                var (supper777, check) = Supertrend.Calculate(arrHigh1m, arrLow1m, arrCloses1m, STPeriod, STFactor);
                var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));

                if (check == true)
                {
                    enter_points += 1;
                }*/

                //STOCH**********************************************
                /*                var (fastKValues1m, slowKValues1m) = STOCHRSI(7, 7, 1);
                                var fastK_val1m = fastKValues1m[fastKValues1m.Length - 1];
                                var slowD_val1m = slowKValues1m[slowKValues1m.Length - 1];
                                var fastK_val_format1m = Math.Round(fastK_val1m, 3);
                                var slowD_val_format1m = Math.Round(slowD_val1m, 3);
                                if (fastK_val_format1m > 80)
                                {

                                    enter_points += 1;
                                    Console.WriteLine("STOCH1m: " + enter_points);
                                }*/
                /*Console.WriteLine("STOCH fastKValues1m " + fastK_val_format1m + " Stoch slowKValues1m " + slowD_val_format1m);*/

                double[] prices = arrCloses1m;
                double[] sma;
                double[] upperBand;
                double[] lowerBand;

                CalculateBollingerBands(arrCloses1m, period, deviation, out sma, out upperBand, out lowerBand);
                Console.WriteLine("Price: {0}, SMA: {1}, Upper Band: {2}, Lower Band: {3}", Math.Round(prices[prices.Length - 1], 2), Math.Round(sma[sma.Length - 1], 2), Math.Round(upperBand[upperBand.Length - 1], 2), Math.Round(lowerBand[lowerBand.Length - 1], 2));
                if (Math.Round(prices[prices.Length - 1], 2) >= Math.Round(sma[sma.Length - 1], 2))
                {
                    enter_points += 1;
                }
                Console.WriteLine(enter_points);

                if (enter_points == 1)
                {
                    SellOrderFururesLong();

                    Thread.Sleep(5000);

                    Start();
                    break;


                }
            }



        }

        static void ShortMarketTakeProfit()
        {

            Console.WriteLine("ShortMarketTakeProfit");
            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();

            var client1m = new BinanceClient();

            while (true)
            {
                int enter_points = 0;
                var result1m = client1m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m.Success)
                {
                    foreach (var candle in result1m.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m.Error.Message}");*/
                }
                double[] arrCloses1m = ConvertToDouble(closePrices1m);
                double[] arrHigh1m = ConvertToDouble(highPrices1m);
                double[] arrLow1m = ConvertToDouble(lowPrices1m);


                /*//GetPrice*********************************************
                var resultPrice = client1m.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result;
                var price5m = Math.Round(resultPrice.Data.Price, 2);
                var priceToDouble = Convert.ToDouble(price5m);

                //SMA10*30*********************************************
                var sma9 = CalculateSMA(arrCloses1m, 9);
                var sma9_val = sma9[sma9.Length - 1];
                var sma9_valFormat = Math.Round(sma9_val, 2);

                var sma30 = CalculateSMA(arrCloses1m, 30);
                var sma30_val = sma30[sma30.Length - 1];
                var sma30_valFormat = Math.Round(sma30_val, 2);

                if (sma9_valFormat > sma30_valFormat)
                {
                    enter_points += 1;
                }*/

                //SUPERTREND**********************************************
                /*                var (supper777, check) = Supertrend.Calculate(arrHigh1m, arrLow1m, arrCloses1m, STPeriod, STFactor);
                                var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                                var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));

                                if (check == false)
                                {
                                    enter_points += 1;
                                }*/

                //STOCH**********************************************
                /*                var (fastKValues1m, slowKValues1m) = STOCHRSI(7, 7, 1);
                                var fastK_val1m = fastKValues1m[fastKValues1m.Length - 1];
                                var slowD_val1m = slowKValues1m[slowKValues1m.Length - 1];
                                var fastK_val_format1m = Math.Round(fastK_val1m, 3);
                                var slowD_val_format1m = Math.Round(slowD_val1m, 3);
                                if (fastK_val_format1m < 20)
                                {

                                    enter_points += 1;
                                    Console.WriteLine("STOCH1m: " + enter_points);
                                }*/
                /* Console.WriteLine("STOCH fastKValues1m " + fastK_val_format1m + " Stoch slowKValues1m " + slowD_val_format1m);*/

                double[] prices = arrCloses1m;
                double[] sma;
                double[] upperBand;
                double[] lowerBand;

                CalculateBollingerBands(arrCloses1m, period, deviation, out sma, out upperBand, out lowerBand);
                Console.WriteLine("Price: {0}, SMA: {1}, Upper Band: {2}, Lower Band: {3}", Math.Round(prices[prices.Length - 1], 2), Math.Round(sma[sma.Length - 1], 2), Math.Round(upperBand[upperBand.Length - 1], 2), Math.Round(lowerBand[lowerBand.Length - 1], 2));
                if (Math.Round(prices[prices.Length - 1], 2) <= Math.Round(sma[sma.Length - 1], 2))
                {
                    enter_points += 1;
                }

                if (enter_points == 1)
                {
                    SellOrderFururesShort();

                    Thread.Sleep(5000);

                    Start();
                    break;


                }
            }


        }


        //methods buy order BuyOrder() - Long, BuyOrderShort() - Short
        static void BuyOrder()
        {
            var buyClient = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });

            var openPositionResult = buyClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Market, quantityDec).Result;
            Console.WriteLine(openPositionResult.Error);
            Thread.Sleep(2000);

            var dateTime = System.DateTime.Now;

            List<decimal> orderIdArray = new List<decimal>();
            List<decimal> priceOrderArray = new List<decimal>();
            List<decimal> quantityArray = new List<decimal>();
            List<decimal> quoteQuantyArray = new List<decimal>();
            List<decimal> feeArray = new List<decimal>();
            var userTradesResult = buyClient.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 1).Result;
            foreach (var item in userTradesResult.Data)
            {
                orderIdArray.Add(item.OrderId);
                priceOrderArray.Add(item.Price);
                quantityArray.Add(item.Quantity);
                quoteQuantyArray.Add(item.QuoteQuantity);
                feeArray.Add(item.Fee);

            }
            lastOrderId = orderIdArray[0];
            decimal lastPrice = priceOrderArray[0];
            decimal lastQuantity = quantityArray[0];
            decimal lastQuoteQuanty = quoteQuantyArray[0];
            decimal lastFee = feeArray[0];
            /*            Console.WriteLine(lastOrderId);
                        Console.WriteLine(lastPrice);
                        Console.WriteLine(lastQuantity);
                        Console.WriteLine(lastQuoteQuanty);
                        Console.WriteLine(lastFee);*/

            double feeOrder = Convert.ToDouble(lastFee);
            double priceJSONBuy = Convert.ToDouble(lastPrice);
            double qtyJSON = Convert.ToDouble(lastQuantity);
            double cummulativeQuoteQtyJSON = Convert.ToDouble(lastQuantity);
            priceJSONglobal = priceJSONBuy;
            qtyJSONglobalBase = qtyJSON;
            cummulativeJSONglobalBase = cummulativeQuoteQtyJSON;

            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine("****************************************");
            Console.WriteLine("********  СОЗДАН ОРДЕР LONG  *********");
            Console.WriteLine("****************************************");
            Console.WriteLine("********   " + dateTime + "   ******");
            Console.WriteLine(symbol + " OrderID: " + lastOrderId + "\n" + "Цена ордера: " + priceJSONBuy + "\n" + baseQ + "  <<: " + qtyJSON + "\n" + quoteQ + " >>: " + cummulativeQuoteQtyJSON + "\n" + "Fee >>: " + feeOrder + "\n");
            Console.WriteLine("****************************************" + "\n");


        }

        static void BuyOrderShort()
        {
            var buyClient = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });

            var openPositionResult = buyClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Market, quantityDec).Result;
            Console.WriteLine(openPositionResult.Error);
            Thread.Sleep(2000);

            var dateTime = System.DateTime.Now;

            List<decimal> orderIdArray = new List<decimal>();
            List<decimal> priceOrderArray = new List<decimal>();
            List<decimal> quantityArray = new List<decimal>();
            List<decimal> quoteQuantyArray = new List<decimal>();
            List<decimal> feeArray = new List<decimal>();
            var userTradesResult = buyClient.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 1).Result;
            foreach (var item in userTradesResult.Data)
            {
                orderIdArray.Add(item.OrderId);
                priceOrderArray.Add(item.Price);
                quantityArray.Add(item.Quantity);
                quoteQuantyArray.Add(item.QuoteQuantity);
                feeArray.Add(item.Fee);

            }
            decimal lastOrderId = orderIdArray[0];
            decimal lastPrice = priceOrderArray[0];
            decimal lastQuantity = quantityArray[0];
            decimal lastQuoteQuanty = quoteQuantyArray[0];
            decimal lastFee = feeArray[0];
            /*            Console.WriteLine(lastOrderId);
                        Console.WriteLine(lastPrice);
                        Console.WriteLine(lastQuantity);
                        Console.WriteLine(lastQuoteQuanty);
                        Console.WriteLine(lastFee);*/

            double feeOrder = Convert.ToDouble(lastFee);
            double priceJSONBuy = Convert.ToDouble(lastPrice);
            double qtyJSON = Convert.ToDouble(lastQuantity);
            double cummulativeQuoteQtyJSON = Convert.ToDouble(lastQuantity);
            priceJSONglobal = priceJSONBuy;
            qtyJSONglobalBase = qtyJSON;
            cummulativeJSONglobalBase = cummulativeQuoteQtyJSON;

            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine("****************************************");
            Console.WriteLine("********  СОЗДАН ОРДЕР SHORT  *********");
            Console.WriteLine("****************************************");
            Console.WriteLine("********   " + dateTime + "   ******");
            Console.WriteLine(symbol + " OrderID: " + lastOrderId + "\n" + "Цена ордера: " + priceJSONBuy + "\n" + baseQ + "  <<: " + qtyJSON + "\n" + quoteQ + " >>: " + cummulativeQuoteQtyJSON + "\n" + "Fee >>: " + feeOrder + "\n");
            Console.WriteLine("****************************************" + "\n");

        }



        private static async void SellOrderMartin(double quantity)
        {
            HttpClient clientSELL = new HttpClient();
            long timestamp2 = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;

            string queryString2 = "symbol=BTCUSDT&side=SELL&type=MARKET&quantity=" + quantity.ToString().Replace(",", ".") + "&recvWindow=5000&timestamp=" + timestamp2;
            string signature2 = GetSignature(queryString2);

            clientSELL.DefaultRequestHeaders.Add("X-MBX-APIKEY", API_KEY);
            HttpResponseMessage response3 = await clientSELL.PostAsync($"https://api.binance.com/api/v3/order?{queryString2}&signature={signature2}", null);
            string data3 = await response3.Content.ReadAsStringAsync();
            /*Console.WriteLine(data3);*/

            string jsonData3 = data3.ToString();
            dynamic jsonObj3 = JsonConvert.DeserializeObject(jsonData3);

            // Получение значения price массива ключа fils
            try
            {
                double sellPriceDouble = jsonObj3.fills[0].price;
                double sellQTYJSON = jsonObj3.fills[0].qty;
                double cummulativeSELLqtyJSON = jsonObj3.cummulativeQuoteQty;
                double buyPrice = priceJSONglobal;
                sellPrice = sellPriceDouble;

                double summOrder2 = sellPrice - buyPrice; //от цены продажи за 1 ордер вычитается цена покупки

                double percentageProfitSession2 = (summOrder2 / buyPrice) * 100;


                string percentageProfitFormat2 = string.Format("{0:f4}", percentageProfitSession2);
                percent_profitSummaryOneOrder = percentageProfitSession2;


                profit_summary = cummulativeSELLqtyJSON - cummulativeJSONglobalBase;
                percent_profitSummary = (profit_summary / cummulativeJSONglobalBase) * 100;

                summaryProfit = summaryProfit + profit_summary;
                percentageProfitSummary = percentageProfitSummary + percent_profitSummary;
                string profit_usd_Format = string.Format("{0:f4}", profit_summary);
                string profit_all_summFormat = string.Format("{0:f4}", summaryProfit);
                string percentageProfitSummaryFormat = string.Format("{0:f4}", percentageProfitSummary);

                Console.WriteLine("\n");
                Console.WriteLine("**************************************************");
                Console.WriteLine("*************   ОРДЕР НА ПРОДАЖУ   ***************");
                Console.WriteLine("**************************************************" + "\n");
                Console.Write("Цена покупки : ");  //"Профит: " + percentageProfitFormat + "%" + "\n");
                /*Console.ForegroundColor = ConsoleColor.Green;*/
                Console.Write(priceJSONglobal + "\n");
                Console.ResetColor();
                Console.Write("Цена продажи : ");
                /*Console.ForegroundColor = ConsoleColor.Red;*/
                Console.Write(sellPriceDouble + "\n");
                Console.ResetColor();

                //summaryProfit

                Console.Write("\nДоход за 1 ордер    : ");
                if (Convert.ToDouble(percentageProfitFormat2) < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(percentageProfitFormat2 + " %" + "  " + profit_usd_Format + " $");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(percentageProfitFormat2 + " %" + "  " + profit_usd_Format + " $");

                }

                Console.ResetColor();
                Console.Write("\nДоход за все ордера : ");
                if (Convert.ToDouble(profit_all_summFormat) < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(percentageProfitSummaryFormat + " %" + "  " + profit_all_summFormat + " $" + "\n");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(percentageProfitSummaryFormat + " %" + "  " + profit_all_summFormat + " $" + "\n");

                }

                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("SELL >>  " + "USDT: " + cummulativeSELLqtyJSON + "  BTC: " + sellQTYJSON);
                Console.WriteLine("BUY  >>  " + "USDT: " + cummulativeJSONglobalBase + "  BTC: " + qtyJSONglobalBase + "\n");

                Console.WriteLine("**************************************************");

                isSell = true;
                Console.WriteLine("Выполнено!\n");

            }
            catch
            {
                Console.WriteLine("Error");

            }
        }

        private static void SellOrderFururesLong()
        {
            var sellClient = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });




            var executedOrder = sellClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol,
                OrderSide.Sell,
                FuturesOrderType.Market,
                quantity: quantityDec, // Количество не нужно указывать, так как оно уже задано в ордере
                newClientOrderId: lastOrderId.ToString() // orderId ордера, который нужно исполнить
            ).Result;

            Console.WriteLine(executedOrder.Error);

            var dateTime = System.DateTime.Now;

            Thread.Sleep(2000);

            List<decimal> orderIdArray = new List<decimal>();
            List<decimal> priceOrderArray = new List<decimal>();
            List<decimal> quantityArray = new List<decimal>();
            List<decimal> quoteQuantyArray = new List<decimal>();
            List<decimal> feeArray = new List<decimal>();

            var userTradesResult = sellClient.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 1).Result;
            foreach (var item in userTradesResult.Data)
            {
                orderIdArray.Add(item.OrderId);
                priceOrderArray.Add(item.Price);
                quantityArray.Add(item.Quantity);
                quoteQuantyArray.Add(item.QuoteQuantity);
                feeArray.Add(item.Fee);

            }
            lastOrderId = orderIdArray[0];
            decimal lastPrice = priceOrderArray[0];
            decimal lastQuantity = quantityArray[0];
            decimal lastQuoteQuanty = quoteQuantyArray[0];
            decimal lastFee = feeArray[0];

            double feeOrder = Convert.ToDouble(lastFee);


            // Получение значения price массива ключа fils
            try
            {
                double sellPriceDouble = Convert.ToDouble(lastPrice);
                double sellQTYJSON = Convert.ToDouble(lastQuantity);
                double cummulativeSELLqtyJSON = Convert.ToDouble(lastQuantity);
                double buyPrice = priceJSONglobal;
                sellPrice = sellPriceDouble;

                double summOrder2 = sellPrice - buyPrice; //от цены продажи за 1 ордер вычитается цена покупки

                double percentageProfitSession2 = (summOrder2 / buyPrice) * 100;


                string percentageProfitFormat2 = string.Format("{0:f4}", percentageProfitSession2);
                percent_profitSummaryOneOrder = percentageProfitSession2;


                profit_summary = cummulativeSELLqtyJSON - cummulativeJSONglobalBase;
                percent_profitSummary = (profit_summary / cummulativeJSONglobalBase) * 100;

                summaryProfit = summaryProfit + profit_summary;
                percentageProfitSummary = percentageProfitSummary + percent_profitSummary;
                string profit_usd_Format = string.Format("{0:f4}", profit_summary);
                string profit_all_summFormat = string.Format("{0:f4}", summaryProfit);
                string percentageProfitSummaryFormat = string.Format("{0:f4}", percentageProfitSummary);

                Console.WriteLine("\n");
                Console.WriteLine("**************************************************");
                Console.WriteLine("*************   ОРДЕР НА ПРОДАЖУ   ***************");
                Console.WriteLine("*************         LONG         ***************");
                Console.WriteLine("**************************************************" + "\n");
                Console.Write("Цена покупки : ");  //"Профит: " + percentageProfitFormat + "%" + "\n");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(priceJSONglobal + "\n");
                Console.ResetColor();
                Console.Write("Цена продажи : ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(sellPriceDouble + "\n");
                Console.ResetColor();
                Console.WriteLine("Комиссия (Fee): " + feeOrder);
                //summaryProfit

                Console.Write("\nДоход за 1 ордер    : ");
                if (Convert.ToDouble(percentageProfitFormat2) < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(percentageProfitFormat2 + " %" + "  " + profit_usd_Format + " $");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(percentageProfitFormat2 + " %" + "  " + profit_usd_Format + " $");

                }

                Console.ResetColor();
                Console.Write("\nДоход за все ордера : ");
                if (Convert.ToDouble(profit_all_summFormat) < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(percentageProfitSummaryFormat + " %" + "  " + profit_all_summFormat + " $" + "\n");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(percentageProfitSummaryFormat + " %" + "  " + profit_all_summFormat + " $" + "\n");

                }

                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("SELL >>  " + "USDT: " + cummulativeSELLqtyJSON + "  BTC: " + sellQTYJSON);
                Console.WriteLine("BUY  >>  " + "USDT: " + cummulativeJSONglobalBase + "  BTC: " + qtyJSONglobalBase + "\n");

                Console.WriteLine("**************************************************");

                isSell = true;
                Console.WriteLine("Выполнено!\n");

            }
            catch
            {
                Console.WriteLine("Error");

            }
        }

        private static void SellOrderFururesShort()
        {
            var sellClient = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials("", ""),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
                    AutoTimestamp = false
                },
                UsdFuturesApiOptions = new BinanceApiClientOptions
                {
                    TradeRulesBehaviour = TradeRulesBehaviour.ThrowError,
                    BaseAddress = BinanceApiAddresses.Default.UsdFuturesRestClientAddress,
                    AutoTimestamp = true
                }
            });




            var executedOrder = sellClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol,
                OrderSide.Buy,
                FuturesOrderType.Market,
                quantity: quantityDec, // Количество не нужно указывать, так как оно уже задано в ордере
                newClientOrderId: lastOrderId.ToString() // orderId ордера, который нужно исполнить
            ).Result;

            Console.WriteLine(executedOrder.Error);

            var dateTime = System.DateTime.Now;

            Thread.Sleep(2000);

            List<decimal> orderIdArray = new List<decimal>();
            List<decimal> priceOrderArray = new List<decimal>();
            List<decimal> quantityArray = new List<decimal>();
            List<decimal> quoteQuantyArray = new List<decimal>();
            List<decimal> feeArray = new List<decimal>();

            var userTradesResult = sellClient.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 1).Result;
            foreach (var item in userTradesResult.Data)
            {
                orderIdArray.Add(item.OrderId);
                priceOrderArray.Add(item.Price);
                quantityArray.Add(item.Quantity);
                quoteQuantyArray.Add(item.QuoteQuantity);
                feeArray.Add(item.Fee);

            }
            lastOrderId = orderIdArray[0];
            decimal lastPrice = priceOrderArray[0];
            decimal lastQuantity = quantityArray[0];
            decimal lastQuoteQuanty = quoteQuantyArray[0];
            decimal lastFee = feeArray[0];

            double feeOrder = Convert.ToDouble(lastFee);


            // Получение значения price массива ключа fils
            try
            {
                double sellPriceDouble = Convert.ToDouble(lastPrice);
                double sellQTYJSON = Convert.ToDouble(lastQuantity);
                double cummulativeSELLqtyJSON = Convert.ToDouble(lastQuantity);
                double buyPrice = priceJSONglobal;
                sellPrice = sellPriceDouble;

                double summOrder2 = sellPrice - buyPrice; //от цены продажи за 1 ордер вычитается цена покупки

                double percentageProfitSession2 = (summOrder2 / buyPrice) * 100;


                string percentageProfitFormat2 = string.Format("{0:f4}", percentageProfitSession2);
                percent_profitSummaryOneOrder = percentageProfitSession2;


                profit_summary = cummulativeSELLqtyJSON - cummulativeJSONglobalBase;
                percent_profitSummary = (profit_summary / cummulativeJSONglobalBase) * 100;

                summaryProfit = summaryProfit + profit_summary;
                percentageProfitSummary = percentageProfitSummary + percent_profitSummary;
                string profit_usd_Format = string.Format("{0:f4}", profit_summary);
                string profit_all_summFormat = string.Format("{0:f4}", summaryProfit);
                string percentageProfitSummaryFormat = string.Format("{0:f4}", percentageProfitSummary);

                Console.WriteLine("\n");
                Console.WriteLine("**************************************************");
                Console.WriteLine("*************   ОРДЕР НА ПРОДАЖУ   ***************");
                Console.WriteLine("*************         SHORT         ***************");
                Console.WriteLine("**************************************************" + "\n");
                Console.Write("Цена покупки : ");  //"Профит: " + percentageProfitFormat + "%" + "\n");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(priceJSONglobal + "\n");
                Console.ResetColor();
                Console.Write("Цена продажи : ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(sellPriceDouble + "\n");
                Console.ResetColor();
                Console.WriteLine("Комиссия (Fee): " + feeOrder);
                //summaryProfit

                Console.Write("\nДоход за 1 ордер    : ");
                if (Convert.ToDouble(percentageProfitFormat2) < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(percentageProfitFormat2 + " %" + "  " + profit_usd_Format + " $");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(percentageProfitFormat2 + " %" + "  " + profit_usd_Format + " $");

                }

                Console.ResetColor();
                Console.Write("\nДоход за все ордера : ");
                if (Convert.ToDouble(profit_all_summFormat) < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(percentageProfitSummaryFormat + " %" + "  " + profit_all_summFormat + " $" + "\n");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(percentageProfitSummaryFormat + " %" + "  " + profit_all_summFormat + " $" + "\n");

                }

                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("SELL >>  " + "USDT: " + cummulativeSELLqtyJSON + "  BTC: " + sellQTYJSON);
                Console.WriteLine("BUY  >>  " + "USDT: " + cummulativeJSONglobalBase + "  BTC: " + qtyJSONglobalBase + "\n");

                Console.WriteLine("**************************************************");

                isSell = true;
                Console.WriteLine("Выполнено!\n");

            }
            catch
            {
                Console.WriteLine("Error");

            }
        }



        public static void CheckIndicators()
        {
            while (true)
            {
                CheckLong();

                if (isLong)
                {
                    Console.WriteLine("Цикл на проверку Лонг останлвлен");
                    Thread.Sleep(1000);


                    CheckPoints();
                    break;
                }


                CheckShort();

                if (isShort)
                {
                    Console.WriteLine("Цикл на проверку Лонг останлвлен");
                    Thread.Sleep(1000);

                    CheckPoints();
                    break;
                }



            }
        }


        public static void CheckLong()
        {
            /*MACD macd = new MACD();*/


            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();
            /*dynamic openPrices1m = new List<decimal>();*/

            var client1m = new BinanceClient();
            while (true)
            {
                var result1m = client1m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m.Success)
                {
                    foreach (var candle in result1m.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);
                        /*openPrices1m.Add(candle.OpenPrice);*/

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m.Error.Message}");*/
                }
                double[] arrCloses1m = ConvertToDouble(closePrices1m);
                /*            double[] arrOpen1m = ConvertToDouble(openPrices1m);*/
                double[] arrHigh1m = ConvertToDouble(highPrices1m);
                double[] arrLow1m = ConvertToDouble(lowPrices1m);
                int enter_points = 0;

                //MACD**********************************************
                /*var (macdLine1m, signalLine1m) = macd.MainMACD();
                var macd1m_val = macdLine1m[macdLine1m.Length - 1];
                var signalLine_val = signalLine1m[signalLine1m.Length - 1];
                var macd1m_val_format = Math.Round(macd1m_val, 3);
                var sign1m_val_format = Math.Round(signalLine_val, 3);

                *//*if (macd1m_val_format > sign1m_val_format && macd1m_val_format < 0 && sign1m_val_format < 0)*//*
                if (macd1m_val_format > sign1m_val_format)
                {
                    enter_points += 1;
                    *//*Console.WriteLine("Long MACD: {0}, Signal: {1}", macd1m_val_format, sign1m_val_format);*//*
                }*/


                //SMA10*30*********************************************
                decimal[] arrSMA = closePrices1m.ToArray();

                var SMA1m10 = Indicators.Sma(arrSMA, 10);
                var SMA1m10_val = SMA1m10.Ma[SMA1m10.Ma.Length - 1];
                var SMA1m10_val_format1m = Math.Round(SMA1m10_val, 2);

                var SMA1m30 = Indicators.Sma(arrSMA, 30);
                var SMA1m30_val = SMA1m30.Ma[SMA1m30.Ma.Length - 1];
                var SMA1m30_val_format1m = Math.Round(SMA1m30_val, 2);

                /*            Console.WriteLine();
                            Console.WriteLine("SMA10: " + SMA1m10_val_format1m);
                            Console.WriteLine("SMA30: " + SMA1m30_val_format1m);*/


                if (SMA1m10_val_format1m > SMA1m30_val_format1m)
                {
                    enter_points += 1;
                }


                //RSI**********************************************
                decimal[] array = closePrices1m.ToArray();
                var RSI1m = Indicators.Rsi(array, 8);

                var rsi1m_val = RSI1m.Rsi[RSI1m.Rsi.Length - 1];
                var rsi_val_format1m = Math.Round(rsi1m_val, 2);

                if (rsi_val_format1m > 40 && rsi_val_format1m > 25 && rsi_val_format1m < 50)
                {
                    enter_points += 1;
                }
                /*Console.WriteLine("RSI: " + rsi_val_format1m);*/


                //STOCH**********************************************
                /*var (fastKValues1m, slowKValues1m) = STOCH(arrHigh1m, arrLow1m, arrCloses1m, fastK1m, slowD1m, smooth1m);
                var fastK_val1m = fastKValues1m[fastKValues1m.Length - 1];
                var slowD_val1m = slowKValues1m[slowKValues1m.Length - 1];
                var fastK_val_format1m = Math.Round(fastK_val1m, 3);
                var slowD_val_format1m = Math.Round(slowD_val1m, 3);
                if (fastK_val_format1m > slowD_val_format1m && fastK_val_format1m < 50 && slowD_val_format1m < 50 && fastK_val_format1m > 5 && slowD_val_format1m > 5)
                {

                    enter_points += 1;
                    *//*Console.WriteLine("STOCH1m: " + enter_points);*//*
                }*/
                /* Console.WriteLine("STOCH fastKValues1m " + fastK_val_format1m + " Stoch slowKValues1m " + slowD_val_format1m);*/


                if (enter_points == POINTS_TO_ENTER)
                {
                    BuyOrder();

                    Console.WriteLine("Покупка Long");
                    Thread.Sleep(1000);

                    MarketOrderLong();
                    Thread.Sleep(1000);
                    break;
                }
                else
                {
                    continue;
                }
            }

        }

        public static void CheckShort()
        {
            MACD macd = new MACD();

            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();
            /*dynamic openPrices1m = new List<decimal>();*/

            var client1m = new BinanceClient();

            while (true)
            {
                var result1m = client1m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m.Success)
                {
                    foreach (var candle in result1m.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);
                        /*openPrices1m.Add(candle.OpenPrice);*/

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m.Error.Message}");*/
                }
                double[] arrCloses1m = ConvertToDouble(closePrices1m);
                /*            double[] arrOpen1m = ConvertToDouble(openPrices1m);*/
                double[] arrHigh1m = ConvertToDouble(highPrices1m);
                double[] arrLow1m = ConvertToDouble(lowPrices1m);
                int enter_points = 0;

                //MACD**********************************************
                /*var (macdLine1m, signalLine1m) = macd.MainMACD();
                var macd1m_val = macdLine1m[macdLine1m.Length - 1];
                var signalLine_val = signalLine1m[signalLine1m.Length - 1];
                var macd1m_val_format = Math.Round(macd1m_val, 3);
                var sign1m_val_format = Math.Round(signalLine_val, 3);

                *//*if (macd1m_val_format > sign1m_val_format && macd1m_val_format < 0 && sign1m_val_format < 0)*//*
                if (macd1m_val_format > sign1m_val_format)
                {
                    enter_points += 1;
                    *//*Console.WriteLine("Long MACD: {0}, Signal: {1}", macd1m_val_format, sign1m_val_format);*//*
                }*/


                //SMA10*30*********************************************
                decimal[] arrSMA = closePrices1m.ToArray();

                var SMA1m10 = Indicators.Sma(arrSMA, 10);
                var SMA1m10_val = SMA1m10.Ma[SMA1m10.Ma.Length - 1];
                var SMA1m10_val_format1m = Math.Round(SMA1m10_val, 2);

                var SMA1m30 = Indicators.Sma(arrSMA, 30);
                var SMA1m30_val = SMA1m30.Ma[SMA1m30.Ma.Length - 1];
                var SMA1m30_val_format1m = Math.Round(SMA1m30_val, 2);

                /*            Console.WriteLine();
                            Console.WriteLine("SMA10: " + SMA1m10_val_format1m);
                            Console.WriteLine("SMA30: " + SMA1m30_val_format1m);*/


                if (SMA1m10_val_format1m < SMA1m30_val_format1m)
                {
                    enter_points += 1;
                }


                //RSI**********************************************
                decimal[] array = closePrices1m.ToArray();
                var RSI1m = Indicators.Rsi(array, 8);

                var rsi1m_val = RSI1m.Rsi[RSI1m.Rsi.Length - 1];
                var rsi_val_format1m = Math.Round(rsi1m_val, 2);

                if (rsi_val_format1m < 60 && rsi_val_format1m < 75 && rsi_val_format1m > 50)
                {
                    enter_points += 1;
                }
                /*Console.WriteLine("RSI: " + rsi_val_format1m);*/


                //STOCH**********************************************
                /*var (fastKValues1m, slowKValues1m) = STOCH(arrHigh1m, arrLow1m, arrCloses1m, fastK1m, slowD1m, smooth1m);
                var fastK_val1m = fastKValues1m[fastKValues1m.Length - 1];
                var slowD_val1m = slowKValues1m[slowKValues1m.Length - 1];
                var fastK_val_format1m = Math.Round(fastK_val1m, 3);
                var slowD_val_format1m = Math.Round(slowD_val1m, 3);
                if (fastK_val_format1m > slowD_val_format1m && fastK_val_format1m < 50 && slowD_val_format1m < 50 && fastK_val_format1m > 5 && slowD_val_format1m > 5)
                {

                    enter_points += 1;
                    *//*Console.WriteLine("STOCH1m: " + enter_points);*//*
                }*/
                /* Console.WriteLine("STOCH fastKValues1m " + fastK_val_format1m + " Stoch slowKValues1m " + slowD_val_format1m);*/


                if (enter_points == POINTS_TO_ENTER)
                {
                    BuyOrderShort();

                    Console.WriteLine("Покупка Long");
                    Thread.Sleep(1000);

                    MarketOrderShort();
                    Thread.Sleep(1000);
                    break;
                }
                else
                {
                    continue;
                }
            }

        }



        public static void CheckIndicatorsStart()
        {
            Console.WriteLine("Проверка индикаторов для входа в сделку...");
            while (true)
            {
                CheckLongStart();

                if (isLong)
                {
                    
                    break;
                }

                Thread.Sleep(500);

                CheckShortStart();

                if (isShort)
                {
                    
                    break;
                }



            }
        }


        public static void CheckLongStart()
        {

            int enter_points = 0;

            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();
            /*dynamic openPrices1m = new List<decimal>();*/

            /*            dynamic closePrices15m = new List<decimal>();
                        dynamic highPrices15m = new List<decimal>();
                        dynamic lowPrices15m = new List<decimal>();*/


            var client1m = new BinanceClient();
            var client15m = new BinanceClient();

            var result1m = client1m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
            if (result1m.Success)
            {
                foreach (var candle in result1m.Data)
                {
                    closePrices1m.Add(candle.ClosePrice);
                    highPrices1m.Add(candle.HighPrice);
                    lowPrices1m.Add(candle.LowPrice);
                    /*openPrices1m.Add(candle.OpenPrice);*/

                }
            }
            else
            {
                Console.WriteLine($"Ошибка: {result1m.Error.Message}");
            }
            double[] arrCloses1m = ConvertToDouble(closePrices1m);
            /*double[] arrOpen1m = ConvertToDouble(openPrices1m);*/
            double[] arrHigh1m = ConvertToDouble(highPrices1m);
            double[] arrLow1m = ConvertToDouble(lowPrices1m);


/*            double[] highValues = arrHigh1m;
            double[] lowValues = arrLow1m;*/

            /*IchimokuCloudIndicator ichimoku = new IchimokuCloudIndicator(highValues, lowValues);
            double[] tenkanSen = ichimoku.GetTenkanSen();
            double[] kijunSen = ichimoku.GetKijunSen();
            double[] senkouSpanA = ichimoku.GetSenkouSpanA();
            double[] senkouSpanB = ichimoku.GetSenkouSpanB();
            bool bear = false;
            bool bull = false;


            for (int i = 0; i < highValues.Length; i++)
            {
              Console.WriteLine($"Tenkan-sen: {tenkanSen[i]}, Kijun-sen: {kijunSen[i]}, Close: {arrCloses1m[i]}, Senkou Span A: {senkouSpanA[i]}, Senkou Span B: {senkouSpanB[i]}");
              if (ichimoku.IsBullish(i))
                {
                    bull = true;
                    
                }
                else if (ichimoku.IsBearish(i))
                {
                    bear = true;
                    
                }
            }

            if (bull == true)
            {
                Console.WriteLine("Bullish signal");

            }
            else if (bear == true)
            {
                Console.WriteLine("Bearish signal");
            }*/


            /*var result15m = client15m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
            if (result1m.Success)
            {
                foreach (var candle in result1m.Data)
                {
                    closePrices15m.Add(candle.ClosePrice);
                    highPrices15m.Add(candle.HighPrice);
                    lowPrices15m.Add(candle.LowPrice);
                    *//*openPrices1m.Add(candle.OpenPrice);*//*

                }
            }
            else
            {
                *//*Console.WriteLine($"Ошибка: {result1m.Error.Message}");*//*
            }
            double[] arrCloses15m = ConvertToDouble(closePrices15m);
            double[] arrHigh15m = ConvertToDouble(highPrices15m);
            double[] arrLow15m = ConvertToDouble(lowPrices15m);*/


            //MACD**********************************************
            /*var (macdLine1m, signalLine1m) = macd.MainMACD();
            var macd1m_val = macdLine1m[macdLine1m.Length - 1];
            var signalLine_val = signalLine1m[signalLine1m.Length - 1];
            var macd1m_val_format = Math.Round(macd1m_val, 3);
            var sign1m_val_format = Math.Round(signalLine_val, 3);

            *//*if (macd1m_val_format > sign1m_val_format && macd1m_val_format < 0 && sign1m_val_format < 0)*//*
            if (macd1m_val_format > sign1m_val_format)
            {
                enter_points += 1;
                *//*Console.WriteLine("Long MACD: {0}, Signal: {1}", macd1m_val_format, sign1m_val_format);*//*
            }*/

            /*var resultPrice = client1m.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result;
            var price5m = Math.Round(resultPrice.Data.Price, 2);
            var priceToDouble = Convert.ToDouble(price5m);

            //SMA10*30*********************************************
            var sma9 = CalculateSMA(arrCloses1m, 9);
            var sma9_val = sma9[sma9.Length - 1];
            var sma9_valFormat = Math.Round(sma9_val, 2);
            *//*Console.WriteLine("SMA9 : " + sma9_valFormat);*//*

            var sma30 = CalculateSMA(arrCloses1m, 30);
            var sma30_val = sma30[sma30.Length - 1];
            var sma30_valFormat = Math.Round(sma30_val, 2);
            *//*Console.WriteLine("SMA30 : " + sma30_valFormat);*//*

            if (sma9_valFormat > sma30_valFormat)
            {
                enter_points += 1;
            }*/

            //SUPERTREND1m**********************************************
            /*            var (supper777, check) = Supertrend.Calculate(arrHigh1m, arrLow1m, arrCloses1m, STPeriod, STFactor);
                        var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                        var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));
                        *//*            Console.WriteLine();
                                    Console.WriteLine("Supertrend: " + fastKer777_val1mSUPP_DOUBLE);
                                    Console.WriteLine("Supertrend Check: " + check);
                                    Console.WriteLine("Price: " + priceToDouble);*//*
                        if (check == false)
                        {
                            enter_points += 1;
                        }*/

            //SUPERTREND15m**********************************************
            /*            var (supper15m, check15m) = Supertrend.Calculate(arrHigh1m, arrLow1m, arrCloses1m, STPeriod, STFactor);
                        var fastKer15m_val1mSUPP = supper15m[supper15m.Length - 1];
                        var fastKer15m_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer15m_val1mSUPP, 2));
                        *//*            Console.WriteLine();
                                    Console.WriteLine("Supertrend: " + fastKer777_val1mSUPP_DOUBLE);
                                    Console.WriteLine("Supertrend Check: " + check);
                                    Console.WriteLine("Price: " + priceToDouble);*//*
                        if (check15m == false)
                        {
                            enter_points += 1;
                        }*/

            //RSI**********************************************
            decimal[] array = closePrices1m.ToArray();
            var RSI1m = CalculateRSI(RSI_Period);

            var rsi1m_val = RSI1m[RSI1m.Length - 1];
            var rsi_val_format1m = Math.Round(rsi1m_val, 2);

            if (rsi_val_format1m < 35)
            {
                enter_points += 1;
            }
            Console.WriteLine("RSI: " + rsi_val_format1m);


            /*//STOCH**********************************************
            var (fastKValues1m, slowKValues1m) = STOCHRSI(7, 7, 1);
            var fastK_val1m = fastKValues1m[fastKValues1m.Length - 1];
            var slowD_val1m = slowKValues1m[slowKValues1m.Length - 1];
            var fastK_val_format1m = Math.Round(fastK_val1m, 3);
            var slowD_val_format1m = Math.Round(slowD_val1m, 3);
            if (fastK_val_format1m > slowD_val_format1m && fastK_val_format1m < 50 && slowD_val_format1m < 50 && fastK_val_format1m > 5 && slowD_val_format1m > 5)
            {

                enter_points += 1;
                Console.WriteLine("STOCH1m: " + enter_points);
            }
            Console.WriteLine("STOCH fastKValues1m " + fastK_val_format1m + " Stoch slowKValues1m " + slowD_val_format1m);*/


            double[] prices = arrCloses1m;
            double[] sma;
            double[] upperBand;
            double[] lowerBand;

            CalculateBollingerBands(arrCloses1m, period, deviation, out sma, out upperBand, out lowerBand);
            Console.WriteLine("BollingerBands SMA: {1}, Upper Band: {2}, Lower Band: {3}", Math.Round(prices[prices.Length - 1], 2), Math.Round(sma[sma.Length - 1], 2), Math.Round(upperBand[upperBand.Length - 1], 2), Math.Round(lowerBand[lowerBand.Length - 1], 2));
            if (Math.Round(prices[prices.Length - 1], 2) <= Math.Round(lowerBand[lowerBand.Length - 1], 2))
            {
                enter_points += 1;
            }



            Console.WriteLine("Long: " + enter_points + "  " + POINTS_TO_ENTER);
            if (enter_points == POINTS_TO_ENTER)
            {
                isBuy = true;
                BuyOrder();

                Console.WriteLine("Покупка Long");
                Thread.Sleep(1000);

                LongMarketTakeProfit();
                Thread.Sleep(1000);

            }

            
            
        }

        public static void CheckShortStart()
        {

            int enter_points = 0;

            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();
            /*dynamic openPrices1m = new List<decimal>();*/

            var client1m = new BinanceClient();

                var result1m = client1m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m.Success)
                {
                    foreach (var candle in result1m.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);
                        /*openPrices1m.Add(candle.OpenPrice);*/

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m.Error.Message}");*/
                }
                double[] arrCloses1m = ConvertToDouble(closePrices1m);
                /*            double[] arrOpen1m = ConvertToDouble(openPrices1m);*/
                double[] arrHigh1m = ConvertToDouble(highPrices1m);
                double[] arrLow1m = ConvertToDouble(lowPrices1m);


            //MACD**********************************************
            /*var (macdLine1m, signalLine1m) = macd.MainMACD();
            var macd1m_val = macdLine1m[macdLine1m.Length - 1];
            var signalLine_val = signalLine1m[signalLine1m.Length - 1];
            var macd1m_val_format = Math.Round(macd1m_val, 3);
            var sign1m_val_format = Math.Round(signalLine_val, 3);

            *//*if (macd1m_val_format > sign1m_val_format && macd1m_val_format < 0 && sign1m_val_format < 0)*//*
            if (macd1m_val_format > sign1m_val_format)
            {
                enter_points += 1;
                *//*Console.WriteLine("Long MACD: {0}, Signal: {1}", macd1m_val_format, sign1m_val_format);*//*
            }*/


            //GetPrice*********************************************
            /*var resultPrice = client1m.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result;
            var price5m = Math.Round(resultPrice.Data.Price, 2);
            var priceToDouble = Convert.ToDouble(price5m);

            //SMA10*30*********************************************
            var sma9 = CalculateSMA(arrCloses1m, 9);
            var sma9_val = sma9[sma9.Length - 1];
            var sma9_valFormat = Math.Round(sma9_val, 2);
            *//*Console.WriteLine("SMA9 : " + sma9_valFormat);*//*

            var sma30 = CalculateSMA(arrCloses1m, 30);
            var sma30_val = sma30[sma30.Length - 1];
            var sma30_valFormat = Math.Round(sma30_val, 2);
            *//*Console.WriteLine("SMA30 : " + sma30_valFormat);*//*

            if (sma9_valFormat < sma30_valFormat)
            {
                enter_points += 1;
            }*/

            //SUPERTREND**********************************************
            /*            var (supper777, check) = Supertrend.Calculate(arrHigh1m, arrLow1m, arrCloses1m, STPeriod, STFactor);
                        var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                        var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));
                        *//*Console.WriteLine();
                        Console.WriteLine("Supertrend: " + fastKer777_val1mSUPP_DOUBLE);
                        Console.WriteLine("Price: " + priceToDouble);*//*
                        if (check == true)
                        {
                            enter_points += 1;
                        }*/

            /*            Console.WriteLine();
                        Console.WriteLine("SMA10: " + SMA1m10_val_format1m);
                        Console.WriteLine("SMA30: " + SMA1m30_val_format1m);*/



            //RSI**********************************************
            decimal[] array = closePrices1m.ToArray();
            var RSI1m = CalculateRSI(RSI_Period);

            var rsi1m_val = RSI1m[RSI1m.Length - 1];
            var rsi_val_format1m = Math.Round(rsi1m_val, 2);

            if (rsi_val_format1m > 65)
            {
                enter_points += 1;
            }
            Console.WriteLine("RSI: " + rsi_val_format1m);

            //STOCH**********************************************
            /*var (fastKValues1m, slowKValues1m) = STOCH(arrHigh1m, arrLow1m, arrCloses1m, fastK1m, slowD1m, smooth1m);
            var fastK_val1m = fastKValues1m[fastKValues1m.Length - 1];
            var slowD_val1m = slowKValues1m[slowKValues1m.Length - 1];
            var fastK_val_format1m = Math.Round(fastK_val1m, 3);
            var slowD_val_format1m = Math.Round(slowD_val1m, 3);
            if (fastK_val_format1m > slowD_val_format1m && fastK_val_format1m < 50 && slowD_val_format1m < 50 && fastK_val_format1m > 5 && slowD_val_format1m > 5)
            {

                enter_points += 1;
                *//*Console.WriteLine("STOCH1m: " + enter_points);*//*
            }*/
            /* Console.WriteLine("STOCH fastKValues1m " + fastK_val_format1m + " Stoch slowKValues1m " + slowD_val_format1m);*/

            

            double[] prices = arrCloses1m;
            double[] sma;
            double[] upperBand;
            double[] lowerBand;

            CalculateBollingerBands(arrCloses1m, period, deviation, out sma, out upperBand, out lowerBand);
            Console.WriteLine("BollingerBands  SMA: {1}, Upper Band: {2}, Lower Band: {3}", Math.Round(prices[prices.Length - 1], 2), Math.Round(sma[sma.Length - 1], 2), Math.Round(upperBand[upperBand.Length - 1], 2), Math.Round(lowerBand[lowerBand.Length - 1], 2));
            if (Math.Round(prices[prices.Length - 1], 2) >= Math.Round(upperBand[upperBand.Length - 1], 2))
            {
                enter_points += 1;
            }

            Console.WriteLine("Short: " + enter_points + "  " + POINTS_TO_ENTER);
            Console.WriteLine();


            if (enter_points == POINTS_TO_ENTER)
                {
                    isSell = true;
                    BuyOrderShort();

                    Thread.Sleep(1000);

                    ShortMarketTakeProfit();
                    Thread.Sleep(1000);

                }

            

        }


        public static void CheckPoints()
        {

            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();



            var client2 = new BinanceClient();
            var result1m2 = client2.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
            if (result1m2.Success)
            {
                foreach (var candle in result1m2.Data)
                {
                    closePrices1m.Add(candle.ClosePrice);
                    highPrices1m.Add(candle.HighPrice);
                    lowPrices1m.Add(candle.LowPrice);

                }
            }
            else
            {
                /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
            }

            double[] arrCloses1m = ConvertToDouble(closePrices1m);
            double[] arrHigh1m = ConvertToDouble(highPrices1m);
            double[] arrLow1m = ConvertToDouble(lowPrices1m);


            int enter_points = 0;

            //STOCH*********************************************
            /*                var (fastKValues1m2, slowKValues1m2) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, fastK1m, slowD1m, smooth1m);
                            var fastK_val = fastKValues1m2[fastKValues1m2.Length - 1];
                            var slowD_val = slowKValues1m2[slowKValues1m2.Length - 1];
                            var fastK_val_format = Math.Round(fastK_val, 3);
                            var slowD_val_format = Math.Round(slowD_val, 3);

                            if (fastK_val_format < slowD_val_format)
                            *//*if (fastK_val_format > slowD_val_format)*//*
                            {
                                // Быстрая линия стохастика выше медленной, вход
                                enter_points += 1;
                                *//*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*//*
                            }*/

            //SMA10*30*********************************************
            /*                decimal[] arrSMA = closePrices1m.ToArray();

                            var SMA1m10 = Indicators.Sma(arrSMA, 10);
                            var SMA1m10_val = SMA1m10.Ma[SMA1m10.Ma.Length - 1];
                            var SMA1m10_val_format1m = Math.Round(SMA1m10_val, 2);

                            var SMA1m30 = Indicators.Sma(arrSMA, 30);
                            var SMA1m30_val = SMA1m30.Ma[SMA1m30.Ma.Length - 1];
                            var SMA1m30_val_format1m = Math.Round(SMA1m30_val, 2);


                            if (SMA1m10_val_format1m < SMA1m30_val_format1m)
                            {
                                enter_points += 1;
                            }*/

            //SUPERTREND**********************************************
            /*                var (supper777, check) = Supertrend.Calculate(arrHigh1m2, arrLow1m2, arrCloses1m2, STPeriod, STFactor);
                            var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                            var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));

                            if (check == true)
                            {
                                enter_points += 1;
                            }*/

            //RSI**********************************************
            /*                decimal[] array = closePrices1m.ToArray();
                            var RSI1m = Indicators.Rsi(array, 8);

                            var rsi1m_val = RSI1m.Rsi[RSI1m.Rsi.Length - 1];
                            var rsi_val_format1m = Math.Round(rsi1m_val, 2);

                            if (rsi_val_format1m < 40 && rsi_val_format1m > 25 && rsi_val_format1m < 50)
                            {
                                enter_points += 1;
                            }*/
            /*Console.WriteLine("RSI: " + rsi_val_format1m);*/

            double[] prices = arrCloses1m;
            double[] sma;
            double[] upperBand;
            double[] lowerBand;

            CalculateBollingerBands(arrCloses1m, period, deviation, out sma, out upperBand, out lowerBand);
            Console.WriteLine("Price: {0}, SMA: {1}, Upper Band: {2}, Lower Band: {3}", Math.Round(prices[prices.Length - 1], 2), Math.Round(sma[sma.Length - 1], 2), Math.Round(upperBand[upperBand.Length - 1], 2), Math.Round(lowerBand[lowerBand.Length - 1], 2));
            if (Math.Round(prices[prices.Length - 1], 2) <= Math.Round(lowerBand[lowerBand.Length - 1], 2))
            {
                enter_points += 1;
            }

            if (enter_points == 1)
            {
                isLong = true;
                CheckLongStart();
                

            }
            else
            {

                /*_timer = new Timer(CheckPrice, null, 0, 400);*/

            }
            
        }

        public static void CheckPointsSUPER()
        {

            /*Thread.Sleep(checkPointTimeOut);*/

            Console.WriteLine("Ожидание снижения индикаторов...");
            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();
            /*            dynamic open1m = new List<decimal>();
                        dynamic volume1m = new List<decimal>();*/


            while (true)
            {

                var client2 = new BinanceClient();
                var result1m2 = client2.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m2.Success)
                {

                    foreach (var candle in result1m2.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);
                        /*                        open1m.Add(candle.OpenPrice);
                                                volume1m.Add(candle.Volume);*/


                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
                }



                double[] arrCloses1m2 = ConvertToDouble(closePrices1m);
                double[] arrHigh1m2 = ConvertToDouble(highPrices1m);
                double[] arrLow1m2 = ConvertToDouble(lowPrices1m);
                /*                double[] arrOpen1m = ConvertToDouble(open1m);
                                double[] arrVolume1m = ConvertToDouble(volume1m);*/

                double[] dataArr = new double[500];
                for (int i = 0; i < 500; i++)
                {

                    double high = arrHigh1m2[i];
                    double low = arrLow1m2[i];
                    double close = arrCloses1m2[i];
                    /*double open = arrOpen1m[i];
                    double volume = arrVolume1m[i];*/

                    // Вычисляем среднее значение OHLCV для каждой свечи
                    /*dataArr[i] = (open + high + low + close + volume) / 5;*/
                }

                // Присваиваем полученный массив значений dataArr массиву data


                int enter_points2 = 0;

                var resultPrice = client2.SpotApi.ExchangeData.GetPriceAsync(symbol).Result;
                var price5m = Math.Round(resultPrice.Data.Price, 2);
                var priceToDouble = Convert.ToDouble(price5m);


                /*                var supp = CalculateCCI(arrHigh1m2, arrLow1m2, arrCloses1m2, 20);
                                var fastK_val1mSUPP = supp[supp.Length - 1];
                                var fastK_val1mSUPP_DOUBLE = Convert.ToDouble(fastK_val1mSUPP);
                                Console.WriteLine();
                                Console.WriteLine("CCi: " + fastK_val1mSUPP_DOUBLE);
                                Console.WriteLine("Price: " + priceToDouble);

                                var atrr = ATR_array(arrHigh1m2, arrLow1m2, arrCloses1m2, 14);
                                var atrr_val1mSUPP = atrr[atrr.Length - 1];
                                var atrr2222_val1mSUPP = Math.Round(atrr_val1mSUPP, 2);
                                var atrr_val1mSUPP_DOUBLE = Convert.ToDouble(atrr2222_val1mSUPP);
                                Console.WriteLine("***************************");
                                Console.WriteLine("ATR Array: " + atrr_val1mSUPP_DOUBLE);

                                var atrr222 = ATR_(arrHigh1m2, arrLow1m2, arrCloses1m2, 14);
                                var atrr222_val1mSUPP = Math.Round(atrr222, 2);
                                var atrr222_val1mSUPP_DOUBLE = Convert.ToDouble(atrr222_val1mSUPP);
                                Console.WriteLine("ATR ___: " + atrr222_val1mSUPP_DOUBLE);
                                Console.WriteLine("***************************");*/


                var sma9 = CalculateSMA(arrCloses1m2, 9);
                var sma9_val = sma9[sma9.Length - 1];
                var sma9_valFormat = Math.Round(sma9_val, 2);
                /*Console.WriteLine("SMA9 : " + sma9_valFormat);*/

                var sma30 = CalculateSMA(arrCloses1m2, 30);
                var sma30_val = sma30[sma30.Length - 1];
                var sma30_valFormat = Math.Round(sma30_val, 2);
                /*Console.WriteLine("SMA30 : " + sma30_valFormat);*/

                /*
                                var supper = CalculateSuperTrend3(SuperTrendPeriod, SuperTrendMulti);
                                var fastKer_val1mSUPP = supper[supper.Length - 1];
                                var fastKer_val1mSUPP_DOUBLE = Convert.ToDouble(fastKer_val1mSUPP);
                                Console.WriteLine("Supertrend: " + fastKer_val1mSUPP_DOUBLE);
                                Console.WriteLine("Price: " + priceToDouble);*/

                var (supper777, check) = Supertrend.Calculate(arrHigh1m2, arrLow1m2, arrCloses1m2, STPeriod, STFactor);
                var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));
                /* Console.WriteLine();
                 Console.WriteLine("Supertrend ATR777: " + fastKer777_val1mSUPP_DOUBLE);*/




                /*                double[] high11 = arrHigh1m2;
                                double[] low11 = arrLow1m2;
                                int period11 = 20;

                                var (upper11, lower11, middle11) = DonchianChannels.Calculate(high11, low11, period11);
                                var upper11_val = upper11[upper11.Length - 1];
                                var lower11_val = lower11[lower11.Length - 1];
                                var middle11_val = middle11[middle11.Length - 1];
                                Console.WriteLine($"Upper: {upper11_val}, Lower: {lower11_val}, Middle: {middle11_val}");
                */







                /*                    Console.WriteLine("Supertrend: " + fastK_val1mSUPP);
                                    Console.WriteLine("Price: " + priceToDouble);
                                    Console.WriteLine();*/
                /*                    var supTess = Indicators.Atr(arrHigh1mDEC, arrLow1mDEC, arrCloses1mDEC, 10);
                                    var supr_val = supTess.Atr[supTess.Atr.Length - 1];
                                    Console.WriteLine("Supertrend TEST: " + supr_val);*/

                /*                var (fastKValues1m22, slowKValues1m22) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, 7, 3, 3);
                                var fastK_val22 = fastKValues1m22[fastKValues1m22.Length - 1];
                                var slowD_val22 = slowKValues1m22[slowKValues1m22.Length - 1];
                                var fastK_val_format22 = Math.Round(fastK_val22, 3);
                                var slowD_val_format22 = Math.Round(slowD_val22, 3);

                                if (fastK_val_format22 < slowD_val_format22)
                                {
                                    // Быстрая линия стохастика выше медленной, вход
                                    enter_points2 += 1;
                                    *//*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*//*
                                }*/


                /*                var (fastKValues1m2, slowKValues1m2) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, fastK1m, slowD1m, smooth1m);
                                var fastK_val = fastKValues1m2[fastKValues1m2.Length - 1];
                                var slowD_val = slowKValues1m2[slowKValues1m2.Length - 1];
                                var fastK_val_format = Math.Round(fastK_val, 3);
                                var slowD_val_format = Math.Round(slowD_val, 3);

                                if (fastK_val_format < slowD_val_format && fastK_val_format < 30 && slowD_val_format < 30)
                                *//*if (fastK_val_format > slowD_val_format)*//*
                                {
                                    // Быстрая линия стохастика выше медленной, вход
                                    enter_points2 += 1;
                                    *//*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*//*
                                }*/




                /*                var (fast2, slow2) = STOCHRSI(arrCloses1m2, STOCHRSI_period, 3, 3);

                                if (fast2[fast2.Length - 1] > slow2[slow2.Length - 1])
                                {
                                    // Быстрая линия STOCHRSI выше медленной, вход
                                    enter_points2 += 1;
                                    //*Console.WriteLine("Сработал STOCHRSI-1m, текущее значение " + enter_points2)*//*
                                    ;
                                }*/

                if (enter_points2 == 1)
                {
                    Start();
                    break;
                }
                else
                {
                    continue;
                    /*_timer = new Timer(CheckPrice, null, 0, 400);*/

                }
            }
        }

        public static void CheckPointsShort()
        {

            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();


                var client2 = new BinanceClient();
                var result1m2 = client2.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m2.Success)
                {
                    foreach (var candle in result1m2.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
                }

                double[] arrCloses1m = ConvertToDouble(closePrices1m);
                double[] arrHigh1m = ConvertToDouble(highPrices1m);
                double[] arrLow1m = ConvertToDouble(lowPrices1m);


                int enter_points = 0;

                //STOCH*********************************************
                /*                var (fastKValues1m2, slowKValues1m2) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, fastK1m, slowD1m, smooth1m);
                                var fastK_val = fastKValues1m2[fastKValues1m2.Length - 1];
                                var slowD_val = slowKValues1m2[slowKValues1m2.Length - 1];
                                var fastK_val_format = Math.Round(fastK_val, 3);
                                var slowD_val_format = Math.Round(slowD_val, 3);

                                if (fastK_val_format < slowD_val_format)
                                *//*if (fastK_val_format > slowD_val_format)*//*
                                {
                                    // Быстрая линия стохастика выше медленной, вход
                                    enter_points += 1;
                                    *//*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*//*
                                }*/

                //SMA10*30*********************************************
                /*                decimal[] arrSMA = closePrices1m.ToArray();

                                var SMA1m10 = Indicators.Sma(arrSMA, 10);
                                var SMA1m10_val = SMA1m10.Ma[SMA1m10.Ma.Length - 1];
                                var SMA1m10_val_format1m = Math.Round(SMA1m10_val, 2);

                                var SMA1m30 = Indicators.Sma(arrSMA, 30);
                                var SMA1m30_val = SMA1m30.Ma[SMA1m30.Ma.Length - 1];
                                var SMA1m30_val_format1m = Math.Round(SMA1m30_val, 2);


                                if (SMA1m10_val_format1m > SMA1m30_val_format1m)
                                {
                                    enter_points += 1;
                                }*/

/*                var (supper777, check) = Supertrend.Calculate(arrHigh1m2, arrLow1m2, arrCloses1m2, STPeriod, STFactor);
                var fastKer777_val1mSUPP = supper777[supper777.Length - 1];
                var fastKer777_val1mSUPP_DOUBLE = Convert.ToDouble(Math.Round(fastKer777_val1mSUPP, 2));

                if (check == false)
                {
                    enter_points += 1;
                }*/


            //RSI**********************************************
            /*                decimal[] array = closePrices1m.ToArray();
                            var RSI1m = Indicators.Rsi(array, 8);

                            var rsi1m_val = RSI1m.Rsi[RSI1m.Rsi.Length - 1];
                            var rsi_val_format1m = Math.Round(rsi1m_val, 2);

                            if (rsi_val_format1m < 40 && rsi_val_format1m > 25 && rsi_val_format1m < 50)
                            {
                                enter_points += 1;
                            }*/
            /*Console.WriteLine("RSI: " + rsi_val_format1m);*/

            double[] prices = arrCloses1m;
            double[] sma;
            double[] upperBand;
            double[] lowerBand;

            CalculateBollingerBands(prices, period, deviation, out sma, out upperBand, out lowerBand);
            Console.WriteLine("Price: {0}, SMA: {1}, Upper Band: {2}, Lower Band: {3}", Math.Round(prices[prices.Length - 1], 2), Math.Round(sma[sma.Length - 1], 2), Math.Round(upperBand[upperBand.Length - 1], 2), Math.Round(lowerBand[lowerBand.Length - 1], 2));
            if (Math.Round(prices[prices.Length - 1], 2) >= Math.Round(upperBand[upperBand.Length - 1], 2))
            {
                
                enter_points += 1;
            }

            if (enter_points == 1)
            {
                isShort = true;
                CheckShortStart();

            }

            
        }

        public static void CheckPointsStopLoss()
        {
            Thread.Sleep(6000);
            Console.WriteLine("Ожидание снижения индикаторов...");
            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();

            while (true)
            {

                var client2 = new BinanceClient();
                var result1m2 = client2.SpotApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m2.Success)
                {
                    foreach (var candle in result1m2.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
                }

                double[] arrCloses1m2 = ConvertToDouble(closePrices1m);
                double[] arrHigh1m2 = ConvertToDouble(highPrices1m);
                double[] arrLow1m2 = ConvertToDouble(lowPrices1m);


                int enter_points = 0;

                //STOCH*********************************************
                /*                var (fastKValues1m2, slowKValues1m2) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, fastK1m, slowD1m, smooth1m);
                                var fastK_val = fastKValues1m2[fastKValues1m2.Length - 1];
                                var slowD_val = slowKValues1m2[slowKValues1m2.Length - 1];
                                var fastK_val_format = Math.Round(fastK_val, 3);
                                var slowD_val_format = Math.Round(slowD_val, 3);

                                if (fastK_val_format < slowD_val_format)
                                *//*if (fastK_val_format > slowD_val_format)*//*
                                {
                                    // Быстрая линия стохастика выше медленной, вход
                                    enter_points += 1;
                                    *//*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*//*
                                }*/

                //SMA10*30*********************************************
                decimal[] arrSMA = closePrices1m.ToArray();

                var SMA1m10 = Indicators.Sma(arrSMA, 10);
                var SMA1m10_val = SMA1m10.Ma[SMA1m10.Ma.Length - 1];
                var SMA1m10_val_format1m = Math.Round(SMA1m10_val, 2);

                var SMA1m30 = Indicators.Sma(arrSMA, 30);
                var SMA1m30_val = SMA1m30.Ma[SMA1m30.Ma.Length - 1];
                var SMA1m30_val_format1m = Math.Round(SMA1m30_val, 2);

                /*                Console.WriteLine();
                                Console.WriteLine("SMA10: " + SMA1m10_val_format1m);
                                Console.WriteLine("SMA30: " + SMA1m30_val_format1m);*/


                if (SMA1m10_val_format1m < SMA1m30_val_format1m)
                {
                    enter_points += 1;
                }


                //RSI**********************************************
                /*                decimal[] array = closePrices1m.ToArray();
                                var RSI1m = Indicators.Rsi(array, 8);

                                var rsi1m_val = RSI1m.Rsi[RSI1m.Rsi.Length - 1];
                                var rsi_val_format1m = Math.Round(rsi1m_val, 2);

                                if (rsi_val_format1m < 40 && rsi_val_format1m > 25 && rsi_val_format1m < 50)
                                {
                                    enter_points += 1;
                                }*/
                /*Console.WriteLine("RSI: " + rsi_val_format1m);*/

                if (enter_points == 1)
                {
                    Start();
                    break;
                }
                else
                {
                    continue;
                    /*_timer = new Timer(CheckPrice, null, 0, 400);*/

                }
            }
        }

        public static void CheckPointsStart()
        {

            Console.WriteLine("Ожидание индикаторов...");
            dynamic closePrices1m = new List<decimal>();
            dynamic highPrices1m = new List<decimal>();
            dynamic lowPrices1m = new List<decimal>();

            while (true)
            {

                var client2 = new BinanceClient();
                var result1m2 = client2.SpotApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m2.Success)
                {
                    foreach (var candle in result1m2.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);
                        highPrices1m.Add(candle.HighPrice);
                        lowPrices1m.Add(candle.LowPrice);

                    }
                }
                else
                {
                    /*Console.WriteLine($"Ошибка: {result1m2.Error.Message}");*/
                }

                double[] arrCloses1m2 = ConvertToDouble(closePrices1m);
                double[] arrHigh1m2 = ConvertToDouble(highPrices1m);
                double[] arrLow1m2 = ConvertToDouble(lowPrices1m);


                int enter_points2 = 0;

                var (fastKValues1m2, slowKValues1m2) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, fastK1m, slowD1m, smooth1m);
                var fastK_val = fastKValues1m2[fastKValues1m2.Length - 1];
                var slowD_val = slowKValues1m2[slowKValues1m2.Length - 1];
                var fastK_val_format = Math.Round(fastK_val, 3);
                var slowD_val_format = Math.Round(slowD_val, 3);

                if (fastK_val_format < slowD_val_format)
                {
                    // Быстрая линия стохастика выше медленной, вход
                    enter_points2 += 1;
                    /*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*/
                }


                /*                var (fastKValues1m22, slowKValues1m22) = STOCH(arrHigh1m2, arrLow1m2, arrCloses1m2, 7, 3, 3);
                                var fastK_val22 = fastKValues1m22[fastKValues1m22.Length - 1];
                                var slowD_val22 = slowKValues1m22[slowKValues1m22.Length - 1];
                                var fastK_val_format22 = Math.Round(fastK_val22, 3);
                                var slowD_val_format22 = Math.Round(slowD_val22, 3);

                                if (fastK_val_format22 < slowD_val_format22)
                                {
                                    // Быстрая линия стохастика выше медленной, вход
                                    enter_points2 += 1;
                                    *//*Console.WriteLine("Сработал STOCH-1m, текущее значение " + enter_points2);*//*
                                }*/




                /*                var (fast2, slow2) = STOCHRSI(arrCloses1m2, STOCHRSI_period, 3, 3);

                                if (fast2[fast2.Length - 1] > slow2[slow2.Length - 1])
                                {
                                    // Быстрая линия STOCHRSI выше медленной, вход
                                    enter_points2 += 1;
                                    //*Console.WriteLine("Сработал STOCHRSI-1m, текущее значение " + enter_points2)*//*
                                    ;
                                }*/

                if (enter_points2 == 1)
                {
                    Start();
                    break;
                }
                else
                {
                    continue;
                    /*_timer = new Timer(CheckPrice, null, 0, 400);*/

                }
            }
        }




        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        //INDICATORS AND SOFT METODS////////////////////////////////////////////////////////////////////////////
        private class FibonacciLevels
        {
            public decimal Level1 { get; set; }
            public decimal Level2 { get; set; }
            public decimal Level3 { get; set; }
            public decimal Level4 { get; set; }
            public decimal Level5 { get; set; }
            public decimal Level6 { get; set; }
        }

        private static FibonacciLevels GetFibonacciLevels(decimal currentPrice, decimal low, decimal high)
        {
            var range = high - low;
            return new FibonacciLevels
            {
                Level1 = high - range * 0.236m,
                Level2 = high - range * 0.382m,
                Level3 = high - range * 0.5m,
                Level4 = high - range * 0.618m,
                Level5 = high - range * 0.786m,
                Level6 = low + range * 1.618m
            };

        }

        public static (double[], double[]) STOCH(double[] high, double[] low, double[] close, int fastK_Period, int slowK_Period, int slowD_Period)
        {
            int len = high.Length;
            double[] fastK = new double[len];
            double[] slowK = new double[len];
            double[] slowD = new double[len];

            for (int i = 0; i < len; i++)
            {
                double highestHigh = high.Skip(i - fastK_Period + 1).Take(fastK_Period).Max();
                double lowestLow = low.Skip(i - fastK_Period + 1).Take(fastK_Period).Min();
                double currentClose = close[i];

                fastK[i] = (currentClose - lowestLow) / (highestHigh - lowestLow) * 100;
            }

            for (int i = 0; i < len; i++)
            {
                slowK[i] = fastK.Skip(i - slowK_Period + 1).Take(slowK_Period).Average();
            }

            for (int i = 0; i < len; i++)
            {
                slowD[i] = slowK.Skip(i - slowD_Period + 1).Take(slowD_Period).Average();
            }

            return (slowK, slowD);
        }

        public static (double[], double[]) STOCHRSI(int period, int fastk_period, int fastd_period)
        {
            var rsi = CalculateRSI(period);
            return STOCH(rsi, rsi, rsi, 3, fastk_period, fastd_period);
        }


        public class MACD
        {
            public (double[], double[]) MainMACD()
            {
                var client111m = new BinanceClient();
                dynamic closePrices1m = new List<decimal>();


                var result1m = client111m.SpotApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
                if (result1m.Success)
                {
                    foreach (var candle in result1m.Data)
                    {
                        closePrices1m.Add(candle.ClosePrice);

                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка: {result1m.Error.Message}");
                }
                double[] closingPrices = ConvertToDouble(closePrices1m);

                /*double[] closingPrices = new double[500]; // массив с ценами закрытия*/



                // заполнение массива closingPrices данными
                // ...

                int slowEMA = 26; // количество свечей для расчета медленной EMA
                int fastEMA = 12; // количество свечей для расчета быстрой EMA
                int signalEMA = 9; // количество свечей для расчета сигнальной линии

                double[] emaSlow = EMA(closingPrices, slowEMA); // расчет медленной EMA
                double[] emaFast = EMA(closingPrices, fastEMA); // расчет быстрой EMA

                double[] macdLine = new double[500]; // массив значений MACD-линии
                for (int i = 0; i < 500; i++)
                {
                    macdLine[i] = emaFast[i] - emaSlow[i]; // вычисление значения MACD-линии
                }

                double[] signalLine = EMA(macdLine, signalEMA); // расчет сигнальной линии


                // вывод значений MACD-линии и сигнальной линии

                return (macdLine, signalLine);

            }

            // функция для расчета EMA
            static double[] EMA(double[] closingPrices, int period)
            {
                double[] ema = new double[closingPrices.Length];

                double k = 2.0 / (period + 1);

                ema[0] = closingPrices[0];

                for (int i = 1; i < closingPrices.Length; i++)
                {
                    ema[i] = closingPrices[i] * k + ema[i - 1] * (1 - k);
                }

                return ema;
            }
        }


        public static double[] EMA(double[] data, int period)
        {
            double[] ema = new double[data.Length];
            double multiplier = 2.0 / (period + 1);

            // Calculate initial SMA
            double sma = 0;
            for (int i = 0; i < period; i++)
            {
                sma += data[i];
            }
            sma /= period;
            ema[period - 1] = sma;

            // Calculate subsequent EMAs
            for (int i = period; i < data.Length; i++)
            {
                ema[i] = (data[i] - ema[i - 1]) * multiplier + ema[i - 1];
            }

            return ema;
        }


        public static double[] CalculateRSI(int period)
        {
            var client111m = new BinanceClient();
            dynamic closePrices1m = new List<decimal>();


            var result1m = client111m.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute).Result;
            if (result1m.Success)
            {
                foreach (var candle in result1m.Data)
                {
                    closePrices1m.Add(candle.ClosePrice);

                }
            }
            else
            {
                Console.WriteLine($"Ошибка: {result1m.Error.Message}");
            }
            double[] closingPrices = ConvertToDouble(closePrices1m);
            double[] prices = closingPrices;


            double[] gains = new double[prices.Length];
            double[] losses = new double[prices.Length];

            for (int i = 1; i < prices.Length; i++)
            {
                double diff = prices[i] - prices[i - 1];

                if (diff > 0)
                {
                    gains[i] = diff;
                    losses[i] = 0;
                }
                else
                {
                    gains[i] = 0;
                    losses[i] = Math.Abs(diff);
                }
            }

            double[] avgGains = new double[prices.Length];
            double[] avgLosses = new double[prices.Length];

            avgGains[period] = gains.Take(period).Average();
            avgLosses[period] = losses.Take(period).Average();

            for (int i = period + 1; i < prices.Length; i++)
            {
                avgGains[i] = ((avgGains[i - 1] * (period - 1)) + gains[i]) / period;
                avgLosses[i] = ((avgLosses[i - 1] * (period - 1)) + losses[i]) / period;
            }

            double[] rs = new double[prices.Length];

            for (int i = period; i < prices.Length; i++)
            {
                if (avgLosses[i] == 0)
                {
                    rs[i] = 100;
                }
                else
                {
                    rs[i] = (avgGains[i] / avgLosses[i]);
                }
            }

            double[] rsi = new double[prices.Length];

            for (int i = period; i < prices.Length; i++)
            {
                rsi[i] = 100 - (100 / (1 + rs[i]));
            }

            return rsi;
        }


        public static double[] CalculateCCI(double[] high, double[] low, double[] close, int period)
        {
            double[] typicalPrice = new double[high.Length];
            for (int i = 0; i < high.Length; i++)
            {
                typicalPrice[i] = (high[i] + low[i] + close[i]) / 3;
            }

            double[] cci = new double[typicalPrice.Length];
            for (int i = period - 1; i < typicalPrice.Length; i++)
            {
                double sum = 0;
                for (int j = i; j > i - period; j--)
                {
                    sum += typicalPrice[j];
                }
                double sma = sum / period;
                double meanDeviation = 0;
                for (int j = i; j > i - period; j--)
                {
                    meanDeviation += Math.Abs(typicalPrice[j] - sma);
                }
                double cciValue = (typicalPrice[i] - sma) / (0.015 * meanDeviation / period);
                cci[i] = cciValue;
            }
            return cci;
        }

        public static double ATR_(double[] high, double[] low, double[] close, int period)
        {
            double sum = 0;
            for (int i = period; i < high.Length; i++)
            {
                sum += Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));
            }

            double atr = sum / period;

            for (int i = period; i < high.Length; i++)
            {
                double tr = Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));
                atr = ((period - 1) * atr + tr) / period;
            }

            return atr;
        }

        public static double[] CalculateTREND(double[] high, double[] low, double[] close, int period, double multiplier)
        {
            int length = close.Length;
            double[] trendUp = new double[length];
            double[] trendDown = new double[length];
            double[] atr = ATR_array(high, low, close, period);
            double[] supertrend = new double[length];

            trendUp[0] = (high[0] + low[0]) / 2;
            trendDown[0] = (high[0] + low[0]) / 2;
            supertrend[0] = -1;

            for (int i = 1; i < length; i++)
            {
                double basicUp = (high[i] + low[i]) / 2 + multiplier * atr[i];
                double basicDown = (high[i] + low[i]) / 2 - multiplier * atr[i];

                trendUp[i] = (basicUp < trendUp[i - 1] || close[i - 1] > trendUp[i - 1]) ? basicUp : trendUp[i - 1];
                trendDown[i] = (basicDown > trendDown[i - 1] || close[i - 1] < trendDown[i - 1]) ? basicDown : trendDown[i - 1];

                supertrend[i] = supertrend[i - 1];

                if (supertrend[i - 1] == -1 && close[i] > trendDown[i - 1])
                {
                    supertrend[i] = trendUp[i];
                }
                else if (supertrend[i - 1] == 1 && close[i] < trendUp[i - 1])
                {
                    supertrend[i] = trendDown[i];
                }
            }

            return supertrend;
        }


        public class Supertrend
        {
            public static (double[], bool) Calculate(double[] high, double[] low, double[] close, int period, double multiplier)
            {
                int length = close.Length;
                double[] trendUp = new double[length];
                double[] trendDown = new double[length];
                double[] atr = ATR_array(high, low, close, period);

                trendUp[0] = (high[0] + low[0]) / 2;
                trendDown[0] = (high[0] + low[0]) / 2;

                for (int i = 1; i < length; i++)
                {
                    double basicUp = (high[i] + low[i]) / 2 + multiplier * atr[i];
                    double basicDown = (high[i] + low[i]) / 2 - multiplier * atr[i];

                    trendUp[i] = (basicUp < trendUp[i - 1] || close[i - 1] > trendUp[i - 1]) ? basicUp : trendUp[i - 1];
                    trendDown[i] = (basicDown > trendDown[i - 1] || close[i - 1] < trendDown[i - 1]) ? basicDown : trendDown[i - 1];
                }

                double[] supertrend = new double[length];
                bool uptrend = true;

                for (int i = 0; i < length; i++)
                {
                    if (i == 0)
                    {
                        supertrend[i] = (uptrend) ? trendUp[i] : trendDown[i];
                    }
                    else
                    {
                        double prevClose = close[i - 1];

                        if (uptrend)
                        {
                            if (prevClose > trendUp[i - 1])
                            {
                                uptrend = false;
                                supertrend[i] = trendDown[i];

                            }
                            else
                            {
                                supertrend[i] = trendUp[i];

                            }
                        }
                        else
                        {
                            if (prevClose < trendDown[i - 1])
                            {
                                uptrend = true;
                                supertrend[i] = trendUp[i];

                            }
                            else
                            {
                                supertrend[i] = trendDown[i];

                            }
                        }
                    }
                }

                return (supertrend, uptrend);
            }





            public static double[] ATR(double[] high, double[] low, double[] close, int period)
            {
                int length = close.Length;
                double[] atr = new double[length];
                double sum = 0;

                for (int i = 0; i < period; i++)
                {
                    double range = high[i] - low[i];
                    sum += range;
                }

                double average = sum / period;
                atr[period - 1] = average;

                for (int i = period; i < length; i++)
                {
                    double range = Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));
                    atr[i] = ((period - 1) * atr[i - 1] + range) / period;
                }

                return atr;
            }
        }

        public static double[] CalculateSMA(double[] input, int period)
        {
            int length = input.Length;
            double[] output = new double[length];

            for (int i = period; i < length; i++)
            {
                double sum = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    sum += input[j];
                }
                output[i] = sum / period;
            }

            return output;
        }

        public static double[] ATR_array(double[] high, double[] low, double[] close, int period)
        {
            double[] atrValues = new double[close.Length];
            double sum = 0;

            for (int i = 0; i < period; i++)
            {
                sum += Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i]), Math.Abs(low[i] - close[i])));
                atrValues[i] = 0;
            }

            double atr = sum / period;

            for (int i = period; i < close.Length; i++)
            {
                double tr = Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));
                atr = ((period - 1) * atr + tr) / period;
                atrValues[i] = atr;
            }

            return atrValues;
        }


        public static (double[], double[], double[]) DonchianChannel(double[] data, int period, out double[] upper, out double[] lower, out double[] middle)
        {
            upper = new double[data.Length];
            lower = new double[data.Length];
            middle = new double[data.Length];

            for (int i = period - 1; i < data.Length; i++)
            {
                double highest = double.MinValue;
                double lowest = double.MaxValue;

                for (int j = i - period + 1; j <= i; j++)
                {
                    if (data[j] > highest)
                    {
                        highest = data[j];
                    }
                    if (data[j] < lowest)
                    {
                        lowest = data[j];
                    }
                }

                upper[i] = highest;
                lower[i] = lowest;
                middle[i] = (highest + lowest) / 2;
            }
            return (upper, lower, middle);
        }


        public static class DonchianChannels
        {
            public static (double[] upper, double[] lower, double[] middle) Calculate(double[] high, double[] low, int period)
            {
                double[] upper = new double[high.Length];
                double[] lower = new double[low.Length];
                double[] middle = new double[high.Length];

                for (int i = period; i < high.Length; i++)
                {
                    double[] range = high[(i - period + 1)..(i + 1)];
                    upper[i] = range.Max();
                    range = low[(i - period + 1)..(i + 1)];
                    lower[i] = range.Min();
                    middle[i] = (upper[i] + lower[i]) / 2;
                }

                return (upper, lower, middle);
            }
        }

        public static void CalculateBollingerBands(double[] prices, int period, double deviation, out double[] sma, out double[] upperBand, out double[] lowerBand)
        {
            sma = new double[prices.Length - period + 1];
            upperBand = new double[prices.Length - period + 1];
            lowerBand = new double[prices.Length - period + 1];

            for (int i = period - 1; i < prices.Length; i++)
            {
                double sum = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    sum += prices[j];
                }
                double movingAverage = sum / period;
                sma[i - period + 1] = movingAverage;

                double sumOfSquares = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    double difference = prices[j] - movingAverage;
                    sumOfSquares += difference * difference;
                }
                double standardDeviation = Math.Sqrt(sumOfSquares / period);

                double upper = movingAverage + deviation * standardDeviation;
                double lower = movingAverage - deviation * standardDeviation;
                upperBand[i - period + 1] = upper;
                lowerBand[i - period + 1] = lower;
            }
        }


        public class IchimokuCloudIndicator
        {
            private double[] highValues;
            private double[] lowValues;
            private double[] tenkanSen;
            private double[] kijunSen;
            private double[] senkouSpanA;
            private double[] senkouSpanB;

            public IchimokuCloudIndicator(double[] highValues, double[] lowValues, int tenkanSenPeriod = 9, int kijunSenPeriod = 26, int senkouSpanBPeriod = 52)
            {
                this.highValues = highValues;
                this.lowValues = lowValues;

                // Calculate Tenkan-sen
                double[] tenkanSenHigh = new double[highValues.Length];
                double[] tenkanSenLow = new double[lowValues.Length];
                for (int i = 0; i < highValues.Length; i++)
                {
                    if (i >= tenkanSenPeriod - 1)
                    {
                        double highestHigh = highValues.Skip(i - tenkanSenPeriod + 1).Take(tenkanSenPeriod).Max();
                        double lowestLow = lowValues.Skip(i - tenkanSenPeriod + 1).Take(tenkanSenPeriod).Min();
                        tenkanSenHigh[i] = highestHigh;
                        tenkanSenLow[i] = lowestLow;
                    }
                }
                tenkanSen = tenkanSenHigh.Zip(tenkanSenLow, (h, l) => (h + l) / 2).ToArray();

                // Calculate Kijun-sen
                double[] kijunSenHigh = new double[highValues.Length];
                double[] kijunSenLow = new double[lowValues.Length];
                for (int i = 0; i < highValues.Length; i++)
                {
                    if (i >= kijunSenPeriod - 1)
                    {
                        double highestHigh = highValues.Skip(i - kijunSenPeriod + 1).Take(kijunSenPeriod).Max();
                        double lowestLow = lowValues.Skip(i - kijunSenPeriod + 1).Take(kijunSenPeriod).Min();
                        kijunSenHigh[i] = highestHigh;
                        kijunSenLow[i] = lowestLow;
                    }
                }
                kijunSen = kijunSenHigh.Zip(kijunSenLow, (h, l) => (h + l) / 2).ToArray();

                // Calculate Senkou Span A
                senkouSpanA = new double[highValues.Length];
                for (int i = 0; i < highValues.Length; i++)
                {
                    int senkouSpanAIndex = i;
                    if (senkouSpanAIndex >= kijunSenPeriod && senkouSpanAIndex < senkouSpanA.Length)
                    {
                        senkouSpanA[senkouSpanAIndex] = (tenkanSen[i] + kijunSen[i]) / 2;
                    }
                }

                // Calculate Senkou Span B
                double[] senkouSpanBHigh = new double[highValues.Length];
                double[] senkouSpanBLow = new double[lowValues.Length];
                for (int i = 0; i < highValues.Length; i++)
                {
                    if (i >= senkouSpanBPeriod - 1)
                    {
                        double highestHigh = highValues.Skip(i - senkouSpanBPeriod + 1).Take(senkouSpanBPeriod).Max();
                        double lowestLow = lowValues.Skip(i - senkouSpanBPeriod + 1).Take(senkouSpanBPeriod).Min();
                        senkouSpanBHigh[i] = highestHigh;
                        senkouSpanBLow[i] = lowestLow;
                    }
                }
                senkouSpanB = senkouSpanBHigh.Zip(senkouSpanBLow, (h, l) => (h + l) / 2).ToArray();
            }

            public double[] GetTenkanSen()
            {
                return tenkanSen;
            }

            public double[] GetKijunSen()
            {
                return kijunSen;
            }

            public double[] GetSenkouSpanA()
            {
                return senkouSpanA;
            }

            public double[] GetSenkouSpanB()
            {
                return senkouSpanB;
            }

            public bool IsBullish(int index)
            {
                return senkouSpanA[index] > senkouSpanB[index] && highValues[index] > senkouSpanA[index] && lowValues[index] > senkouSpanB[index];
            }

            public bool IsBearish(int index)
            {
                return senkouSpanA[index] < senkouSpanB[index] && highValues[index] < senkouSpanA[index] && lowValues[index] < senkouSpanB[index];
            }
        }


        static string GetSignature(string queryString)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(API_SECRET)))
            {
                byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                return BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
            }
        }

        static double[] ConvertToDouble(List<decimal> list)
        {
            double[] result = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (double)list[i];
            }
            return result;
        }


    }
}

