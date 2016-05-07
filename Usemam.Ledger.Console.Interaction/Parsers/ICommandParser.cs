using Usemam.Ledger.Console.Interaction.Commands;

namespace Usemam.Ledger.Console.Interaction.Parsers
{
    public interface ICommandParser
    {
        #region Public methods

        IInteractionCommand Parse(params string[] parameters);

        #endregion
    }
}