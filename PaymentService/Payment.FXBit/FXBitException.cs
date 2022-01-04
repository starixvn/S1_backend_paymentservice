using System;
using System.Runtime.Serialization;

namespace Payment.FXBit
{
    internal class FXBitException : Exception
    {
        public FXBitException()
        {
        }

        public FXBitException(string message)
            : base(message)
        {
        }

        public FXBitException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public FXBitException(string message, string code)
            : base(message)
        {
            Code = code;
        }

        protected FXBitException(SerializationInfo info, StreamingContext context)
        {
        }

        /// <summary>
        /// Error Code
        /// </summary>
        public string Code { get; set; }
    }
}