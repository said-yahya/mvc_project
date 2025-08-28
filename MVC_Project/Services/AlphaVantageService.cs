using System.Text.Json;
using System.Text.Json.Serialization;
using MVC_Project.Models;
using Microsoft.Extensions.Caching.Memory;

namespace MVC_Project.Services
{
	public sealed class AlphaVantageService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<AlphaVantageService> _logger;
		private readonly string _apiKey;
		private readonly IMemoryCache _cache;
		private static readonly SemaphoreSlim RequestLock = new SemaphoreSlim(1, 1);

		public AlphaVantageService(HttpClient httpClient, IConfiguration config, ILogger<AlphaVantageService> logger, IMemoryCache cache)
		{
			_httpClient = httpClient;
			_logger = logger;
			_apiKey = config["AlphaVantage:ApiKey"] ?? string.Empty;
			_cache = cache;
		}

		private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken ct)
		{
			if (_cache.TryGetValue(url, out JsonDocument cached))
			{
				return cached;
			}
			await RequestLock.WaitAsync(ct);
			try
			{
				// small delay to avoid hitting per-second limits
				await Task.Delay(300, ct);
				using var req = new HttpRequestMessage(HttpMethod.Get, url);
				using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
				resp.EnsureSuccessStatusCode();
				await using var stream = await resp.Content.ReadAsStreamAsync(ct);
				var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
				// check for rate-limit note or error
				if (json.RootElement.TryGetProperty("Note", out var note))
				{
					_logger.LogWarning("Alpha Vantage note: {Note}", note.GetString());
					return null;
				}
				if (json.RootElement.TryGetProperty("Information", out var info))
				{
					_logger.LogWarning("Alpha Vantage info: {Info}", info.GetString());
					return null;
				}
				_cache.Set(url, json, TimeSpan.FromMinutes(1));
				return json;
			}
			finally
			{
				RequestLock.Release();
			}
		}

		public async Task<StockSeries?> GetIntradayAsync(string symbol, string interval = "5min", CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("Alpha Vantage API key is missing.");

			var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={Uri.EscapeDataString(symbol)}&interval={Uri.EscapeDataString(interval)}&outputsize=compact&apikey={_apiKey}";
			using var json = await GetJsonAsync(url, ct);
			if (json == null) return null;
			if (!json.RootElement.TryGetProperty($"Time Series ({interval})", out var seriesEl))
			{
				_logger.LogWarning("Alpha Vantage response missing series for {Symbol} {Interval}", symbol, interval);
				return null;
			}

			DateTime lastRefreshed = DateTime.UtcNow;
			if (json.RootElement.TryGetProperty("Meta Data", out var meta) && meta.TryGetProperty("3. Last Refreshed", out var last))
			{
				DateTime.TryParse(last.GetString(), out lastRefreshed);
			}

			var points = new List<StockSeriesPoint>();
			foreach (var kv in seriesEl.EnumerateObject())
			{
				if (!DateTime.TryParse(kv.Name, out var ts)) continue;
				var o = kv.Value.GetProperty("1. open").GetString();
				var h = kv.Value.GetProperty("2. high").GetString();
				var l = kv.Value.GetProperty("3. low").GetString();
				var c = kv.Value.GetProperty("4. close").GetString();
				var v = kv.Value.GetProperty("5. volume").GetString();
				points.Add(new StockSeriesPoint
				{
					Timestamp = ts,
					Open = decimal.TryParse(o, out var od) ? od : 0,
					High = decimal.TryParse(h, out var hd) ? hd : 0,
					Low = decimal.TryParse(l, out var ld) ? ld : 0,
					Close = decimal.TryParse(c, out var cd) ? cd : 0,
					Volume = long.TryParse(v, out var vl) ? vl : 0
				});
			}

			points.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
			return new StockSeries { Symbol = symbol.ToUpperInvariant(), Interval = interval, LastRefreshed = lastRefreshed, Points = points };
		}

		public async Task<StockSeries?> GetDailyAsync(string symbol, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("Alpha Vantage API key is missing.");
			var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={Uri.EscapeDataString(symbol)}&outputsize=compact&apikey={_apiKey}";
			using var json = await GetJsonAsync(url, ct);
			if (json == null) return null;
			if (!json.RootElement.TryGetProperty("Time Series (Daily)", out var seriesEl))
			{
				_logger.LogWarning("Alpha Vantage daily missing series for {Symbol}", symbol);
				return null;
			}
			DateTime lastRefreshed = DateTime.UtcNow;
			if (json.RootElement.TryGetProperty("Meta Data", out var meta) && meta.TryGetProperty("3. Last Refreshed", out var last))
			{
				DateTime.TryParse(last.GetString(), out lastRefreshed);
			}
			var points = new List<StockSeriesPoint>();
			foreach (var kv in seriesEl.EnumerateObject())
			{
				if (!DateTime.TryParse(kv.Name, out var ts)) continue;
				var o = kv.Value.GetProperty("1. open").GetString();
				var h = kv.Value.GetProperty("2. high").GetString();
				var l = kv.Value.GetProperty("3. low").GetString();
				var c = kv.Value.GetProperty("4. close").GetString();
				var v = kv.Value.GetProperty("5. volume").GetString();
				points.Add(new StockSeriesPoint
				{
					Timestamp = ts,
					Open = decimal.TryParse(o, out var od) ? od : 0,
					High = decimal.TryParse(h, out var hd) ? hd : 0,
					Low = decimal.TryParse(l, out var ld) ? ld : 0,
					Close = decimal.TryParse(c, out var cd) ? cd : 0,
					Volume = long.TryParse(v, out var vl) ? vl : 0
				});
			}
			points.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
			return new StockSeries { Symbol = symbol.ToUpperInvariant(), Interval = "1day", LastRefreshed = lastRefreshed, Points = points };
		}

		public async Task<StockQuote?> GetGlobalQuoteAsync(string symbol, CancellationToken ct = default)
		{
			var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={_apiKey}";
			using var json = await GetJsonAsync(url, ct);
			if (json == null) return null;
			if (!json.RootElement.TryGetProperty("Global Quote", out var quote)) return null;
			var ps = quote.TryGetProperty("05. price", out var priceEl) ? priceEl.GetString() : null;
			var cs = quote.TryGetProperty("10. change percent", out var cpEl) ? cpEl.GetString() : null;
			decimal price = 0;
			decimal changePct = 0;
			decimal.TryParse(ps, out price);
			if (!string.IsNullOrWhiteSpace(cs))
			{
				cs = cs!.Trim().TrimEnd('%');
				decimal.TryParse(cs, out changePct);
			}
			return new StockQuote { Symbol = symbol.ToUpperInvariant(), Price = price, ChangePercent = changePct };
		}

		public async Task<string?> ResolveSymbolAsync(string query, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(query)) return null;
			var url = $"https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords={Uri.EscapeDataString(query)}&apikey={_apiKey}";
			using var json = await GetJsonAsync(url, ct);
			if (json == null) return null;
			if (!json.RootElement.TryGetProperty("bestMatches", out var matches) || matches.ValueKind != JsonValueKind.Array || matches.GetArrayLength() == 0) return null;
			var first = matches[0];
			if (first.TryGetProperty("1. symbol", out var symEl))
			{
				return symEl.GetString();
			}
			return null;
		}
	}
} 