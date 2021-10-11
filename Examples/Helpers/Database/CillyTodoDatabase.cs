
using System.IO;
using System.Reflection;

namespace Examples.Helpers.Database
{
    public class CillyTodoDatabase : SqlServerDatabase
    {

        public CillyTodoDatabase(string connectionString) : base(connectionString)
        {
        }
        public override void Init()
        {
            RunSQLFile("Examples.Assets.CillyTodo_SqlServer.sql");
        }
    }
}