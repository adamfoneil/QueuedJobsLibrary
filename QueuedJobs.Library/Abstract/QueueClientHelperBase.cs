using Azure.Storage.Queues;
using System.Collections.Generic;

namespace QueuedJobs.Abstract
{
    /// <summary>
    /// provides central access to all queues in the application that share a connection string.
    /// Extend with your own QueueClient objects that wrap this[string]
    /// </summary>
    public abstract class QueueClientHelperBase
    {
        private readonly string _storageConnection;
        private readonly Dictionary<string, QueueClient> _queues;

        public QueueClientHelperBase(string connectionString)
        {
            _storageConnection = connectionString;
            _queues = new Dictionary<string, QueueClient>();
        }

        public QueueClient this[string queueName]
        {
            get
            {
                if (!_queues.ContainsKey(queueName))
                {
                    _queues.Add(queueName, new QueueClient(_storageConnection, queueName));
                }

                return _queues[queueName];
            }
        }
    }
}
