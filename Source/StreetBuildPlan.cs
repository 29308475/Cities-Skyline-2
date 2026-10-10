using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CityStreetOverlay
{
    public sealed class CircleStamp
    {
        public Point Center;
        public double Radius;
    }

    // Pure preparation: no source mutations and no automatic partial build on overflow.
    public sealed class StreetBuildPlan
    {
        public readonly List<CircleStamp> Circles = new List<CircleStamp>();
        public readonly HashSet<string> Elevated = new HashSet<string>();
        public List<Point[]> Lines;
        // Topological endpoints stay in Lines; physical curves can meet off-center at the same native node.
        public List<Point[]> GeometryLines;
        public List<Point[]> VisibleLines => GeometryLines ?? Lines;
        public HashSet<string> ShortIsolatedNodes;
        public static string Key(Point p) => RoadGraph.Coordinate(p.X).ToString(CultureInfo.InvariantCulture)+":"+RoadGraph.Coordinate(p.Z).ToString(CultureInfo.InvariantCulture);
        public static StreetBuildPlan Create(StreetMap source, RoadType category, float scale, HashSet<int> selection,int limit=2000,bool convertCircles=true)
        {
            source=RoadIntersections.Split(source,ref selection);
            if(convertCircles)source=RoundaboutGeometry.Infer(source,scale);
            var result=new StreetBuildPlan();
            result.ShortIsolatedNodes=IsolatedRoads.ShortComponentNodes(source,scale);
            var chosen=new HashSet<int>();
            for(int i=0;i<source.Roads.Count;i++)
                if((source.HasRoadTypes?source.Types[i]:RoadType.LocalRoad)==category && (selection==null||selection.Contains(i)))chosen.Add(i);
            var replacements=new Dictionary<string,Point>();
            var removed=new HashSet<int>();
            if(source.HasDetails)
            {
                var at=new Dictionary<string,List<int>>();
                foreach(int i in chosen)if(source.Details[i].Roundabout && !source.Details[i].Bridge)
                    foreach(var p in source.Roads[i]) {var k=Key(p);if(!at.ContainsKey(k))at[k]=new List<int>();at[k].Add(i);}
                var visited=new HashSet<int>();
                foreach(int seed in chosen)
                {
                    if(!convertCircles||!source.Details[seed].Roundabout||source.Details[seed].Bridge||!visited.Add(seed))continue;
                    var component=new List<int>();var queue=new Queue<int>();queue.Enqueue(seed);
                    while(queue.Count>0){int i=queue.Dequeue();component.Add(i);foreach(var p in source.Roads[i])foreach(int j in at[Key(p)])if(visited.Add(j))queue.Enqueue(j);}
                    var points=component.SelectMany(i=>source.Roads[i]).GroupBy(Key).ToDictionary(g=>g.Key,g=>g.First());
                    if(points.Count<6||points.Keys.Any(k=>at[k].Count!=2))continue; // Clipped/partly selected ring.
                    var center=new Point((points.Values.Min(p=>p.X)+points.Values.Max(p=>p.X))/2,(points.Values.Min(p=>p.Z)+points.Values.Max(p=>p.Z))/2);
                    var radii=points.Values.Select(p=>Math.Sqrt((p.X-center.X)*(p.X-center.X)+(p.Z-center.Z)*(p.Z-center.Z))).ToArray();
                    double radius=radii.Average();
                    if(radius*scale<4||radius*scale>30||radii.Any(r=>Math.Abs(r-radius)>radius*0.2))continue;
                    var entrances=new HashSet<string>();
                    foreach(int i in chosen)if(!source.Details[i].Roundabout)foreach(var p in source.Roads[i])if(points.ContainsKey(Key(p)))entrances.Add(Key(p));
                    if(entrances.Count<2)continue; // A complete ring can have only two approaches.
                    // Never detach roads of another category or outside the selection.
                    bool omitted=false;
                    for(int i=0;i<source.Roads.Count&&!omitted;i++)if(!chosen.Contains(i))omitted=source.Roads[i].Any(p=>points.ContainsKey(Key(p)));
                    if(omitted)continue;
                    foreach(int i in component)removed.Add(i);
                    foreach(var k in points.Keys)replacements[k]=center;
                    result.Circles.Add(new CircleStamp{Center=center,Radius=radius*scale});
                }
                // Ground approaches meet bridge termini; a road under an interior bridge node stays on the ground.
                var bridgeDegree=new Dictionary<string,int>();
                for(int i=0;i<source.Roads.Count;i++)if(source.Details[i].Bridge)
                    foreach(var p in source.Roads[i]){var key=Key(p);bridgeDegree[key]=bridgeDegree.TryGetValue(key,out var n)?n+1:1;}
                foreach(int i in chosen)foreach(var p in source.Roads[i])
                    if(source.Details[i].Bridge||(bridgeDegree.TryGetValue(Key(p),out var n)&&n==1))result.Elevated.Add(Key(p));
            }
            var map=new StreetMap();
            foreach(int i in chosen)
            {
                if(removed.Contains(i))continue;
                var r=source.Roads[i];
                map.Roads.Add(r.Select(p=>replacements.TryGetValue(Key(p),out var c)?c:p).ToArray());
                map.Types.Add(category);
                if(source.HasDetails)map.Details.Add(source.Details[i]);
            }
            // Preserve junctions with omitted roads and all elevation transitions.
            var stops=new HashSet<string>();
            for(int i=0;i<source.Roads.Count;i++)if(!source.HasDetails||!source.Details[i].Bridge)
                foreach(var p in source.Roads[i])if(result.Elevated.Contains(Key(p)))stops.Add(Key(p));
            for(int i=0;i<source.Roads.Count;i++)if(!chosen.Contains(i))
            {
                // Unbuilt pedestrian paths should not reserve tiny road edges beside a junction.
                // Their later build attaches to the existing native road via edge splitting.
                if(category!=RoadType.FootCyclePath&&source.HasRoadTypes&&source.Types[i]==RoadType.FootCyclePath)continue;
                foreach(var p in source.Roads[i])stops.Add(Key(p));
            }
            map.DetailsVersion=source.HasDetails?1:0;
            result.Lines=StampPlan.Create(map,category,scale,stops,true,limit);
            return result;
        }
    }
}
