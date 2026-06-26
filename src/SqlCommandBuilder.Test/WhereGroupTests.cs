using System.Collections.ObjectModel;

namespace SqlCommandBuilder.Test
{
    public class WhereGroupTests
    {
        [Test]
        public void WhereGroupAnd_FirstConditionUsesOuterAnd()
        {
            var r = CommandBuilder.Init()
                .Select("t", Array.Empty<string>())
                .WhereAnd("status", CommandMatchType.Equal, "active")
                .WhereGroupAnd(new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "a", CommandMatchType.Equal, 1),
                    (CommandOperation.Or, "b", CommandMatchType.Equal, 2)
                })
                .Build();

            Assert.That(r.Script, Does.Contain("AND ("));
            Assert.That(r.Script, Does.Contain("OR b ="));
            Assert.That(r.Script, Does.Contain(")"));
        }

        [Test]
        public void WhereGroupOr_FirstConditionUsesOuterOr()
        {
            var r = CommandBuilder.Init()
                .Select("t", Array.Empty<string>())
                .WhereAnd("status", CommandMatchType.Equal, "active")
                .WhereGroupOr(new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "a", CommandMatchType.Equal, 1),
                    (CommandOperation.And, "b", CommandMatchType.Equal, 2)
                })
                .Build();

            Assert.That(r.Script, Does.Contain("OR ("));
            Assert.That(r.Script, Does.Contain("AND b ="));
            Assert.That(r.Script, Does.Contain(")"));
        }

        [Test]
        public void WhereGroup_SingleCondition_BeginAndEndGroup()
        {
            var r = CommandBuilder.Init()
                .Select("t", Array.Empty<string>())
                .WhereGroupAnd(new Collection<(CommandOperation, string, CommandMatchType, object?)>
                {
                    (CommandOperation.And, "a", CommandMatchType.Equal, 1)
                })
                .Build();
            Assert.That(r.Script, Does.Contain("(a ="));
            Assert.That(r.Script, Does.Contain(")"));
        }

        [Test]
        public void WhereGroup_NullCollection_NoOp()
        {
            var r = CommandBuilder.Init()
                .Select("t", Array.Empty<string>())
                .WhereGroupAnd(null!)
                .Build();
            Assert.That(r.Script, Does.Not.Contain("WHERE"));
        }
    }
}
