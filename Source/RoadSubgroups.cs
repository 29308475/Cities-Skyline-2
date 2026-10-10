using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public sealed class RoadSubgroup
    {
        public string Id,Name;
        public int Category;
        public RoadWidthProfile Profile;
        public List<string> Roads=new List<string>();
    }
    public sealed class RoadBatch
    {
        public int Category;
        public RoadWidthProfile Profile;
        public HashSet<int> Indices=new HashSet<int>();
        public bool IsRing;
        public bool IsApproach;
        public bool IsRamp;
        public StreetBuildPlan Plan;
    }
    public static class RoadSubgroups
    {
        public static void Validate(List<RoadSubgroup> groups)
        {
            if(groups==null||groups.Count>64)throw new FormatException("Ungültige Untergruppen.");
            var ids=new HashSet<string>();var keys=new HashSet<string>();
            foreach(var g in groups)
            {
                if(g==null||string.IsNullOrEmpty(g.Id)||!ids.Add(g.Id)||g.Category<0||g.Category>3||g.Roads==null||g.Roads.Count>320000)
                    throw new FormatException("Ungültige Straßenuntergruppe.");
                RoadWidths.Validate(new[]{g.Profile,g.Profile,g.Profile,g.Profile});
                foreach(var key in g.Roads)if(string.IsNullOrEmpty(key)||!keys.Add(key))throw new FormatException("Mehrfache Straßenzuordnung.");
            }
        }
        public static Dictionary<string,RoadSubgroup> Lookup(List<RoadSubgroup> groups)
        {
            var result=new Dictionary<string,RoadSubgroup>();
            foreach(var group in groups)foreach(var key in group.Roads)result[key]=group;
            return result;
        }
        public static List<RoadBatch> Plan(StreetMap source,List<RoadSubgroup> groups,RoadWidthProfile[] profiles,float scale,int? category,bool circles,int limit,RoadWidthProfile ringProfile=null,bool leftTraffic=false,RoadWidthProfile approachProfile=null,StreetMap alignmentContext=null)
        {
            var originalRoads=source.Roads;
            source=HighwayAlignment.Prepare(source,groups,profiles,scale,leftTraffic,alignmentContext);
            var rings=new HashSet<int>();
            if(ringProfile!=null)source=RingRouting.Prepare(source,scale,leftTraffic,out rings);
            var approaches=approachProfile==null?new HashSet<int>():RingRouting.Approaches(source,rings);
            var lookup=Lookup(groups);var effective=new StreetMap{Roads=source.Roads,Details=source.Details,DetailsVersion=source.DetailsVersion};
            var batches=new Dictionary<string,RoadBatch>();
            for(int i=0;i<source.Roads.Count;i++)
            {
                lookup.TryGetValue(RoadExclusions.Key(originalRoads[i]),out var group);
                if(rings.Contains(i))group=null;
                int type=group?.Category??(source.HasRoadTypes?(int)source.Types[i]:2);
                effective.Types.Add((RoadType)type);
                if(category.HasValue&&category.Value!=type)continue;
                var selectedProfile=group?.Profile??profiles[type];
                // Editing groups need not introduce native nodes when the actual road is identical.
                string id="road:"+type+":"+selectedProfile.PrefabId+":"+selectedProfile.Meters.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if(rings.Contains(i))id="ring:"+type;
                // Dedicated roundabout connectors must not inherit a wider recognition subgroup.
                bool approach=approaches.Contains(i);
                if(approach)id="approach:"+type;
                bool ramp=source.HasDetails&&source.Details[i].OneWay&&(source.Details[i].Highway=="motorway_link"||source.Details[i].Highway=="trunk_link");
                if(ramp)id+=":ramp";
                // Different height regimes remain separate courses, even with the same road asset.
                if(source.HasDetails){var d=source.Details[i];id+=":"+d.Layer+":"+d.Bridge+":"+d.Tunnel;}
                if(!batches.TryGetValue(id,out var batch))batches[id]=batch=new RoadBatch{Category=type,IsRing=rings.Contains(i),IsApproach=approach,IsRamp=ramp,Profile=rings.Contains(i)?ringProfile:approach?approachProfile:group?.Profile??profiles[type]};
                batch.Indices.Add(i);
            }
            int total=0;
            foreach(var batch in batches.Values)
            {
                batch.Plan=StreetBuildPlan.Create(effective,(RoadType)batch.Category,scale,batch.Indices,limit,circles&&ringProfile==null);
                total+=batch.Plan.Lines.Count;
                if(total>limit)throw new InvalidOperationException("Mehr als 2.000 Bauabschnitte. Bitte weitere Bereiche ausschließen.");
            }
            var result=batches.Values.ToList();
            // Full context keeps partial selection/highlighting on the same ramp curve.
            var context=alignmentContext==null?source:HighwayAlignment.Prepare(alignmentContext,groups,profiles,scale,leftTraffic);
            RampConnections.Apply(result,context,alignmentContext?.Roads??originalRoads,groups,profiles,scale,source);
            HighwayJunctionGeometry.Apply(result,context,alignmentContext?.Roads??originalRoads,groups,profiles,scale,leftTraffic);
            return result;
        }
    }
}
