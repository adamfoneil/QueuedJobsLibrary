using Notification.Shared.Requests;
using Notification.Shared.Responses;
using QueuedJobs.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Notification.Shared.Models
{
    [Table("JobTracker", Schema = "queue")]
    public class JobTracker : BackgroundJobInfo<int>
    {
        public const string ZipBuilderQueue = "zip-builder";
        
        public bool IsZipRequest(out ZipRequest request, out ZipResult result)
        {
            if (!IsCompleted)
            {
                request = null;
                result = null;
                return false;
            }

            if (RequestType.Equals(nameof(ZipRequest)))
            {
                request = JsonSerializer.Deserialize<ZipRequest>(RequestData);
                result = JsonSerializer.Deserialize<ZipResult>(ResultData);
                return true;
            }

            request = null;
            result = null;
            return false;
        }
    }
}
