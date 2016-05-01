using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Entities
{
    public class DebitOperation : Entity
    {
        #region Properties & Indexers

        public Deposit Source { get; set; }

        public Money Value { get; set; }

        public DebitCategory Category { get; set; }

        public string Note { get; set; }

        #endregion
    }
}