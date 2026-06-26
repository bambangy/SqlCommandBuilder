using System.Collections.Generic;

namespace SqlCommandBuilder
{
    public abstract class QueryCommandResult : IQueryCommandResult
    {
        public string Script { get; set; } = string.Empty;
        public Dictionary<string, object?> Parameters { get; set; } = new();
    }
}
