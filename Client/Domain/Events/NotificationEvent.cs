using Client.Domain.Enums;
using System;

namespace Client.Domain.Events
{
    public class NotificationEvent : EventInterface
    {
        public string Message { get; }
        public NotificationLevel Level { get; }
        public DateTime Timestamp { get; }

        public NotificationEvent(string message, NotificationLevel level)
        {
            Message = message;
            Level = level;
            Timestamp = DateTime.Now;
        }
    }
}
