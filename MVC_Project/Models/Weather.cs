using System;

namespace MVC_Project.Models
{
	// Simple weather row table
	public sealed class WeatherRow
	{
		public string City { get; set; } = string.Empty;
		public DateTime Date { get; set; }
		public decimal TemperatureC { get; set; }
		public int Humidity { get; set; }
		public decimal PrecipitationMm { get; set; }
	}

	// Current weather data for the main dashboard card
	public sealed class CurrentWeather
	{
		public string City { get; set; } = string.Empty;
		public decimal TemperatureC { get; set; }
		public string Condition { get; set; } = string.Empty;
		public string ConditionIcon { get; set; } = string.Empty;
		public bool IsDay { get; set; }
		public int Humidity { get; set; }
		public decimal PrecipitationMm { get; set; }
		public decimal WindSpeedKmh { get; set; }
		public DateTime LastUpdated { get; set; }
	}

	// Hourly data point for charts
	public sealed class HourlyData
	{
		public string Time { get; set; } = string.Empty;
		public decimal Temperature { get; set; }
		public decimal Precipitation { get; set; }
		public decimal WindSpeed { get; set; }
	}

	// 7-day forecast data
	public sealed class ForecastDay
	{
		public string DayName { get; set; } = string.Empty;
		public string Condition { get; set; } = string.Empty;
		public string ConditionIcon { get; set; } = string.Empty;
		public decimal HighTemp { get; set; }
		public decimal LowTemp { get; set; }
	}
} 