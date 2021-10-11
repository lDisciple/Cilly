using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Examples.Helpers.Database
{
    public abstract class SqliteDatabase: Database
    {
        public SqliteDatabase(): base(new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared"))
        {
        }
    }
}