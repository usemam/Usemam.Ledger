namespace Usemam.Ledger.Domain.ValueObjects
{
    public class CreditCategory : ICategory
    {
        #region Properties & Indexers

        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        #endregion
    }
}