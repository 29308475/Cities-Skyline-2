using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CityStreetOverlay
{
    public sealed class TemplateEntry
    {
        public string Id, City, Location, Preview;
        public int Version;
        public int TileCount;
        public DateTime SavedUtc;
    }

    public sealed class TemplateLibrary
    {
        readonly string directory, legacyPath;
        public int UnreadableCount { get; private set; }
        public TemplateLibrary(string dataDirectory)
        {
            directory = Path.Combine(dataDirectory, "Templates");
            legacyPath = Path.Combine(dataDirectory, "last-template.json");
        }
        public static string CityName(string name)
        {
            var city = (name ?? "").Split(',')[0].Trim();
            return city.Length == 0 ? "Unbenannte Stadt" : city;
        }
        string Resolve(string id)
        {
            if (id == "legacy") return legacyPath;
            Guid guid;
            if (!Guid.TryParseExact(id, "N", out guid)) throw new ArgumentException("Ungültige Vorlagenauswahl.");
            return Path.Combine(directory, guid.ToString("N") + ".json");
        }
        public SavedTemplate Load(string id)
        {
            var saved = JsonConvert.DeserializeObject<SavedTemplate>(File.ReadAllText(Resolve(id)));
            if (saved?.Map?.Roads == null) throw new FormatException("Ungültige Vorlage.");
            return saved;
        }
        public void Delete(string id)
        {
            // Resolve accepts only a GUID or the explicit legacy file; never a supplied path.
            var path=Resolve(id);
            if(!File.Exists(path))throw new FileNotFoundException("Diese Vorlage wurde bereits entfernt.");
            File.Delete(path);
        }
        public List<TemplateEntry> List()
        {
            UnreadableCount = 0;
            var ids = new List<string>();
            if (File.Exists(legacyPath)) ids.Add("legacy");
            if (Directory.Exists(directory)) ids.AddRange(Directory.GetFiles(directory, "*.json").Select(Path.GetFileNameWithoutExtension));
            var result = new List<TemplateEntry>();
            foreach (var id in ids)
            {
                try
                {
                    var saved = Load(id);
                    result.Add(new TemplateEntry { Id=id, City=CityName(saved.Map.Name), Location=saved.Map.Name,
                        Version=Math.Max(1,saved.Version), SavedUtc=saved.SavedUtc == default(DateTime) ? File.GetLastWriteTimeUtc(Resolve(id)) : saved.SavedUtc,
                        Preview=saved.Preview ?? "", TileCount=saved.Tiles?.Count ?? 1 });
                }
                catch (Exception) { UnreadableCount++; }
            }
            return result.OrderBy(e=>e.City,StringComparer.OrdinalIgnoreCase).ThenByDescending(e=>e.Version).ToList();
        }
        public TemplateEntry Save(SavedTemplate saved)
        {
            if (saved?.Map?.Roads == null) throw new ArgumentException("Keine Vorlage geladen.");
            var city = CityName(saved.Map.Name);
            var entries = List();
            if (UnreadableCount != 0) throw new IOException("Eine gespeicherte Vorlage ist nicht lesbar. Versionsnummer kann nicht sicher bestimmt werden.");
            saved.Version = entries.Where(e=>string.Equals(e.City,city,StringComparison.OrdinalIgnoreCase)).Select(e=>e.Version).DefaultIfEmpty(0).Max()+1;
            saved.SavedUtc = DateTime.UtcNow;
            Directory.CreateDirectory(directory);
            var id = Guid.NewGuid().ToString("N");
            var path = Resolve(id);
            var temp = path + ".tmp";
            try { File.WriteAllText(temp,JsonConvert.SerializeObject(saved)); File.Move(temp,path); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return new TemplateEntry { Id=id,City=city,Version=saved.Version,SavedUtc=saved.SavedUtc,Location=saved.Map.Name,Preview=saved.Preview };
        }
    }
}
