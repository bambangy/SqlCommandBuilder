# Changelog

All notable changes to this project are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] — 2026-06-26

A reset-the-foundation release. The public fluent API is mostly compatible with `1.x`, but the surrounding namespaces, the generated SQL, and the target framework have all moved forward. Anyone consuming this library should treat it as a breaking upgrade.

### Breaking

- **Target framework upgraded from `net6.0` to `net10.0`.** Consumers must build on the .NET 10 SDK or later.
- **Generated SQL output changed.** `WHERE 1=1 AND ...` boilerplate is gone, the leading orphan operator is gone, and parameter names now use the readable `p{n}_{column}` pattern instead of 32-char GUIDs. Anyone snapshot-testing the raw script string needs to refresh fixtures.
- **`IDapperCommandExtensions` renamed to `QueryCommandExtensions`.** Old `using SqlCommandBuilder;` calls keep working (the class lives in the same namespace), but direct references to the class name must be updated.
- **`CommandConditionCollection` removed.** It was unused dead code.
- **JOIN `ON` values are now validated.** The right-hand side of each join condition must match `^[A-Za-z_][A-Za-z0-9_\.]*$`. Anything else throws `InvalidOperationException`. This closes a real SQL-injection hole — code that was passing identifiers (e.g. `"a.id"`) keeps working unchanged; code that was passing arbitrary strings will now fail loudly.
- **Build-time validation errors changed type.** `Build()` now throws `InvalidOperationException` (instead of `Exception` / `NullReferenceException`) for missing table names and empty INSERT/UPDATE bindings, with clearer messages.
- **Redundant `SetAdapter` extension method removed.** It shadowed the interface method with an identical signature and was never reachable. The interface method `IQueryCommand.SetAdapter(...)` remains.

### Added

- **`GroupBy(column)` fluent extension** and `IQueryCommand.AddGroupBy` is now actually surfaced on the public API. Previously declared on the interface but with no extension wrapper.
- **`Having(column, matchType, value, operation)` fluent extension** with a new `AddHaving` interface method for adding HAVING clauses to grouped queries.
- **CHANGELOG.md** (this file).
- **Expanded README** with full coverage of every operator, the new HAVING/GROUP BY surface, paging behavior per adapter, and the JOIN-injection rules.

### Fixed

- **`AddWhereAndGroup` bug**: the first item of an AND-group was previously emitted with `OR` (copy/paste mistake from the OR-group variant). Now correctly emits `AND (...)`.
- **`InitSelect` no longer throws on `AddField`**: the underlying `Collection<string>` was being wrapped over a read-only array, so any subsequent `.Select(new[]{...})` call crashed with `NotSupportedException`. Now wraps a fresh `List<string>`.
- **Nullable warnings**: `QueryCommandResult.Script`/`Parameters` and `CommandSort.Column` now initialize to non-null defaults; the library builds with zero warnings under `<Nullable>enable</Nullable>`.
- **Whitespace and stray spaces** in generated SQL eliminated by switching the script generator from token-replacement on a template string to a streamed `StringBuilder`.

### Internal

- `QueryCommand.CommandBuilderFunction` rewritten: switch statements replaced with switch-expressions, `Guid`-based parameter naming replaced with a counter, SQL assembly streamed through `StringBuilder` instead of repeated `String.Replace`.
- Dropped vulnerable dependency `Microsoft.Data.SqlClient 5.1.2` from the test project. Upgraded NUnit (3 → 4), Test SDK, MySql.Data, Dapper, and coverlet.
- Live-DB tests marked `[Explicit]` so CI runs cleanly without a MySQL server. Connection string is overridable via the `MYSQL_CONN` environment variable.
- Added 8 new offline test files (`SelectTests`, `InsertTests`, `UpdateTests`, `DeleteTests`, `JoinTests`, `WhereGroupTests`, `MatchTypeTests`, `ModelTests`) and a `SqlAssert` whitespace-normalizing helper. Total: 69 tests, **100% line / 100% branch / 100% method** coverage.

### Migration notes from 1.x

1. Re-target your consuming project to `net10.0`.
2. If you stored generated SQL strings (e.g. in golden tests or migrations), regenerate them — the output is cleaner but byte-different.
3. If you relied on `GroupBy` being absent and were calling `AddGroupBy` directly through the interface, you can now switch to the fluent `.GroupBy(...)` extension.
4. If you were passing arbitrary user input as the `value` of a JOIN `ON` condition, audit those call sites — that path now throws. JOIN ON values should always be column references.
5. Replace any `catch (NullReferenceException)` / `catch (Exception)` around `Build()` with `catch (InvalidOperationException)`.

---

## [1.0.0-alpha]

Initial public release. Provided the original fluent builder (`Select`, `Insert`, `Update`, `Delete`, `WhereAnd`/`WhereOr`, `WhereGroupAnd`/`WhereGroupOr`, `Join`, `Sort`, `Take`/`Skip`, `SetAdapter`) targeting `net6.0`.
