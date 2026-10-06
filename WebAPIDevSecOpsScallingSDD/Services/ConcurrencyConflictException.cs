using System;

namespace WebAPIDevSecOpsScallingSDD.Services
{
    public sealed class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException()
            : base("The record was modified by another process.")
        {
        }

        public ConcurrencyConflictException(string message)
            : base(message)
        {
        }

        public ConcurrencyConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
