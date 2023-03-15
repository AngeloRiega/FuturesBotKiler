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
using FuturesBotKiler.Shared.Models;
using System.Net.Http.Headers;
using CryptoExchange.Net.CommonObjects;
using Binance.Net;
using CryptoExchange.Net.Authentication;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using FuturesBotKiler.Shared;

namespace FuturesBotKiler.Api.Controllers
{
    public class Process
    {
        int id;
        string? name;
        string? machineName;
        string? href;
        string? user_name;

        public int Id { get => id; set => id = value; }
        public string? Name { get => name; set => name = value; }
        public string? MachineName { get => machineName; set => machineName = value; }
        public string? Href { get => href; set => href = value; }
        public string? User_name { get => user_name; set => user_name = value; }
    }

    [ApiController]
    [Route("[controller]")]
    public class BinanceController : ControllerBase
    {

        [HttpGet]
        public string Get()
        {
            return "Esperando alertas.";
        }

        [HttpGet]
        [Route("positions/{symbol}")]
        public async Task<ActionResult<BinancePositionDetailsUsdt>> GetPosition(string symbol)
        {
            using (BinanceClient client = new BinanceClient())
            {
                var position = await client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
                return Ok(position.Data);
            }
        }

        [HttpDelete]
        [Route("positions/{symbol}/close")]
        public async Task<ActionResult<bool>> ClosePosition(string symbol)
        {
            if (await Binance.ClosePosition(symbol))
            {
                return Ok(true);
            }
            else
            {
                return Ok(false);
            }
        }

        [HttpPost("iniciarwebjob")]
        public async Task<string> IniciarWebJob()
        {
            //CREDENCIALES WEBJOB
            string username = Parametros.WebJobUsername;
            string password = Parametros.WebJobPassword;
            string authorization = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorization);

                var iniciarWebJob = await client.PostAsync("https://futuresbotkilerapp.scm.azurewebsites.net/api/triggeredwebjobs/WebJob/run", null);

                if (iniciarWebJob.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    TelegramMessage.Message($"SE INICIO WEBJOB EXITOSAMENTE");
                    return $"SE INICIO WEBJOB EXITOSAMENTE";

                }
                else
                {
                    TelegramMessage.Message($"ERROR AL INICIAR WEBJOB {iniciarWebJob.StatusCode}");
                    return $"ERROR AL INICIAR WEBJOB {iniciarWebJob.StatusCode}";
                }
            }
        }

        [HttpDelete("detenerwebjob")]
        public async Task<string> DetenerWebJob()
        {
            //CREDENCIALES WEBJOB
            string username = Parametros.WebJobUsername;
            string password = Parametros.WebJobPassword;
            string authorization = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorization);

                //BUSCO EL ID DEL PROCESO DEL WEBJOB Y LE HAGO KILL
                var listaProcesos = await client.GetAsync($"https://futuresbotkilerapp.scm.azurewebsites.net/api/processes/");

                if (listaProcesos.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Process[] procesos = JsonConvert.DeserializeObject<Process[]>(listaProcesos.Content.ReadAsStringAsync().Result);
                    var proceso = procesos.FirstOrDefault(o => o.Name == "WebJob");

                    if (proceso != null)
                    {
                        var detenerWebJob = await client.DeleteAsync($"https://futuresbotkilerapp.scm.azurewebsites.net/api/processes/{proceso.Id}");

                        if (detenerWebJob.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            TelegramMessage.Message($"SE DETUVO WEBJOB");
                            return $"SE DETUVO WEBJOB";
                        }
                        else
                        {
                            TelegramMessage.Message($"ERROR AL DETENER WEBJOB {detenerWebJob.StatusCode}");
                            return $"ERROR AL DETENER WEBJOB {detenerWebJob.StatusCode}";
                        }
                    }
                    else
                    {
                        TelegramMessage.Message($"NO SE PUDO DETENER WEBJOB NO SE ENCONTRO PROCESO");
                        return $"NO SE PUDO DETENER WEBJOB NO SE ENCONTRO PROCESO";
                    }
                }
                else
                {
                    TelegramMessage.Message($"NO SE PUDO DETENER WEBJOB USER/PW");
                    return $"NO SE PUDO DETENER WEBJOB USER/PW";
                }
            }
        }


        [HttpPost("alertatradingview")]
        public async Task<ActionResult<string>> AlertaTradingView()
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
                    tradingViewAlert.Symbol != string.Empty)
                {
                    Enum.TryParse(tradingViewAlert.Side, true, out OrderSide orderSide);

                    TelegramMessage.Message($"CREACION ORDEN {tradingViewAlert.Symbol} - {orderSide}");

                    //CREO ORDEN
                    Orden orden = new Orden(
                        symbol: tradingViewAlert.Symbol,
                        side: orderSide,
                        size: tradingViewAlert.Size,
                        porcentajeStopLoss: tradingViewAlert.PorcentajeStopLoss,
                        porcentajeTakeProfit: tradingViewAlert.PorcentajeTakeProfit
                    );
  
                    await Binance.CalcularUltimoIdOrden(orden);
                  
                    await Binance.CrearOrden(orden);

                    return Ok("ORDEN CREADA");
                }

                TelegramMessage.Message($"ERROR PARAMETROS CREACION ORDEN");
                return BadRequest("ERROR PARAMETROS CREACION ORDEN");
            }
            else
            {
                TelegramMessage.Message($"ERROR PARAMETROS CREACION ORDEN");
                return BadRequest("ERROR PARAMETROS CREACION ORDEN");
            }   
        }
    }
}