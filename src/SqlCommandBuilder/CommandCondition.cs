namespace SqlCommandBuilder
{
    public class CommandCondition
    {
        public CommandCondition(string column, CommandMatchType match, object? value, CommandOperation? operation = null, bool beginGroup = false, bool endGroup = false)
        {
            Column = column;
            Match = match;
            Value = value;
            BeginGroup = beginGroup;
            EndGroup = endGroup;
            Operation = operation;
        }

        public static CommandCondition Add(string column, CommandMatchType match, object? value, CommandOperation? operation = null, bool beginGroup = false, bool endGroup = false)
            => new(column, match, value, operation, beginGroup, endGroup);

        public string Column { get; }
        public CommandMatchType Match { get; }
        public object? Value { get; }
        public bool BeginGroup { get; }
        public bool EndGroup { get; }
        public CommandOperation? Operation { get; }
    }
}
