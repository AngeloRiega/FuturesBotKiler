using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuturesBotKiler.Shared.Models
{
    public class TradingViewAlert
    {
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal Size { get; set; }
        public decimal PorcentajeTakeProfit { get; set; }
        public decimal PorcentajeStopLoss { get; set; }
    }
}
