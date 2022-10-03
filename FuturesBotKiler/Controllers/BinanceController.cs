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
            

            if (tradingViewAlert is not null)
            {
                if (tradingViewAlert.Side != string.Empty &&
                    tradingViewAlert.Size != 0 &&
                    tradingViewAlert.Symbol != string.Empty &&
                    tradingViewAlert.PorcentajeStopLoss != 0 &&
                    tradingViewAlert.PorcentajeTakeProfit != 0)
                {
                    Enum.TryParse(tradingViewAlert.Side, out OrderSide side);

                    TelegramMessage.Message($"ALERTA TRADINGVIEW {tradingViewAlert.Symbol} - {side}");

                    //CREO ORDEN
                    Orden orden = new Orden(
                        symbol: tradingViewAlert.Symbol,
                        side: side,
                        size: tradingViewAlert.Size,
                        porcentajeStopLoss: tradingViewAlert.PorcentajeStopLoss,
                        porcentajeTakeProfit: tradingViewAlert.PorcentajeTakeProfit
                    );


                    await Program.CrearOrden(orden);

                    return "ORDEN CREADA";
                }

                return "ERROR PARAMETROS";
            }
            else
            {
                return "ERROR PARAMETROS";
            }   
        }
    }
}
