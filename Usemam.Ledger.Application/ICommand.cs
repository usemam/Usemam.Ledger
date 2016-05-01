namespace Usemam.Ledger.Application
{
    public interface ICommand
    {
        #region Public methods

        void Execute();

        void Rollback();

        #endregion
    }
}