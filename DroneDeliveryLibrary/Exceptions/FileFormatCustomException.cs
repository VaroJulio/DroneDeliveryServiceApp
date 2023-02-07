using System;

namespace DroneDeliveryLibrary.Exceptions
{
    public class FileFormatCustomException : Exception
    {
        public FileFormatCustomException(string message) : base(message) { }
    }
}
