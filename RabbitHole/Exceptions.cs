using System;

namespace RabbitHole
{
    [Serializable]
    public class ParseDestinationException : Exception
    {
        public ParseDestinationException() : base() { }
        public ParseDestinationException(string message) : base(message) { }
    }
}