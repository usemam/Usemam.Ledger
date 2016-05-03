namespace Usemam.Ledger.Domain.ValueObjects
{
    public interface ICategory
    {
        #region Properties & Indexers

        int Id { get; set; }

        string Name { get; set; }

        string Description { get; set; }

        #endregion
    }
}