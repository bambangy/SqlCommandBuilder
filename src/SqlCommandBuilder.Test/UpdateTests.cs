namespace SqlCommandBuilder.Test
{
    public class UpdateTests
    {
        [Test]
        public void Update_WithWhere_BuildsCorrectly()
        {
            var r = CommandBuilder.Init()
                .Update("category", new Dictionary<string, object?> { { "name", "Karya" } })
                .WhereAnd("id", CommandMatchType.Equal, 5)
                .Build();

            Assert.That(r.Script, Does.StartWith("UPDATE category SET"));
            Assert.That(r.Script, Does.Contain("name = @"));
            Assert.That(r.Script, Does.Contain("WHERE"));
            Assert.That(r.Script, Does.Contain("id ="));
            Assert.That(r.Parameters, Has.Count.EqualTo(2));
        }

        [Test]
        public void Update_MultipleSets_AllAppear()
        {
            var r = CommandBuilder.Init()
                .Update("category", new Dictionary<string, object?>
                {
                    { "name", "X" },
                    { "active", false }
                })
                .WhereAnd("id", CommandMatchType.Equal, 1)
                .Build();

            Assert.That(r.Script, Does.Contain("name = @"));
            Assert.That(r.Script, Does.Contain("active = @"));
            Assert.That(r.Parameters, Has.Count.EqualTo(3));
        }

        [Test]
        public void Update_WithoutWhere_HasNoWhereClause()
        {
            var r = CommandBuilder.Init()
                .Update("category", new Dictionary<string, object?> { { "name", "Z" } })
                .Build();
            Assert.That(r.Script, Does.Not.Contain("WHERE"));
        }

        [Test]
        public void Update_EmptyBindings_Throws()
        {
            var cmd = CommandBuilder.Init().Update("category", new Dictionary<string, object?>());
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }
    }
}
