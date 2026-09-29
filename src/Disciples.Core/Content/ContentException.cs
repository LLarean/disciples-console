using System;

namespace Disciples.Core.Content
{
    /// <summary>Content, scenario or save data is inconsistent.</summary>
    public sealed class ContentException : Exception
    {
        public ContentException(string message) : base(message)
        {
        }
    }
}
