namespace SqlCommandBuilder.Test
{
    public class DeleteTests
    {
        [Test]
        public void Delete_WithWhere()
        {
            var r = CommandBuilder.Init()
                .Delete("category")
                .WhereAnd("id", CommandMatchType.Equal, 5)
                .Build();

            Assert.That(r.Script, Does.StartWith("DELETE FROM category"));
            Assert.That(r.Script, Does.Contain("WHERE"));
            Assert.That(r.Script, Does.Contain("id ="));
            Assert.That(r.Parameters, Has.Count.EqualTo(1));
            Assert.That(r.Parameters.Values.First(), Is.EqualTo(5));
        }

        [Test]
        public void Delete_WithoutWhere_NoWhereClause()
        {
            var r = CommandBuilder.Init().Delete("category").Build();
            SqlAssert.Equal("DELETE FROM category", r.Script);
            Assert.That(r.Parameters, Is.Empty);
        }

        [Test]
        public void Delete_NoTable_Throws()
        {
            var cmd = CommandBuilder.Init();
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }
    }
}
