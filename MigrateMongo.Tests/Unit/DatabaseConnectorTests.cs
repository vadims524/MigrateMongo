using MigrateMongo;
using Xunit;

namespace MigrateMongo.Tests.Unit;

// MongoClient construction is lazy — no real MongoDB connection is needed here.
[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class DatabaseConnectorTests
{
    [Fact]
    public void WhenConfigHasDatabaseNameThenConnectUsesThat()
    {
        var config = new MigrateMongoConfig
        {
            MongoDB = new MongoDbSettings
            {
                Url = "mongodb://localhost:27017",
                DatabaseName = "config_db"
            }
        };

        var (db, _) = DatabaseConnector.Connect(config);

        Assert.Equal("config_db", db.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void WhenUrlContainsDatabaseNameAndConfigDoesNotThenConnectUsesUrlDatabase()
    {
        var config = new MigrateMongoConfig
        {
            MongoDB = new MongoDbSettings
            {
                Url = "mongodb://localhost:27017/url_db",
                DatabaseName = null
            }
        };

        var (db, _) = DatabaseConnector.Connect(config);

        Assert.Equal("url_db", db.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void WhenBothConfigAndUrlHaveDatabaseNameThenConnectPrefersConfig()
    {
        var config = new MigrateMongoConfig
        {
            MongoDB = new MongoDbSettings
            {
                Url = "mongodb://localhost:27017/url_db",
                DatabaseName = "config_db"
            }
        };

        var (db, _) = DatabaseConnector.Connect(config);

        Assert.Equal("config_db", db.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void WhenNeitherConfigNorUrlHaveDatabaseNameThenConnectThrowsInvalidOperationException()
    {
        var config = new MigrateMongoConfig
        {
            MongoDB = new MongoDbSettings
            {
                Url = "mongodb://localhost:27017",
                DatabaseName = null
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => DatabaseConnector.Connect(config));
        Assert.Contains("No database name specified", ex.Message);
    }

    [Fact]
    public void WhenConfigIsNullThenConnectThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DatabaseConnector.Connect(null!));
    }
}
