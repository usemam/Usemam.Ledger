using System.Data;

namespace Usemam.Ledger.Infrastructure.Persistence
{
    public interface IDbConnectionProvider
    {
        #region Public methods

        IDbConnection CreateConnection();

        #endregion
    }
}