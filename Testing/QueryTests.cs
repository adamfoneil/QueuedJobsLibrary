using Dapper.QX;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notification.Demo.Queries;
using SqlServer.LocalDb;

namespace Testing
{
    [TestClass]
    public class QueryTests
    {
        [TestMethod]
        public void JobDashboard() => QueryHelper.Test<JobDashboard>(GetConnection);

        private SqlConnection GetConnection() => LocalDb.GetConnection("NotifyDemo");
    }
}
