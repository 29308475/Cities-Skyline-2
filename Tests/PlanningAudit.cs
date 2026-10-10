using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityStreetOverlay;
using Newtonsoft.Json;
static class PlanningAudit
{
    public static void Run(string file)
    {
        var saved=JsonConvert.DeserializeObject<SavedTemplate>(File.ReadAllText(file));
        var map=RoadExclusions.Apply(MapMosaic.Compose(saved.Tiles??new List<StreetMap>{saved.Map}),new HashSet<string>(saved.ExcludedRoads??new List<string>()));
        var batches=RoadSubgroups.Plan(map,saved.Subgroups??new List<RoadSubgroup>(),saved.WidthProfiles??RoadWidths.Manual(),saved.Scale,null,false,int.MaxValue);
        foreach(var batch in batches.Where(b=>b.Category==1))foreach(var line in batch.Plan.Lines)
            if(line.Take(2).Any(p=>Math.Abs(p.X+117.20)<0.02&&Math.Abs(p.Z+109.71)<0.02))
                Console.WriteLine("Lukkaer Strasse junction course: "+JsonConvert.SerializeObject(new {line,length=CurvePlanner.Length(line)}));
        Console.WriteLine(JsonConvert.SerializeObject(new {city=map.Name,sourceSegments=map.Roads.Count,
            categories=batches.GroupBy(b=>b.Category).Select(g=>new {category=g.Key,straight=g.Sum(b=>b.Plan.Lines.Count(c=>c.Length==2)),curved=g.Sum(b=>b.Plan.Lines.Count(c=>c.Length==4)),shorter16=g.Sum(b=>b.Plan.Lines.Count(c=>CurvePlanner.Length(c)*saved.Scale<16)),maxLength=g.Max(b=>b.Plan.Lines.Max(CurvePlanner.Length))*saved.Scale})},Formatting.Indented));
    }
    public static List<Point> SparseArc()
    {
        var points=new List<Point>();
        for(int i=0;i<4;i++)
        {
            var a=new Point(100*Math.Cos(i*Math.PI/8),100*Math.Sin(i*Math.PI/8));
            var b=new Point(100*Math.Cos((i+1)*Math.PI/8),100*Math.Sin((i+1)*Math.PI/8));
            points.Add(a);points.Add(new Point((a.X+b.X)/2,(a.Z+b.Z)/2));
        }
        points.Add(new Point(0,100));return points;
    }
    public static void Fixture()
    {
        var output=new List<Point[]>();CurvePlanner.Append(SparseArc(),1,output);
        Console.WriteLine("Sparse quarter-circle: "+output.Count+" courses, "+output.Count(c=>c.Length==4)+" cubic curves");
    }
}
