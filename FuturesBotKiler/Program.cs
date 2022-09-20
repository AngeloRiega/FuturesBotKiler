using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using System.Globalization;
using CryptoExchange.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using System.Collections;

namespace FuturesBotKiler
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            _ = CreateHostBuilder(args).Build().StartAsync();

            var client = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Parametros.BinanceKey, Parametros.BinanceSecret)
            });
            var socketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(Parametros.BinanceKey, Parametros.BinanceSecret)
            });

            //PARAMETROS
            string symbol = "BTCUSDT";
            bool posicionAbierta = false;
            decimal porcentajeTakeProfit = 1.10m;
            decimal porcentajeStopLoss = 0.30m;
            OrderSide orderSide = OrderSide.Buy; //Buy O Sell
            //PRUEBAS
            //decimal porcentajeTakeProfit = 0.15m;
            //decimal porcentajeStopLoss = 0.15m;

            //PRUEBA PARA REVISAR IDS
            //var ordersData = await client.UsdFuturesApi.Trading.GetOrdersAsync(symbol, limit: 100);
            //var userTrades = await client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 10);
            //var filtrado = userTrades.Data.Where(l => l.OrderId == 76349962931).ToList();
            //var income = await client.UsdFuturesApi.Account.GetIncomeHistoryAsync(symbol, incomeType: "REALIZED_PNL");
            //var income2 = await client.UsdFuturesApi.Account.GetIncomeHistoryAsync(symbol);

            //OBTENER ORDEN POR ID
            //var ordersData = await client.UsdFuturesApi.Trading.GetOpenOrderAsync(symbol, origClientOrderId: "1");

            //OBTENER ORDENES POR UN PAR (NUEVAS Y CANCELADAS)
            //var ordersData = await client.UsdFuturesApi.Trading.GetOrdersAsync("BTCUSDT");

            //CANCELAR ORDEN ESPECIFICA
            //var orderData = await client.UsdFuturesApi.Trading.CancelOrderAsync("BTCUSDT", 71768100654);
            //if (orderData.Success)
            //76335970039

            //CALCULO ETAPA, REVISO SI TENGO POSICION EN ESE PAR, SI NO TENGO ES ETAPA 0, SI TENGO REVISO SI ES UNA ORDEN PUESTA POR EL BOT
            var posiciones = await client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
            if (posiciones.Data.First().Quantity == 0)
            {
                Parametros.Etapa = 0;
            }
            else
            {
                //PARA EVITAR ABRIR LA POSICION INICIAL
                posicionAbierta = true;

                //REVISO ORDENES ABIERTAS PARA CALCULAR LA ETAPA
                var ordenesAbiertas = await client.UsdFuturesApi.Trading.GetOpenOrdersAsync(symbol);
                if (ordenesAbiertas.Data.Count() > 0)
                {
                    string[] spliteo = ordenesAbiertas.Data.Last().ClientOrderId.Split("-");
                    if (spliteo.Count() > 1)
                    {
                        Parametros.Etapa = Convert.ToInt32(spliteo[1]);
                        Console.WriteLine($"Etapa: {Parametros.Etapa}");
                    }
                }
            }

            //CALCULO ID, BUSCO LOS IDS DE TODAS LAS ORDENES
            var ordenes = await client.UsdFuturesApi.Trading.GetOrdersAsync(symbol, limit: 100);
            if (ordenes.Data.Count() > 0)
            {
                //PARA REINICIAR EL ID DE LAS ORDENES
                if (Parametros.ClientId == 0) { }
                else
                {
                    //AGARRO SOLAMENTE LOS CLIENTORDERID Y BUSCO EL ULTIMO
                    List<string> listaClientOrderId = ordenes.Data.Select(l => l.ClientOrderId).ToList();
                    listaClientOrderId.Reverse();
                    foreach (var orden in listaClientOrderId)
                    {
                        string[] spliteo = orden.Split("-");
                        if (spliteo.Count() > 1)
                        {
                            //SETEO ULTIMO ID
                            Parametros.ClientId = Convert.ToInt32(spliteo[0]) + 1;
                            break;
                        }
                    }
                }
            }

            //SUSCRIPCION
            var listenKey = await client.UsdFuturesApi.Account.StartUserStreamAsync();
            if (!listenKey.Success)
            {
                // Handler failure
                return;
            }
            var sub = await socketClient.UsdFuturesStreams.SubscribeToUserDataUpdatesAsync(listenKey.Data,
                data =>
                {
                    // Handle leverage update
                },
                data =>
                {
                    // Handle margin update
                },
                data =>
                {
                    // Handle account balance update, caused by trading
                },
                async data =>
                {
                    // Handle order update

                    //SI LA ORDEN DE STOP LOSS SE COMPLETO Y ES UNA DEL BOT, PASO A LA SIGUIENTE ETAPA 
                    if (data.Data.UpdateData.Type == FuturesOrderType.StopMarket && data.Data.UpdateData.Status == OrderStatus.Expired) //CAMBIAR POR EXPIRED / CANCELED
                    {
                        //VERIFICO SI ES UNA ORDEN DEL BOT
                        string[] spliteo = data.Data.UpdateData.ClientOrderId.Split("-");
                        if (spliteo.Count() > 1)
                        {
                            await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);
                            Parametros.Etapa++;
                            TelegramMessage.Message($"SE ACTIVÓ STOP LOSS. {Environment.NewLine}" +
                                                    $"ID: {data.Data.UpdateData.ClientOrderId} {Environment.NewLine}" +
                                                    $"STOP PRICE: {data.Data.UpdateData.StopPrice} {Environment.NewLine}" +
                                                    $"DATE:" + DateTime.Now);
                            await CrearOrden(client, symbol, data.Data.UpdateData.Side, porcentajeTakeProfit, porcentajeStopLoss, Parametros.Quantity);

                            //CALCULAR PNL BUSCANDO EL TRADE
                            var userTrades = await client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 10);
                            var filtrado = userTrades.Data.Where(l => l.OrderId == data.Data.UpdateData.OrderId).ToList();
                            TelegramMessage.Message($"PNL STOP LOSS {Environment.NewLine}" +
                                                    $"FEE: {decimal.Round(filtrado[0].Fee, 3)} {Environment.NewLine}" +
                                                    $"PNL: {decimal.Round(filtrado[0].RealizedPnl, 3)}");
                        }
                    }
                    else if (data.Data.UpdateData.Type == FuturesOrderType.TakeProfitMarket && data.Data.UpdateData.Status == OrderStatus.Expired) //CAMBIAR POR EXPIRED / CANCELED
                    {
                        //VERIFICO SI ES UNA ORDEN DEL BOT
                        string[] spliteo = data.Data.UpdateData.ClientOrderId.Split("-");
                        if (spliteo.Count() > 1)
                        {
                            await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);
                            TelegramMessage.Message($"SE ACTIVÓ TAKE PROFIT. {Environment.NewLine}" +
                                                    $"ID: {data.Data.UpdateData.ClientOrderId} {Environment.NewLine}" +
                                                    $"STOP PRICE: {data.Data.UpdateData.StopPrice} {Environment.NewLine}" +
                                                    $"DATE:" + DateTime.Now);

                            //CALCULAR PNL BUSCANDO EL TRADE
                            var userTrades = await client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 10);
                            var filtrado = userTrades.Data.Where(l => l.OrderId == data.Data.UpdateData.OrderId).ToList();
                            TelegramMessage.Message($"PNL TAKE PROFIT {Environment.NewLine}" +
                                                    $"FEE: {decimal.Round(filtrado[0].Fee, 3)} {Environment.NewLine}" +
                                                    $"PNL: {decimal.Round(filtrado[0].RealizedPnl, 3)}");
                        }
                    }

                    //PRUEBA
                    //if (data.Data.UpdateData.Type == FuturesOrderType.StopMarket && data.Data.UpdateData.Status == OrderStatus.Canceled) //CAMBIAR POR EXPIRED / CANCELED
                    //{
                    //    var ordersData = await client.UsdFuturesApi.Trading.GetOrdersAsync("BTCUSDT");
                    //}
                },
                data =>
                {
                    // Handle listen key expired

                    //NO DEBERIA LLEGAR AQUI
                    client.UsdFuturesApi.Account.KeepAliveUserStreamAsync(listenKey.Data);
                    Console.WriteLine($"EXPIRÓ LISTENKEY {DateTime.Now}");
                });

            //KEEP ALIVE LISTEN KEY CADA 55 MINUTOS
            System.Timers.Timer timer = new(interval: 3300000);
            timer.Elapsed += async (sender, e) => {
                await client.UsdFuturesApi.Account.KeepAliveUserStreamAsync(listenKey.Data);
                Console.WriteLine($"SE REINICIÓ EL LISTENKEY {DateTime.Now}");
                TelegramMessage.Message($"ESPERANDO (SE REINICIÓ EL LISTENKEY {DateTime.Now})");
            };
            timer.Start();

            //CREO LA ORDEN INICIAL
            if (Parametros.Etapa == 0 && posicionAbierta == false)
            {
                await CrearOrden(client, symbol, orderSide, porcentajeTakeProfit, porcentajeStopLoss, Parametros.Quantity);
            }
            else
            {
                Console.WriteLine($"YA EXISTE POSICIÓN. ETAPA: {Parametros.Etapa}");
            }

            Console.ReadLine();
        }

        public static async Task CrearOrden(BinanceClient client, string symbol, OrderSide orderSide, decimal porcentajeTakeProfit, decimal porcentajeStopLoss, decimal quantity)
        {
            //MULTIPLICO EL QUANTITY POR LA ETAPA
            decimal factor = 1.40m;

            //PRUEBA
            //Parametros.Etapa = 4;
            //if (Parametros.Etapa == 7)
            //{}

            for (int i = 0; i < Parametros.Etapa; i++)
            {
                Parametros.Quantity = quantity * factor;
            }
            //CALCULAR QUANTITY EN MONEDA
            quantity = decimal.Round(Parametros.Quantity / client.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result.Data.Price, 3); //CAPTURO PRECIO Y DIVIDO

            //CREO ORDEN
            var openPositionResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, orderSide, FuturesOrderType.Market, quantity, newClientOrderId: $"{Parametros.ClientId}-{Parametros.Etapa}-{symbol}-{orderSide}");
            if (openPositionResult.Success)
            {
                //OBTENGO PRECIO MARKET DE LA ORDEN RECIEN CREADA 
                var marketPrice = await client.UsdFuturesApi.Trading.GetOrderAsync(symbol, openPositionResult.Data.Id);
                decimal orderPrice = decimal.Round(marketPrice.Data.AvgPrice, 2);

                //MANDO MENSAJE POR TELEGRAM
                TelegramMessage.Message($"SE CREARON ÓRDENES EXITOSAMENTE. {Environment.NewLine}" +
                                        $"ID: {Parametros.ClientId}-{Parametros.Etapa}-{symbol}-{orderSide} {Environment.NewLine}" +
                                        $"PRICE: {orderPrice} {Environment.NewLine}" +
                                        $"DATE: {DateTime.Now}");

                //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                Parametros.ClientId++;

                if (orderSide == OrderSide.Buy)
                {
                    //CALCULO PRECIOS DE STOP LOSS Y TAKE PROFIT
                    decimal stopLossPrice = decimal.Round(orderPrice - (orderPrice * porcentajeStopLoss / 100), 2);
                    decimal takeProfitPrice = decimal.Round(orderPrice + (orderPrice * porcentajeTakeProfit / 100), 2);

                    //CREO ORDEN DE STOP LOSS
                    var stopLossResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.StopMarket, quantity: null, closePosition: true, stopPrice: stopLossPrice, newClientOrderId: $"{Parametros.ClientId}-{Parametros.Etapa}-{symbol}-sl");

                    //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                    if (stopLossResult.Success) Parametros.ClientId++;

                    //CREO ORDEN DE TAKE PROFIT
                    var takeProfitResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.TakeProfitMarket, quantity: null, closePosition: true, stopPrice: takeProfitPrice, newClientOrderId: $"{Parametros.ClientId}-{Parametros.Etapa}-{symbol}-tp");

                    //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                    if (takeProfitResult.Success) Parametros.ClientId++;
                }
                else if (orderSide == OrderSide.Sell)
                {
                    //CALCULO PRECIOS DE STOP LOSS Y TAKE PROFIT
                    decimal stopLossPrice = decimal.Round(orderPrice + (orderPrice * porcentajeStopLoss / 100), 2);
                    decimal takeProfitPrice = decimal.Round(orderPrice - (orderPrice * porcentajeTakeProfit / 100), 2);

                    //CREO ORDEN DE STOP LOSS
                    var stopLossResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.StopMarket, quantity: null, closePosition: true, stopPrice: stopLossPrice, newClientOrderId: $"{Parametros.ClientId}-{Parametros.Etapa}-{symbol}-sl");

                    //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                    if (stopLossResult.Success) Parametros.ClientId++;

                    //CREO ORDEN DE TAKE PROFIT
                    var takeProfitResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.TakeProfitMarket, quantity: null, closePosition: true, stopPrice: takeProfitPrice, newClientOrderId: $"{Parametros.ClientId}-{Parametros.Etapa}-{symbol}-tp");

                    //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                    if (takeProfitResult.Success) Parametros.ClientId++;
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}