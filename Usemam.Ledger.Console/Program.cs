using System.Linq;

using Usemam.Ledger.Console.Interaction.Parsers;

namespace Usemam.Ledger.Console
{
    internal class Program
    {
        #region Static Fields and Constants

        private static readonly ICommandParser CommandParser = new RootCommandParser();

        #endregion

        #region Methods

        private static void Main(string[] args)
        {
            while (true)
            {
                System.Console.Write(">: ");
                var input = System.Console.ReadLine();
                if (input == null)
                {
                    return;
                }

                input = input.Trim();
                var parameters = input.Split(' ').Where(s => !string.IsNullOrEmpty(s)).ToArray();

                var command = CommandParser.Parse(parameters);
                command.Execute();
            }
        }

        #endregion
    }
}