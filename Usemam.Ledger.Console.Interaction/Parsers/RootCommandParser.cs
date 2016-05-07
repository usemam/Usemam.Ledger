using System;
using System.Linq;

using Usemam.Ledger.Console.Interaction.Commands;

namespace Usemam.Ledger.Console.Interaction.Parsers
{
    public class RootCommandParser : ICommandParser
    {
        #region Public methods

        public IInteractionCommand Parse(params string[] parameters)
        {
            if (parameters.Length == 0)
            {
                return new EmptyInputCommand();
            }

            var action = parameters.First();
            switch (action)
            {
                case "add":
                    throw new NotImplementedException();
                case "show":
                    throw new NotImplementedException();
                case "help":
                    throw new NotImplementedException();
                case "exit":
                    return new ExitCommand();
                default:
                    return new ActionNotFoundCommand(parameters);
            }
        }

        #endregion
    }
}