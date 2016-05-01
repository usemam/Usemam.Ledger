using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Entities
{
    public class Deposit : Entity
    {
        #region Properties & Indexers

        public string Name { get; set; }

        public Money Value { get; set; }

        #endregion
    }
}