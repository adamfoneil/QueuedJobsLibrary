using AO.Models.Models;
using Dapper.QX.Abstract;
using Dapper.QX.Attributes;
using Dapper.QX.Interfaces;
using Notification.Shared.Models;
using System;
using System.Collections.Generic;

namespace Notification.Demo.Queries
{
    public class JobDashboard : TestableQuery<JobTracker>
    {
        public JobDashboard() : base("SELECT * FROM [queue].[JobTracker] {where} ORDER BY [Created] DESC {offset}")
        {
        }

        [Offset(10)]
        public int? Page { get; set; }

        [Where("[UserName]=@userName")]
        public string UserName { get; set; }

        [Where("[RequestType]=@requestType")]
        public string RequestType { get; set; }

        [Where("CONVERT(date, [Created])>=@fromDate")]
        public DateTime? FromDate { get; set; }

        [Where("CONVERT(date, [Created])<=@throughDate")]
        public DateTime? ThroughDate { get; set; }

        [Where("[Status]=@status")]
        public JobStatus? Status { get; set; }

        /// <summary>
        /// this is for Blazor binding
        /// </summary>
        [Where("[Status]=@statusId")]
        public int? StatusId { get; set; }

        protected override IEnumerable<ITestableQuery> GetTestCasesInner()
        {
            yield return new JobDashboard() { Page = 1 };
            yield return new JobDashboard() { UserName = "jojo" };
            yield return new JobDashboard() { RequestType = "whatever" };
            yield return new JobDashboard() { FromDate = DateTime.Now };
            yield return new JobDashboard() { ThroughDate = DateTime.Now };
            yield return new JobDashboard() { Status = JobStatus.Succeeded };
            yield return new JobDashboard() { StatusId = 1 };
        }
    }
}
