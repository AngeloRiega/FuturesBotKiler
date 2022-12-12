using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
using FuturesBotKiler.Shared;
using FuturesBotKiler.Shared.Models;

namespace WebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    internal class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static async Task Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(config);

            BinanceClient.SetDefaultOptions(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(Parametros.BinanceSubKey, Parametros.BinanceSubSecret)
            });
            BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions
            {
                ApiCredentials = new ApiCredentials(Parametros.BinanceSubKey, Parametros.BinanceSubSecret)
            });

            BinanceClient client = new BinanceClient();
            var listenKey = await client.UsdFuturesApi.Account.StartUserStreamAsync();

            if (listenKey.Success)
            {
                TelegramMessage.Message($"listenKey.Success");

                BinanceSocketClient socketClient = new BinanceSocketClient();
                var sub = await socketClient.UsdFuturesStreams.SubscribeToUserDataUpdatesAsync(listenKey.Data, null, null, null,
                onOrderUpdate: async data =>
                {
                    // Handle order update

                    //SI LA ORDEN DE STOP LOSS SE COMPLETO Y ES UNA DEL BOT, PASO A LA SIGUIENTE ETAPA 
                    if (data.Data.UpdateData.Type == FuturesOrderType.StopMarket && data.Data.UpdateData.Status == OrderStatus.Expired) //CAMBIAR POR EXPIRED / CANCELED
                    {
                        //VERIFICO SI ES UNA ORDEN DEL BOT
                        string[] spliteo = data.Data.UpdateData.ClientOrderId.Split('-');
                        if (spliteo.Count() > 1)
                        {
                            //CANCELO TODAS LAS ORDENES PENDIENTES
                            await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(data.Data.UpdateData.Symbol);
                            TelegramMessage.Message($"SE ACTIVÓ STOP LOSS. {Environment.NewLine}" +
                                                    $"ID: {data.Data.UpdateData.ClientOrderId} {Environment.NewLine}" +
                                                    $"STOP PRICE: {data.Data.UpdateData.StopPrice} {Environment.NewLine}" +
                                                    $"DATE:" + DateTime.Now);

                            //CAPTURO CLIENTORDERID Y CREO UN OBJETO ORDEN
                            Orden orden = new Orden()
                            {
                                Id = Convert.ToInt32(spliteo[0]),
                                Etapa = Convert.ToInt32(spliteo[1]),
                                PorcentajeTakeProfit = Convert.ToDecimal(spliteo[2]),
                                PorcentajeStopLoss = Convert.ToDecimal(spliteo[3]),
                                Size = Convert.ToDecimal(spliteo[4]),
                                Symbol = spliteo[5],
                                Side = data.Data.UpdateData.Side
                            };

                            //AUMENTO LA ETAPA PARA LA SIGUIENTE ORDEN
                            orden.Etapa++;

                            //BUSCO EL ID CORRECTO
                            await Program.CalcularUltimoIdOrden(orden);

                            //CREO ORDEN CON LA SIGUIENTE ETAPA Y DIRECCION CONTRARIA
                            await Program.CrearOrden(orden);

                            //CALCULAR PNL BUSCANDO LOS TRADES
                            var userTrades = await client.UsdFuturesApi.Trading.GetUserTradesAsync(data.Data.UpdateData.Symbol, limit: 10);
                            var filtrado = userTrades.Data.Where(l => l.OrderId == data.Data.UpdateData.OrderId).ToList();
                            TelegramMessage.Message($"PNL STOP LOSS {Environment.NewLine}" +
                                                    $"FEE: {decimal.Round(filtrado.Sum(o => o.Fee), 3)} {Environment.NewLine}" +
                                                    $"PNL: {decimal.Round(filtrado.Sum(o => o.RealizedPnl), 3)}");
                        }
                    }
                    else if (data.Data.UpdateData.Type == FuturesOrderType.TakeProfitMarket && data.Data.UpdateData.Status == OrderStatus.Expired) //CAMBIAR POREXPIREDCANCELED
                    {
                        //VERIFICO SI ES UNA ORDEN DEL BOT
                        string[] spliteo = data.Data.UpdateData.ClientOrderId.Split('-');
                        if (spliteo.Count() > 1)
                        {
                            //CANCELO TODAS LAS ORDENES PENDIENTES
                            await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(data.Data.UpdateData.Symbol);
                            TelegramMessage.Message($"SE ACTIVÓ TAKE PROFIT. {Environment.NewLine}" +
                                                    $"ID: {data.Data.UpdateData.ClientOrderId} {Environment.NewLine}" +
                                                    $"STOP PRICE: {data.Data.UpdateData.StopPrice} {Environment.NewLine}" +
                                                    $"DATE:" + DateTime.Now);

                            //CALCULAR PNL BUSCANDO LOS TRADES
                            var userTrades = await client.UsdFuturesApi.Trading.GetUserTradesAsync(data.Data.UpdateData.Symbol, limit: 10);
                            var filtrado = userTrades.Data.Where(l => l.OrderId == data.Data.UpdateData.OrderId).ToList();
                            TelegramMessage.Message($"PNL TAKE PROFIT {Environment.NewLine}" +
                                                    $"FEE: {decimal.Round(filtrado.Sum(o => o.Fee), 3)} {Environment.NewLine}" +
                                                    $"PNL: {decimal.Round(filtrado.Sum(o => o.RealizedPnl), 3)}");
                        }
                    }

                    //PRUEBA
                    if (data.Data.UpdateData.Type == FuturesOrderType.StopMarket && data.Data.UpdateData.Status == OrderStatus.Canceled) //CAMBIAR POR EXPIRED / CANCELED
                    {
                        var ordersData = await client.UsdFuturesApi.Trading.GetOrdersAsync("BTCUSDT");
                    }
                },
                data =>
                {
                    // Handle listen key expired
                    TelegramMessage.Message($"EXPIRÓ LISTENKEY {DateTime.Now}");
                    Console.WriteLine($"EXPIRÓ LISTENKEY {DateTime.Now}");
                });


                //KEEP ALIVE LISTEN KEY CADA 5 MINUTOS
                System.Timers.Timer timer = new System.Timers.Timer(interval: 300000);
                timer.Elapsed += async (sender, e) =>
                {
                    await client.UsdFuturesApi.Account.KeepAliveUserStreamAsync(listenKey.Data);
                    Console.WriteLine($"SE REINICIÓ EL LISTENKEY {DateTime.Now}");
                    TelegramMessage.Message($"ESPERANDO (SE REINICIÓ EL LISTENKEY {DateTime.Now})");
                };
                timer.Start();
                TelegramMessage.Message(timer.Enabled.ToString());
            }
            else
            {
                // Handler failure

                Console.WriteLine($"NO SE PUDO SUSCRIBIR {DateTime.Now}");
                Console.WriteLine($"{listenKey.Error}");
                Console.WriteLine($"{listenKey.Data[0]}");
                TelegramMessage.Message($"NO SE PUDO SUSCRIBIR {DateTime.Now}");
                TelegramMessage.Message($"{listenKey.Error}");
                TelegramMessage.Message($"{listenKey.Data[0]}");
            }

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
            //host.Start();
            //await host.StartAsync();
        }

        private static async Task CalcularEtapaOrden(Orden orden)
        {
            using (BinanceClient client = new BinanceClient())
            {
                //SI NO EXISTE POSICION ABIERTA SE PONE LA ETAPA EN 0
                var posiciones = await client.UsdFuturesApi.Account.GetPositionInformationAsync(orden.Symbol);
                if (posiciones.Data.First().Quantity == 0)
                {
                    //Parametros.Etapa = 0;
                    orden.Etapa = 0;
                }
                else
                {
                    //PARA EVITAR ABRIR LA POSICION INICIAL DE PRUEBA DEL BOT
                    //posicionAbierta = true;

                    //REVISO ORDENES ABIERTAS PARA CALCULAR LA ETAPA
                    var ordenesAbiertas = await client.UsdFuturesApi.Trading.GetOpenOrdersAsync(orden.Symbol);
                    if (ordenesAbiertas.Data.Count() > 0)
                    {
                        string[] spliteo = ordenesAbiertas.Data.Last().ClientOrderId.Split('-');
                        if (spliteo.Count() > 1)
                        {
                            orden.Etapa = Convert.ToInt32(spliteo[1]);
                            //Parametros.Etapa = Convert.ToInt32(spliteo[1]);
                            //Console.WriteLine($"Etapa: {Parametros.Etapa}");
                        }
                    }
                }
            }
        }

        public static async Task CalcularUltimoIdOrden(Orden orden)
        {
            using (BinanceClient client = new BinanceClient())
            {
                //CALCULO ID, BUSCO LOS IDS DE TODAS LAS ORDENES
                var ordenes = await client.UsdFuturesApi.Trading.GetOrdersAsync(orden.Symbol, limit: 50);
                if (ordenes.Data.Count() > 0)
                {
                    //PARA REINICIAR EL ID DE LAS ORDENES
                    //if (Parametros.ClientId == 0) { }
                    if (orden.Id == 0) { }
                    else
                    {
                        //AGARRO SOLAMENTE LOS CLIENTORDERID Y BUSCO EL ULTIMO
                        List<string> listaClientOrderId = ordenes.Data.Select(l => l.ClientOrderId).Reverse().ToList();

                        foreach (var ordenesAbiertas in listaClientOrderId)
                        {
                            string[] spliteo = ordenesAbiertas.Split('-');
                            if (spliteo.Count() > 1)
                            {
                                //SETEO ULTIMO ID
                                //Parametros.ClientId = Convert.ToInt32(spliteo[0]) + 1;
                                orden.Id = Convert.ToInt32(spliteo[0]) + 1;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static async Task CrearOrden(Orden orden)
        {
            //MULTIPLICO EL QUANTITY POR LA ETAPA
            decimal factor = 1.40m;
            for (int i = 0; i < orden.Etapa; i++)
            {
                orden.Size = orden.Size * factor;
            }
            orden.Size = decimal.Round(orden.Size, 2);

            using (BinanceClient client = new BinanceClient())
            {
                //CALCULAR QUANTITY EN MONEDA
                orden.Quantity = decimal.Round(orden.Size / client.UsdFuturesApi.ExchangeData.GetPriceAsync(orden.Symbol).Result.Data.Price, 3); //CAPTURO PRECIO Y DIVIDO

                //CREO ORDEN
                string clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-{orden.Side}";
                var openPositionResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, orden.Side, FuturesOrderType.Market, orden.Quantity, newClientOrderId: clientOrderId);
                if (openPositionResult.Success)
                {
                    //OBTENGO PRECIO MARKET DE LA ORDEN RECIEN CREADA 
                    var marketPrice = await client.UsdFuturesApi.Trading.GetOrderAsync(orden.Symbol, openPositionResult.Data.Id);
                    decimal orderPrice = decimal.Round(marketPrice.Data.AvgPrice, 2);

                    //MANDO MENSAJE POR TELEGRAM
                    TelegramMessage.Message($"SE CREARON ÓRDENES EXITOSAMENTE. {Environment.NewLine}" +
                                            $"ID: {clientOrderId} {Environment.NewLine}" +
                                            $"PRICE: {orderPrice} {Environment.NewLine}" +
                                            $"DATE: {DateTime.Now}");

                    //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                    //Parametros.ClientId++;
                    orden.Id++;

                    if (orden.Side == OrderSide.Buy)
                    {
                        //CALCULO PRECIOS DE STOP LOSS Y TAKE PROFIT
                        decimal stopLossPrice = decimal.Round(orderPrice - (orderPrice * orden.PorcentajeStopLoss / 100), 2);
                        decimal takeProfitPrice = decimal.Round(orderPrice + (orderPrice * orden.PorcentajeTakeProfit / 100), 2);

                        //CREO ORDEN DE STOP LOSS
                        clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-SL";
                        var stopLossResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, OrderSide.Sell, FuturesOrderType.StopMarket, quantity: null, closePosition: true, stopPrice: stopLossPrice, newClientOrderId: clientOrderId);

                        //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                        //if (stopLossResult.Success) Parametros.ClientId++;
                        if (stopLossResult.Success) orden.Id++;

                        //CREO ORDEN DE TAKE PROFIT
                        clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-TP";
                        var takeProfitResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, OrderSide.Sell, FuturesOrderType.TakeProfitMarket, quantity: null, closePosition: true, stopPrice: takeProfitPrice, newClientOrderId: clientOrderId);

                        //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                        //if (takeProfitResult.Success) Parametros.ClientId++;
                        //if (takeProfitResult.Success) orden.Id++;
                    }
                    else if (orden.Side == OrderSide.Sell)
                    {
                        //CALCULO PRECIOS DE STOP LOSS Y TAKE PROFIT
                        decimal stopLossPrice = decimal.Round(orderPrice + (orderPrice * orden.PorcentajeStopLoss / 100), 2);
                        decimal takeProfitPrice = decimal.Round(orderPrice - (orderPrice * orden.PorcentajeTakeProfit / 100), 2);

                        //CREO ORDEN DE STOP LOSS
                        clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-SL";
                        var stopLossResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, OrderSide.Buy, FuturesOrderType.StopMarket, quantity: null, closePosition: true, stopPrice: stopLossPrice, newClientOrderId: clientOrderId);

                        //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                        //if (stopLossResult.Success) Parametros.ClientId++;
                        if (stopLossResult.Success) orden.Id++;

                        //CREO ORDEN DE TAKE PROFIT
                        clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-TP";
                        var takeProfitResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, OrderSide.Buy, FuturesOrderType.TakeProfitMarket, quantity: null, closePosition: true, stopPrice: takeProfitPrice, newClientOrderId: clientOrderId);

                        //SI LA ORDEN ES EXITOSA AUMENTA EL ID
                        //if (takeProfitResult.Success) Parametros.ClientId++;
                        //if (takeProfitResult.Success) orden.Id++;
                    }
                }
            }
        }
    }
}
