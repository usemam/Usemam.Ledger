namespace Usemam.Ledger.Domain.ValueObjects
{
    public class Money : ValueObject
    {
        #region Properties & Indexers

        public decimal Amount { get; set; }

        public string CurrencyCode { get; set; }

        #endregion
    }
}