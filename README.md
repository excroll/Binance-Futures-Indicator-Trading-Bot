# Binance-Future-Indicators-Trading-Bot

![Image alt](https://github.com/eXcroll/Binance-Future-Indicators-Trading-Bot/blob/master/apii.png)

Paste your API into the appropriate fields in the code, use CTRL+F to find all the lines where you need to enter API keys.<br />
The bot trades on 2 indicators (can be changed in the code) <b>RSI</b> and <b>Bollinger</b> bands by changing the variable <b>POINTS_TO_ENTER = 2 </b> <br />
You can set points to enter the transaction, now they are +1 on RSI and + 1 on Bollinger, the same logic for sale. The bot is not 100% finalized, but it sometimes showed me profits and more often losses, it needs to be tested and configured, which I don't want.<br />
It would be even better to attach a database to it, but this is optional.<br />


<b> <font color="red" size="24">Change the leverage on BINANCE Futures to x1 !!!!</font> </b>

<b>Attention! This bot is not finalized and not configured until the final, so be careful when trading on the stock exchange, I am not responsible for your possible financial losses. </b><br />

**If you have a delay of more than 1000 milliseconds, then set Time_Sync_Tool, otherwise the Binance API will reject your connection**


This project was created in Visual Studio 2022, DotNet-6.0<br />
Press **DEBUG** to run the script<br />
**Indicators data was tested on Binance spot Tradingview terminal**<br />
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
