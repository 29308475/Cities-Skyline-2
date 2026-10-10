using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CityStreetOverlay
{
    public sealed class Place
    {
        public string Name;
        public double Latitude, Longitude;
    }

    public struct Point
    {
        public double X, Z;
        public Point(double x, double z) { X = x; Z = z; }
    }

    public sealed class RoadDetails
    {
        public long WayId;
        public bool Bridge, Roundabout;
        public bool DirectionKnown, OneWay, Tunnel;
        public int Layer;
        public string Highway;
        public int Lanes;
        public float WidthMeters;
    }

    public sealed class StreetMap
    {
        public string Name;
        public double Latitude, Longitude, SizeMeters;
        public List<Point[]> Roads = new List<Point[]>();
        // Parallel metadata keeps saved v0.1/v0.2 point arrays readable.
        public List<RoadType> Types = new List<RoadType>();
        public List<RoadDetails> Details = new List<RoadDetails>();
        public int DetailsVersion;
        public int RemovedDriveways;
        [JsonIgnore]
        public bool HasDetails => DetailsVersion >= 1 && Details != null && Details.Count == Roads.Count;
        [JsonIgnore]
        public bool HasRoadTypes => Roads != null && Roads.Count > 0 && Types != null && Types.Count == Roads.Count;

        public void ValidateRoadTypes()
        {
            if (DetailsVersion > 0 && (!HasDetails || Details.Exists(d => d == null))) throw new FormatException("Ungültige Straßendetails.");
            if (Types == null || Types.Count == 0) return; // Legacy template, not guessed classifications.
            if (Roads == null || Types.Count != Roads.Count) throw new FormatException("Straßentypen passen nicht zur Vorlage.");
            foreach (var type in Types)
                if ((int)type < 0 || (int)type > 3) throw new FormatException("Ungültiger Straßentyp.");
        }
    }

    public static class Geography
    {
        const double Radius = 6378137;
        const double Radians = Math.PI / 180;
        public static Point Project(double lat, double lon, double originLat, double originLon)
        {
            return new Point((lon - originLon) * Radians * Radius * Math.Cos(originLat * Radians),
                (lat - originLat) * Radians * Radius);
        }

        public static string Bounds(double lat, double lon, double size)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon) || double.IsNaN(size) ||
                Math.Abs(lat) > 80 || Math.Abs(lon) > 180 || size < 500 || size > 10000)
                throw new ArgumentException("Ausschnitt muss 0,5–10 km groß sein; Breitengrad maximal 80°.");
            var dy = size / 2 / Radius / Radians;
            var dx = dy / Math.Cos(lat * Radians);
            if (lon - dx < -180 || lon + dx > 180)
                throw new ArgumentException("Ausschnitte über der Datumsgrenze werden noch nicht unterstützt.");
            return string.Join(",", new[] { lat - dy, lon - dx, lat + dy, lon + dx }
                .MapInvariant());
        }

        static IEnumerable<string> MapInvariant(this double[] values)
        {
            foreach (var v in values) yield return v.ToString("F7", CultureInfo.InvariantCulture);
        }

        // Liang–Barsky clipping retains crossing roads even if both endpoints lie outside.
        public static bool Clip(ref Point a, ref Point b, double half)
        {
            var dx = b.X - a.X; var dz = b.Z - a.Z;
            double lo = 0, hi = 1;
            var p = new[] { -dx, dx, -dz, dz };
            var q = new[] { a.X + half, half - a.X, a.Z + half, half - a.Z };
            for (var i = 0; i < 4; i++)
            {
                if (Math.Abs(p[i]) < 1e-12) { if (q[i] < 0) return false; continue; }
                var r = q[i] / p[i];
                if (p[i] < 0) lo = Math.Max(lo, r); else hi = Math.Min(hi, r);
                if (lo > hi) return false;
            }
            b = new Point(a.X + hi * dx, a.Z + hi * dz);
            a = new Point(a.X + lo * dx, a.Z + lo * dz);
            return true;
        }

        public static StreetMap Parse(string json, Place place, double size)
        {
            var root = JObject.Parse(json);
            if (root["remark"] != null) throw new InvalidOperationException("Kartendienst: " + root["remark"]);
            var map = new StreetMap { Name = place.Name, Latitude = place.Latitude, Longitude = place.Longitude, SizeMeters = size };
            var elements = root["elements"] as JArray ?? throw new FormatException("Straßendaten fehlen.");
            foreach (var way in elements)
            {
                var geometry = way["geometry"] as JArray;
                if ((string)way["type"] != "way" || geometry == null) continue;
                if (!RoadTypes.TryClassify((string)way["tags"]?["highway"], out var type)) continue;
                // Pedestrian plazas are areas, not road centerlines.
                if ((string)way["tags"]?["area"] == "yes") continue;
                var service = (string)way["tags"]?["service"];
                if ((string)way["tags"]?["highway"] == "service" &&
                    (service == "driveway" || service == "parking_aisle"))
                { map.RemovedDriveways++; continue; }
                var bridge = (string)way["tags"]?["bridge"];
                var details = new RoadDetails { WayId = (long?)way["id"] ?? 0,
                    Highway=(string)way["tags"]?["highway"],
                    Bridge = !string.IsNullOrEmpty(bridge) && bridge != "no",
                    Roundabout = (string)way["tags"]?["junction"] == "roundabout",
                    DirectionKnown=true, OneWay=(string)way["tags"]?["oneway"]=="yes"||(string)way["tags"]?["oneway"]=="1"||(string)way["tags"]?["oneway"]=="-1"||(string)way["tags"]?["junction"]=="roundabout",
                    Tunnel=(string)way["tags"]?["tunnel"]=="yes" };
                if(int.TryParse((string)way["tags"]?["layer"],out var layer))details.Layer=layer;
                if(int.TryParse((string)way["tags"]?["lanes"],out var lanes)&&lanes>0&&lanes<=16)details.Lanes=lanes;
                var widthText=((string)way["tags"]?["width"]??"").Trim();
                if(widthText.EndsWith(" m",StringComparison.Ordinal))widthText=widthText.Substring(0,widthText.Length-2).Trim();
                if(float.TryParse(widthText,NumberStyles.Float,CultureInfo.InvariantCulture,out var roadWidth)&&RoadWidths.Valid(roadWidth))details.WidthMeters=roadWidth;
                if((string)way["tags"]?["highway"]=="motorway" && (string)way["tags"]?["oneway"]!="no")details.OneWay=true;
                for (var i = 1; i < geometry.Count; i++)
                {
                    var a = Project((double)geometry[i-1]["lat"], (double)geometry[i-1]["lon"], place.Latitude, place.Longitude);
                    var b = Project((double)geometry[i]["lat"], (double)geometry[i]["lon"], place.Latitude, place.Longitude);
                    if (!Clip(ref a, ref b, size / 2)) continue;
                    if((string)way["tags"]?["oneway"]=="-1"){var swap=a;a=b;b=swap;}
                    if (double.IsNaN(a.X + a.Z + b.X + b.Z) || double.IsInfinity(a.X + a.Z + b.X + b.Z))
                        throw new FormatException("Ungültige Koordinaten.");
                    // Subdivide for terrain-following rendering.
                    int count = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(Math.Pow(b.X-a.X, 2)+Math.Pow(b.Z-a.Z, 2))/30));
                    for (int j = 0; j < count; j++)
                    {
                        map.Roads.Add(new[] { new Point(a.X+(b.X-a.X)*j/count, a.Z+(b.Z-a.Z)*j/count),
                            new Point(a.X+(b.X-a.X)*(j+1)/count, a.Z+(b.Z-a.Z)*(j+1)/count) });
                        map.Types.Add(type);
                        map.Details.Add(details);
                        if (map.Roads.Count > 80000) throw new InvalidOperationException("Zu viele Straßen. Bitte kleineren Ausschnitt wählen.");
                    }
                }
            }
            if (map.Roads.Count == 0) throw new InvalidOperationException("Keine Straßen im Ausschnitt gefunden.");
            map.DetailsVersion = 1;
            return map;
        }
    }
}
