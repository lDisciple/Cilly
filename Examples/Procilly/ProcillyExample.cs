using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using Dapper;
using Examples.Helpers.Database;
using Examples.Procilly.Models;
using Examples.Procilly.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Procilly;

namespace Examples.Procilly
{
    public static class ProcillyExample
    {
        private const string ConnectionString =
            "Data Source=.;Initial Catalog=Cilly;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;";

        /**
         * TODO:
         * Audit fields attribute
         */
        public static void Main()
        {
            Database db = new CillyTodoDatabase(ConnectionString);
            db.Init();

            var builder = new ProcedureRepositoryBuilder<SqlConnection>();
            ICillyRepository repository = builder.BuildType<ICillyRepository>(ConnectionString);
            
            Console.WriteLine("\n- GetAllTodoItems");
            repository.GetAllTodoItems().ToList().ForEach(x => Console.WriteLine("\t" + x));
            
            Console.WriteLine("\n- CreateTodoItem");
            repository.CreateTodoItem("Fix Cilly", "I added this", out var id);
            Console.WriteLine(id);

            Console.WriteLine($"\n- GetTodoItemById {id}");
            Console.WriteLine(repository.GetTodoItemById(id));
            
            Console.WriteLine($"\n- MarkItemAsComplete {id}");
            repository.MarkItemAsComplete(id);

            Console.WriteLine("\n- DeleteTodoItem 3");
            repository.DeleteTodoItem(3);

            Console.WriteLine("\n- UpdateTodoItem 4 -> (Sleep v2, Bzzzzzz, true)");
            repository.UpdateTodoItem(4, "Sleep v2", "Bzzzzzz", true);
            
            Console.WriteLine("\n- GetAllTodoItems");
            repository.GetAllTodoItems().ToList().ForEach(x => Console.WriteLine("\t" + x));
            
        }
    }
}