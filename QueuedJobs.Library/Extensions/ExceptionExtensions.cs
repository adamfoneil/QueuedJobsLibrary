using System;

namespace QueuedJobs.Extensions
{
    public static class ExceptionExtensions
    {
        // combines all inner exception messages into one message
        public static string FullMessage(this Exception exception)
        {
            string result = exception.Message;
            
            if (exception.InnerException !=  null)
            {
                int indent = 0;
                var inner = exception.InnerException;
                while (inner != null)
                {

                    indent += 2;
                    result += $"\r\n{new string(' ', indent)}- {inner.Message}";
                    inner = inner.InnerException;
                }
            }

            return result;
        }
    }
}
