using System.Text.RegularExpressions;

namespace SqlCommandBuilder.Test
{
    internal static class SqlAssert
    {
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string sql) => WhitespaceRegex.Replace(sql ?? string.Empty, " ").Trim();

        public static void Equal(string expected, string actual)
        {
            Assert.That(Normalize(actual), Is.EqualTo(Normalize(expected)));
        }
    }
}
