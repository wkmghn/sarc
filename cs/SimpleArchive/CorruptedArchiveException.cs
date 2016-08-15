using System;

namespace SimpleArchive
{
    public class CorruptedArchiveException : Exception
    {
        public CorruptedArchiveException() : base() { }
        public CorruptedArchiveException(string message) : base(message) { }
        public CorruptedArchiveException(string message, Exception innerException) : base(message, innerException) { }
    }
}
