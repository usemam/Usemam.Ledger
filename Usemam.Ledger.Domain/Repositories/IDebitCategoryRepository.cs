using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface IDebitCategoryRepository : IValueObjectRepository<DebitCategory>
    {
        #region Public methods

        DebitCategory GetByName(string name);

        #endregion
    }
}