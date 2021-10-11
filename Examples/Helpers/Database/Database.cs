using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Examples.Helpers.Database
{
    public abstract class Database: IDisposable
    {
        protected string ConnectionString;
        protected DbConnection LiveConnection; // Used to keep DB alive

        protected Database(DbConnection connection)
        {
            ConnectionString = connection.ConnectionString;
            LiveConnection = connection;
            LiveConnection.Open();
        }

        public abstract void Init();

        public void RunSQLFile(string path)
        {
            using (var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                var sql = reader.ReadToEnd();
                foreach (var batch in Regex.Split(sql, @"\sGO(?:\s|$)").Where(x => x.Length > 0))
                {
                    using var command = LiveConnection.CreateCommand();
                    command.CommandText = batch;
                    command.ExecuteNonQuery();
                }
            }
        }

        public string GetConnectionString()
        {
            return ConnectionString; 
        }

        public void Dispose()
        {
            LiveConnection.Dispose();
        }
    }
}