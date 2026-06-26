using System.Collections.Generic;

namespace SqlCommandBuilder
{
    public interface IQueryCommandResult
    {
        string Script { get; set; }
        Dictionary<string, object?> Parameters { get; set; }
    }
}
