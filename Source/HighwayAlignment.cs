using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    // Offset wider carriageways towards the outer shoulder. Original keys remain
    // authoritative for editing; every incident road receives the same node offset.
    public static class HighwayAlignment
    {
        sealed class Offset { public double X,Z,Distance; }
        public static StreetMap Prepare(StreetMap source,List<RoadSubgroup> groups,RoadWidthProfile[] profiles,float scale,bool left,StreetMap context=null)
        {
            context=context??source;
            if(!source.HasDetails||!context.HasDetails||scale<=0)return source;
            var lookup=RoadSubgroups.Lookup(groups);
            var sides=new CarriagewaySides(context,scale);
            var offsets=new Dictionary<string,Offset>();
            var regimes=new Dictionary<string,string>();var blocked=new HashSet<string>();
            foreach(var i in Enumerable.Range(0,context.Roads.Count))
            {
                var d=context.Details[i];string regime=d.Layer+":"+d.Bridge+":"+d.Tunnel;
                foreach(var p in context.Roads[i])
                {
                    var key=StreetBuildPlan.Key(p);
                    if(regimes.TryGetValue(key,out var old)&&old!=regime)blocked.Add(key);
                    else regimes[key]=regime;
                }
            }
            for(int i=0;i<context.Roads.Count;i++)
            {
                var d=context.Details[i];var road=context.Roads[i];
                if(!d.DirectionKnown||!d.OneWay||d.Roundabout||(d.Highway!="motorway"&&d.Highway!="trunk"))continue;
                lookup.TryGetValue(RoadExclusions.Key(road),out var group);
                if((group?.Category??(context.HasRoadTypes?(int)context.Types[i]:2))!=0)continue;
                var selected=group?.Profile??profiles[0];
                double dx=road[1].X-road[0].X,dz=road[1].Z-road[0].Z,length=Math.Sqrt(dx*dx+dz*dz);
                if(length<0.001)continue;
                // X/Z plane: right of travel = (dz,-dx); left-hand traffic mirrors it.
                double sign=sides.Sign(i,left?-1:1,scale);
                if(sign==0)continue;
                double extra;
                if(!LaneAlignment.TryShift(profiles[0],selected,sign,out extra))
                {
                    // With known but incompatible lane layouts, do not invent a width-based match.
                    if(profiles[0].DrivingLaneOffsets!=null&&selected.DrivingLaneOffsets!=null)continue;
                    extra=(selected.Meters-profiles[0].Meters)/2.0;
                }
                extra/=scale;
                if(extra<=.001)continue;
                foreach(var p in road)
                {
                    var key=StreetBuildPlan.Key(p);
                    if(!offsets.TryGetValue(key,out var o))offsets[key]=o=new Offset();
                    o.X+=sign*dz/length;o.Z-=sign*dx/length;o.Distance=Math.Max(o.Distance,extra);
                }
            }
            if(offsets.Count==0)return source;
            // Spread an attached ramp's displacement along its first section instead
            // of moving only the junction and abruptly changing the approach tangent.
            var at=new Dictionary<string,List<int>>();
            for(int i=0;i<context.Roads.Count;i++)foreach(var p in context.Roads[i])
            {var key=StreetBuildPlan.Key(p);if(!at.TryGetValue(key,out var list))at[key]=list=new List<int>();list.Add(i);}
            bool Ramp(int i)=>context.Details[i].Highway?.EndsWith("_link",StringComparison.Ordinal)==true&&context.Details[i].OneWay;
            foreach(var start in offsets.ToArray())
            {
                if(blocked.Contains(start.Key))continue;
                foreach(int seed in at[start.Key])
                {
                    if(!Ramp(seed))continue;
                    var samples=new List<KeyValuePair<string,double>>();var visited=new HashSet<int>();
                    string node=start.Key;int edge=seed;double distance=0;
                    while(visited.Add(edge))
                    {
                        var r=context.Roads[edge];distance+=CurvePlanner.Length(r);
                        node=StreetBuildPlan.Key(r[0])==node?StreetBuildPlan.Key(r[1]):StreetBuildPlan.Key(r[0]);
                        samples.Add(new KeyValuePair<string,double>(node,distance));
                        if(blocked.Contains(node)||offsets.ContainsKey(node)||at[node].Count!=2||distance*scale>=64)break;
                        int next=at[node][0]==edge?at[node][1]:at[node][0];
                        if(!Ramp(next))break;
                        edge=next;
                    }
                    double reach=Math.Min(64/scale,distance);
                    foreach(var sample in samples)
                    {
                        if(sample.Value>=reach||offsets.ContainsKey(sample.Key)||blocked.Contains(sample.Key))continue;
                        double t=sample.Value/reach,weight=1-t*t*(3-2*t);
                        offsets[sample.Key]=new Offset{X=start.Value.X,Z=start.Value.Z,Distance=start.Value.Distance*weight};
                    }
                }
            }
            Point Move(Point p)
            {
                var key=StreetBuildPlan.Key(p);
                if(blocked.Contains(key)||!offsets.TryGetValue(key,out var o))return p;
                double length=Math.Sqrt(o.X*o.X+o.Z*o.Z);
                if(length<0.1)return p; // Opposing directions at one node: do not guess a shoulder.
                return new Point(p.X+o.X/length*o.Distance,p.Z+o.Z/length*o.Distance);
            }
            return new StreetMap{Name=source.Name,Latitude=source.Latitude,Longitude=source.Longitude,SizeMeters=source.SizeMeters,
                Roads=source.Roads.Select(r=>r.Select(Move).ToArray()).ToList(),Types=source.Types.ToList(),
                Details=source.Details.ToList(),DetailsVersion=source.DetailsVersion,RemovedDriveways=source.RemovedDriveways};
        }
    }
}
