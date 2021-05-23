using Azure.Storage.Queues;
using Notification.Shared.Models;
using QueuedJobs.Abstract;

namespace Notification.Demo.Services
{
    /// <summary>
    /// provides central access to all queues in the application that share a connection string
    /// </summary>
    public class QueueManager : QueueClientHelperBase
    {
        public QueueManager(string connectionString) : base(connectionString)
        {                
        }     

        public QueueClient ZipBuilder => this[JobTracker.ZipBuilderQueue];
    }
}
