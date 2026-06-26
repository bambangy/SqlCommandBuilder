using System.Collections.ObjectModel;

namespace SqlCommandBuilder
{
    public class CommandReference
    {
        public CommandReference(CommandReferenceType referenceType, string tableName, Collection<CommandCondition> onConditions)
        {
            ReferenceType = referenceType;
            TableName = tableName;
            OnConditions = onConditions;
        }

        public static CommandReference Add(CommandReferenceType referenceType, string tableName, Collection<CommandCondition> onConditions)
            => new(referenceType, tableName, onConditions);

        public CommandReferenceType ReferenceType { get; }
        public string TableName { get; }
        public Collection<CommandCondition> OnConditions { get; }
    }
}
