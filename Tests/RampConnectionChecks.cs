using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;

static class RampConnectionChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var main=new RoadWidthProfile{Meters=24,DrivingLaneOffsets=new[]{-6f,-2f,2f,6f}};
        foreach(int side in new[]{-1,1})foreach(float scale in new[]{.5f,1f,2f})
        {
            var line=new[]{new Point(0,0),new Point(80,side*30),new Point(25,side*.2),new Point(50,side*15)};
            var shaped=RampConnections.Shape(line,true,new Point(1,0),main,8,scale);
            Check(shaped[0].X==0&&shaped[0].Z==0&&shaped[1].X==80&&shaped[1].Z==side*30,"Both native endpoint positions preserved");
            Check(shaped[2].Z*side>1&&shaped[3].Z==line[3].Z,"Only connecting tangent moves toward outside shoulder");
            var reverse=new[]{line[1],line[0],line[3],line[2]};
            var incoming=RampConnections.Shape(reverse,false,new Point(1,0),main,8,scale);
            Check(Math.Abs(incoming[3].X-shaped[2].X)<.001&&Math.Abs(incoming[3].Z-shaped[2].Z)<.001,"Incoming and outgoing ramps use identical junction geometry");
            Check(line[2].Z==side*.2,"Source curve remains immutable");
        }
        var shortLine=new[]{new Point(0,0),new Point(5,2)};
        Check(ReferenceEquals(shortLine,RampConnections.Shape(shortLine,true,new Point(1,0),main,8,1)),"Short required connections are retained without forced bend");
        var map=new StreetMap{DetailsVersion=1};
        map.Roads.Add(new[]{new Point(-80,0),new Point(0,0)});map.Roads.Add(new[]{new Point(0,0),new Point(80,0)});
        map.Roads.Add(new[]{new Point(0,0),new Point(80,-10)});
        for(int i=0;i<3;i++){map.Types.Add(RoadType.Motorway);map.Details.Add(new RoadDetails{Highway=i==2?"motorway_link":"motorway",OneWay=true,DirectionKnown=true});}
        var plans=RoadSubgroups.Plan(map,new List<RoadSubgroup>(),RoadWidths.Manual(16),1,0,false,2000);
        Check(plans.Where(b=>!b.IsRamp).SelectMany(b=>b.Plan.Lines).All(l=>l.All(p=>p.Z==0)),"Main carriageway stays unchanged");
        Check(plans.Single(b=>b.IsRamp).Plan.Lines.Single().Length==4,"Shallow ramp receives native curve at shared main-road node");
        var fixture=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"Fixtures/highway-43-courses.json")));
        Point Position(Newtonsoft.Json.Linq.JToken p)=>new Point((double)p["x"],(double)p["z"]);
        var recorded=fixture.GroupBy(p=>(string)p["prefab"]).Select(g=>new RoadBatch{Category=0,IsRamp=g.Key=="Entity(15912:1)",
            Plan=new StreetBuildPlan{Lines=g.Select(p=>new[]{Position(p["start"]),Position(p["end"]),Position(p["control1"]),Position(p["control2"])}).ToList()}}).ToList();
        var before=recorded.SelectMany(b=>b.Plan.Lines).Select(c=>c.ToArray()).ToArray();
        var axes=RampConnections.AlignThroughRoads(recorded,1);
        int ramps=0;
        foreach(var line in recorded.Single(b=>b.IsRamp).Plan.Lines)foreach(int end in new[]{0,1})
        {
            string key=StreetBuildPlan.Key(line[end]);if(!axes.ContainsKey(key))continue;
            var tangents=recorded.Where(b=>!b.IsRamp).SelectMany(b=>b.Plan.Lines).SelectMany(c=>new[]{0,1}.Where(e=>StreetBuildPlan.Key(c[e])==key).Select(e=>new Point((c[e==0?2:3].X-c[e].X)*(e==0?1:-1),(c[e==0?2:3].Z-c[e].Z)*(e==0?1:-1)))).ToArray();
            double cross=tangents[0].X*tangents[1].Z-tangents[0].Z*tangents[1].X;
            Check(Math.Abs(cross)<.001,"Recorded motorway junction has continuous through-road tangent");ramps++;
        }
        Check(ramps==4,"All four recorded ramp junctions coordinated");
        var after=recorded.SelectMany(b=>b.Plan.Lines).ToArray();
        for(int i=0;i<before.Length;i++)
        {
            for(int e=0;e<2;e++)Check(before[i][e].X==after[i][e].X&&before[i][e].Z==after[i][e].Z,"Recorded carriageway endpoints stay in place");
            for(int k=0;k<=20;k++)
            {
                var a=CurvePlanner.Evaluate(before[i],k/20.0);var b=CurvePlanner.Evaluate(after[i],k/20.0);
                Check(Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z))<=3.01,"Recorded geometry adjustment stays local");
            }
        }
        Console.WriteLine("PASS: recorded 43-course fixture, four coordinated highway joins, fixed nodes and bounded deviation");
        Console.WriteLine("PASS: ramp approach shaping, both sides/directions/scales, fixed shared nodes and unchanged highway");
    }
}
