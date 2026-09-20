using System;
using System.IO;
using LiteDB;

class Program
{
    static void Main()
    {
        string path = "test_corrupt.db";
        File.WriteAllText(path, "This is garbage plain text data!!!");
        try
        {
            using var db = new LiteDatabase($"Filename={path};Password=testpass;Connection=shared");
            var col = db.GetCollection("test");
            col.EnsureIndex("id", "$.id");
            Console.WriteLine("EnsureIndex succeeded!");
            var doc = col.FindById(1);
            Console.WriteLine("FindById succeeded!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception caught: {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
