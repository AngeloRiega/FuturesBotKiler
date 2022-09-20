using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;
using Newtonsoft.Json;
using Binance;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Enums;
using System.Threading;
using FuturesBotKiler.Models;

namespace FuturesBotKiler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BinanceController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "Esperando alertas.";
        }

        [HttpPost]
        public async Task<string> PostAsync()
        {
            string content = string.Empty;

            using (Stream receiveStream = Request.Body)
            using (StreamReader readStream = new StreamReader(receiveStream))
            { content = await readStream.ReadToEndAsync(); }

            TradingViewAlert? tradingViewAlert = JsonConvert.DeserializeObject<TradingViewAlert>(content);
            
            var api = new BinanceClient();

            if (tradingViewAlert is not null)
            {
                if (tradingViewAlert.Operation != string.Empty &&
                //tradingViewAlert.Price != 0 &&
                tradingViewAlert.Size != 0 &&
                tradingViewAlert.Ticker != string.Empty) /*&&
                tradingViewAlert.Offset != 0)*/
                {
                    //using (var user = new BinanceApiUser(Bot.BinanceKeyKiler, Bot.BinanceSecretKiler))
                    //{
                    //    //Tipo de operacion LONG / SHORT
                    //    Binance.Net.Enums.OrderSide orderSide = 0;

                    //    if (tradingViewAlert.Operation.ToUpper() == "LONG" || tradingViewAlert.Operation.ToUpper() == "BUY")
                    //    {
                    //        orderSide = OrderSide.Buy;
                    //        //precioOrden = tradingViewAlert.Price - tradingViewAlert.Offset; //TRAILING
                    //    }
                    //    else if (tradingViewAlert.Operation.ToUpper() == "SHORT" || tradingViewAlert.Operation.ToUpper() == "SELL")
                    //    {
                    //        orderSide = OrderSide.Sell;
                    //        //precioOrden = tradingViewAlert.Price + tradingViewAlert.Offset; //TRAILING
                    //    }
                    //    else
                    //    {
                    //        return "Sin operación.";
                    //    }

                    //    //Ticker - Symbol válido
                    //    string orderSymbol = string.Empty;

                    //    if (Symbol.IsValid(tradingViewAlert.Ticker))
                    //    {
                    //        orderSymbol = tradingViewAlert.Ticker;
                    //    }
                    //    else
                    //    {
                    //        return "Symbol inválido.";
                    //    }

                    //    //Size - Posiciones abiertas
                    //    decimal orderSize = tradingViewAlert.Size;

                    //    List<PositionInformation> positionInformation = api.GetPositionInformationAsync(user).Result.ToList();
                    //    foreach (PositionInformation position in positionInformation)
                    //    {
                    //        if (position.Symbol == orderSymbol.Replace("_", ""))
                    //        {
                    //            if (Math.Abs(position.PositionAmt) > 0)
                    //            {
                    //                orderSize = Math.Abs(position.PositionAmt) * 2;
                    //                break;
                    //            }
                    //        }
                    //    }

                    //    //Creo orden market
                    //    Bot.BinanceMarketOrder = new MarketOrder(user)
                    //    {
                    //        Symbol = orderSymbol,
                    //        Side = orderSide,
                    //        Quantity = orderSize
                    //    };

                    //    try
                    //    {
                    //        //Capturo precio antes de poner la orden
                    //        Bot.BinanceSymbolPrice = api.GetPriceAsync(Bot.BinanceMarketOrder.Symbol).Result.Value;

                    //        //Mando la orden
                    //        Bot.BinanceOrderResult = Task.Run(async () => await api.PlaceAsync(Bot.BinanceMarketOrder)).Result;
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        TelegramMessage.Message("OrderPost: " + e.Message);
                    //        return e.Message;
                    //    }
                    //}
                }
            }
            
            //string message = $"Id: {Bot.BinanceOrderResult.ClientOrderId}, Symbol: {Bot.BinanceOrderResult.Symbol}, Price: {Bot.BinanceSymbolPrice}, Size: {Bot.BinanceOrderResult.OriginalQuantity}, Side: {Bot.BinanceOrderResult.Side}, Status: {Bot.BinanceOrderResult.Status}";
            //Console.WriteLine(message);
            //TelegramMessage.Message(message);

            return "Ok";
        }
    }
}
