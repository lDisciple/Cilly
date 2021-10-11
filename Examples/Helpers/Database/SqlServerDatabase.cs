using Microsoft.Data.SqlClient;

namespace Examples.Helpers.Database
{
    public abstract class SqlServerDatabase: Database
    {
        protected SqlServerDatabase(string connectionString): base(new SqlConnection(connectionString))
        {
        }
    }
}