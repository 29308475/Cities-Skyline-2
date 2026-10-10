using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;

static class HighwayJunctionGeometryChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static double Distance(Point a,Point b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    public static void Run()
    {
        foreach(bool left in new[]{false,true})foreach(bool entry in new[]{false,true})foreach(float scale in new[]{.5f,1f,2f})foreach(double angle in new[]{0.0,.7,2.9})
        {
            double side=left?1:-1;
            Point P(double x,double z)=>new Point((x*Math.Cos(angle)-z*Math.Sin(angle))/scale,(x*Math.Sin(angle)+z*Math.Cos(angle))/scale);
            var map=new StreetMap{DetailsVersion=1};
            map.Roads.Add(new[]{P(-100,0),P(0,0)});
            map.Roads.Add(new[]{P(0,0),P(100,0)});
            map.Roads.Add(entry?new[]{P(-80,side*40),P(0,0)}:new[]{P(0,0),P(80,side*40)});
            for(int i=0;i<3;i++){map.Types.Add(RoadType.Motorway);map.Details.Add(new RoadDetails{Highway=i==2?"motorway_link":"motorway",DirectionKnown=true,OneWay=true});}
            var profiles=RoadWidths.Manual(16);
            profiles[0]=new RoadWidthProfile{PrefabId="three",Meters=16,IndependentCurveEnds=true,DrivingLaneOffsets=new[]{-4f,0,4}};
            var wide=new RoadWidthProfile{PrefabId="four",Meters=20,IndependentCurveEnds=true,DrivingLaneOffsets=new[]{-6f,-2,2,6}};
            var ramp=new RoadWidthProfile{PrefabId="one",Meters=8,IndependentCurveEnds=true,DrivingLaneOffsets=new[]{0f}};
            int wideIndex=entry?1:0;
            var groups=new List<RoadSubgroup>{new RoadSubgroup{Id="wide",Category=0,Profile=wide,Roads=new List<string>{RoadExclusions.Key(map.Roads[wideIndex])}},
                new RoadSubgroup{Id="ramp",Category=0,Profile=ramp,Roads=new List<string>{RoadExclusions.Key(map.Roads[2])}}};
            var plans=RoadSubgroups.Plan(map,groups,profiles,scale,0,false,2000,null,left);
            var w=plans.Single(b=>b.Profile==wide).Plan;var n=plans.Single(b=>b.Profile==profiles[0]).Plan;var r=plans.Single(b=>b.IsRamp).Plan;
            int we=entry?0:1,ne=1-we,re=entry?1:0;
            Check(w.GeometryLines!=null&&n.GeometryLines!=null&&r.GeometryLines!=null,"Separate physical highway curves are used");
            Check(Distance(w.Lines[0][we],n.Lines[0][ne])<.001&&Distance(w.Lines[0][we],r.Lines[0][re])<.001,"All three roads keep the same topological node");
            Check(Distance(n.VisibleLines[0][ne],P(0,0))*scale<.025,"Narrow road keeps its original through-lane position");
            Check(Distance(w.VisibleLines[0][we],P(0,side*2))*scale<.025,"Wide road keeps the outward widening");
            Check(Distance(r.VisibleLines[0][re],P(0,side*8))*scale<.025,"Ramp center meets the extra outer lane");
            var normal=P(0,-scale);
            foreach(float lane in profiles[0].DrivingLaneOffsets)
            {
                var narrowCenter=n.VisibleLines[0][ne];
                var lanePoint=new Point(narrowCenter.X+normal.X*lane/scale,narrowCenter.Z+normal.Z*lane/scale);
                Check(wide.DrivingLaneOffsets.Any(v=>Distance(lanePoint,new Point(w.VisibleLines[0][we].X+normal.X*v/scale,w.VisibleLines[0][we].Z+normal.Z*v/scale))*scale<.025),"Every through lane aligns across the width change");
            }
            Check(Distance(map.Roads[0][1],P(0,0))<.001,"Source map stays unchanged");
            var subset=new StreetMap{DetailsVersion=1,Roads=new List<Point[]>{map.Roads[2]},Types=new List<RoadType>{RoadType.Motorway},Details=new List<RoadDetails>{map.Details[2]}};
            var highlight=RoadSubgroups.Plan(subset,groups,profiles,scale,0,false,2000,null,left,null,map).Single().Plan;
            Check(Distance(highlight.VisibleLines[0][re],r.VisibleLines[0][re])<.001,"Partial ramp highlight uses the full junction's lateral attachment");
            ramp.IndependentCurveEnds=false;
            var strict=RoadSubgroups.Plan(map,groups,profiles,scale,0,false,2000,null,left);
            Check(strict.All(b=>b.Plan.GeometryLines==null),"Strict-node prefab disables the whole junction adjustment");
            ramp.IndependentCurveEnds=true;ramp.DrivingLaneOffsets=new[]{-2f,2};
            Check(RoadSubgroups.Plan(map,groups,profiles,scale,0,false,2000,null,left).All(b=>b.Plan.GeometryLines==null),"Mismatched ramp lane counts never choose arbitrary lane offsets");
        }
        // Replay the recorded courses as junction arms; prefab roles match the
        // recorded 1/3/4-lane motorway selection (entity IDs are fixture-local).
        var fixture=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"Fixtures/highway-43-courses.json")));
        Point Read(Newtonsoft.Json.Linq.JToken p)=>new Point((double)p["x"],(double)p["z"]);
        var recorded=new StreetMap{DetailsVersion=1};var recordedBatches=new List<RoadBatch>();var recordedGroups=new List<RoadSubgroup>();
        foreach(var group in fixture.GroupBy(p=>(string)p["prefab"]))
        {
            int count=group.Key=="Entity(15912:1)"?1:group.Key=="Entity(15914:1)"?3:4;
            var profile=new RoadWidthProfile{PrefabId=group.Key,Meters=4*(count+1),IndependentCurveEnds=true,DrivingLaneOffsets=Enumerable.Range(0,count).Select(i=>(float)(4*i-2*(count-1))).ToArray()};
            var batch=new RoadBatch{Category=0,IsRamp=count==1,Profile=profile,Plan=new StreetBuildPlan{Lines=new List<Point[]>()}};
            var subgroup=new RoadSubgroup{Id=group.Key,Category=0,Profile=profile};
            foreach(var p in group)
            {
                batch.Indices.Add(recorded.Roads.Count);
                var ends=new[]{Read(p["start"]),Read(p["end"])};
                recorded.Roads.Add(ends);recorded.Types.Add(RoadType.Motorway);recorded.Details.Add(new RoadDetails{Highway=count==1?"motorway_link":"motorway",OneWay=true,DirectionKnown=true});
                batch.Plan.Lines.Add(new[]{ends[0],ends[1],Read(p["control1"]),Read(p["control2"])});subgroup.Roads.Add(RoadExclusions.Key(ends));
            }
            recordedGroups.Add(subgroup);recordedBatches.Add(batch);
        }
        HighwayJunctionGeometry.Apply(recordedBatches,recorded,recorded.Roads,recordedGroups,RoadWidths.Manual(16),1,false);
        var rampBatch=recordedBatches.Single(b=>b.IsRamp);int shifted=0;
        Check(rampBatch.Plan.GeometryLines!=null,"Recorded motorway ramps support independent physical ends");
        for(int i=0;i<rampBatch.Plan.Lines.Count;i++)for(int end=0;end<2;end++)
        {
            double delta=Distance(rampBatch.Plan.Lines[i][end],rampBatch.Plan.VisibleLines[i][end]);
            if(delta>.1){Check(Math.Abs(delta-6)<.01,"Recorded ramp attaches six meters outside the logical node");shifted++;}
        }
        Check(shifted==4,"All four recorded ramps attach to their outer lane");
        Console.WriteLine("PASS: physical 3+1/4 lane joins, shared topology, left/right traffic, entry/exit, scales, rotations, partial preview and unsupported-prefab fallback");
    }
}
