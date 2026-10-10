using System;
using System.Globalization;
using CityStreetOverlay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;
class Program
{
    static int checks;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    static void Reject(Action action, string name) { try { action(); } catch { checks++; return; } throw new Exception(name); }
    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        if(args.Length>0&&args[0]=="--planning-audit"){PlanningAudit.Fixture();PlanningAudit.Run(args[1]);return;}
        TemplateLibraryChecks.Run();
        MosaicChecks.Run();
        StampPlanChecks.Run();
        StreetBuildChecks.Run();
        RoadDefaultChecks.Run();
        JunctionRegressionChecks.Run();
        HighwayAlignmentChecks.Run();
        LaneAlignmentChecks.Run();
        RampConnectionChecks.Run();
        HighwayJunctionGeometryChecks.Run();
        IsolatedRoadChecks.Run();
        CurvePlannerChecks.Run();
        RoadGraphChecks.Run();
        var origin = Geography.Project(48,11,48,11);
        Check(origin.X == 0 && origin.Z == 0, "Origin");
        var east = Geography.Project(48,11.001,48,11);
        Check(east.X > 74 && east.X < 75 && east.Z == 0, "Longitude in meters at latitude 48");
        var north = Geography.Project(48.001,11,48,11);
        Check(north.Z > 111 && north.Z < 112 && north.X == 0, "North orientation");
        Check(Geography.Bounds(48,11,1000).Split(',').Length == 4, "Invariant bbox under German locale");
        var a = new Point(-1000,0); var b = new Point(1000,0);
        Check(Geography.Clip(ref a,ref b,500) && a.X == -500 && b.X == 500, "Crossing segment clipped");
        a = new Point(-1000,600); b = new Point(1000,600);
        Check(!Geography.Clip(ref a,ref b,500), "Outside segment rejected");
        Reject(()=>Geography.Bounds(89,0,1000), "Polar bounds");
        Reject(()=>Geography.Bounds(0,180,1000), "Dateline bounds");
        Reject(()=>Geography.Bounds(0,0,double.NaN), "NaN bounds");
        Reject(()=>Geography.Bounds(0,0,20000), "Oversized region");
        var place = new Place { Name="Fixture", Latitude=0, Longitude=0 };
        var map = Geography.Parse("{\"elements\":[{\"type\":\"way\",\"tags\":{\"highway\":\"residential\"},\"geometry\":[{\"lat\":0,\"lon\":-0.01},{\"lat\":0,\"lon\":0.01}]}]}",place,1000);
        Check(map.Roads.Count == 34, "Clipping and terrain subdivision");
        Check(Math.Abs(map.Roads[0][0].X+500)<0.001 && Math.Abs(map.Roads[33][1].X-500)<0.001, "Boundary endpoints");
        Reject(()=>Geography.Parse("{\"elements\":[]}",place,1000), "Empty map");
        Reject(()=>Geography.Parse("{\"remark\":\"timeout\",\"elements\":[]}",place,1000), "Partial response rejected");
        Check(map.HasRoadTypes && map.Types.All(t => t == RoadType.LocalRoad), "Subdivision retains road classification");
        CheckTypes("motorway,motorway_link", RoadType.Motorway);
        CheckTypes("trunk,trunk_link,primary,primary_link,secondary,secondary_link,tertiary,tertiary_link", RoadType.MainRoad);
        CheckTypes("residential,unclassified,living_street,service", RoadType.LocalRoad);
        CheckTypes("pedestrian,footway,cycleway,path,steps", RoadType.FootCyclePath);
        Check(!RoadTypes.TryClassify("construction", out _), "Construction not guessed as a road");
        Check(!RoadTypes.TryClassify(null, out _), "Missing tag not guessed");
        var ways = new JArray();
        foreach (var highway in new[] { "motorway", "secondary", "residential", "cycleway", "construction" })
            ways.Add(JObject.Parse("{\"type\":\"way\",\"tags\":{\"highway\":\""+highway+"\"},\"geometry\":[{\"lat\":0,\"lon\":0},{\"lat\":0,\"lon\":0.0001}]}"));
        var plaza = (JObject)ways[3].DeepClone();
        plaza["tags"]["area"] = "yes";
        ways.Add(plaza);
        var mixed = Geography.Parse(new JObject { ["elements"] = ways }.ToString(), place, 1000);
        Check(mixed.Roads.Count == 4 && mixed.Types.Distinct().Count() == 4, "Four classes parsed; areas and unsupported ways skipped");
        for (int mask = 0; mask < 16; mask++)
        {
            var filter = (RoadFilter)mask;
            var expectedCount = Enumerable.Range(0,4).Count(bit => (mask & (1 << bit)) != 0);
            Check(mixed.Types.Count(type => RoadTypes.IsVisible(filter,type)) == expectedCount, "Filter combination " + mask);
        }
        var saved = new SavedTemplate { Map = mixed, Filter = RoadFilter.Motorway | RoadFilter.FootCyclePath, OffsetX = 120, Scale = 1.5f };
        var restored = JsonConvert.DeserializeObject<SavedTemplate>(JsonConvert.SerializeObject(saved));
        restored.Map.ValidateRoadTypes();
        Check(restored.Map.Types.SequenceEqual(mixed.Types) && restored.Filter == saved.Filter && restored.OffsetX == 120 && restored.Scale == 1.5f,
            "Offline roundtrip retains road types, filters and placement");
        saved.Filter = RoadFilter.None;
        Check(JsonConvert.DeserializeObject<SavedTemplate>(JsonConvert.SerializeObject(saved)).Filter == RoadFilter.None, "All hidden survives save/load");
        var legacy = JsonConvert.DeserializeObject<SavedTemplate>("{\"Map\":{\"Roads\":[[{\"X\":0,\"Z\":0},{\"X\":10,\"Z\":0}]]},\"OffsetX\":321,\"Scale\":1.25}");
        legacy.Map.ValidateRoadTypes();
        Check(!legacy.Map.HasRoadTypes && legacy.Map.Roads.Count == 1 && legacy.Filter == RoadFilter.All && legacy.OffsetX == 321,
            "Legacy saves retain geometry and default to all filters without guessing types");
        mixed.Types.RemoveAt(0);
        Reject(mixed.ValidateRoadTypes, "Mismatched category metadata rejected");
        mixed.Types.Add((RoadType)99);
        Reject(mixed.ValidateRoadTypes, "Invalid road category rejected");
        CheckRoadWidths();
        Console.WriteLine($"PASS: {checks} geography/data checks");
        if (Array.IndexOf(args, "--online") >= 0)
        {
            using var service = new MapService();
            var places = service.Search("Berlin, Deutschland", System.Threading.CancellationToken.None).GetAwaiter().GetResult();
            Check(places.Count > 0, "Live search");
            var mapOnline = service.Load(places[0], 500, System.Threading.CancellationToken.None).GetAwaiter().GetResult();
            Check(mapOnline.Roads.Count > 0, "Live roads");
            Console.WriteLine($"PASS: online search and 500 m road region ({mapOnline.Roads.Count} segments)");
        }
    }

    static void CheckTypes(string tags, RoadType expected)
    {
        foreach (var tag in tags.Split(','))
        {
            Check(RoadTypes.TryClassify(tag, out var actual) && actual == expected, "Classification: " + tag);
            Check(Regex.IsMatch(tag, RoadTypes.HighwayPattern), "Download query includes " + tag);
        }
    }

    static void CheckRoadWidths()
    {
        var profiles = RoadWidths.Manual(5);
        RoadWidths.Validate(profiles);
        Check(profiles.Length == 4 && profiles.All(p => p.Meters == 5), "Legacy manual width kept for each class");
        profiles[0].Meters = 7;
        Check(profiles[1].Meters == 5, "Independent width settings per class");
        var option = new RoadOption { Id = "RoadPrefab:small", Name = "Kleine Straße", Width = 16 };
        profiles[2] = RoadWidths.Select(option);
        Check(profiles[2].Meters == 16 && profiles[2].PrefabId == option.Id, "Full road width, not half-width");
        option.Width = 20;
        Check(profiles[2].Meters == 16, "Selected profile stores a snapshot");
        Check(RoadWidths.Refresh(profiles, new[] { option }) && profiles[2].Meters == 20, "Loaded prefab width updates saved selection");
        Check(!RoadWidths.Refresh(profiles, new[] { option }), "Unchanged catalog avoids preview rebuild");
        var different = new RoadOption { Id = "RoadPrefab:other", Name = option.Name, Width = 40 };
        Check(!RoadWidths.Refresh(profiles, new[] { different }) && profiles[2].Meters == 20, "Missing road never replaced by same-name road");
        var invalid = new RoadOption { Id = option.Id, Width = float.NaN };
        Check(!RoadWidths.Refresh(profiles, new[] { invalid }) && profiles[2].Meters == 20, "Invalid catalog width ignored");
        var saved = new SavedTemplate { Scale = 2, WidthProfiles = profiles };
        var restored = JsonConvert.DeserializeObject<SavedTemplate>(JsonConvert.SerializeObject(saved));
        RoadWidths.Validate(restored.WidthProfiles);
        Check(restored.WidthProfiles[2].Meters == 20 && restored.WidthProfiles[2].PrefabId == option.Id && restored.Scale == 2,
            "Offline save retains actual width independently of map scale");
        Check(Math.Abs(RoadWidths.PreviewPixels(20, 1000, 1, 101) - 2) < 0.0001, "Preview uses world meters at 1:1");
        Check(Math.Abs(RoadWidths.PreviewPixels(20, 1000, 2, 101) - 1) < 0.0001, "Scaling extent does not double street width");
        Check(Math.Abs(RoadWidths.PreviewPixels(20, 1000, 0.5f, 101) - 4) < 0.0001, "Smaller map keeps physical road width");
        Check(JsonConvert.DeserializeObject<SavedTemplate>("{\"Width\":8}").WidthProfiles == null, "Older save permits manual width migration");
        Reject(() => RoadWidths.Manual(0), "Zero width rejected");
        Reject(() => RoadWidths.Manual(float.NaN), "NaN width rejected");
        Reject(() => RoadWidths.Manual(float.PositiveInfinity), "Infinite width rejected");
        Reject(() => RoadWidths.Manual(-5), "Negative width rejected");
        Reject(() => RoadWidths.Validate(new RoadWidthProfile[3]), "Incomplete profiles rejected");
        Reject(() => RoadWidths.Validate(new RoadWidthProfile[4]), "Null profile entries rejected");
        Reject(() => RoadWidths.Select(new RoadOption { Width = 16 }), "Prefab without stable ID rejected");
    }
}
