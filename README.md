# SqlCommandBuilder

A small, dependency-free .NET library for building parameterized raw SQL queries with a fluent API. It produces a `Script` plus a `Parameters` dictionary that drops straight into Dapper, `DbCommand`, or any ADO.NET pipeline.

- Targets **.NET 10**
- Zero runtime dependencies
- Adapter-aware paging for MySQL, PostgreSQL, SQL Server, and Oracle
- Parameterized everywhere — JOIN `ON` values are validated as identifiers to block SQL injection

## Install

```
dotnet add package SqlCommandBuilder
```

## Quick start

Every query begins with `CommandBuilder.Init()` and ends with `.Build()`. The result implements `IQueryCommandResult`:

```csharp
public interface IQueryCommandResult
{
    string Script { get; set; }
    Dictionary<string, object?> Parameters { get; set; }
}
```

Pass `Script` and `Parameters` straight into Dapper:

```csharp
var cmd = CommandBuilder.Init()
    .Select("film", new[] { "film_id", "title" })
    .WhereAnd("rating", CommandMatchType.Equal, "PG")
    .Build();

var films = connection.Query<Film>(cmd.Script, cmd.Parameters);
```

## SELECT

```csharp
CommandBuilder.Init()
    .Select("category", new[] { "name", "category_id" })
    .Build();
// SELECT name, category_id FROM category
```

- `Select(tableName, columns)` initializes the SELECT. Pass an empty array to select `*`.
- `Select(columns)` appends additional columns to the existing SELECT list.

### WHERE

```csharp
CommandBuilder.Init()
    .Select("film", Array.Empty<string>())
    .WhereAnd("rating", CommandMatchType.Equal, "PG")
    .WhereOr("rating", CommandMatchType.Equal, "G")
    .Build();
// SELECT * FROM film WHERE rating = @p0_rating OR rating = @p1_rating
```

Grouped conditions (parentheses) — combine with the rest of the clause via AND or OR:

```csharp
.WhereGroupAnd(new List<(CommandOperation, string, CommandMatchType, object?)>
{
    (CommandOperation.And, "length", CommandMatchType.Greater, 100),
    (CommandOperation.Or,  "length", CommandMatchType.Lesser,  50)
})
// ... AND (length > @p1_length OR length < @p2_length)
```

`CommandMatchType` covers: `Equal`, `NotEqual`, `Lesser`, `LesserOrEqual`, `Greater`, `GreaterOrEqual`, `Contains` (`LIKE`), `NotContains` (`NOT LIKE`), `IsIn`, `IsNotIn`, `IsNull`, `IsNotNull`. `Null`/`NotNull` skip binding the value.

> For `Contains`/`NotContains`, supply the wildcard yourself (e.g. `"%foo%"`).
> For `IsIn`/`IsNotIn`, pass an `IEnumerable` value — Dapper expands it; raw ADO.NET will not.

### JOIN

```csharp
CommandBuilder.Init()
    .Select("film_category a", new[] { "b.title", "c.name Category" })
    .Join(CommandReferenceType.InnerJoin, "film b", new List<(CommandOperation, string, CommandMatchType, object?)>
    {
        (CommandOperation.And, "b.film_id", CommandMatchType.Equal, "a.film_id")
    })
    .Join(CommandReferenceType.InnerJoin, "category c", new List<(CommandOperation, string, CommandMatchType, object?)>
    {
        (CommandOperation.And, "c.category_id", CommandMatchType.Equal, "a.category_id")
    })
    .Build();
```

Available joins: `Join`, `InnerJoin`, `LeftJoin`, `RightJoin`, `OuterJoin`.

The right-hand `value` of each ON condition is a **column reference**, not a parameter. It is validated against `^[A-Za-z_][A-Za-z0-9_\.]*$` — anything else throws `InvalidOperationException`.

### GROUP BY / HAVING

```csharp
CommandBuilder.Init()
    .Select("film", new[] { "rating", "COUNT(*) total" })
    .GroupBy("rating")
    .Having("COUNT(*)", CommandMatchType.Greater, 10)
    .Build();
// SELECT rating, COUNT(*) total FROM film GROUP BY rating HAVING COUNT(*) > @p0_COUNT___
```

### ORDER BY

```csharp
.Sort("title", CommandOrderDirection.Desc)
.Sort("year",  CommandOrderDirection.Asc)
```

### Paging (Take / Skip)

`Take` requires `SetAdapter` because LIMIT syntax varies:

```csharp
CommandBuilder.Init()
    .SetAdapter(CommandAdapter.SqlServer)
    .Select("film", Array.Empty<string>())
    .Sort("title", CommandOrderDirection.Asc)
    .Take(take: 10, skip: 20)
    .Build();
// SELECT * FROM film ORDER BY title ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
```

| Adapter      | Paging syntax |
| ------------ | -------------- |
| `Mysql`, `PgSql`     | `LIMIT n OFFSET m` |
| `SqlServer`, `Oracle` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` |

## INSERT

```csharp
CommandBuilder.Init()
    .Insert("category", new Dictionary<string, object?>
    {
        { "name", "Karya Ilmiah" }
    })
    .Build();
// INSERT INTO category(name) VALUES(@p0_name)
```

## UPDATE

```csharp
CommandBuilder.Init()
    .Update("category", new Dictionary<string, object?> { { "name", "Karya Ilmiah" } })
    .WhereAnd("category_id", CommandMatchType.Equal, 5)
    .Build();
// UPDATE category SET name = @p0_name WHERE category_id = @p1_category_id
```

Forgetting `WhereAnd`/`WhereOr` on `Update`/`Delete` produces a script with no `WHERE` — by design. Add a condition.

## DELETE

```csharp
CommandBuilder.Init()
    .Delete("category")
    .WhereAnd("category_id", CommandMatchType.Equal, 5)
    .Build();
// DELETE FROM category WHERE category_id = @p0_category_id
```

## Building

```
dotnet build src/SqlCommandBuilder/SqlCommandBuilder.csproj
dotnet test  src/SqlCommandBuilder.Test/SqlCommandBuilder.Test.csproj
```

The integration tests against a live MySQL `sakila` database are tagged `[Explicit]` and skipped by default. Set `MYSQL_CONN` and run them with `dotnet test --filter "TestCategory!=Explicit"` removed (NUnit explicit tests must be selected by name).

## License

MIT — see [LICENSE](LICENSE).
