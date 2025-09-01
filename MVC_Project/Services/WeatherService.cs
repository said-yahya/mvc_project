using System.Text.Json;
using MVC_Project.Models;

namespace MVC_Project.Services
{
	public sealed class WeatherService
	{
		private readonly HttpClient _http;
		private readonly string _apiKey;
		private readonly ILogger<WeatherService> _logger;

		public WeatherService(HttpClient http, IConfiguration cfg, ILogger<WeatherService> logger)
		{
			_http = http;
			_apiKey = cfg["WeatherApi:ApiKey"] ?? string.Empty;
			_logger = logger;
		}

		/// <summary>
		/// Fetches current weather data for 10 major Turkish cities
		/// Used for the bottom panel table display
		/// </summary>
		public async Task<IReadOnlyList<WeatherRow>> GetTurkeySampleAsync(CancellationToken ct = default)
		{
			var cities = new[] { "Istanbul","Ankara","Izmir","Bursa","Adana","Gaziantep","Konya","Antalya","Kayseri","Mersin" };
			var tasks = cities.Select(async city => await GetCurrentAsync(city, ct));
			var results = await Task.WhenAll(tasks);
			return results.Where(r => r != null).Select(r => r!).ToList();
		}

		/// <summary>
		/// Fetches comprehensive current weather data for a specific city
		/// Used for the main dashboard card display
		/// </summary>
		public async Task<CurrentWeather?> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("WeatherAPI key missing");
			var url = $"https://api.weatherapi.com/v1/current.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&aqi=no";
			using var req = new HttpRequestMessage(HttpMethod.Get, url);
			using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
			if (!resp.IsSuccessStatusCode)
			{
				_logger.LogWarning("WeatherAPI failed for {City}: {Status}", city, resp.StatusCode);
				return null;
			}
			await using var stream = await resp.Content.ReadAsStreamAsync(ct);
			using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
			var root = json.RootElement;
			var location = root.GetProperty("location");
			var current = root.GetProperty("current");
			var condition = current.GetProperty("condition");
			
			var name = location.GetProperty("name").GetString() ?? city;
			var epoch = current.GetProperty("last_updated_epoch").GetInt64();
			var tempC = current.GetProperty("temp_c").GetDecimal();
			var humidity = current.GetProperty("humidity").GetInt32();
			var precipMm = current.GetProperty("precip_mm").GetDecimal();
			var windKmh = current.GetProperty("wind_kph").GetDecimal();
			var isDay = current.GetProperty("is_day").GetInt32() == 1;
			var conditionText = condition.GetProperty("text").GetString() ?? "Unknown";
			var conditionIcon = condition.GetProperty("icon").GetString() ?? "";
			
			return new CurrentWeather 
			{ 
				City = name, 
				LastUpdated = DateTimeOffset.FromUnixTimeSeconds(epoch).DateTime,
				TemperatureC = tempC, 
				Humidity = humidity, 
				PrecipitationMm = precipMm,
				WindSpeedKmh = windKmh,
				IsDay = isDay,
				Condition = conditionText,
				ConditionIcon = conditionIcon
			};
		}

		/// <summary>
		/// Fetches hourly forecast data for the next 24 hours
		/// Used for the temperature/precipitation/wind charts
		/// </summary>
		public async Task<IReadOnlyList<HourlyData>> GetHourlyForecastAsync(string city, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("WeatherAPI key missing");
			var url = $"https://api.weatherapi.com/v1/forecast.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&days=1&aqi=no";
			using var req = new HttpRequestMessage(HttpMethod.Get, url);
			using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
			if (!resp.IsSuccessStatusCode)
			{
				_logger.LogWarning("WeatherAPI hourly forecast failed for {City}: {Status}", city, resp.StatusCode);
				return new List<HourlyData>();
			}
			await using var stream = await resp.Content.ReadAsStreamAsync(ct);
			using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
			var root = json.RootElement;
			var forecast = root.GetProperty("forecast");
			var forecastday = forecast.GetProperty("forecastday")[0];
			var hour = forecastday.GetProperty("hour");
			
			var hourlyData = new List<HourlyData>();
			foreach (var hourData in hour.EnumerateArray())
			{
				var time = hourData.GetProperty("time").GetString() ?? "";
				var timeOnly = DateTime.Parse(time).ToString("HH:mm");
				var temp = hourData.GetProperty("temp_c").GetDecimal();
				var precip = hourData.GetProperty("precip_mm").GetDecimal();
				var wind = hourData.GetProperty("wind_kph").GetDecimal();
				
				hourlyData.Add(new HourlyData 
				{ 
					Time = timeOnly, 
					Temperature = temp, 
					Precipitation = precip, 
					WindSpeed = wind 
				});
			}
			return hourlyData;
		}

