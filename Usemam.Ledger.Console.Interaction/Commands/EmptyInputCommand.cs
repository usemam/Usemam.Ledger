namespace Usemam.Ledger.Console.Interaction.Commands
{
    public class EmptyInputCommand : IInteractionCommand
    {
        #region Public methods

        public void Execute()
        {
            System.Console.WriteLine("Enter your command.");
        }

        #endregion
    }
}