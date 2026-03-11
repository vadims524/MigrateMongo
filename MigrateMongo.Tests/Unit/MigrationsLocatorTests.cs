using System.Reflection;
using MigrateMongo;
using MigrateMongo.Tests.Fakes;
using Xunit;

namespace MigrateMongo.Tests.Unit;

[Trait(TestCategories.Category, TestCategories.Unit)]
public sealed class MigrationsLocatorTests
{
    // ── ExtractTimestamp ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("20160608155948-blacklist_the_beatles", 20160608155948L)]
    [InlineData("20210101000001-first", 20210101000001L)]
    [InlineData("20991231235959-last_migration", 20991231235959L)]
    public void WhenFileNameHasTimestampPrefixThenExtractTimestampReturnsIt(string fileName, long expected)
    {
        var result = MigrationsLocator.ExtractTimestamp(fileName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("no-timestamp-here")]
    [InlineData("1234-short")]
    [InlineData("")]
    [InlineData("description-only")]
    public void WhenFileNameHasNoTimestampThenExtractTimestampReturnsNull(string fileName)
    {
        var result = MigrationsLocator.ExtractTimestamp(fileName);

        Assert.Null(result);
    }

    // ── FileNameToTypeName ────────────────────────────────────────────────────

    [Fact]
    public void WhenFileNameConvertedToTypeNameThenHasMigrationPrefixAndUnderscoreSeparator()
    {
        var result = MigrationsLocator.FileNameToTypeName("20160608155948-blacklist_the_beatles.cs");

        Assert.Equal("Migration_20160608155948_blacklist_the_beatles", result);
    }

    [Fact]
    public void WhenFileNameHasNoExtensionThenFileNameToTypeNameStillProducesValidName()
    {
        var result = MigrationsLocator.FileNameToTypeName("20210101000001-first");

        Assert.Equal("Migration_20210101000001_first", result);
    }

    // ── TypeNameToFileName ────────────────────────────────────────────────────

    [Fact]
    public void WhenTypeNameConvertedToFileNameThenTimestampAndDescriptionSeparatedByHyphen()
    {
        var result = MigrationsLocator.TypeNameToFileName("Migration_20160608155948_blacklist_the_beatles");

        Assert.Equal("20160608155948-blacklist_the_beatles", result);
    }

    [Fact]
    public void WhenTypeNameHasNoMigrationPrefixThenTypeNameToFileNameStillConverts()
    {
        var result = MigrationsLocator.TypeNameToFileName("20210101000001_first");

        Assert.Equal("20210101000001-first", result);
    }

    [Fact]
    public void WhenFileNameRoundTrippedThroughTypeNameThenOriginalIsRestored()
    {
        const string original = "20210101000001-first";

        var typeName = MigrationsLocator.FileNameToTypeName(original);
        var restored = MigrationsLocator.TypeNameToFileName(typeName);

        Assert.Equal(original, restored);
    }

    // ── GenerateFileName ──────────────────────────────────────────────────────

    [Fact]
    public void WhenGenerateFileNameCalledThenResultHas14DigitTimestampAndDescription()
    {
        var result = MigrationsLocator.GenerateFileName("my_migration", ".cs");

        Assert.Matches(@"^\d{14}-my_migration\.cs$", result);
    }

    [Fact]
    public void WhenDescriptionHasSpacesAndSpecialCharsThenGenerateFileNameSanitizes()
    {
        var result = MigrationsLocator.GenerateFileName("My Migration!", ".cs");

        // Spaces → underscore, '!' → underscore, lowercased
        Assert.Matches(@"^\d{14}-my_migration_\.cs$", result);
    }

    [Fact]
    public void WhenGenerateFileNameCalledTwiceInSuccessionThenTimestampsAreOrdered()
    {
        var first = MigrationsLocator.GenerateFileName("a", ".cs");
        var second = MigrationsLocator.GenerateFileName("b", ".cs");

        var ts1 = MigrationsLocator.ExtractTimestamp(first);
        var ts2 = MigrationsLocator.ExtractTimestamp(second);

        Assert.True(ts1 <= ts2);
    }

    // ── FindMigrations ────────────────────────────────────────────────────────

    [Fact]
    public void WhenAssemblyHasNoMigrationClassesThenFindMigrationsReturnsEmpty()
    {
        // mscorlib contains no IMigration implementations
        var result = MigrationsLocator.FindMigrations(typeof(string).Assembly);

        Assert.Empty(result);
    }

    [Fact]
    public void WhenAssemblyHasMigrationClassesThenFindMigrationsReturnsAllOrderedByTimestamp()
    {
        var result = MigrationsLocator.FindMigrations(typeof(Migration_20210101000001_First).Assembly);

        Assert.Contains(result, m => m.FileName == "20210101000001-First");
        Assert.Contains(result, m => m.FileName == "20210101000002-Second");

        // Verify ascending timestamp order
        var timestamps = result.Select(m => m.Timestamp).ToList();
        Assert.Equal(timestamps.OrderBy(t => t).ToList(), timestamps);
    }

    [Fact]
    public void WhenAssemblyIsNullThenFindMigrationsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MigrationsLocator.FindMigrations(null!));
    }

    // ── ComputeFileHash ───────────────────────────────────────────────────────

    [Fact]
    public void WhenFileExistsThenComputeFileHashReturnsSha256HexString()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "hello world");

        var hash = MigrationsLocator.ComputeFileHash(path);

        Assert.Equal(64, hash.Length);          // SHA-256 = 32 bytes = 64 lowercase hex chars
        Assert.Matches("^[0-9a-f]+$", hash);
    }

    [Fact]
    public void WhenFileContentsChangeThenComputeFileHashProducesDifferentHash()
    {
        var path = Path.GetTempFileName();

        File.WriteAllText(path, "version 1");
        var hash1 = MigrationsLocator.ComputeFileHash(path);

        File.WriteAllText(path, "version 2");
        var hash2 = MigrationsLocator.ComputeFileHash(path);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void WhenSameFileReadTwiceThenComputeFileHashIsConsistent()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "stable content");

        Assert.Equal(
            MigrationsLocator.ComputeFileHash(path),
            MigrationsLocator.ComputeFileHash(path));
    }
}
