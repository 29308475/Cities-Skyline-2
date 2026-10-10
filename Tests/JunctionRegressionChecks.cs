using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;

static class JunctionRegressionChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Add(StreetMap map,Point a,Point b,int type,bool ring=false,bool oneWay=false)
    {
        map.Roads.Add(new[]{a,b});map.Types.Add((RoadType)type);
        map.Details.Add(new RoadDetails{Roundabout=ring,DirectionKnown=true,OneWay=oneWay});
    }
    static string Key(Point p)=>StreetBuildPlan.Key(p);
    static int Ends(IEnumerable<Point[]> lines,Point point)=>lines.Sum(l=>l.Take(2).Count(p=>Key(p)==Key(point)));
    public static void Run()
    {
        var profiles=Enumerable.Range(0,4).Select(i=>new RoadWidthProfile{PrefabId="category"+i,Meters=16}).ToArray();
        var map=new StreetMap{DetailsVersion=1};
        Add(map,new Point(-60,-30),new Point(60,30),1);
        Add(map,new Point(-30,60),new Point(30,-60),2);
        var group=new RoadSubgroup{Id="edited",Category=2,Profile=new RoadWidthProfile{PrefabId="narrow",Meters=8},Roads=new List<string>{RoadExclusions.Key(map.Roads[1])}};
        foreach(float scale in new[]{.5f,1f,2f})foreach(float width in new[]{8f,32f})
        {
            group.Profile=new RoadWidthProfile{PrefabId="edited-"+width,Meters=width};
            var plans=new[]{1,2}.SelectMany(c=>RoadSubgroups.Plan(map,new List<RoadSubgroup>{group},profiles,scale,c,false,2000)).ToList();
            Check(Ends(plans.SelectMany(b=>b.Plan.Lines),new Point(0,0))==4,"Separate-category X builds retain four common endpoints after profile change");
            Check(plans.Single(b=>b.Category==2).Profile==group.Profile,"Updated subgroup prefab reaches build plan");
        }

        // Four one-way arms, including closely spaced entry/exit points on each side.
        map=new StreetMap{DetailsVersion=1};
        var points=Enumerable.Range(0,48).Select(i=>new Point(18*Math.Cos(i*Math.PI/24),18*Math.Sin(i*Math.PI/24))).ToArray();
        for(int i=0;i<48;i++)Add(map,points[i],points[(i+1)%48],1,true,true);
        var joins=new[]{1,47,23,25};
        for(int n=0;n<joins.Length;n++)
        {
            var p=points[joins[n]];var outer=new Point(p.X*4,p.Z*4);
            Add(map,n%2==0?outer:p,n%2==0?p:outer,n%2==0?1:2,false,true);
        }
        group=new RoadSubgroup{Id="arm-edit",Category=2,Profile=new RoadWidthProfile{PrefabId="custom-arm",Meters=8},Roads=new List<string>{RoadExclusions.Key(map.Roads[49])}};
        var ring=new RoadWidthProfile{PrefabId="ring",Meters=8};
        var arms=new RoadWidthProfile{PrefabId="arms",Meters=8};
        foreach(bool left in new[]{false,true})foreach(float scale in new[]{.5f,1f,2f})
        {
            var plans=new[]{1,2}.SelectMany(c=>RoadSubgroups.Plan(map,new List<RoadSubgroup>{group},profiles,scale,c,true,2000,ring,left,arms)).ToList();
            var ringLines=plans.Where(b=>b.IsRing).SelectMany(b=>b.Plan.Lines).ToList();
            Check(ringLines.Count>0&&ringLines.Any(l=>l.Length==4),"Ring uses native curves rather than only short straight segments");
            Check(ringLines.SelectMany(l=>l.Take(2)).GroupBy(Key).All(g=>g.Count()==2),"Ring remains closed after category splitting");
            foreach(int join in joins)
                Check(Ends(ringLines,points[join])==2&&Ends(plans.SelectMany(b=>b.Plan.Lines),points[join])==3,"Each separate arm meets two ring curves at one exact junction");
            Check(plans.Any(b=>b.IsApproach&&b.Indices.Contains(49)&&b.Profile==arms),"Dedicated roundabout arm profile overrides subgroup road");
            var area=ringLines.Sum(l=>l[0].X*l[1].Z-l[1].X*l[0].Z);
            Check((area<0)==left,"Planned ring curves retain correct traffic direction");
            Check(plans.All(b=>b.Plan.Circles.Count==0),"No duplicate center-island construction for custom ring");
        }
        Console.WriteLine("PASS: X junctions after subgroup edits; multi-arm roundabout closure, connections, curves and both traffic sides across three scales");
    }
}
