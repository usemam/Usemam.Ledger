using System.Data;

using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public class CreditCategoryRepository : CategoryRepositoryBase<CreditCategory>
    {
        #region Constructors

        public CreditCategoryRepository(IDbConnection connection) : base(connection)
        {
        }

        #endregion

        #region Properties & Indexers

        protected override string TableName => "CreditCategory";

        #endregion
    }
}