using System.Collections.ObjectModel;

namespace SqlCommandBuilder.Test
{
    public class SelectTests
    {
        [Test]
        public void Select_AllColumns_UsesStar()
        {
            var r = CommandBuilder.Init().Select("film", Array.Empty<string>()).Build();
            SqlAssert.Equal("SELECT * FROM film", r.Script);
            Assert.That(r.Parameters, Is.Empty);
        }

        [Test]
        public void Select_SpecificColumns_ListsThem()
        {
            var r = CommandBuilder.Init().Select("film", new[] { "id", "title" }).Build();
            SqlAssert.Equal("SELECT id, title FROM film", r.Script);
        }

        [Test]
        public void Select_AddFieldAppendsColumns()
        {
            var r = CommandBuilder.Init()
                .Select("film", new[] { "id" })
                .Select(new[] { "title", "rating" })
                .Build();
            SqlAssert.Equal("SELECT id, title, rating FROM film", r.Script);
        }

        [Test]
        public void Select_WhereAnd_SingleCondition()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereAnd("rating", CommandMatchType.Equal, "PG")
                .Build();
            Assert.That(r.Script, Does.Contain("WHERE"));
            Assert.That(r.Script, Does.Contain("rating ="));
            Assert.That(r.Parameters, Has.Count.EqualTo(1));
            Assert.That(r.Parameters.Values.First(), Is.EqualTo("PG"));
        }

