using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public static class RingRouting
    {
        public static HashSet<int> Approaches(StreetMap map,HashSet<int> rings)
        {
            var result=new HashSet<int>();if(!map.HasDetails)return result;
            var at=new Dictionary<string,List<int>>();
            for(int i=0;i<map.Roads.Count;i++)foreach(var p in map.Roads[i]){var k=StreetBuildPlan.Key(p);if(!at.ContainsKey(k))at[k]=new List<int>();at[k].Add(i);}
            bool Eligible(int i)=>!rings.Contains(i)&&map.Details[i].DirectionKnown&&map.Details[i].OneWay&&!map.Details[i].Bridge&&!map.Details[i].Tunnel&&
                (!map.HasRoadTypes||map.Types[i]!=RoadType.FootCyclePath);
            foreach(var ring in rings)foreach(var point in map.Roads[ring])
            {
                var start=StreetBuildPlan.Key(point);
                foreach(var seed in at[start])
                {
                    if(!Eligible(seed)||map.Details[seed].Layer!=map.Details[ring].Layer)continue;
                    int edge=seed;string node=start;var visited=new HashSet<int>();
                    while(visited.Add(edge))
                    {
                        result.Add(edge);var road=map.Roads[edge];
                        node=StreetBuildPlan.Key(road[0])==node?StreetBuildPlan.Key(road[1]):StreetBuildPlan.Key(road[0]);
                        // A path crossing is not the end of the road's splitter island.
                        // Stop at real road branches/merges, not pedestrian side connections.
                        var next=at[node].Where(i=>!map.HasRoadTypes||map.Types[i]!=RoadType.FootCyclePath).ToList();if(next.Count!=2)break;
                        int following=next[0]==edge?next[1]:next[0];
                        if(!Eligible(following)||map.Details[following].Layer!=map.Details[edge].Layer)break;
                        // A change of directed flow is a real boundary, not a continuation.
                        if((StreetBuildPlan.Key(road[1])==node)!=(StreetBuildPlan.Key(map.Roads[following][0])==node))break;
                        edge=following;
                    }
                }
            }
            return result;
        }
        public static StreetMap Prepare(StreetMap source,float scale,bool left,out HashSet<int> rings)
        {
            var inferred=RoundaboutGeometry.Infer(source,scale);rings=new HashSet<int>();
            if(!inferred.HasDetails)return source;
            var result=new StreetMap{Roads=source.Roads.Select(r=>r.ToArray()).ToList(),Types=source.HasRoadTypes?source.Types.ToList():Enumerable.Repeat(RoadType.LocalRoad,source.Roads.Count).ToList(),Details=inferred.Details.ToList(),DetailsVersion=1};
            var at=new Dictionary<string,List<int>>();
            for(int i=0;i<result.Roads.Count;i++)if(inferred.Details[i].Roundabout&&!inferred.Details[i].Bridge)
                foreach(var p in result.Roads[i]){var k=StreetBuildPlan.Key(p);if(!at.ContainsKey(k))at[k]=new List<int>();at[k].Add(i);}
            var visited=new HashSet<int>();
            foreach(int seed in at.Values.SelectMany(v=>v).Distinct())
            {
                if(visited.Contains(seed))continue;
                var component=new HashSet<int>();var queue=new Queue<int>();queue.Enqueue(seed);visited.Add(seed);
                while(queue.Count>0){int i=queue.Dequeue();component.Add(i);foreach(var p in result.Roads[i])foreach(int j in at[StreetBuildPlan.Key(p)])if(visited.Add(j))queue.Enqueue(j);}
                if(component.Count<3||component.SelectMany(i=>result.Roads[i]).Any(p=>at[StreetBuildPlan.Key(p)].Count!=2))continue;
                var ordered=new List<int>();var oriented=new List<Point[]>();var current=result.Roads[seed][0];int edge=seed;
                do
                {
                    var r=result.Roads[edge];var end=StreetBuildPlan.Key(r[0])==StreetBuildPlan.Key(current)?r[1]:r[0];
                    ordered.Add(edge);oriented.Add(new[]{current,end});current=end;
                    edge=at[StreetBuildPlan.Key(current)].First(i=>i!=edge);
                }while(edge!=seed&&ordered.Count<=component.Count);
                if(ordered.Count!=component.Count)continue;
                double area=oriented.Sum(r=>r[0].X*r[1].Z-r[1].X*r[0].Z);
                bool reverse=(area>0)==left;
                var category=component.GroupBy(i=>result.Types[i]).OrderByDescending(g=>g.Count()).ThenBy(g=>(int)g.Key).First().Key;
                for(int k=0;k<ordered.Count;k++)
                {
                    int i=ordered[k];var r=oriented[k];var d=inferred.Details[i];
                    result.Roads[i]=reverse?new[]{r[1],r[0]}:r;result.Types[i]=category;
                    result.Details[i]=new RoadDetails{WayId=d.WayId,Highway=d.Highway,Lanes=d.Lanes,WidthMeters=d.WidthMeters,Roundabout=true,OneWay=true,DirectionKnown=true,Layer=d.Layer,Tunnel=d.Tunnel};rings.Add(i);
                }
            }
            return result;
        }
    }
}
