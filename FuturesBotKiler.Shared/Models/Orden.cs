using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Enums;

namespace FuturesBotKiler.Shared.Models
{
    public class Orden
    {
        public Orden()
        {
        }

        public Orden(string symbol, decimal porcentajeTakeProfit, decimal porcentajeStopLoss, decimal size, OrderSide side = default, int etapa = 0, int id = 1, decimal quantity = 0)
        {
            Symbol = symbol;
            PorcentajeTakeProfit = porcentajeTakeProfit;
            PorcentajeStopLoss = porcentajeStopLoss;
            Size = size;
            Etapa = etapa;
            Id = id;
            Side = side;
            Quantity = quantity;
        }

        public string Symbol { get; set; }
        public int Etapa { get; set; }
        public int Id { get; set; }
        public OrderSide Side { get; set; }
        public decimal PorcentajeTakeProfit { get; set; }
        public decimal PorcentajeStopLoss { get; set; }
        public decimal Size { get; set; }
        public decimal Quantity { get; set; }
    }
}
