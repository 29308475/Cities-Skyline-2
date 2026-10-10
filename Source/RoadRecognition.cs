using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CityStreetOverlay
{
    public static class RoadRecognition
    {
        public static string DisplayName(string name)
        {
            var labels=new Dictionary<string,string>{{"motorway_link","Autobahnrampe"},{"trunk_link","Schnellstraßenrampe"},{"primary_link","Verbindungsrampe"},{"secondary_link","Verbindungsrampe"},{"tertiary_link","Verbindungsrampe"},{"motorway","Autobahn"},{"trunk","Schnellstraße"},{"primary","Hauptverkehrsstraße"},{"secondary","Hauptstraße"},{"tertiary","Verbindungsstraße"},{"residential","Wohnstraße"},{"unclassified","Nebenstraße"},{"service","Zufahrt"},{"footway","Fußweg"},{"cycleway","Radweg"},{"path","Weg"},{"pedestrian","Fußgängerstraße"}};
            foreach(var pair in labels)if(name==pair.Key||name?.StartsWith(pair.Key+" ·",StringComparison.Ordinal)==true)return pair.Value+name.Substring(pair.Key.Length);
            return name;
        }
        public static bool StandardRoadLabel(string label)
        {
            // Deliberately conservative allow-list: modifiers such as covered bridges,
            // gravel, tram or bus roads must never win a width-only comparison.
            return Regex.IsMatch(label??"",@"^(One|Two|Three|Four|Five|Six|Seven|Eight)-Lane (One-Way )?(Road|Highway)$",RegexOptions.IgnoreCase)||
                Regex.IsMatch(label??"",@"^(Ein|Zwei|Drei|Vier|Fünf|Sechs|Sieben|Acht)spurige (Einbahnstraße|Straße|Autobahn|Einbahnschnellstraße|Schnellstraße)$",RegexOptions.IgnoreCase);
        }
        // A catalogue label is a hint, not a substitute for a full lane-layout inspection.
        public static int LabelLanes(string label)
        {
            var s=(label??"").ToLowerInvariant();
            string[] en={"one","two","three","four","five","six","seven","eight"};
            string[] de={"ein","zwei","drei","vier","fünf","sechs","sieben","acht"};
            for(int i=0;i<en.Length;i++)if(s.Contains(en[i]+"-lane")||s.Contains(de[i]+"spurig"))return i+1;
            var match=Regex.Match(s,@"\b([1-8])[- ]lane\b");return match.Success?int.Parse(match.Groups[1].Value):0;
        }
        public static List<RoadSubgroup> Suggest(StreetMap map,List<RoadOption> options,RoadWidthProfile[] fallback,List<RoadSubgroup> existing,HashSet<string> excluded)
        {
            var result=new List<RoadSubgroup>();if(!map.HasDetails)return result;
            var assigned=RoadSubgroups.Lookup(existing);var buckets=new Dictionary<string,RoadSubgroup>();
            for(int i=0;i<map.Roads.Count;i++)
            {
                var d=map.Details[i];var key=RoadExclusions.Key(map.Roads[i]);
                if(string.IsNullOrEmpty(d.Highway)||d.Roundabout||excluded.Contains(key)||assigned.ContainsKey(key))continue;
                int category=map.HasRoadTypes?(int)map.Types[i]:2;
                bool ramp=d.Highway.EndsWith("_link",StringComparison.Ordinal);
                if(!ramp&&d.Lanes==0&&d.WidthMeters==0&&category!=3)continue;
                int lanes=d.Lanes;bool estimated=ramp&&lanes==0;if(estimated)lanes=1;
                var candidates=options.Where(o=>category!=3&&lanes>0&&o.AutomaticEligible&&o.IsHighway==(category==0)&&!o.IsPath&&d.DirectionKnown&&o.DirectionKnown&&o.IsOneWay==d.OneWay);
                if(lanes>0)candidates=candidates.Where(o=>o.Lanes==lanes);
                var matches=candidates.ToList();
                // Ambiguous variants remain manual; OSM carriageway width is not total prefab width.
                var option=matches.Count==1?matches[0]:matches.FirstOrDefault(o=>o.Id==fallback[category].PrefabId);
                // Never present an unchecked category fallback as a recognized road.
                // It may have the wrong direction, lane count or highway rules.
                if(option==null)continue;
                var profile=RoadWidths.Select(option);
                profile.Opacity=fallback[category].Opacity;
                string bucket=category+":"+d.Highway+":"+lanes+":"+d.OneWay+":"+d.WidthMeters+":"+profile.PrefabId;
                if(!buckets.TryGetValue(bucket,out var group))
                {
                    string name=DisplayName(d.Highway)+(lanes>0?" · "+lanes+(lanes==1?" Spur":" Spuren"):"")+(estimated?" (geschätzt)":"");
                    group=new RoadSubgroup{Id=Guid.NewGuid().ToString("N"),Name=name,Category=category,Profile=profile};
                    buckets[bucket]=group;result.Add(group);
                }
                group.Roads.Add(key);
            }
            return result;
        }
    }
}
