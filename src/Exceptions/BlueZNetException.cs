using System;

namespace BlueZNet.Exceptions
{
    /// <summary>
    /// Represents errors that occur during Bluetooth operations.
    /// </summary>
    public class BlueZNetException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlueZNetException"/> class.
        /// </summary>
        public BlueZNetException() : base("A Bluetooth operation failed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlueZNetException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BlueZNetException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlueZNetException"/> class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public BlueZNetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
