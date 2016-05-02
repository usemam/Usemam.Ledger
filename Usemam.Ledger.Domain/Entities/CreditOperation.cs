using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Entities
{
    public class CreditOperation : Entity
    {
        #region Properties & Indexers

        public Money Value { get; set; }

        public Deposit Deposit { get; set; }

        public CreditCategory Category { get; set; }

        public string Note { get; set; }

        #endregion
    }
}