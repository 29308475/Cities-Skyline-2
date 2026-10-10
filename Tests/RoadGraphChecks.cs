using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;
using Newtonsoft.Json;
static class RoadGraphChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static void Add(StreetMap m,Point a,Point b,bool bridge=false,int layer=0,bool oneWay=false)
    {m.Roads.Add(new[]{a,b});m.Types.Add(RoadType.Motorway);m.Details.Add(new RoadDetails{DirectionKnown=true,Bridge=bridge,Layer=layer,OneWay=oneWay});}
    public static void Run()
    {
        var map=new StreetMap{DetailsVersion=1};
        Add(map,new Point(0,0),new Point(30,0));Add(map,new Point(60,0),new Point(30,0));
        var courses=StampPlan.Create(map,RoadType.Motorway,1,null,true);
        Check(courses.Count==1&&CurvePlanner.Length(courses[0])==60,"Bidirectional OSM ways merge despite opposite digitization");
        map.Details.ForEach(d=>d.OneWay=true);
        Check(StampPlan.Create(map,RoadType.Motorway,1,null,true).Count==2,"One-way direction must not be reversed");
        map.Details.ForEach(d=>d.OneWay=false);
        Add(map,new Point(30.004,0.001),new Point(30,40));
        courses=StampPlan.Create(map,RoadType.Motorway,1,null,true);
        Check(courses.Count==3,"T junction remains split");
        var shared=courses.SelectMany(c=>new[]{c[0],c[1]}).Where(p=>Math.Abs(p.X-30)<0.01&&Math.Abs(p.Z)<0.01).ToList();
        Check(shared.Count==3&&shared.All(p=>p.X==30&&p.Z==0),"Native node positions must be exactly identical, not merely close");
        var profile=RoadWidths.Manual(16);profile[0].PrefabId="highway";
        var group=new RoadSubgroup{Id="ramp",Name="Auf-/Abfahrt",Category=0,Profile=new RoadWidthProfile{PrefabId="ramp",Meters=8},Roads=new List<string>{RoadExclusions.Key(map.Roads[2])}};
        var groups=new List<RoadSubgroup>{group};RoadSubgroups.Validate(groups);
        var batches=RoadSubgroups.Plan(map,groups,profile,1,0,false,2000);
        Check(batches.Count==2&&batches.Sum(b=>b.Plan.Lines.Count)==3,"Subgroup keeps its own prefab and a shared junction");
        Check(batches.Single(b=>b.Profile.PrefabId=="ramp").Plan.Lines.Count==1,"Only assigned ramp gets narrower road");
        var restored=JsonConvert.DeserializeObject<SavedTemplate>(JsonConvert.SerializeObject(new SavedTemplate{Map=map,Subgroups=groups}));
        RoadSubgroups.Validate(restored.Subgroups);
        Check(RoadSubgroups.Plan(restored.Map,restored.Subgroups,profile,1,0,false,2000).Any(b=>b.Profile.PrefabId=="ramp"),"Offline roundtrip retains subgroup prefab");
        group.Category=2;
        Check(RoadSubgroups.Plan(map,groups,profile,1,2,false,2000).Single().Profile.PrefabId=="ramp","Reassignment changes category");
        var crossing=new StreetMap{DetailsVersion=1};
        Add(crossing,new Point(-30,0),new Point(0,0),true,1);Add(crossing,new Point(0,0),new Point(30,0),true,1);
        Add(crossing,new Point(0,-30),new Point(0,0));Add(crossing,new Point(0,0),new Point(0,30));
        batches=RoadSubgroups.Plan(crossing,new List<RoadSubgroup>(),profile,1,0,false,2000);
        Check(batches.Count==2&&batches.Any(b=>b.Plan.Elevated.Count==0)&&batches.Any(b=>b.Plan.Elevated.Count==3),"Ground crossing beneath bridge does not share elevation");
        Console.WriteLine("PASS: canonical shared nodes, bidirectional and one-way chains, subgroups/prefabs, category reassignment, offline persistence and grade separation");
    }
}
