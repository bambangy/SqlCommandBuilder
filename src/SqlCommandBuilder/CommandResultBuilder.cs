namespace SqlCommandBuilder
{
    public class CommandResultBuilder
    {
        private class CommandResult : QueryCommandResult
        {
        }

        public static IQueryCommandResult Create() => new CommandResult();
    }
}
