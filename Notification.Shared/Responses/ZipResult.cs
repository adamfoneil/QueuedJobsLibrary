using System;

namespace Notification.Shared.Responses
{
    public class ZipResult
    {
        /// <summary>
        /// public URL for accessing zip file
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// when does link stop working?
        /// </summary>
        public DateTime ExpiresAfter { get; set; }
    }
}
