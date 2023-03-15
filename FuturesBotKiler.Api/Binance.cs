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
using CryptoExchange.Net.CommonObjects;
using FuturesBotKiler.Shared.Models;
using FuturesBotKiler.Shared;

namespace FuturesBotKiler.Api
{
    public class Binance
    {
        private static async Task CalcularEtapaOrden(Orden orden)
        {
            using (BinanceClient client = new BinanceClient())
            {
                //SI NO EXISTE POSICION ABIERTA SE PONE LA ETAPA EN 0
                var posiciones = await client.UsdFuturesApi.Account.GetPositionInformationAsync(orden.Symbol);
                if (posiciones.Data.First().Quantity == 0)
                {
                    orden.Etapa = 0;
                }
                else
                {
                    //REVISO ORDENES ABIERTAS PARA CALCULAR LA ETAPA
                    var ordenesAbiertas = await client.UsdFuturesApi.Trading.GetOpenOrdersAsync(orden.Symbol);
                    if (ordenesAbiertas.Data.Count() > 0)
                    {
                        string[] spliteo = ordenesAbiertas.Data.Last().ClientOrderId.Split("-");
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
                            string[] spliteo = ordenesAbiertas.Split("-");
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
                var precioActual = client.UsdFuturesApi.ExchangeData.GetPriceAsync(orden.Symbol).Result;
                if (precioActual.Error?.Code == -1121) //INVALID SYMBOL
                {
                    TelegramMessage.Message($"NO EXISTE PAR {orden.Symbol}");
                    throw new Exception($"NO EXISTE PAR {orden.Symbol}");
                }
                orden.Quantity = decimal.Round(orden.Size / precioActual.Data.Price, 3); //CAPTURO PRECIO Y DIVIDO

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
                        if (stopLossResult.Success) orden.Id++;

                        //CREO ORDEN DE TAKE PROFIT
                        clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-TP";
                        var takeProfitResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, OrderSide.Sell, FuturesOrderType.TakeProfitMarket, quantity: null, closePosition: true, stopPrice: takeProfitPrice, newClientOrderId: clientOrderId);
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
                        if (stopLossResult.Success) orden.Id++;

                        //CREO ORDEN DE TAKE PROFIT
                        clientOrderId = $"{orden.Id}-{orden.Etapa}-{orden.PorcentajeTakeProfit}-{orden.PorcentajeStopLoss}-{orden.Size}-{orden.Symbol}-TP";
                        var takeProfitResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(orden.Symbol, OrderSide.Buy, FuturesOrderType.TakeProfitMarket, quantity: null, closePosition: true, stopPrice: takeProfitPrice, newClientOrderId: clientOrderId);
                    }
                }
            }
        }

        public static async Task<bool> ClosePosition(string symbol)
        {
            using (BinanceClient client = new BinanceClient())
            {
                //BUSCO LA POSICION DE ESE PAR
                var position = await client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
                decimal quantityPosition = position.Data.First().Quantity;
                string stringPositionOrderSide = "";

                //SI ENCUENTRO POSICION ABIERTA
                if (quantityPosition != 0)
                {
                    string clientOrderId = "closepos"; // TODO: CONFIGURAR EL ID
                    OrderSide positionOrderSide = default;

                    //ORDER SIDE CONTRARIO PARA CERRAR LA POSICION
                    if (quantityPosition > 0)
                    {
                        positionOrderSide = OrderSide.Sell;
                        stringPositionOrderSide = "BUY"; //DEBE SER CONTRARIA A LA POSICION QUE SE ESTA ABRIENDO PARA CERRAR
                    }
                    else if (quantityPosition < 0)
                    {
                        positionOrderSide = OrderSide.Buy;
                        stringPositionOrderSide = "SELL";
                    }

                    var closePositionResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, positionOrderSide, FuturesOrderType.Market, quantity: Math.Abs(quantityPosition), reduceOnly: true, newClientOrderId: clientOrderId);

                    if (closePositionResult.Success)
                    {
                        //CANCELO TODAS LAS ORDENES PENDIENTES
                        await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);

                        //CALCULAR PNL BUSCANDO LOS TRADES
                        var userTrades = await client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, limit: 10);
                        var filtrado = userTrades.Data.Where(l => l.OrderId == closePositionResult.Data.Id).ToList();
                        TelegramMessage.Message($"PNL CLOSE POSITION {Environment.NewLine}" +
                                                $"FEE: {decimal.Round(filtrado.Sum(o => o.Fee), 3)} {Environment.NewLine}" +
                                                $"PNL: {decimal.Round(filtrado.Sum(o => o.RealizedPnl), 3)}");

                        TelegramMessage.Message($"POSICION {stringPositionOrderSide} {symbol.ToUpper()} CERRADA");

                        return true;
                    }
                    else
                    {
                        TelegramMessage.Message($"POSICION {stringPositionOrderSide} {symbol.ToUpper()} NO SE PUDO CERRAR");

                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}