using System.Collections.ObjectModel;

namespace SqlCommandBuilder.Test
{
    public class JoinTests
    {
        private static Collection<(CommandOperation, string, CommandMatchType, object?)> On(string left, string right, CommandOperation op = CommandOperation.And)
            => new() { (op, left, CommandMatchType.Equal, right) };

        [TestCase(CommandReferenceType.Join, "JOIN")]
        [TestCase(CommandReferenceType.InnerJoin, "INNER JOIN")]
        [TestCase(CommandReferenceType.LeftJoin, "LEFT JOIN")]
        [TestCase(CommandReferenceType.RightJoin, "RIGHT JOIN")]
        [TestCase(CommandReferenceType.OuterJoin, "OUTER JOIN")]
        public void JoinTypes_RenderCorrectly(CommandReferenceType type, string keyword)
        {
            var r = CommandBuilder.Init()
                .Select("a", Array.Empty<string>())
                .Join(type, "b", On("b.id", "a.id"))
                .Build();
            Assert.That(r.Script, Does.Contain($"{keyword} b ON b.id = a.id"));
        }

        [Test]
        public void Join_MultipleConditions_CombinedWithOperator()
        {
            var r = CommandBuilder.Init()
                .Select("a", Array.Empty<string>())
                .Join(CommandReferenceType.InnerJoin, "b", new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "b.id", CommandMatchType.Equal, "a.id"),
                    (CommandOperation.Or, "b.alt_id", CommandMatchType.Equal, "a.alt_id")
                })
                .Build();
            Assert.That(r.Script, Does.Contain("b.id = a.id"));
            Assert.That(r.Script, Does.Contain("OR b.alt_id = a.alt_id"));
        }

        [Test]
        public void Join_MultipleTables_AllAppear()
        {
            var r = CommandBuilder.Init()
                .Select("a", Array.Empty<string>())
                .Join(CommandReferenceType.Join, "b", On("b.id", "a.b_id"))
                .Join(CommandReferenceType.LeftJoin, "c", On("c.id", "a.c_id"))
                .Build();
            Assert.That(r.Script, Does.Contain("JOIN b ON"));
            Assert.That(r.Script, Does.Contain("LEFT JOIN c ON"));
        }

        [Test]
        public void Join_NullValue_Throws()
        {
            var cmd = CommandBuilder.Init()
                .Select("a", Array.Empty<string>())
                .Join(CommandReferenceType.Join, "b", new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "b.id", CommandMatchType.Equal, null)
                });
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Join_InvalidIdentifier_Throws()
        {
            var cmd = CommandBuilder.Init()
                .Select("a", Array.Empty<string>())
                .Join(CommandReferenceType.Join, "b", new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "b.id", CommandMatchType.Equal, "a.id; DROP TABLE users--")
                });
            Assert.That(() => cmd.Build(), Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("not a valid identifier"));
        }

        [Test]
        public void Join_SimpleIdentifierAccepted()
        {
            var r = CommandBuilder.Init()
                .Select("a", Array.Empty<string>())
                .Join(CommandReferenceType.Join, "b", On("b_id", "a_id"))
                .Build();
            Assert.That(r.Script, Does.Contain("b_id = a_id"));
        }
    }
}