		/// <summary>
		/// Fetches 7-day forecast data
		/// Used for the weekly forecast display
		/// </summary>
		public async Task<IReadOnlyList<ForecastDay>> GetWeeklyForecastAsync(string city, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("WeatherAPI key missing");
			var url = $"https://api.weatherapi.com/v1/forecast.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&days=7&aqi=no";
			using var req = new HttpRequestMessage(HttpMethod.Get, url);
			using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
			if (!resp.IsSuccessStatusCode)
			{
				_logger.LogWarning("WeatherAPI weekly forecast failed for {City}: {Status}", city, resp.StatusCode);
				return new List<ForecastDay>();
			}
			await using var stream = await resp.Content.ReadAsStreamAsync(ct);
			using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
			var root = json.RootElement;
			var forecast = root.GetProperty("forecast");
			var forecastday = forecast.GetProperty("forecastday");
			
			var weeklyData = new List<ForecastDay>();
			foreach (var day in forecastday.EnumerateArray())
			{
				var date = day.GetProperty("date").GetString() ?? "";
				var dayName = DateTime.Parse(date).ToString("ddd");
				var dayData = day.GetProperty("day");
				var condition = dayData.GetProperty("condition");
				
				var highTemp = dayData.GetProperty("maxtemp_c").GetDecimal();
				var lowTemp = dayData.GetProperty("mintemp_c").GetDecimal();
				var conditionText = condition.GetProperty("text").GetString() ?? "Unknown";
				var conditionIcon = condition.GetProperty("icon").GetString() ?? "";
				
				weeklyData.Add(new ForecastDay 
				{ 
					DayName = dayName, 
					HighTemp = highTemp, 
					LowTemp = lowTemp,
					Condition = conditionText,
					ConditionIcon = conditionIcon
				});
			}
			return weeklyData;
		}

		/// <summary>
		/// Fetches basic current weather data for the existing table
		/// Legacy method for the bottom panel
		/// </summary>
		private async Task<WeatherRow?> GetCurrentAsync(string city, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("WeatherAPI key missing");
			var url = $"https://api.weatherapi.com/v1/current.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&aqi=no";
			using var req = new HttpRequestMessage(HttpMethod.Get, url);
			using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
			if (!resp.IsSuccessStatusCode)
			{
				_logger.LogWarning("WeatherAPI failed for {City}: {Status}", city, resp.StatusCode);
				return null;
			}
			await using var stream = await resp.Content.ReadAsStreamAsync(ct);
			using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
			var root = json.RootElement;
			var location = root.GetProperty("location");
			var current = root.GetProperty("current");
			var name = location.GetProperty("name").GetString() ?? city;
			var epoch = current.GetProperty("last_updated_epoch").GetInt64();
			var tempC = current.GetProperty("temp_c").GetDecimal();
			var humidity = current.GetProperty("humidity").GetInt32();
			var precipMm = current.GetProperty("precip_mm").GetDecimal();
			return new WeatherRow { City = name, Date = DateTimeOffset.FromUnixTimeSeconds(epoch).DateTime, TemperatureC = tempC, Humidity = humidity, PrecipitationMm = precipMm };
		}
	}
} 