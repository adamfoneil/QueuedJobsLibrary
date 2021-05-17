using Azure.Storage.Queues;
using Microsoft.AspNetCore.Components;
using Notification.Shared.Models;
using System.Collections.Generic;

namespace Notification.Demo.Services
{
    /// <summary>
    /// provides central access to all queues in the application that share a connection string
    /// </summary>
    public class QueueManager
    {
        private readonly string _storageConnection;
        private readonly Dictionary<string, QueueClient> _queues;        

        public QueueManager(string connectionString)
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

        public QueueClient ZipBuilder => this[JobTracker.ZipBuilderQueue];
    }
}
