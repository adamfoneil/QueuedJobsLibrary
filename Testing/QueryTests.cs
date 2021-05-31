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

        /// <summary>
        /// note I'm actually using Sendhook Azure for the real demo
        /// </summary>
        private SqlConnection GetConnection() => LocalDb.GetConnection("NotifyDemo");
    }
}
