# Binance-Futures-Indicators-Trading-Bot

**Paste your API_KEY, API_SECRET to the corresponding variables.<br />**

The bot trades on 2 indicators (can be changed in the code) <b>RSI</b> and <b>BollingerBands</b> by changing the variable <b>POINTS_TO_ENTER = 2 </b> (uncomment lines of code with other indicators). You can set points to enter the transaction and more indicators, this strategy when the RSI value falls below 35 (oversold zone) now they are +1 on **RSI** and when the price crosses the lower Bollinger + 1 on **BollingerBands**, the same logic for sale (when the price crosses the Bollinger SMA  **enter_points +1** realizes the profit and completes the Short/Long Market transaction). </br> 

Also implements a stop loss function and trailng stop, the code can be changed so that the sale takes place by any percentage (for example: the price increased by 2% - the sale of an asset) the percentage can of course be changed.</br>
Bot is not 100% finalized, but it sometimes showed me profits, sometimes losses, it needs to be tested and configured, which I don't want do.<br />
It would be even better to attach a database to it, but this is optional.<br />


<b> <font color="red" size="24">Change the leverage on BINANCE Futures to x1 !!!!</font> </b>

<b>Attention! This bot is not finalized and not configured until the final, so be careful when trading on the stock exchange, I am not responsible for your possible financial losses. </b><br />

If you have a delay of more than 1000 milliseconds, then set **Time_Sync_Tool**, otherwise the Binance API will reject your connection. Or download https://gunbot.shop/time-sync-tool-for-gunbot/


This project was created in Visual Studio 2022, DotNet-6.0<br />
Press **DEBUG** to run the script<br />
**Indicators data was tested on Binance Tradingview terminal**<br />
The libraries used where:<br />
**Binance.Net-8.0.0 -JKorf, CryptoExchange.Net-5.0.0 -JKorf, Newtonsoft.Json 13.0.2, NLog 5.1.2, TechnicalAnalysis.Net 2.0.0**<br />
**Install** if needed the necessary dependencies before testing from **NuGet**<br />

Indicators are implemented in this bot:
- [x] SUPERTREND
- [x] MACD
- [x] SMA
- [x] RSI
- [x] STOCH
- [x] BollingerBands
- [x] DonchianChannels
- [x] IchimokuCloud
- [x] AwesomeOscilator(Bill Williams)
- [ ] and more...

When you get rich with the help of this bot and want to thank me, then I will be very happy with any donation :)
XRP: rJn2zAPdFA193sixJwuFixRkYDUtx3apQh 
XRP Tag (memo): 500425854
LTC: LZS5oKRVDMwRwXnvwZvXHLiGnn9WkzZiwk
