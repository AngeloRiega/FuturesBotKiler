using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuturesBotKiler.Models
{
    public class TradingViewAlert
    {
        public string Ticker { get; set; }
        public decimal Price { get; set; }
        public string Operation { get; set; }
        public decimal Size { get; set; }
        public decimal Offset { get; set; }
    }
}
