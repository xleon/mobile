using System;
using System.Runtime.Serialization;

namespace Toggl.Phoebe.Data.Json.Converters
{
    [Serializable]
    public class RelationRemoteIdMissingException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:LocalOnlyRelationException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        public RelationRemoteIdMissingException (Type type, Guid id) : base (MakeMessage (type, id))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LocalOnlyRelationException"/> class
        /// </summary>
        /// <param name="context">The contextual information about the source or destination.</param>
        /// <param name="info">The object that holds the serialized object data.</param>
        protected RelationRemoteIdMissingException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }

        private static string MakeMessage (Type type, Guid id)
        {
            return String.Format (
                       "Cannot export data with local-only relation ({0}#{1}) to JSON.",
                       type.Name, id
                   );
        }
    }
}
