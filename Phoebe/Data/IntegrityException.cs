using System;
using System.Runtime.Serialization;

namespace Toggl.Phoebe.Data
{
    /// <summary>
    /// Integrity exception is used to indicate that particular change to the data would break the integrity
    /// of the database.
    /// </summary>
    [Serializable]
    public class IntegrityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IntegrityException"/> class
        /// </summary>
        public IntegrityException ()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IntegrityException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        public IntegrityException (string message) : base (message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IntegrityException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        /// <param name="inner">The exception that is the cause of the current exception. </param>
        public IntegrityException (string message, Exception inner) : base (message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IntegrityException"/> class
        /// </summary>
        /// <param name="context">The contextual information about the source or destination.</param>
        /// <param name="info">The object that holds the serialized object data.</param>
        protected IntegrityException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
    }
}
