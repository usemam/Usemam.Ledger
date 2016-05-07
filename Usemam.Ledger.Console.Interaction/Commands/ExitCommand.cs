using System;

namespace Usemam.Ledger.Console.Interaction.Commands
{
    public class ExitCommand : IInteractionCommand
    {
        #region Public methods

        public void Execute()
        {
            Environment.Exit(0);
        }

        #endregion
    }
}