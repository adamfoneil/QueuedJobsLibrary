using QueuedJobs.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notification.Shared.Models
{
    [Table("JobTracker", Schema = "queue")]
    public class JobTracker : QueuedJob<int>
    {
    }
}
