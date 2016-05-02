using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface IValueObjectRepository<T> where T : ValueObject
    {
        #region Public methods

        void Save(T valueObject);

        T GetById(int id);

        #endregion
    }
}