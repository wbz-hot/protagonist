using System;
using System.Collections.Generic;

namespace Engine.Messaging
{
    public interface IMessagingEvent
    {
        /// <summary>
        /// Gets the type of MessagingEvent.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Gets the date this message was created.
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// Gets the type of this message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// A collection of additional parameters associated with event. 
        /// </summary>
        /// <remarks>this could be an Asset but with stringX and numberX rather than ReferenceX and NumberReferenceX</remarks>
        Dictionary<string, string> Params { get; }
    }
}