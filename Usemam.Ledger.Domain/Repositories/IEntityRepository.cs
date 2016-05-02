using Usemam.Ledger.Domain.Entities;

namespace Usemam.Ledger.Domain.Repositories
{
    public interface IEntityRepository<T> where T : Entity
    {
        #region Public methods

        void Save(T entity);

        void Delete(T entity);

        T GetById(int id);

        #endregion
    }
}