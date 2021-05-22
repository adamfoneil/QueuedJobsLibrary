using QueuedJobs.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Notification.Shared.Models
{
    [Table("JobTracker", Schema = "queue")]
    public class JobTracker : BackgroundJobInfo<int>
    {
        public const string ZipBuilderQueue = "zip-builder";
        
        public bool IsJobType<TRequest, TResult>(out TRequest request, out TResult result)
        {
            if (!IsCompleted)
            {
                request = default;
                result = default;
                return false;
            }

            if (RequestType.Equals(typeof(TRequest).Name))
            {
                request = JsonSerializer.Deserialize<TRequest>(RequestData);
                result = JsonSerializer.Deserialize<TResult>(ResultData);
                return true;
            }

            request = default;
            result = default;
            return false;
        }
    }
}
