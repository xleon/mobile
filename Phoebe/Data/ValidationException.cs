using System;
using System.Runtime.Serialization;

namespace Toggl.Phoebe.Data
{
    
    [Serializable]
    public class ValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:ValidationException"/> class
        /// </summary>
        public ValidationException ()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ValidationException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        public ValidationException (string message) : base (message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ValidationException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        /// <param name="inner">The exception that is the cause of the current exception. </param>
        public ValidationException (string message, Exception inner) : base (message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ValidationException"/> class
        /// </summary>
        /// <param name="context">The contextual information about the source or destination.</param>
        /// <param name="info">The object that holds the serialized object data.</param>
        protected ValidationException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
    }
}
