namespace SqlCommandBuilder.Test
{
    public class MatchTypeTests
    {
        [TestCase(CommandMatchType.Equal, "=")]
        [TestCase(CommandMatchType.NotEqual, "<>")]
        [TestCase(CommandMatchType.Lesser, "<")]
        [TestCase(CommandMatchType.LesserOrEqual, "<=")]
        [TestCase(CommandMatchType.Greater, ">")]
        [TestCase(CommandMatchType.GreaterOrEqual, ">=")]
        [TestCase(CommandMatchType.Contains, "LIKE")]
        [TestCase(CommandMatchType.NotContains, "NOT LIKE")]
        [TestCase(CommandMatchType.IsIn, "IN")]
        [TestCase(CommandMatchType.IsNotIn, "NOT IN")]
        public void MatchType_RendersOperator(CommandMatchType match, string op)
        {
            var r = CommandBuilder.Init()
                .Select("t", Array.Empty<string>())
                .WhereAnd("col", match, "v")
                .Build();
            Assert.That(r.Script, Does.Contain($"col {op}"));
            Assert.That(r.Parameters, Has.Count.EqualTo(1));
        }

        [TestCase(CommandMatchType.IsNull, "IS NULL")]
        [TestCase(CommandMatchType.IsNotNull, "IS NOT NULL")]
        public void MatchType_Null_DoesNotBindValue(CommandMatchType match, string op)
        {
            var r = CommandBuilder.Init()
                .Select("t", Array.Empty<string>())
                .WhereAnd("col", match, null)
                .Build();
            Assert.That(r.Script, Does.Contain($"col {op}"));
            Assert.That(r.Parameters, Is.Empty);
        }
    }
}
