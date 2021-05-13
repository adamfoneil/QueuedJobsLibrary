using AO.Models.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace QueuedJobs.Models
{
    public enum Status
    {
        Pending, // job queued, but not started yet
        Running, // job is currently running
        Succeeded, // no errors in QueuedJobBase.OnExecuteAsync
        Failed, // error in QueuedJobBase.OnExecuteAsync
        Aborted // job failed to queue (bad Azure connection string?)
    }

    /// <summary>
    /// represents a queued job that we want to capture request and result info,
    /// and enable notifications to display. This would normally be implemented
    /// as a database table (but it really could be anything storable)
    /// </summary>    
    public abstract class QueuedJob<TKey> : IModel<TKey>
    {
        public TKey Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; }
        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; }
        public string RequestData { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Started { get; set; }
        public DateTime? Completed { get; set; }
        public int? Duration { get; set; } // in seconds
        public Status Status { get; set; }
        public bool IsCleared { get; set; } // records that the user cleared this notification
        public string ResultData { get; set; }
        public string ExceptionData { get; set; }
        public int RetryCount { get; set; }

        public bool IsRunning => Status == Status.Running;
    }
}
