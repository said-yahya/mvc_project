using System;

namespace MVC_Project.Models
{
    public class TelegramUpdate
    {
        public long ChatId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }

    public class TelegramResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}