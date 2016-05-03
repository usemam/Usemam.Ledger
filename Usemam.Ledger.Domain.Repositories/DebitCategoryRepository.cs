using System.Data;

using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public class DebitCategoryRepository : CategoryRepositoryBase<DebitCategory>
    {
        #region Constructors

        public DebitCategoryRepository(IDbConnection connection) : base(connection)
        {
        }

        #endregion

        #region Properties & Indexers

        protected override string TableName => "DebitCategory";

        #endregion
    }
}