namespace SqlCommandBuilder
{
    public class CommandSort
    {
        public string Column { get; set; } = string.Empty;
        public CommandOrderDirection Direction { get; set; }
        public static CommandSort Add(string column, CommandOrderDirection direction) => new CommandSort
        {
            Column = column,
            Direction = direction
        };
    }
}
