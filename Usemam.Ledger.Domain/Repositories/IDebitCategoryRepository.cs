using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface IDebitCategoryRepository
    {
        #region Public methods

        DebitCategory GetByName(string name);

        void Save(DebitCategory category);

        DebitCategory GetById(int id);

        #endregion
    }
}