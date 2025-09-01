using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_Project.Services;

namespace MVC_Project.Controllers
{
	[Authorize]
	public class WeatherController : Controller
	{
		private readonly WeatherService _weather;
		private readonly ILogger<WeatherController> _logger;

		public WeatherController(WeatherService weather, ILogger<WeatherController> logger)
		{
			_weather = weather;
			_logger = logger;
		}

		/// <summary>
		/// Main weather page - displays both the dashboard and the city table
		/// GET: /Weather/Index
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
		/// GET: /Weather/Current?city=Istanbul
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
		/// GET: /Weather/Hourly?city=Istanbul
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
		/// GET: /Weather/Weekly?city=Istanbul
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
	}
} 