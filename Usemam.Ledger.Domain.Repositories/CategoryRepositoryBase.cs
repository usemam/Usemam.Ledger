using System.Data;
using System.Linq;

using Dapper;

using Usemam.Ledger.Domain.ValueObjects;

namespace Usemam.Ledger.Domain.Repositories
{
    public abstract class CategoryRepositoryBase<T> : ICategoryRepository<T> where T : ICategory
    {
        #region Fields

        private const string InsertSql = @"insert into dbo.{0} ([Name],[Description]) values(@Name,@Description)";

        private const string SelectSql = @"select [Id],[Name],[Description] from dbo.{0} ";

        private const string UpdateSql = @"update dbo.{0} set [Name] = @Name, [Description] = @Description where [Id] = @Id";

        private readonly IDbConnection _connection;

        #endregion

        protected abstract string TableName { get; }

        #region Constructors

        protected CategoryRepositoryBase(IDbConnection connection)
        {
            this._connection = connection;
        }

        #endregion

        #region Public methods

        public T GetByName(string name)
        {
            return this.Get(SelectSql + "where [Name] = @Name", new {Name = name});
        }

        public void Save(T category)
        {
            var sql = string.Format(category.Id > 0 ? UpdateSql : InsertSql, this.TableName);
            this._connection.Execute(sql, category);
        }

        public T GetById(int id)
        {
            return this.Get(SelectSql + "where [Id] = @Id", new {Id = id});
        }

        #endregion

        #region Methods

        private T Get(string sql, object param)
        {
            return this._connection.Query<T>(string.Format(sql, this.TableName), param).SingleOrDefault();
        }

        #endregion
    }
}