using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using MVC_Project.Services;

namespace MVC_Project.Services
{
    public class TelegramPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramPollingService> _logger;
        private readonly TelegramBotClient _botClient;

        public TelegramPollingService(
            IServiceProvider serviceProvider,
            ILogger<TelegramPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _botClient = new TelegramBotClient("8316488502:AAG03iMaaZhJR2wNgALQJf32wfG7GcXRht0");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: stoppingToken
            );

            var me = await _botClient.GetMeAsync(stoppingToken);
            _logger.LogInformation($"Telegram bot {me.Username} started listening");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
            
            await telegramService.ProcessUpdate(update);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram bot error occurred");
            return Task.CompletedTask;
        }
    }
}