#pragma warning disable SYSLIB0050 // FormatterServices is obsolete
using System.Reflection;
using System.Runtime.Serialization;
using LiteDB;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests;

public class OuiDatabaseTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly OuiDatabase _ouiDb;

    public OuiDatabaseTests()
    {
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);

        _ouiDb = (OuiDatabase)FormatterServices.GetUninitializedObject(typeof(OuiDatabase));

        var dbField = typeof(OuiDatabase).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        dbField!.SetValue(_ouiDb, _liteDb);

        var collection = _liteDb.GetCollection<OuiEntry>("oui_entries");
        collection.EnsureIndex(x => x.Prefix, true);

        var colField = typeof(OuiDatabase).GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
        colField!.SetValue(_ouiDb, collection);
    }

    public void Dispose()
    {
        _liteDb.Dispose();
        _ms.Dispose();
    }

    [Fact]
    public void OuiEntry_DefaultInitialization_SetsExpectedDefaults()
    {
        var entry = new OuiEntry();
        Assert.NotNull(entry.Id);
        Assert.Equal("", entry.Prefix);
        Assert.Equal("", entry.Vendor);
        Assert.Equal("", entry.ShortVendor);
    }

    [Fact]
    public void SeedAndGetCount_PopulatesCollectionCorrectly()
    {
        var entries = new List<OuiEntry>
        {
            new OuiEntry { Prefix = "00:0C:42", Vendor = "MikroTik", ShortVendor = "MikroTik" },
            new OuiEntry { Prefix = "00:50:56", Vendor = "VMware, Inc.", ShortVendor = "VMware" }
        };

        _ouiDb.Seed(entries);

        Assert.Equal(2, _ouiDb.GetCount());
    }

    [Fact]
    public void GetVendor_ExistingPrefix_ReturnsShortVendorOrVendor()
    {
        var entries = new List<OuiEntry>
        {
            new OuiEntry { Prefix = "00:0C:42", Vendor = "Routerboard.com", ShortVendor = "MikroTik" },
            new OuiEntry { Prefix = "00:11:22", Vendor = "Vendor Without Short", ShortVendor = "" }
        };

        _ouiDb.Seed(entries);

        Assert.Equal("MikroTik", _ouiDb.GetVendor("00:0C:42"));
        Assert.Equal("Vendor Without Short", _ouiDb.GetVendor("00:11:22"));
    }

    [Fact]
    public void GetVendor_NonExistentPrefix_ReturnsNull()
    {
        var entries = new List<OuiEntry>
        {
            new OuiEntry { Prefix = "00:0C:42", Vendor = "MikroTik", ShortVendor = "MikroTik" }
        };

        _ouiDb.Seed(entries);

        Assert.Null(_ouiDb.GetVendor("FF:FF:FF"));
    }

    [Fact]
    public void Seed_OverwritesPreviousEntries()
    {
        var initialEntries = new List<OuiEntry>
        {
            new OuiEntry { Prefix = "00:0C:42", Vendor = "Old Vendor", ShortVendor = "Old" }
        };
        _ouiDb.Seed(initialEntries);
        Assert.Equal(1, _ouiDb.GetCount());

        var newEntries = new List<OuiEntry>
        {
            new OuiEntry { Prefix = "00:50:56", Vendor = "VMware", ShortVendor = "VMware" },
            new OuiEntry { Prefix = "00:14:22", Vendor = "Dell Inc.", ShortVendor = "Dell" }
        };
        _ouiDb.Seed(newEntries);

        Assert.Equal(2, _ouiDb.GetCount());
        Assert.Null(_ouiDb.GetVendor("00:0C:42"));
        Assert.Equal("VMware", _ouiDb.GetVendor("00:50:56"));
    }

    [Fact]
    public void Instance_ReturnsNonNullSingleton()
    {
        Assert.NotNull(OuiDatabase.Instance);
    }

    [Fact]
    public void Dispose_DisposesUnderlyingDatabase()
    {
        using var ms = new MemoryStream();
        using var liteDb = new LiteDatabase(ms);

        var dbInstance = (OuiDatabase)FormatterServices.GetUninitializedObject(typeof(OuiDatabase));
        var dbField = typeof(OuiDatabase).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        dbField!.SetValue(dbInstance, liteDb);

        var exception = Record.Exception(() =>
        {
            dbInstance.Dispose();
            dbInstance.Dispose();
        });

        Assert.Null(exception);
    }
}
#pragma warning restore SYSLIB0050
