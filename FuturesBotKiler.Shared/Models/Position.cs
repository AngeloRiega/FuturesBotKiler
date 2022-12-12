using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuturesBotKiler.Shared.Models
{
    public class Position
    {
        public int? maxNotional { get; set; }
        public int? marginType { get; set; }
        public bool? isAutoAddMargin { get; set; }
        public double? isolatedMargin { get; set; }
        public double? liquidationPrice { get; set; }
        public double? markPrice { get; set; }
        public double? quantity { get; set; }
        public DateTime? updateTime { get; set; }
        public string symbol { get; set; }
        public double? entryPrice { get; set; }
        public int? leverage { get; set; }
        public double? unrealizedPnl { get; set; }
        public int? positionSide { get; set; }
    }
}
