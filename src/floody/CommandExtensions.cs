using System.CommandLine;

namespace floody
{
    internal static class CommandExtensions
    {
        public static void AddRange(this Command rootCommand, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (symbol is Argument arg)
                {
                    rootCommand.Add(arg);
                    continue;
                }

                if (symbol is Option option)
                {
                    rootCommand.Add(option);
                    continue;
                }

                if (symbol is Command command)
                {
                    rootCommand.Add(command);
                    continue;
                }
            }
        }

    }
}