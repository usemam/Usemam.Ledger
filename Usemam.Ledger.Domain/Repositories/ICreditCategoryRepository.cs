using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface ICreditCategoryRepository
    {
        #region Public methods

        CreditCategory GetByName(string name);

        void Save(CreditCategory category);

        CreditCategory GetById(int id);

        #endregion
    }
}