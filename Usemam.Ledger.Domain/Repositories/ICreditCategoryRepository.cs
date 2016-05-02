using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface ICreditCategoryRepository : IValueObjectRepository<CreditCategory>
    {
        #region Public methods

        CreditCategory GetByName(string name);

        #endregion
    }
}