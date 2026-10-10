using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;
static class CurvePlannerChecks
{
    static void Check(bool condition,string text){if(!condition)throw new Exception(text);}
    static StreetMap Map(List<Point> points)
    {
        var map=new StreetMap();
        for(int i=1;i<points.Count;i++){map.Roads.Add(new[]{points[i-1],points[i]});map.Types.Add(RoadType.LocalRoad);}
        return map;
    }
    public static void Run()
    {
        var sparse=new List<Point[]>();
        CurvePlanner.Append(PlanningAudit.SparseArc(),1,sparse);
        Check(sparse.Count==1&&sparse[0].Length==4,"Sparse bend with artificial chord midpoints becomes one native curve");
        Check(Math.Abs(CurvePlanner.Length(sparse[0])-Math.PI*50)<1,"Sparse curve preserves quarter-circle arc length");
        for(int i=0;i<=100;i++)
        {
            var p=CurvePlanner.Evaluate(sparse[0],i/100.0);
            Check(Math.Abs(Math.Sqrt(p.X*p.X+p.Z*p.Z)-100)<0.5,"Sparse curve stays near source circle between vertices");
        }
        var straight=Map(Enumerable.Range(0,501).Select(i=>new Point(i*4,0)).ToList());
        var courses=StampPlan.Create(straight,RoadType.LocalRoad,1,null,true);
        Check(courses.Count==1&&CurvePlanner.Length(courses[0])==2000,"Long straight chain is one course, not 120 m fragments");
        foreach(float scale in new[]{0.5f,1f,2f})
        {
            var points=Enumerable.Range(0,81).Select(i=>new Point(100*Math.Cos(i*Math.PI/160),100*Math.Sin(i*Math.PI/160))).ToList();
            courses=StampPlan.Create(Map(points),RoadType.LocalRoad,scale,null,true);
            Check(courses.Count<=4&&courses.Any(c=>c.Length==4),"Smooth quarter circle uses native cubic courses");
            Check(courses.All(c=>CurvePlanner.Length(c)*scale>=16),"Every native course is at least 16 world meters");
            Check(Math.Abs(courses.Sum(CurvePlanner.Length)-Math.PI*50)<0.5,"Curve arc length, not endpoint chord length");
            Check(courses[0][0].X==points[0].X&&courses[courses.Count-1][1].Z==points[80].Z,"Endpoint positions preserved");
        }
        var corner=Map(new List<Point>{new Point(0,0),new Point(20,0),new Point(20,20)});
        courses=StampPlan.Create(corner,RoadType.LocalRoad,1,null,true);
        Check(courses.Count==2&&courses.All(c=>c.Length==2),"Sharp corner is not rounded away");
        straight.Roads.Add(new[]{new Point(400,0),new Point(400,30)});straight.Types.Add(RoadType.MainRoad);
        courses=StampPlan.Create(straight,RoadType.LocalRoad,1,null,true);
        Check(courses.Count==2&&courses[0][1].X==400,"Cross-category junction splits straight chain");
        courses=StampPlan.Create(Map(new List<Point>{new Point(0,0),new Point(15,0)}),RoadType.LocalRoad,1,null,true);
        Check(courses.Count==1&&CurvePlanner.Length(courses[0])==15,"Unmergeable short connection is retained and allowed");
        var junctions=Map(new List<Point>{new Point(0,0),new Point(10,0),new Point(20,0),new Point(100,0)});
        foreach(int x in new[]{10,20}){junctions.Roads.Add(new[]{new Point(x,0),new Point(x,30)});junctions.Types.Add(RoadType.MainRoad);}
        courses=StampPlan.Create(junctions,RoadType.LocalRoad,1,null,true);
        Check(courses.Count==3&&courses.Sum(CurvePlanner.Length)==100&&CurvePlanner.Length(courses[1])==10,"Short junction connections do not block or truncate the rest of the build");
        courses=StampPlan.Create(Map(new List<Point>{new Point(0,0),new Point(8,0)}),RoadType.LocalRoad,2,null,true);
        Check(courses.Count==1,"Minimum uses selected world scale without changing it");
        Console.WriteLine("PASS: native curves, long straights, 16 m preference with short connection exceptions, fixed junctions and sharp corners");
    }
}
