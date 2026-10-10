using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public static class IsolatedRoads
    {
        // Inspect complete components, across all categories, after intersection splitting.
        // Tiny connected pieces are not isolated roads. Closed rings are deliberate geometry.
        public static HashSet<string> ShortComponentNodes(StreetMap map,float scale)
        {
            var at=new Dictionary<string,List<int>>();
            for(int i=0;i<map.Roads.Count;i++)foreach(var p in map.Roads[i])
            {
                var key=StreetBuildPlan.Key(p);
                if(!at.TryGetValue(key,out var edges))at[key]=edges=new List<int>();
                edges.Add(i);
            }
            var result=new HashSet<string>();var visited=new HashSet<int>();
            for(int seed=0;seed<map.Roads.Count;seed++)
            {
                if(!visited.Add(seed))continue;
                var queue=new Queue<int>();queue.Enqueue(seed);var nodes=new HashSet<string>();
                double length=0;int count=0;bool structure=false;
                while(queue.Count>0)
                {
                    int i=queue.Dequeue();count++;
                    length+=CurvePlanner.Length(map.Roads[i])*scale;
                    if(map.HasDetails){var d=map.Details[i];structure|=d.Roundabout||d.Bridge||d.Tunnel;}
                    foreach(var p in map.Roads[i])
                    {
                        var key=StreetBuildPlan.Key(p);nodes.Add(key);
                        foreach(int next in at[key])if(visited.Add(next))queue.Enqueue(next);
                    }
                }
                if(!structure&&length<16&&count<nodes.Count)result.UnionWith(nodes);
            }
            return result;
        }
    }
}
