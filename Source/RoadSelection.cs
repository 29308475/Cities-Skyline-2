using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    // Selection units end at junctions and changes in category, profile or elevation.
    public static class RoadSelection
    {
        public static List<HashSet<int>> Units(StreetMap map,List<RoadSubgroup> groups,HashSet<string> excluded)
        {
            var lookup=RoadSubgroups.Lookup(groups);
            var at=new Dictionary<string,List<int>>();
            var signatures=new Dictionary<int,string>();
            for(int i=0;i<map.Roads.Count;i++)
            {
                var road=map.Roads[i];var key=RoadExclusions.Key(road);
                if(excluded.Contains(key))continue;
                lookup.TryGetValue(key,out var group);
                string signature=group?.Id??"base:"+(map.HasRoadTypes?(int)map.Types[i]:2);
                if(map.HasDetails){var d=map.Details[i];signature+=":"+d.Bridge+":"+d.Tunnel+":"+d.Layer+":"+d.OneWay;}
                signatures[i]=signature;
                foreach(var p in road){var k=StreetBuildPlan.Key(p);if(!at.ContainsKey(k))at[k]=new List<int>();at[k].Add(i);}
            }
            var result=new List<HashSet<int>>();var used=new HashSet<int>();
            foreach(int seed in signatures.Keys)
            {
                if(!used.Add(seed))continue;
                var unit=new HashSet<int>{seed};var queue=new Queue<int>();queue.Enqueue(seed);
                while(queue.Count>0)
                {
                    int i=queue.Dequeue();
                    foreach(var p in map.Roads[i])
                    {
                        var neighbors=at[StreetBuildPlan.Key(p)];if(neighbors.Count!=2)continue;
                        foreach(int j in neighbors)if(signatures[j]==signatures[i]&&used.Add(j)){unit.Add(j);queue.Enqueue(j);}
                    }
                }
                result.Add(unit);
            }
            return result;
        }
        public static void Toggle(HashSet<int> selected,IEnumerable<HashSet<int>> units,HashSet<int> hits,bool remove)
        {
            foreach(var unit in units)
            {
                if(!unit.Overlaps(hits))continue;
                bool clear=remove||unit.All(selected.Contains);
                foreach(int i in unit)if(clear)selected.Remove(i);else selected.Add(i);
            }
        }
    }
}
