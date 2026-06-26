using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlCommandBuilder
{
    public abstract class QueryCommand : IQueryCommand
    {
        private CommandAdapter? adapter;
        private CommandType? type;
        private string tableName;
        private Collection<string> selections;
        private readonly Collection<CommandCondition> conditions;
        private readonly Collection<CommandCondition> havings;
        private readonly Collection<CommandReference> references;
        private readonly Collection<CommandSort> sorts;
        private readonly Collection<string> groupings;
        private int limit;
        private int offset;
        private Dictionary<string, object?> bindings;

        protected QueryCommand()
        {
            adapter = null;
            type = null;
            tableName = string.Empty;
            selections = new Collection<string>();
            conditions = new Collection<CommandCondition>();
            havings = new Collection<CommandCondition>();
            references = new Collection<CommandReference>();
            sorts = new Collection<CommandSort>();
            groupings = new Collection<string>();
            limit = 0;
            offset = 0;
            bindings = new Dictionary<string, object?>();
        }

        public IQueryCommand InitDelete(string tableName)
        {
            this.tableName = tableName;
            this.type = CommandType.DELETE;
            return this;
        }

        public IQueryCommand AddGroupBy(string column)
        {
            groupings.Add(column);
            return this;
        }

        public IQueryCommand AddHaving(string column, CommandMatchType matchType, object? value, CommandOperation operation = CommandOperation.And)
        {
            havings.Add(CommandCondition.Add(column, matchType, value, operation));
            return this;
        }

        public IQueryCommand InitInsert(string tableName, Dictionary<string, object?> parameters)
        {
            this.tableName = tableName;
            this.bindings = parameters;
            this.type = CommandType.INSERT;
            return this;
        }

        public IQueryCommand AddReference(CommandReferenceType type, string tableName, ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> onConditions)
        {
            var inner = new Collection<CommandCondition>(
                onConditions.Select(t => CommandCondition.Add(t.column, t.matchType, t.value, operation: t.operation)).ToList());
            references.Add(CommandReference.Add(type, tableName, inner));
            return this;
        }

        public IQueryCommand AddField(string column)
        {
            selections.Add(column);
            return this;
        }

        public IQueryCommand InitSelect(string tableName, string[] columns)
        {
            this.tableName = tableName;
            selections = new Collection<string>(columns.ToList());
            type = CommandType.SELECT;
            return this;
        }

        public IQueryCommand SetAdapter(CommandAdapter adapter)
        {
            this.adapter = adapter;
            return this;
        }

        public IQueryCommand SetSkip(int offset)
        {
            this.offset = offset;
            return this;
        }

        public IQueryCommand AddSort(string column, CommandOrderDirection direction)
        {
            sorts.Add(CommandSort.Add(column, direction));
            return this;
        }

        public IQueryCommand SetTake(int limit)
        {
            this.limit = limit;
            return this;
        }

        public IQueryCommand InitUpdate(string tableName, Dictionary<string, object?> parameters)
        {
            this.tableName = tableName;
            this.bindings = parameters;
            this.type = CommandType.UPDATE;
            return this;
        }

        public IQueryCommand AddWhereAnd(string column, CommandMatchType matchType, object? value)
        {
            conditions.Add(CommandCondition.Add(column, matchType, value, operation: CommandOperation.And));
            return this;
        }

        public IQueryCommand AddWhereAndGroup(ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> conditions)
        {
            AddGroup(conditions, outerOperation: CommandOperation.And);
            return this;
        }

        public IQueryCommand AddWhereOr(string column, CommandMatchType matchType, object? value)
        {
            conditions.Add(CommandCondition.Add(column, matchType, value, operation: CommandOperation.Or));
            return this;
        }

        public IQueryCommand AddWhereOrGroup(ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> conditions)
        {
            AddGroup(conditions, outerOperation: CommandOperation.Or);
            return this;
        }

        private void AddGroup(
            ICollection<(CommandOperation operation, string column, CommandMatchType matchType, object? value)> input,
            CommandOperation outerOperation)
        {
            if (input == null || input.Count == 0)
                return;

            var list = input.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var cond = list[i];
                bool begin = i == 0;
                bool end = i == list.Count - 1;
                CommandOperation op = begin ? outerOperation : cond.operation;
                conditions.Add(CommandCondition.Add(cond.column, cond.matchType, cond.value, operation: op, beginGroup: begin, endGroup: end));
            }
        }

        public IQueryCommandResult BuildCommand()
        {
            if (string.IsNullOrEmpty(tableName))
                throw new InvalidOperationException("Table name is required. Initialize with Select/Insert/Update/Delete before Build.");

            IQueryCommandResult result = CommandResultBuilder.Create();
            var parameters = new Dictionary<string, object?>();

            switch (type)
            {
                case CommandType.INSERT:
                    if (bindings.Count == 0)
                        throw new InvalidOperationException("Insert requires at least one column/value binding.");
                    result.Script = CommandBuilderFunction.GenerateInsertScript(tableName, bindings, parameters);
                    break;

                case CommandType.UPDATE:
                    if (bindings.Count == 0)
                        throw new InvalidOperationException("Update requires at least one column/value binding.");
                    result.Script = CommandBuilderFunction.GenerateUpdateScript(tableName, bindings, conditions, parameters);
                    break;

                case CommandType.DELETE:
                    result.Script = CommandBuilderFunction.GenerateDeleteScript(tableName, conditions, parameters);
                    break;

                case CommandType.SELECT:
                default:
                    if (limit > 0 && adapter == null)
                        throw new InvalidOperationException("Adapter must be set when using Take/Skip (call SetAdapter).");
                    result.Script = CommandBuilderFunction.GenerateSelectScript(
                        adapter ?? CommandAdapter.Mysql, tableName, selections.ToArray(),
                        references, conditions, groupings.ToArray(), havings, sorts, limit, offset, parameters);
                    break;
            }

            result.Parameters = parameters;
            return result;
        }

        internal static class CommandBuilderFunction
        {
            private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_\.]*$", RegexOptions.Compiled);

            public static string GenerateDeleteScript(string tableName, Collection<CommandCondition> conditions, Dictionary<string, object?> parameters)
            {
                var sb = new StringBuilder();
                sb.Append("DELETE FROM ").Append(tableName);
                AppendWhere(sb, conditions, parameters);
                return sb.ToString();
            }

            public static string GenerateUpdateScript(string tableName, Dictionary<string, object?> binds, Collection<CommandCondition> conditions, Dictionary<string, object?> parameters)
            {
                var sb = new StringBuilder();
                sb.Append("UPDATE ").Append(tableName).Append(" SET ");

                var assignments = new List<string>();
                int i = 0;
                foreach (var kv in binds)
                {
                    string key = NextParamKey(parameters, kv.Key, ref i);
                    assignments.Add($"{kv.Key} = @{key}");
                    parameters.Add(key, kv.Value);
                }
                sb.Append(string.Join(", ", assignments));
                AppendWhere(sb, conditions, parameters);
                return sb.ToString();
            }

            public static string GenerateInsertScript(string tableName, Dictionary<string, object?> binds, Dictionary<string, object?> parameters)
            {
                var sb = new StringBuilder();
                sb.Append("INSERT INTO ").Append(tableName).Append('(');

                var columns = new List<string>();
                var valueRefs = new List<string>();
                int i = 0;
                foreach (var kv in binds)
                {
                    string key = NextParamKey(parameters, kv.Key, ref i);
                    columns.Add(kv.Key);
                    valueRefs.Add("@" + key);
                    parameters.Add(key, kv.Value);
                }
                sb.Append(string.Join(", ", columns));
                sb.Append(") VALUES(");
                sb.Append(string.Join(", ", valueRefs));
                sb.Append(')');
                return sb.ToString();
            }

            public static string GenerateSelectScript(
                CommandAdapter adapter, string tableName, string[] selections,
                Collection<CommandReference> references, Collection<CommandCondition> conditions,
                string[] groupings, Collection<CommandCondition> havings,
                Collection<CommandSort> sorts, int limit, int offset,
                Dictionary<string, object?> parameters)
            {
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append(selections.Length > 0 ? string.Join(", ", selections) : "*");
                sb.Append(" FROM ").Append(tableName);

                foreach (var reference in references)
                {
                    sb.Append(' ').Append(ReferenceType(reference.ReferenceType))
                      .Append(' ').Append(reference.TableName)
                      .Append(" ON ").Append(ExtractReferenceConditions(reference.OnConditions));
                }

                AppendWhere(sb, conditions, parameters);

                if (groupings.Length > 0)
                {
                    sb.Append(" GROUP BY ").Append(string.Join(", ", groupings));
                }

                if (havings.Count > 0)
                {
                    sb.Append(" HAVING ").Append(ExtractConditions(havings, parameters, includeLeadingOperator: false));
                }

                if (sorts.Count > 0)
                {
                    sb.Append(" ORDER BY ").Append(string.Join(", ", sorts.Select(t => $"{t.Column} {SortDirection(t.Direction)}")));
                }

                if (limit > 0)
                {
                    sb.Append(' ');
                    switch (adapter)
                    {
                        case CommandAdapter.Mysql:
                        case CommandAdapter.PgSql:
                            sb.Append($"LIMIT {limit} OFFSET {offset}");
                            break;
                        case CommandAdapter.Oracle:
                        case CommandAdapter.SqlServer:
                        default:
                            sb.Append($"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY");
                            break;
                    }
                }

                return sb.ToString();
            }

            private static void AppendWhere(StringBuilder sb, Collection<CommandCondition> conditions, Dictionary<string, object?> parameters)
            {
                if (conditions.Count == 0)
                    return;
                sb.Append(" WHERE ");
                sb.Append(ExtractConditions(conditions, parameters, includeLeadingOperator: false));
            }

            public static string ExtractConditions(Collection<CommandCondition> conditions, Dictionary<string, object?> parameters, bool includeLeadingOperator)
            {
                var sb = new StringBuilder();
                int idx = 0;
                bool first = true;
                foreach (var condition in conditions)
                {
                    string key = NextParamKey(parameters, SanitizeKey(condition.Column), ref idx);
                    if (!first || includeLeadingOperator)
                    {
                        sb.Append(' ').Append(OperationText(condition.Operation ?? CommandOperation.And)).Append(' ');
                    }
                    if (condition.BeginGroup) sb.Append('(');
                    sb.Append(condition.Column).Append(' ').Append(MatchType(condition.Match));
                    if (IsValueBinding(condition.Match))
                    {
                        sb.Append(" @").Append(key);
                        parameters.Add(key, condition.Value);
                    }
                    if (condition.EndGroup) sb.Append(')');
                    first = false;
                }
                return sb.ToString();
            }

            public static string ExtractReferenceConditions(Collection<CommandCondition> conditions)
            {
                var sb = new StringBuilder();
                bool first = true;
                foreach (var condition in conditions)
                {
                    if (!first)
                    {
                        sb.Append(' ').Append(OperationText(condition.Operation ?? CommandOperation.And)).Append(' ');
                    }
                    sb.Append(condition.Column).Append(' ').Append(MatchType(condition.Match));
                    if (IsValueBinding(condition.Match))
                    {
                        sb.Append(' ').Append(RenderJoinValue(condition.Value));
                    }
                    first = false;
                }
                return sb.ToString();
            }

            private static string RenderJoinValue(object? value)
            {
                if (value == null)
                    throw new InvalidOperationException("JOIN ON conditions cannot have a null right-hand side.");
                string s = value.ToString()!;
                if (!IdentifierRegex.IsMatch(s))
                    throw new InvalidOperationException($"JOIN ON value '{s}' is not a valid identifier. Use column references only (e.g. 'b.id').");
                return s;
            }

            private static string SanitizeKey(string column)
            {
                var sb = new StringBuilder(column.Length);
                foreach (var ch in column)
                    sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
                return sb.ToString();
            }

            private static string NextParamKey(Dictionary<string, object?> parameters, string baseKey, ref int counter)
            {
                string candidate = $"p{counter}_{baseKey}";
                counter++;
                return candidate;
            }

            public static string OperationText(CommandOperation operation) => operation switch
            {
                CommandOperation.Or => "OR",
                _ => "AND"
            };

            public static string SortDirection(CommandOrderDirection direction) => direction switch
            {
                CommandOrderDirection.Desc => "DESC",
                _ => "ASC"
            };

            public static string ReferenceType(CommandReferenceType type) => type switch
            {
                CommandReferenceType.InnerJoin => "INNER JOIN",
                CommandReferenceType.LeftJoin => "LEFT JOIN",
                CommandReferenceType.RightJoin => "RIGHT JOIN",
                CommandReferenceType.OuterJoin => "OUTER JOIN",
                _ => "JOIN"
            };

            public static string MatchType(CommandMatchType match) => match switch
            {
                CommandMatchType.NotEqual => "<>",
                CommandMatchType.Lesser => "<",
                CommandMatchType.LesserOrEqual => "<=",
                CommandMatchType.GreaterOrEqual => ">=",
                CommandMatchType.Greater => ">",
                CommandMatchType.Contains => "LIKE",
                CommandMatchType.NotContains => "NOT LIKE",
                CommandMatchType.IsIn => "IN",
                CommandMatchType.IsNotIn => "NOT IN",
                CommandMatchType.IsNull => "IS NULL",
                CommandMatchType.IsNotNull => "IS NOT NULL",
                _ => "="
            };

            public static bool IsValueBinding(CommandMatchType match) => match switch
            {
                CommandMatchType.IsNull => false,
                CommandMatchType.IsNotNull => false,
                _ => true
            };
        }
    }
}
