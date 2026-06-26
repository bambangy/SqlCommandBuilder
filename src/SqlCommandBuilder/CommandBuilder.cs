namespace SqlCommandBuilder
{
    public class CommandBuilder
    {
        private class Command : QueryCommand
        {
        }

        public static IQueryCommand Init() => new Command();
    }
}
