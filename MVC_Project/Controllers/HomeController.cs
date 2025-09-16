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
	private readonly WeatherService _weather;

	public HomeController(ILogger<HomeController> logger, WeatherService weather)
	{
		_logger = logger;
		_weather = weather;
	}

	/// <summary>
	/// Main dashboard page - displays weather dashboard and city table
	/// GET: /Home/Index
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> Index()
	{
		// Fetch data for the bottom panel table (10 Turkish cities)
		var rows = await _weather.GetTurkeySampleAsync();
		return View(rows);
	}

	/// <summary>
	/// API endpoint to get current weather data for a specific city
	/// Used by the dashboard to fetch weather when user enters a city name
	/// GET: /Home/Current?city=Istanbul
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> Current(string city)
	{
		if (string.IsNullOrWhiteSpace(city))
		{
			return BadRequest(new { error = "City parameter is required" });
		}

		try
		{
			var currentWeather = await _weather.GetCurrentWeatherAsync(city);
			if (currentWeather == null)
			{
				return NotFound(new { error = $"Weather data not found for {city}" });
			}

			_logger.LogInformation("Current weather fetched for {City}: {Temp}°C", city, currentWeather.TemperatureC);
			return Json(currentWeather);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching current weather for {City}", city);
			return StatusCode(500, new { error = "Failed to fetch weather data" });
		}
	}

	/// <summary>
	/// API endpoint to get hourly forecast data for charts
	/// Used by the dashboard to populate temperature/precipitation/wind charts
	/// GET: /Home/Hourly?city=Istanbul
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> Hourly(string city)
	{
		if (string.IsNullOrWhiteSpace(city))
		{
			return BadRequest(new { error = "City parameter is required" });
		}

		try
		{
			var hourlyData = await _weather.GetHourlyForecastAsync(city);
			_logger.LogInformation("Hourly forecast fetched for {City}: {Count} data points", city, hourlyData.Count);
			return Json(hourlyData);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching hourly forecast for {City}", city);
			return StatusCode(500, new { error = "Failed to fetch hourly forecast" });
		}
	}

	/// <summary>
	/// API endpoint to get 7-day forecast data
	/// Used by the dashboard to populate the weekly forecast section
	/// GET: /Home/Weekly?city=Istanbul
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> Weekly(string city)
	{
		if (string.IsNullOrWhiteSpace(city))
		{
			return BadRequest(new { error = "City parameter is required" });
		}

		try
		{
			var weeklyData = await _weather.GetWeeklyForecastAsync(city);
			_logger.LogInformation("Weekly forecast fetched for {City}: {Count} days", city, weeklyData.Count);
			return Json(weeklyData);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching weekly forecast for {City}", city);
			return StatusCode(500, new { error = "Failed to fetch weekly forecast" });
		}
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
