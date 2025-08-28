using System;
using System.Collections.Generic;

namespace MVC_Project.Models
{
	public sealed class StockSeriesPoint
	{
		public DateTime Timestamp { get; set; }
		public decimal Open { get; set; }
		public decimal High { get; set; }
		public decimal Low { get; set; }
		public decimal Close { get; set; }
		public long Volume { get; set; }
	}

	public sealed class StockSeries
	{
		public string Symbol { get; set; } = string.Empty;
		public string Interval { get; set; } = string.Empty;
		public DateTime LastRefreshed { get; set; }
		public IReadOnlyList<StockSeriesPoint> Points { get; set; } = Array.Empty<StockSeriesPoint>();
	}

	public sealed class StockQuote
	{
		public string Symbol { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public decimal ChangePercent { get; set; }
	}
}
