using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class OracleCardCache
{
    private static List<Card.CardIdentifiers>? cachedCards;
    private static string cacheFilePath = Path.Combine(Environment.CurrentDirectory, "cache");
    private static string lastUpdateFile = Path.Combine(cacheFilePath, "last_update.txt");
    private static string? currentCacheFile;

    private static string GetCacheFileName(string bulkDataUrl)
    {
        var uri = new Uri(bulkDataUrl);
        var fileName = Path.GetFileName(uri.LocalPath);
        currentCacheFile = Path.Combine(cacheFilePath, fileName);
        return currentCacheFile;
    }

    public static bool ShouldUpdateCache()
    {
        Directory.CreateDirectory(cacheFilePath);

        if (!File.Exists(lastUpdateFile) || currentCacheFile == null || !File.Exists(currentCacheFile))
            return true;

        var lastUpdate = File.ReadAllText(lastUpdateFile);
        if (DateTime.TryParse(lastUpdate, out DateTime lastUpdateTime))
        {
            return (DateTime.Now - lastUpdateTime).TotalDays >= 1;
        }

        return true;
    }

    public static void SaveCache(List<Card.CardIdentifiers> cards, string bulkDataUrl)
    {
        Directory.CreateDirectory(cacheFilePath);
        cachedCards = cards;
        
        string cacheFile = GetCacheFileName(bulkDataUrl);
        string json = JsonConvert.SerializeObject(cards, Formatting.Indented);
        File.WriteAllText(cacheFile, json);
        File.WriteAllText(lastUpdateFile, DateTime.Now.ToString("O"));
    }

    public static List<Card.CardIdentifiers> LoadCache(string bulkDataUrl)
    {
        if (cachedCards != null)
            return cachedCards;

        string cacheFile = GetCacheFileName(bulkDataUrl);
        if (File.Exists(cacheFile))
        {
            string json = File.ReadAllText(cacheFile);
            cachedCards = JsonConvert.DeserializeObject<List<Card.CardIdentifiers>>(json);
            Console.WriteLine("Loaded cards from local cache");
        }

        return cachedCards ?? new List<Card.CardIdentifiers>();
    }

    public static List<Card.CardIdentifiers> GetCachedCards()
    {
        return cachedCards ?? new List<Card.CardIdentifiers>();
    }
}
