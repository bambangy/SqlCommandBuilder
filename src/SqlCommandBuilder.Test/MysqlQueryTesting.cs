using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;

namespace SqlCommandBuilder.Test
{
    [Explicit("Requires a running MySQL 'sakila' database. Set MYSQL_CONN env var to override the connection string.")]
    public class MySqlQueryTesting
    {
        private MySqlConnection _connection = null!;

        [SetUp]
        public void Setup()
        {
            string connectionString = Environment.GetEnvironmentVariable("MYSQL_CONN")
                ?? "Server=localhost;Database=sakila;Uid=root;Pwd=Developer11!;";
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
        }

        [TearDown]
        public void TearDown() => _connection?.Dispose();

        [Test]
        public void TestComposeScript()
        {
            IQueryCommandResult strCommand = CommandBuilder.Init()
                .Select("film_category a", new[] { "b.title Film_Title", "b.description Film_Description", "c.name Category" })
                .Join(CommandReferenceType.Join, "film b", new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "b.film_id", CommandMatchType.Equal, "a.film_id")
                })
                .Join(CommandReferenceType.Join, "category c", new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "c.category_id", CommandMatchType.Equal, "a.category_id")
                })
                .WhereAnd("b.title", CommandMatchType.Contains, "%G%")
                .Build();
            Assert.That(strCommand.Script, Is.Not.Null);
            Assert.That(strCommand.Parameters, Is.Not.Null);
        }

        [Test]
        public void TestQuerySimpleSelect()
        {
            var strCommand = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Build();

            var films = _connection.Query<dynamic>(strCommand.Script, strCommand.Parameters);
            Assert.That(films, Is.Not.Null);
            Assert.That(films.Any(), Is.True);
        }

        [Test]
        public void TestQueryWhere()
        {
            var strCommand = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .WhereAnd("rating", CommandMatchType.Equal, "PG")
                .Build();

            var films = _connection.Query<dynamic>(strCommand.Script, strCommand.Parameters);
            Assert.That(films, Is.Not.Null);
            Assert.That(films.Any(), Is.True);
        }

        [Test]
        public void TestQueryLimit()
        {
            var strCommand = CommandBuilder.Init()
                .SetAdapter(CommandAdapter.Mysql)
                .Select("film", Array.Empty<string>())
                .Take(5)
                .Build();

            var films = _connection.Query<dynamic>(strCommand.Script, strCommand.Parameters);
            Assert.That(films.Count(), Is.EqualTo(5));
        }

        [Test]
        public void TestQueryOrder()
        {
            var strCommand = CommandBuilder.Init()
                .Select("film", Array.Empty<string>())
                .Sort("title", CommandOrderDirection.Desc)
                .Build();

            var films = _connection.Query<dynamic>(strCommand.Script, strCommand.Parameters).ToList();
            Assert.That(films, Is.Not.Empty);
            string firstTitle = films.First().title;
            Assert.That(firstTitle.Substring(0, 1), Is.EqualTo("Z"));
        }

        [Test]
        public void TestInsertUpdateDelete()
        {
            var insert = CommandBuilder.Init()
                .Insert("category", new Dictionary<string, object?> { { "name", "Fiksi Ilmiah" } })
                .Build();
            _connection.Execute(insert.Script, insert.Parameters);

            var select = CommandBuilder.Init()
                .Select("category", Array.Empty<string>())
                .WhereAnd("name", CommandMatchType.Equal, "Fiksi Ilmiah")
                .Build();
            dynamic? category = _connection.Query<dynamic>(select.Script, select.Parameters).FirstOrDefault();
            Assert.That(category, Is.Not.Null);

            var update = CommandBuilder.Init()
                .Update("category", new Dictionary<string, object?> { { "name", "Karya Ilmiah" } })
                .WhereAnd("category_id", CommandMatchType.Equal, (int)category!.category_id)
                .Build();
            _connection.Execute(update.Script, update.Parameters);

            var delete = CommandBuilder.Init()
                .Delete("category")
                .WhereAnd("category_id", CommandMatchType.Equal, (int)category!.category_id)
                .Build();
            _connection.Execute(delete.Script, delete.Parameters);

            var verify = CommandBuilder.Init()
                .Select("category", Array.Empty<string>())
                .WhereAnd("name", CommandMatchType.Equal, "Karya Ilmiah")
                .Build();
            dynamic? after = _connection.Query<dynamic>(verify.Script, verify.Parameters).FirstOrDefault();
            Assert.That(after, Is.Null);
        }
    }
}
