namespace Usemam.Ledger.Application
{
    public interface IQuery<out T>
    {
        #region Public methods

        T Execute();

        #endregion
    }
}