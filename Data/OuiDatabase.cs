using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;

namespace NodeRadarPro.Data;

public class OuiEntry
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Prefix { get; set; } = ""; // E.g., "00:0C:42"
    public string Vendor { get; set; } = "";
    public string ShortVendor { get; set; } = "";
}

public class OuiDatabase : IDisposable
{
    private static readonly Lazy<OuiDatabase> _instance = new(() => new OuiDatabase());
    public static OuiDatabase Instance => _instance.Value;

    private readonly LiteDatabase _db;
    private readonly ILiteCollection<OuiEntry> _collection;

    private OuiDatabase()
    {
        string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string myFolder = Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro");
        Directory.CreateDirectory(myFolder);
        string dbPath = Path.Combine(myFolder, "oui.db");

        _db = new LiteDatabase($"Filename={dbPath};Connection=shared;");
        _collection = _db.GetCollection<OuiEntry>("oui_entries");
        _collection.EnsureIndex(x => x.Prefix, true);
    }

    public string? GetVendor(string prefix)
    {
        var entry = _collection.FindOne(x => x.Prefix == prefix);
        return entry?.ShortVendor ?? entry?.Vendor;
    }

    public void Seed(IEnumerable<OuiEntry> entries)
    {
        _collection.DeleteAll();
        _collection.InsertBulk(entries);
        _collection.EnsureIndex(x => x.Prefix, true);
    }

    public int GetCount()
    {
        return _collection.Count();
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