        [Test]
        public void Select_WhereAnd_MultipleConditions()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereAnd("rating", CommandMatchType.Equal, "PG")
                .WhereAnd("length", CommandMatchType.Greater, 100)
                .Build();
            Assert.That(r.Script, Does.Contain("WHERE"));
            Assert.That(r.Script, Does.Contain("rating ="));
            Assert.That(r.Script, Does.Contain("AND length >"));
            Assert.That(r.Parameters, Has.Count.EqualTo(2));
        }

        [Test]
        public void Select_WhereOr_CombinesWithOr()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereAnd("rating", CommandMatchType.Equal, "PG")
                .WhereOr("rating", CommandMatchType.Equal, "G")
                .Build();
            Assert.That(r.Script, Does.Contain("WHERE rating ="));
            Assert.That(r.Script, Does.Contain("OR rating ="));
        }

        [Test]
        public void Select_WhereGroupAnd_ProducesParens()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereAnd("rating", CommandMatchType.Equal, "PG")
                .WhereGroupAnd(new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "length", CommandMatchType.Greater, 100),
                    (CommandOperation.Or, "length", CommandMatchType.Lesser, 50)
                })
                .Build();
            Assert.That(r.Script, Does.Contain("AND ("));
            Assert.That(r.Script, Does.Contain("OR length <"));
            Assert.That(r.Script, Does.Contain(")"));
            Assert.That(r.Parameters, Has.Count.EqualTo(3));
        }

        [Test]
        public void Select_WhereGroupOr_StartsWithOr()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereAnd("rating", CommandMatchType.Equal, "PG")
                .WhereGroupOr(new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "length", CommandMatchType.Greater, 100),
                    (CommandOperation.And, "year", CommandMatchType.Equal, 2024)
                })
                .Build();
            Assert.That(r.Script, Does.Contain("OR ("));
        }

        [Test]
        public void Select_WhereGroup_EmptyInput_NoOp()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereGroupAnd(new Collection<(CommandOperation, string, CommandMatchType, object?)>())
                .Build();
            Assert.That(r.Script, Does.Not.Contain("WHERE"));
        }

        [Test]
        public void Select_OrderByAsc()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Sort("title", CommandOrderDirection.Asc)
                .Build();
            SqlAssert.Equal("SELECT * FROM film ORDER BY title ASC", r.Script);
        }

        [Test]
        public void Select_OrderByDesc()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Sort("title", CommandOrderDirection.Desc)
                .Build();
            SqlAssert.Equal("SELECT * FROM film ORDER BY title DESC", r.Script);
        }

        [Test]
        public void Select_MultipleSorts()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Sort("rating", CommandOrderDirection.Asc)
                .Sort("title", CommandOrderDirection.Desc)
                .Build();
            SqlAssert.Equal("SELECT * FROM film ORDER BY rating ASC, title DESC", r.Script);
        }

        [Test]
        public void Select_GroupBy()
        {
            var r = CommandBuilder.Init()
                .Select("film", new[] { "rating", "COUNT(*) total" })
                .GroupBy("rating")
                .Build();
            SqlAssert.Equal("SELECT rating, COUNT(*) total FROM film GROUP BY rating", r.Script);
        }

        [Test]
        public void Select_GroupByMultipleColumns()
        {
            var r = CommandBuilder.Init()
                .Select("film", new[] { "rating", "year" })
                .GroupBy("rating")
                .GroupBy("year")
                .Build();
            SqlAssert.Equal("SELECT rating, year FROM film GROUP BY rating, year", r.Script);
        }

        [Test]
        public void Select_Having_AfterGroupBy()
        {
            var r = CommandBuilder.Init()
                .Select("film", new[] { "rating", "COUNT(*) total" })
                .GroupBy("rating")
                .Having("COUNT(*)", CommandMatchType.Greater, 10)
                .Build();
            Assert.That(r.Script, Does.Contain("GROUP BY rating"));
            Assert.That(r.Script, Does.Contain("HAVING"));
            Assert.That(r.Script, Does.Contain("COUNT(*) >"));
            Assert.That(r.Parameters, Has.Count.EqualTo(1));
            Assert.That(r.Parameters.Values.First(), Is.EqualTo(10));
        }

        [Test]
        public void Select_Having_MultipleConditions()
        {
            var r = CommandBuilder.Init()
                .Select("film", new[] { "rating" })
                .GroupBy("rating")
                .Having("COUNT(*)", CommandMatchType.Greater, 10)
                .Having("SUM(length)", CommandMatchType.Lesser, 1000, CommandOperation.Or)
                .Build();
            Assert.That(r.Script, Does.Contain("HAVING"));
            Assert.That(r.Script, Does.Contain("OR SUM(length) <"));
        }

        [Test]
        public void Select_MysqlAdapter_LimitOffset()
        {
            var r = CommandBuilder.Init()
                .SetAdapter(CommandAdapter.Mysql)
                .Select("film", Array.Empty<string>())
                .Take(10, 20)
                .Build();
            SqlAssert.Equal("SELECT * FROM film LIMIT 10 OFFSET 20", r.Script);
        }

        [Test]
        public void Select_PgSqlAdapter_LimitOffset()
        {
            var r = CommandBuilder.Init()
                .SetAdapter(CommandAdapter.PgSql)
                .Select("film", Array.Empty<string>())
                .Take(10, 20)
                .Build();
            SqlAssert.Equal("SELECT * FROM film LIMIT 10 OFFSET 20", r.Script);
        }

        [Test]
        public void Select_SqlServerAdapter_OffsetFetchNext()
        {
            var r = CommandBuilder.Init()
                .SetAdapter(CommandAdapter.SqlServer)
                .Select("film", Array.Empty<string>())
                .Take(10, 20)
                .Build();
            SqlAssert.Equal("SELECT * FROM film OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY", r.Script);
        }

        [Test]
        public void Select_OracleAdapter_OffsetFetchNext()
        {
            var r = CommandBuilder.Init()
                .SetAdapter(CommandAdapter.Oracle)
                .Select("film", Array.Empty<string>())
                .Take(5)
                .Build();
            SqlAssert.Equal("SELECT * FROM film OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY", r.Script);
        }

        [Test]
        public void Select_NoLimit_NoLimitClause()
        {
            var r = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Build();
            Assert.That(r.Script, Does.Not.Contain("LIMIT"));
            Assert.That(r.Script, Does.Not.Contain("OFFSET"));
            Assert.That(r.Script, Does.Not.Contain("FETCH"));
        }

        [Test]
        public void Select_LimitWithoutAdapter_Throws()
        {
            var cmd = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Take(5);
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Select_NoInit_Throws()
        {
            var cmd = CommandBuilder.Init();
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Select_FullCompose_HasAllParts()
        {
            var r = CommandBuilder.Init()
                .SetAdapter(CommandAdapter.Mysql)
                .Select("film a", new[] { "a.title", "COUNT(*) total" })
                .Join(CommandReferenceType.LeftJoin, "category b", new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "b.category_id", CommandMatchType.Equal, "a.category_id")
                })
                .WhereAnd("a.rating", CommandMatchType.Equal, "PG")
                .GroupBy("a.title")
                .Having("COUNT(*)", CommandMatchType.Greater, 1)
                .Sort("a.title", CommandOrderDirection.Asc)
                .Take(10, 5)
                .Build();
            Assert.That(r.Script, Does.Contain("SELECT a.title, COUNT(*) total FROM film a"));
            Assert.That(r.Script, Does.Contain("LEFT JOIN category b ON b.category_id = a.category_id"));
            Assert.That(r.Script, Does.Contain("WHERE a.rating ="));
            Assert.That(r.Script, Does.Contain("GROUP BY a.title"));
            Assert.That(r.Script, Does.Contain("HAVING COUNT(*) >"));
            Assert.That(r.Script, Does.Contain("ORDER BY a.title ASC"));
            Assert.That(r.Script, Does.Contain("LIMIT 10 OFFSET 5"));
        }
    }
}
