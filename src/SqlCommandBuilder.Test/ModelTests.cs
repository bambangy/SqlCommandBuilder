using System.Collections.ObjectModel;

namespace SqlCommandBuilder.Test
{
    public class ModelTests
    {
        [Test]
        public void CommandCondition_Add_StoresProperties()
        {
            var c = CommandCondition.Add("col", CommandMatchType.Equal, 42, CommandOperation.Or, beginGroup: true, endGroup: true);
            Assert.Multiple(() =>
            {
                Assert.That(c.Column, Is.EqualTo("col"));
                Assert.That(c.Match, Is.EqualTo(CommandMatchType.Equal));
                Assert.That(c.Value, Is.EqualTo(42));
                Assert.That(c.Operation, Is.EqualTo(CommandOperation.Or));
                Assert.That(c.BeginGroup, Is.True);
                Assert.That(c.EndGroup, Is.True);
            });
        }

        [Test]
        public void CommandCondition_DefaultOptionalProps()
        {
            var c = CommandCondition.Add("col", CommandMatchType.Equal, null);
            Assert.Multiple(() =>
            {
                Assert.That(c.Operation, Is.Null);
                Assert.That(c.BeginGroup, Is.False);
                Assert.That(c.EndGroup, Is.False);
                Assert.That(c.Value, Is.Null);
            });
        }

        [Test]
        public void CommandReference_Add_StoresProperties()
        {
            var on = new Collection<CommandCondition> { CommandCondition.Add("a", CommandMatchType.Equal, "b") };
            var r = CommandReference.Add(CommandReferenceType.LeftJoin, "table", on);
            Assert.Multiple(() =>
            {
                Assert.That(r.ReferenceType, Is.EqualTo(CommandReferenceType.LeftJoin));
                Assert.That(r.TableName, Is.EqualTo("table"));
                Assert.That(r.OnConditions, Is.SameAs(on));
            });
        }

        [Test]
        public void CommandSort_Add_StoresProperties()
        {
            var s = CommandSort.Add("col", CommandOrderDirection.Desc);
            Assert.Multiple(() =>
            {
                Assert.That(s.Column, Is.EqualTo("col"));
                Assert.That(s.Direction, Is.EqualTo(CommandOrderDirection.Desc));
            });
        }

        [Test]
        public void CommandBuilder_Init_ReturnsFreshInstance()
        {
            var a = CommandBuilder.Init();
            var b = CommandBuilder.Init();
            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void CommandResultBuilder_Create_ReturnsFreshInstance()
        {
            var a = CommandResultBuilder.Create();
            var b = CommandResultBuilder.Create();
            Assert.Multiple(() =>
            {
                Assert.That(a, Is.Not.SameAs(b));
                Assert.That(a.Script, Is.EqualTo(string.Empty));
                Assert.That(a.Parameters, Is.Not.Null);
            });
        }

        [Test]
        public void IQueryCommandResult_SettersWork()
        {
            var r = CommandResultBuilder.Create();
            r.Script = "x";
            r.Parameters = new Dictionary<string, object?> { { "k", "v" } };
            Assert.Multiple(() =>
            {
                Assert.That(r.Script, Is.EqualTo("x"));
                Assert.That(r.Parameters["k"], Is.EqualTo("v"));
            });
        }
    }
}
