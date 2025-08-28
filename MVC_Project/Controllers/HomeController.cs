using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVC_Project.Models;
using MVC_Project.Data;
using Microsoft.AspNetCore.Authorization;
using MVC_Project.Services;

namespace MVC_Project.Controllers;

[Authorize]
public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;
	private readonly ApplicationDbContext _context;
	private readonly AlphaVantageService _alpha;

	public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, AlphaVantageService alpha)
	{
		_logger = logger;
		_context = context;
		_alpha = alpha;
	}

	public IActionResult Index()
	{
		return View();
	}

	[HttpGet]
	public async Task<IActionResult> Resolve(string q)
	{
		var symbol = await _alpha.ResolveSymbolAsync(q);
		if (string.IsNullOrWhiteSpace(symbol)) return NotFound();
		return Json(new { symbol });
	}

	[HttpGet]
	public async Task<IActionResult> Intraday(string symbol = "MSFT", string interval = "5min")
	{
		try
		{
			var series = await _alpha.GetIntradayAsync(symbol, interval);
			if (series == null) return NotFound();
			Console.WriteLine($"Intraday fetched: {symbol} {interval} -> {series.Points.Count} points, last {series.LastRefreshed:o}");
			return Json(series);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching intraday for {Symbol} {Interval}", symbol, interval);
			return StatusCode(500, new { error = "Failed to fetch stock data" });
		}
	}

	[HttpGet]
	public async Task<IActionResult> Range(string symbol = "MSFT", string range = "1D")
	{
		try
		{
			StockSeries? series;
			switch (range.ToUpperInvariant())
			{
				case "1D":
					series = await _alpha.GetIntradayAsync(symbol, "5min");
					if (series == null) series = await _alpha.GetDailyAsync(symbol);
					if (series != null) series = new StockSeries { Symbol = series.Symbol, Interval = series.Interval, LastRefreshed = series.LastRefreshed, Points = series.Points.TakeLast(1).ToList().Concat(series.Points.TakeLast(6)).ToList() };
					break;
				case "1W":
					series = await _alpha.GetDailyAsync(symbol);
					if (series != null) series = new StockSeries { Symbol = series.Symbol, Interval = series.Interval, LastRefreshed = series.LastRefreshed, Points = series.Points.TakeLast(5).ToList() };
					break;
				case "1M":
					series = await _alpha.GetDailyAsync(symbol);
					if (series != null) series = new StockSeries { Symbol = series.Symbol, Interval = series.Interval, LastRefreshed = series.LastRefreshed, Points = series.Points.TakeLast(22).ToList() };
					break;
				default:
					series = await _alpha.GetIntradayAsync(symbol, "5min");
					break;
			}
			if (series == null) return NotFound();
			Console.WriteLine($"Range fetched: {symbol} {range} -> {series.Points.Count} points, last {series.LastRefreshed:o}");
			return Json(series);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching range for {Symbol} {Range}", symbol, range);
			return StatusCode(500, new { error = "Failed to fetch stock data" });
		}
	}

	[HttpGet]
	public async Task<IActionResult> Popular()
	{
		var symbols = new[] { "AAPL","MSFT","GOOGL","AMZN","META","TSLA","NVDA","NFLX","IBM","ORCL" };
		var tasks = symbols.Select(async s => new { Symbol = s, Quote = await _alpha.GetGlobalQuoteAsync(s) });
		var results = await Task.WhenAll(tasks);
		Console.WriteLine("Popular fetched: " + string.Join(", ", results.Select(r => $"{r.Symbol}:{(r.Quote?.Price.ToString() ?? "-")}")));
		return Json(results.Select(r => new { symbol = r.Symbol, price = r.Quote?.Price ?? 0m, changePercent = r.Quote?.ChangePercent ?? 0m }));
	}

	public IActionResult Privacy()
	{
		return View();
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
