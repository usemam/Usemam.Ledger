using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface ICategoryRepository<T> where T : ICategory
    {
        #region Public methods

        T GetByName(string name);

        void Save(T category);

        T GetById(int id);

        #endregion
    }
}