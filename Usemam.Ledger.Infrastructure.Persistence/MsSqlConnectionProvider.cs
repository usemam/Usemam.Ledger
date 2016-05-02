using System.Configuration;
using System.Data;
using System.Data.Common;

namespace Usemam.Ledger.Infrastructure.Persistence
{
    public class MsSqlConnectionProvider : IDbConnectionProvider
    {
        #region Static Fields and Constants

        private const string ProviderName = "System.Data.SqlClient";

        #endregion

        #region Fields

        private readonly string _connectionString;

        private readonly DbProviderFactory _provider;

        #endregion

        #region Constructors

        public MsSqlConnectionProvider(string connectionName)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[connectionName];
            if (connectionString == null)
                throw new ConfigurationErrorsException($"Failed to find connection string named '{connectionName}'.");

            this._connectionString = connectionString.ConnectionString;
            this._provider = DbProviderFactories.GetFactory(ProviderName);
        }

        #endregion

        #region Public methods

        public IDbConnection CreateConnection()
        {
            var connection = this._provider.CreateConnection();
            if (connection == null)
                throw new ConfigurationErrorsException(
                    $"Failed to create a connection using provider '{ProviderName}'.");

            connection.ConnectionString = this._connectionString;
            connection.Open();
            return connection;
        }

        #endregion
    }
}