using System.Collections.Generic;
using Examples.Procilly.Models;
using Procilly;

namespace Examples.Procilly.Repositories
{
    public interface ICillyRepository: IProcedureRepository
    {
        void CreateTodoItem(string name, string description, out int id);
        void DeleteTodoItem(int id);
        Todo GetTodoItemById(int id);
        IEnumerable<Todo> GetAllTodoItems();
        void UpdateTodoItem(int id, string name, string description, bool completed);
        void MarkItemAsComplete(int id);
    }
}