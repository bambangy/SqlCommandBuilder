namespace SqlCommandBuilder.Test
{
    public class InsertTests
    {
        [Test]
        public void Insert_SingleColumn()
        {
            var r = CommandBuilder.Init()
                .Insert("category", new Dictionary<string, object?> { { "name", "Sci-Fi" } })
                .Build();

            Assert.That(r.Script, Does.StartWith("INSERT INTO category("));
            Assert.That(r.Script, Does.Contain("name"));
            Assert.That(r.Script, Does.Contain("VALUES(@"));
            Assert.That(r.Parameters, Has.Count.EqualTo(1));
            Assert.That(r.Parameters.Values.First(), Is.EqualTo("Sci-Fi"));
        }

        [Test]
        public void Insert_MultipleColumns_ParameterNamesUnique()
        {
            var r = CommandBuilder.Init()
                .Insert("category", new Dictionary<string, object?>
                {
                    { "name", "Sci-Fi" },
                    { "description", "books" },
                    { "active", true }
                })
                .Build();

            Assert.That(r.Parameters, Has.Count.EqualTo(3));
            Assert.That(r.Parameters.Keys, Is.Unique);
            Assert.That(r.Script, Does.Contain("name"));
            Assert.That(r.Script, Does.Contain("description"));
            Assert.That(r.Script, Does.Contain("active"));
        }

        [Test]
        public void Insert_NullValue_Allowed()
        {
            var r = CommandBuilder.Init()
                .Insert("category", new Dictionary<string, object?> { { "name", null } })
                .Build();

            Assert.That(r.Parameters, Has.Count.EqualTo(1));
            Assert.That(r.Parameters.Values.First(), Is.Null);
        }

        [Test]
        public void Insert_EmptyBindings_Throws()
        {
            var cmd = CommandBuilder.Init().Insert("category", new Dictionary<string, object?>());
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Insert_NoTable_Throws()
        {
            var cmd = CommandBuilder.Init();
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }
    }
}
