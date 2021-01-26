using System;

namespace RabbitHole
{
    public abstract class Message
    {
        protected Message()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; }
        public DateTime CreatedAt { get; }
    }
}