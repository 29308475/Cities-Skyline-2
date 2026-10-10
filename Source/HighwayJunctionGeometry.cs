using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    // A native node identifies connectivity; it need not be the center of every
    // incident curve. Keep shared topology while aligning the actual driving lanes.
    public static class HighwayJunctionGeometry
    {
        sealed class Arm
        {
            public int Index;
            public bool Start, Ramp;
            public Point[] Road;
            public RoadDetails Details;
            public RoadWidthProfile Profile;
        }
        sealed class Adjustment { public Point Delta, Axis; public bool Ramp; }
        static Point Sub(Point a,Point b)=>new Point(a.X-b.X,a.Z-b.Z);
        static double Length(Point p)=>Math.Sqrt(p.X*p.X+p.Z*p.Z);
        static Point Unit(Point p) { double n=Length(p);return n<.001?new Point(0,0):new Point(p.X/n,p.Z/n); }
        static double Dot(Point a,Point b)=>a.X*b.X+a.Z*b.Z;
        static bool SameLevel(Arm a,Arm b)=>a.Details.Layer==b.Details.Layer&&a.Details.Bridge==b.Details.Bridge&&a.Details.Tunnel==b.Details.Tunnel;
        static bool Usable(RoadWidthProfile p)=>p.IndependentCurveEnds&&p.DrivingLaneOffsets!=null&&p.DrivingLaneOffsets.Length>0&&p.DrivingLaneOffsets.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v));

        public static void Apply(List<RoadBatch> batches,StreetMap context,List<Point[]> original,List<RoadSubgroup> groups,RoadWidthProfile[] profiles,float scale,bool left)
        {
            if(!context.HasDetails||scale<=0)return;
            var lookup=RoadSubgroups.Lookup(groups);
            var nodes=new Dictionary<string,List<Arm>>();
            var occupied=new Dictionary<string,int>();
            for(int i=0;i<context.Roads.Count;i++)
            {
                var d=context.Details[i];var road=context.Roads[i];
                foreach(var p in road){string k=StreetBuildPlan.Key(p);occupied[k]=occupied.TryGetValue(k,out int n)?n+1:1;}
                if(!d.DirectionKnown||!d.OneWay||d.Roundabout)continue;
                bool ramp=d.Highway=="motorway_link"||d.Highway=="trunk_link";
                if(!ramp&&d.Highway!="motorway"&&d.Highway!="trunk")continue;
                lookup.TryGetValue(RoadExclusions.Key(original[i]),out var group);
                int category=group?.Category??(context.HasRoadTypes?(int)context.Types[i]:2);
                if(category!=0)continue;
                for(int end=0;end<2;end++)
                {
                    string k=StreetBuildPlan.Key(road[end]);
                    if(!nodes.TryGetValue(k,out var arms))nodes[k]=arms=new List<Arm>();
                    arms.Add(new Arm{Index=i,Start=end==0,Ramp=ramp,Road=road,Details=d,Profile=group?.Profile??profiles[0]});
                }
            }
            var adjustments=new Dictionary<string,Dictionary<RoadWidthProfile,Adjustment>>();
            var sides=new CarriagewaySides(context,scale);
            foreach(var pair in nodes)
            {
                var all=pair.Value;var main=all.Where(a=>!a.Ramp).ToArray();var ramps=all.Where(a=>a.Ramp).ToArray();
                if(main.Length!=2||ramps.Length>1||occupied[pair.Key]!=all.Count||main[0].Start==main[1].Start||all.Any(a=>!SameLevel(a,main[0])||!Usable(a.Profile)))continue;
                var narrow=main.OrderBy(a=>a.Profile.DrivingLaneOffsets.Length).First();
                var wide=main.OrderByDescending(a=>a.Profile.DrivingLaneOffsets.Length).First();
                int extra=wide.Profile.DrivingLaneOffsets.Length-narrow.Profile.DrivingLaneOffsets.Length;
                if(extra<=0)continue;
                // Derive the through direction before widening moved the common node.
                var a0=Unit(Sub(original[main[0].Index][1],original[main[0].Index][0]));var a1=Unit(Sub(original[main[1].Index][1],original[main[1].Index][0]));
                if(Dot(a0,a1)<Math.Cos(15*Math.PI/180))continue;
                var axis=Unit(new Point(a0.X+a1.X,a0.Z+a1.Z));var normal=new Point(axis.Z,-axis.X);
                double outside=sides.Sign(wide.Index,left?-1:1,scale);
                if(!LaneAlignment.TryShift(narrow.Profile,wide.Profile,outside,out double shift)||shift<=0||shift>4)continue;
                var changes=new Dictionary<RoadWidthProfile,Adjustment>();
                changes[wide.Profile]=new Adjustment{Delta=new Point(0,0),Axis=axis};
                changes[narrow.Profile]=new Adjustment{Delta=new Point(-normal.X*outside*shift/scale,-normal.Z*outside*shift/scale),Axis=axis};
                if(ramps.Length==1)
                {
                    var ramp=ramps[0];
                    // A lane drop splits 4 into 3+1; a lane gain merges 3+1 into 4.
                    // Reject ambiguous lane counts/directions instead of selecting a middle lane.
                    if(ramp.Start==wide.Start||ramp.Profile.DrivingLaneOffsets.Length!=extra||ReferenceEquals(ramp.Profile,narrow.Profile))continue;
                    var origin=ramp.Road[ramp.Start?0:1];var departure=Sub(ramp.Road[ramp.Start?1:0],origin);
                    if(Dot(departure,normal)*outside<=.01)continue;
                    var outer=wide.Profile.DrivingLaneOffsets.OrderBy(v=>v*outside).Skip(narrow.Profile.DrivingLaneOffsets.Length).ToArray();
                    var lanes=ramp.Profile.DrivingLaneOffsets.OrderBy(v=>v*outside).ToArray();
                    double offset=outer.Zip(lanes,(x,y)=>(double)x-y).Average();
                    if(outer.Zip(lanes,(x,y)=>Math.Abs(x-y-offset)).Any(v=>v>.25))continue;
                    changes[ramp.Profile]=new Adjustment{Delta=new Point(normal.X*offset/scale,normal.Z*offset/scale),Axis=axis,Ramp=true};
                }
                adjustments[pair.Key]=changes;
            }
            foreach(var batch in batches)
            {
                if(batch.Category!=0||batch.IsRing||batch.IsApproach)continue;
                for(int i=0;i<batch.Plan.Lines.Count;i++)for(int end=0;end<2;end++)
                {
                    var logical=batch.Plan.Lines[i];
                    if(!adjustments.TryGetValue(StreetBuildPlan.Key(logical[end]),out var changes)||!changes.TryGetValue(batch.Profile,out var change)||batch.IsRamp!=change.Ramp)continue;
                    if(batch.Plan.GeometryLines==null)batch.Plan.GeometryLines=batch.Plan.Lines.Select(c=>c.ToArray()).ToList();
                    var c=batch.Plan.GeometryLines[i];
                    if(c.Length==2)c=new[]{c[0],c[1],new Point(c[0].X+(c[1].X-c[0].X)/3,c[0].Z+(c[1].Z-c[0].Z)/3),new Point(c[1].X+(c[0].X-c[1].X)/3,c[1].Z+(c[0].Z-c[1].Z)/3)};
                    double handle=Length(Sub(c[end+2],c[end]));
                    c[end]=new Point(c[end].X+change.Delta.X,c[end].Z+change.Delta.Z);
                    c[end+2]=new Point(c[end+2].X+change.Delta.X,c[end+2].Z+change.Delta.Z);
                    // All incident driving lanes share the through direction at the node.
                    // The ramp starts along the extra lane, then curves away.
                    if(change.Ramp)handle=Math.Min(handle,24/scale);
                    double sign=end==0?1:-1;
                    c[end+2]=new Point(c[end].X+change.Axis.X*handle*sign,c[end].Z+change.Axis.Z*handle*sign);
                    batch.Plan.GeometryLines[i]=c;
                }
            }
        }
    }
}
