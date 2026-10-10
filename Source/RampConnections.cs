using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public static class RampConnections
    {
        sealed class Join { public Point Direction; public RoadWidthProfile Profile; public RoadDetails Details; }
        static Point Sub(Point a,Point b)=>new Point(a.X-b.X,a.Z-b.Z);
        static double Length(Point a)=>Math.Sqrt(a.X*a.X+a.Z*a.Z);
        public static Point[] Shape(Point[] line,bool start,Point direction,RoadWidthProfile main,float rampWidth,float scale)
        {
            var origin=line[start?0:1];var far=line[start?1:0];
            double dl=Length(direction),length=CurvePlanner.Length(line);
            if(dl<.001||length*scale<16)return line;
            var axis=new Point(direction.X/dl,direction.Z/dl);var normal=new Point(axis.Z,-axis.X);
            var departure=Sub(CurvePlanner.Evaluate(line,start?.25:.75),origin);
            double side=departure.X*normal.X+departure.Z*normal.Z;
            if(Math.Abs(side)*scale<.25)return line; // cannot establish which side the ramp approaches
            side=Math.Sign(side);
            var control=line.Length==4?line[start?2:3]:new Point(origin.X+(far.X-origin.X)/3,origin.Z+(far.Z-origin.Z)/3);
            var tangent=Sub(control,origin);double handle=Length(tangent);
            double along=tangent.X*axis.X+tangent.Z*axis.Z;
            if(handle<.001||Math.Abs(along)<.001)return line;
            // Native junction geometry trims the course near the carriageway edge.
            // Keep the shared node, but approach that edge from outside instead of
            // following the main road almost tangentially through its driving lanes.
            double outer=main.DrivingLaneOffsets?.Length>0?main.DrivingLaneOffsets.Max(v=>v*side):main.Meters/2-rampWidth/2;
            double lateral=Math.Max(1,outer+rampWidth/2+1);
            double run=Math.Max(16,main.Meters*2);
            double target=Math.Atan2(lateral,run);
            double angle=Math.Atan2((tangent.X*normal.X+tangent.Z*normal.Z)*side,Math.Abs(along));
            if(angle>=target)return line;
            handle=Math.Min(handle,Math.Min(length*.4,run/scale));
            var updated=new Point(origin.X+handle*(Math.Cos(target)*Math.Sign(along)*axis.X+Math.Sin(target)*side*normal.X),
                origin.Z+handle*(Math.Cos(target)*Math.Sign(along)*axis.Z+Math.Sin(target)*side*normal.Z));
            var result=line.Length==4?line.ToArray():new[]{line[0],line[1],new Point(line[0].X+(line[1].X-line[0].X)/3,line[0].Z+(line[1].Z-line[0].Z)/3),new Point(line[1].X+(line[0].X-line[1].X)/3,line[1].Z+(line[0].Z-line[1].Z)/3)};
            result[start?2:3]=updated;
            return result;
        }
        sealed class Arm
        {
            public RoadBatch Batch; public int Index; public bool Start;
            public Point[] Line=>Batch.Plan.Lines[Index];
        }
        // Solve the through-road tangents together, across prefab/subgroup boundaries.
        // Only an unambiguous pair of directed main-road arms is eligible. Node positions,
        // lane offsets and the distant control points remain untouched.
        public static Dictionary<string,Point> AlignThroughRoads(List<RoadBatch> batches,float scale,HashSet<string> eligible=null)
        {
            var nodes=new Dictionary<string,List<Arm>>();
            foreach(var b in batches.Where(b=>b.Category==0&&!b.IsRamp&&!b.IsRing&&!b.IsApproach))
                for(int i=0;i<b.Plan.Lines.Count;i++)foreach(bool start in new[]{true,false})
                {
                    var key=StreetBuildPlan.Key(b.Plan.Lines[i][start?0:1]);
                    if(!nodes.TryGetValue(key,out var arms))nodes[key]=arms=new List<Arm>();
                    arms.Add(new Arm{Batch=b,Index=i,Start=start});
                }
            var axes=new Dictionary<string,Point>();
            foreach(var pair in nodes)
            {
                if(eligible!=null&&!eligible.Contains(pair.Key))continue;
                var arms=pair.Value;
                if(arms.Count!=2||arms[0].Start==arms[1].Start)continue;
                var vectors=new List<Point>();var handles=new List<double>();bool valid=true;
                foreach(var arm in arms)
                {
                    var c=arm.Line;var origin=c[arm.Start?0:1];
                    var control=c.Length==4?c[arm.Start?2:3]:new Point(origin.X+(c[arm.Start?1:0].X-origin.X)/3,origin.Z+(c[arm.Start?1:0].Z-origin.Z)/3);
                    var v=Sub(control,origin);double h=Length(v);
                    if(h*scale<1){valid=false;break;}
                    double sign=arm.Start?1:-1;
                    vectors.Add(new Point(v.X/h*sign,v.Z/h*sign));handles.Add(h);
                }
                if(!valid||vectors[0].X*vectors[1].X+vectors[0].Z*vectors[1].Z<Math.Cos(25*Math.PI/180))continue;
                var sum=new Point(vectors[0].X+vectors[1].X,vectors[0].Z+vectors[1].Z);double size=Length(sum);
                var axis=new Point(sum.X/size,sum.Z/size);
                // Maximum control displacement 4 m (curve displacement is smaller).
                if(Enumerable.Range(0,2).Any(i=>Length(Sub(axis,vectors[i]))*handles[i]*scale>4))continue;
                for(int i=0;i<2;i++)
                {
                    var arm=arms[i];var c=arm.Line;var origin=c[arm.Start?0:1];double sign=arm.Start?1:-1;
                    var result=c.Length==4?c.ToArray():new[]{c[0],c[1],new Point(c[0].X+(c[1].X-c[0].X)/3,c[0].Z+(c[1].Z-c[0].Z)/3),new Point(c[1].X+(c[0].X-c[1].X)/3,c[1].Z+(c[0].Z-c[1].Z)/3)};
                    result[arm.Start?2:3]=new Point(origin.X+axis.X*handles[i]*sign,origin.Z+axis.Z*handles[i]*sign);
                    arm.Batch.Plan.Lines[arm.Index]=result;
                }
                axes[pair.Key]=axis;
            }
            return axes;
        }
        public static void Apply(List<RoadBatch> batches,StreetMap map,List<Point[]> original,List<RoadSubgroup> groups,RoadWidthProfile[] profiles,float scale,StreetMap batchSource=null)
        {
            if(!map.HasDetails)return;
            var lookup=RoadSubgroups.Lookup(groups);var joins=new Dictionary<string,List<Join>>();
            for(int i=0;i<map.Roads.Count;i++)
            {
                var d=map.Details[i];if(!d.DirectionKnown||!d.OneWay||d.Roundabout||(d.Highway!="motorway"&&d.Highway!="trunk"))continue;
                lookup.TryGetValue(RoadExclusions.Key(original[i]),out var group);
                int type=group?.Category??(map.HasRoadTypes?(int)map.Types[i]:2);if(type!=0)continue;
                var r=map.Roads[i];var join=new Join{Direction=Sub(r[1],r[0]),Profile=group?.Profile??profiles[type],Details=d};
                foreach(var p in r){var key=StreetBuildPlan.Key(p);if(!joins.TryGetValue(key,out var list))joins[key]=list=new List<Join>();list.Add(join);}
            }
            var eligible=new HashSet<string>(joins.Where(p=>p.Value.Count==2&&p.Value.Select(j=>j.Details.Layer+":"+j.Details.Bridge+":"+j.Details.Tunnel).Distinct().Count()==1).Select(p=>p.Key));
            var axes=AlignThroughRoads(batches,scale,eligible);
            foreach(var batch in batches.Where(b=>b.IsRamp&&!b.IsApproach&&!b.IsRing))
            {
                var metadata=(batchSource??map).Details[batch.Indices.First()];
                for(int i=0;i<batch.Plan.Lines.Count;i++)foreach(bool start in new[]{true,false})
                {
                    var line=batch.Plan.Lines[i];
                    if(!joins.TryGetValue(StreetBuildPlan.Key(line[start?0:1]),out var candidates))continue;
                    var matching=candidates.Where(j=>j.Details.Layer==metadata.Layer&&j.Details.Bridge==metadata.Bridge&&j.Details.Tunnel==metadata.Tunnel).OrderByDescending(j=>j.Profile.Meters).ToList();
                    if(matching.Count==0)continue;
                    var join=matching[0];
                    batch.Plan.Lines[i]=Shape(line,start,axes.TryGetValue(StreetBuildPlan.Key(line[start?0:1]),out var axis)?axis:join.Direction,join.Profile,batch.Profile.Meters,scale);
                }
            }
        }
    }
}
