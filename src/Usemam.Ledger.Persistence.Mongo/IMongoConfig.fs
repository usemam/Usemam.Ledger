namespace Usemam.Ledger.Persistence.Mongo

type IMongoConfig =
    abstract MongoConnectionString : string
    abstract MongoDatabaseName : string
