using Usemam.Ledger.Domain.Entities;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface IDepositRepository : IEntityRepository<Deposit>
    {
        #region Public methods

        Deposit GetByName(string name);

        #endregion
    }
}