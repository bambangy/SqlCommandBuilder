using System.Collections.Generic;

namespace SqlCommandBuilder
{
    public static class QueryCommandExtensions
    {
        /// <summary>Add additional columns to a SELECT statement.</summary>
        public static IQueryCommand Select(this IQueryCommand command, string[] selections)
        {
            foreach (var selection in selections)
                command.AddField(selection);
            return command;
        }

        /// <summary>Initialize a SELECT statement. Pass an empty array for SELECT *.</summary>
        public static IQueryCommand Select(this IQueryCommand command, string tableName, string[] selections) => command.InitSelect(tableName, selections);

        /// <summary>Initialize an INSERT statement.</summary>
        public static IQueryCommand Insert(this IQueryCommand command, string tableName, Dictionary<string, object?> parameters) => command.InitInsert(tableName, parameters);

        /// <summary>Initialize an UPDATE statement. Don't forget to add a Where() clause to scope the update.</summary>
        public static IQueryCommand Update(this IQueryCommand command, string tableName, Dictionary<string, object?> parameters) => command.InitUpdate(tableName, parameters);

        /// <summary>Initialize a DELETE statement. Don't forget to add a Where() clause to scope the delete.</summary>
        public static IQueryCommand Delete(this IQueryCommand command, string tableName) => command.InitDelete(tableName);

        /// <summary>Append a WHERE condition combined with AND.</summary>
        public static IQueryCommand WhereAnd(this IQueryCommand command, string column, CommandMatchType matchType, object? value) => command.AddWhereAnd(column, matchType, value);

        /// <summary>Append a WHERE condition combined with OR.</summary>
        public static IQueryCommand WhereOr(this IQueryCommand command, string column, CommandMatchType matchType, object? value) => command.AddWhereOr(column, matchType, value);

        /// <summary>Append a parenthesized group of conditions combined with AND.</summary>
        public static IQueryCommand WhereGroupAnd(this IQueryCommand command, ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> conditions) => command.AddWhereAndGroup(conditions);

        /// <summary>Append a parenthesized group of conditions combined with OR.</summary>
        public static IQueryCommand WhereGroupOr(this IQueryCommand command, ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> conditions) => command.AddWhereOrGroup(conditions);

        /// <summary>Append an ORDER BY clause.</summary>
        public static IQueryCommand Sort(this IQueryCommand command, string column, CommandOrderDirection direction) => command.AddSort(column, direction);

        /// <summary>Append a JOIN. The right-hand value of each ON condition is a column reference (e.g. "b.id"), not a parameter value.</summary>
        public static IQueryCommand Join(this IQueryCommand command, CommandReferenceType type, string tableName, ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> onConditions) => command.AddReference(type, tableName, onConditions);

        /// <summary>Append a GROUP BY column.</summary>
        public static IQueryCommand GroupBy(this IQueryCommand command, string column) => command.AddGroupBy(column);

        /// <summary>Append a HAVING condition (use with GroupBy).</summary>
        public static IQueryCommand Having(this IQueryCommand command, string column, CommandMatchType matchType, object? value, CommandOperation operation = CommandOperation.And)
            => command.AddHaving(column, matchType, value, operation);

        /// <summary>Set a LIMIT and OFFSET for paging. Requires SetAdapter().</summary>
        public static IQueryCommand Take(this IQueryCommand command, int take, int skip = 0) => command.SetTake(take).SetSkip(skip);

        /// <summary>Build the script and parameters.</summary>
        public static IQueryCommandResult Build(this IQueryCommand command) => command.BuildCommand();
    }
}
