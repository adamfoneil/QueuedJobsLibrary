using AO.Models.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Notification.Shared.Models
{
    [Table("JobTracker", Schema = "queue")]
    public class JobTracker : BackgroundJobInfo<int>
    {
        public const string ZipBuilderQueue = "zip-builder";

        protected override T DeserializeJson<T>(string json) => JsonSerializer.Deserialize<T>(json);        
    }
}
