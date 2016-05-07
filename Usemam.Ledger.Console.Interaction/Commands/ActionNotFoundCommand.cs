namespace Usemam.Ledger.Console.Interaction.Commands
{
    public class ActionNotFoundCommand : IInteractionCommand
    {
        #region Fields

        private readonly string[] _parameters;

        #endregion

        #region Constructors

        public ActionNotFoundCommand(string[] parameters)
        {
            this._parameters = parameters;
        }

        #endregion

        #region Public methods

        public void Execute()
        {
            System.Console.WriteLine($"Action for '{string.Join(" ", this._parameters)}' not found.");
        }

        #endregion
    }
}