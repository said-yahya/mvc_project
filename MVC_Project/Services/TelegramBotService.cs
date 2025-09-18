using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using MVC_Project.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MVC_Project.Services;

namespace MVC_Project.Services
{
    public interface ITelegramBotService
    {
        Task<bool> SendWeatherMessage(long chatId, string cityName);
        Task<bool> SendWelcomeMessage(long chatId);
        Task<bool> SendHelpMessage(long chatId);
        Task ProcessUpdate(Update update);
    }

    public class TelegramBotService : ITelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly WeatherService _weatherService;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly string _botToken;

        public TelegramBotService(
            IConfiguration configuration,
            WeatherService weatherService,
            ILogger<TelegramBotService> logger)
        {
            _botToken = "8316488502:AAG03iMaaZhJR2wNgALQJf32wfG7GcXRht0";
            _botClient = new TelegramBotClient(_botToken);
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task ProcessUpdate(Update update)
        {
            try
            {
                if (update.Message?.Text == null) return;

                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text.Trim();

                _logger.LogInformation($"Received message: {messageText} from chat: {chatId}");

                if (messageText.StartsWith("/start"))
                {
                    await SendWelcomeMessage(chatId);
                }
                else if (messageText.StartsWith("/help"))
                {
                    await SendHelpMessage(chatId);
                }
                else if (messageText.StartsWith("/weather "))
                {
                    var cityName = messageText.Substring(9).Trim();
                    await SendWeatherMessage(chatId, cityName);
                }
                else
                {
                    // Treat as city name
                    await SendWeatherMessage(chatId, messageText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Telegram update");
            }
        }

        public async Task<bool> SendWelcomeMessage(long chatId)
        {
            try
            {
                var welcomeText = "🌤️ <b>Hoş geldiniz! Yahya Hava Durumu Bot'una!</b>\n\n" +
                               "Herhangi bir şehrin hava durumunu öğrenebilirsiniz.\n\n" +
                               "📍 <b>Nasıl kullanılır:</b>\n" +
                               "• Şehir adı gönderin (örn: 'İstanbul')\n" +
                               "• /weather [şehir adı] komutunu kullanın\n" +
                               "• /help ile daha fazla bilgi alın\n\n" +
                               "Şimdi bir şehir adı göndermeyi deneyin! 🏙️";

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "İstanbul", "Ankara" },
                    new KeyboardButton[] { "İzmir", "Bursa" },
                    new KeyboardButton[] { "Antalya", "Adana" },
                    new KeyboardButton[] { "London", "New York" }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                };

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: welcomeText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome message");
                return false;
            }
        }

        public async Task<bool> SendHelpMessage(long chatId)
        {
            try
            {
                var helpText = "🆘 <b>Yardım - Yahya Hava Durumu Bot</b>\n\n" +
                              "<b>Mevcut komutlar:</b>\n" +
                              "🔸 /start - Bot'u başlatın ve hoş geldin mesajını görün\n" +
                              "🔸 /help - Bu yardım mesajını göster\n" +
                              "🔸 /weather [şehir] - Belirli şehir için hava durumunu al\n\n" +
                              "💡 <b>Doğrudan şehir adı da gönderebilirsiniz!</b>\n\n" +
                              "<b>Örnekler:</b>\n" +
                              "• İstanbul\n" +
                              "• /weather London\n" +
                              "• New York\n\n" +
                              "🌡️ <b>Hava durumu bilgileri içerir:</b>\n" +
                              "• Güncel sıcaklık\n" +
                              "• Hava durumu\n" +
                              "• Nem oranı\n" +
                              "• Rüzgar hızı\n" +
                              "• Son güncellenme zamanı";

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: helpText,
                    parseMode: ParseMode.Html);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending help message");
                return false;
            }
        }

        public async Task<bool> SendWeatherMessage(long chatId, string cityName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cityName))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Lütfen bir şehir adı belirtin.\nÖrnek: İstanbul veya /weather İstanbul");
                    return false;
                }

                // Send typing action
                await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                // Use your existing weather service method
                var weatherData = await _weatherService.GetCurrentWeatherAsync(cityName);

                if (weatherData != null)
                {
                    var weatherMessage = FormatWeatherMessage(weatherData);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: weatherMessage,
                        parseMode: ParseMode.Html);
                    return true;
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"❌ Üzgünüm, '{cityName}' için hava durumu bilgisi bulamadım.\n" +
                              "Şehir adını kontrol edip tekrar deneyin.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending weather message for city: {cityName}");
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "⚠️ Hava durumu bilgisini alırken bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return false;
            }
        }

        private string FormatWeatherMessage(CurrentWeather weatherData)
        {
            // Format based on your existing CurrentWeather model
            return $"🌤️ <b>{weatherData.City} Hava Durumu</b>\n\n" +
                   $"🌡️ <b>Sıcaklık:</b> {weatherData.TemperatureC}°C\n" +
                   $"☁️ <b>Durum:</b> {weatherData.Condition}\n" +
                   $"💧 <b>Nem:</b> {weatherData.Humidity}%\n" +
                   $"💨 <b>Rüzgar:</b> {weatherData.WindSpeedKmh} km/h\n" +
                   $"🌧️ <b>Yağış:</b> {weatherData.PrecipitationMm} mm\n\n" +
                   $"🕐 <i>Son güncelleme: {weatherData.LastUpdated:dd.MM.yyyy HH:mm}</i>";
        }
    }
}