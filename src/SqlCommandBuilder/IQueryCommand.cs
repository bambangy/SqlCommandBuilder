using System.Collections.Generic;

namespace SqlCommandBuilder
{
    public interface IQueryCommand
    {
        /// <summary>
        /// Set query adapter to control adapter-specific syntax (Mysql, SqlServer, PgSql, Oracle).
        /// Currently controls LIMIT/OFFSET vs OFFSET..FETCH NEXT syntax.
        /// </summary>
        IQueryCommand SetAdapter(CommandAdapter adapter);

        /// <summary>Append an additional column to the SELECT list.</summary>
        IQueryCommand AddField(string column);

        /// <summary>Initialize a SELECT statement with a table and an initial column list. Pass an empty array to select all columns (*).</summary>
        IQueryCommand InitSelect(string tableName, string[] columns);

        /// <summary>Initialize an INSERT statement. Keys are column names; values are parameter-bound at build time.</summary>
        IQueryCommand InitInsert(string tableName, Dictionary<string, object?> parameters);

        /// <summary>Initialize an UPDATE statement. Add WHERE conditions to scope the update.</summary>
        IQueryCommand InitUpdate(string tableName, Dictionary<string, object?> parameters);

        /// <summary>Initialize a DELETE statement. Add WHERE conditions to avoid deleting all rows.</summary>
        IQueryCommand InitDelete(string tableName);

        /// <summary>Append a WHERE condition combined with AND.</summary>
        IQueryCommand AddWhereAnd(string column, CommandMatchType matchType, object? value);

        /// <summary>Append a WHERE condition combined with OR.</summary>
        IQueryCommand AddWhereOr(string column, CommandMatchType matchType, object? value);

        /// <summary>Append a parenthesized group of conditions combined with AND with the rest of the WHERE clause.</summary>
        IQueryCommand AddWhereAndGroup(ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> conditions);

        /// <summary>Append a parenthesized group of conditions combined with OR with the rest of the WHERE clause.</summary>
        IQueryCommand AddWhereOrGroup(ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> conditions);

        /// <summary>
        /// Add a JOIN clause. Each ON condition's value is treated as a column reference (e.g. "b.id"),
        /// not as a parameter value. The value is validated as a safe identifier to prevent SQL injection.
        /// </summary>
        IQueryCommand AddReference(CommandReferenceType type, string tableName, ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> onConditions);

        /// <summary>Append an ORDER BY clause.</summary>
        IQueryCommand AddSort(string column, CommandOrderDirection direction);

        /// <summary>Append a GROUP BY column.</summary>
        IQueryCommand AddGroupBy(string column);

        /// <summary>Append a HAVING condition (for use with GROUP BY).</summary>
        IQueryCommand AddHaving(string column, CommandMatchType matchType, object? value, CommandOperation operation = CommandOperation.And);

        /// <summary>Set the row limit (LIMIT/FETCH NEXT). Requires SetAdapter() when greater than zero.</summary>
        IQueryCommand SetTake(int limit);

        /// <summary>Set the row skip (OFFSET).</summary>
        IQueryCommand SetSkip(int offset);

        /// <summary>Build the script and parameters.</summary>
        IQueryCommandResult BuildCommand();
    }
}
