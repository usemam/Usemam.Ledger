using System;

namespace Usemam.Ledger.Domain.Entities
{
    public abstract class Entity
    {
        #region Properties & Indexers

        public int Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        #endregion
    }
}