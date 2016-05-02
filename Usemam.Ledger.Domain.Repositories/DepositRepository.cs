using System;
using System.Data;
using System.Linq;

using Dapper;

using Usemam.Ledger.Domain.Entities;
using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public class DepositRepository : IDepositRepository
    {
        #region Static Fields and Constants

        private const string SelectSql =
            @"select [Id],[Created],[Updated],[Name],[Amount],[CurrencyCode] from dbo.Deposit ";

        private const string UpdateSql =
            @"update dbo.Deposit set [Name] = @Name, [Amount] = @Amount, [Updated] = @Updated where [Id] = @Id";

        private const string InsertSql =
            @"insert into dbo.Deposit ([Name],[Amount],[CurrencyCode],[Created],[Updated]) values (@Name,@Amount,@CurrencyCode,@Created,@Created)";

        private const string DeleteSql =
            @"delete from dbo.Deposit where [Id] = @Id";

        #endregion

        #region Fields

        private readonly IDbConnection _connection;

        #endregion

        #region Constructors

        public DepositRepository(IDbConnection connection)
        {
            this._connection = connection;
        }

        #endregion

        #region Public methods

        public void Save(Deposit deposit)
        {
            if (deposit.Id > 0)
            {
                this._connection.Execute(
                    UpdateSql,
                    new {deposit.Id, deposit.Name, deposit.Value.Amount, Updated = DateTime.UtcNow});
            }
            else
            {
                this._connection.Execute(
                    InsertSql,
                    new {deposit.Name, deposit.Value.Amount, deposit.Value.CurrencyCode, Created = DateTime.UtcNow});
            }
        }

        public void Delete(Deposit deposit)
        {
            if (deposit.Id > 0)
            {
                this._connection.Execute(DeleteSql, new {deposit.Id});
            }
        }

        public Deposit GetById(int id)
        {
            return this.Get(SelectSql + "where [Id] = @Id", new {Id = id});
        }

        public Deposit GetByName(string name)
        {
            return this.Get(SelectSql + "where [Name] = @Name", new {Name = name});
        }

        #endregion

        #region Methods

        private Deposit Get(string sql, object param)
        {
            return this._connection
                .Query<Deposit, Money, Deposit>(
                    sql,
                    param: param,
                    splitOn: "Amount",
                    map: (deposit, money) =>
                    {
                        deposit.Value = money;
                        return deposit;
                    })
                .SingleOrDefault();
        }

        #endregion
    }
}