using System;
using System.Linq;
using CityStreetOverlay;
static class StampPlanChecks
{
    static void Assert(bool ok){if(!ok)throw new Exception("Stamp plan regression");}
    public static void Run()
    {
        var map=new StreetMap();
        for(int i=0;i<8;i++){map.Roads.Add(new[]{new Point(i*30,0),new Point((i+1)*30,0)});map.Types.Add(RoadType.LocalRoad);}
        var plan=StampPlan.Create(map,RoadType.LocalRoad,1);
        Assert(plan.Count==2&&plan[0][0].X==0&&plan[0][1].X==120&&plan[1][1].X==240);
        map.Roads.Add(new[]{new Point(60,0),new Point(60,40)});map.Types.Add(RoadType.MainRoad);
        plan=StampPlan.Create(map,RoadType.LocalRoad,1);
        Assert(plan.Any(r=>r[1].X==60)&&plan.Any(r=>r[0].X==60));
        Assert(StampPlan.Create(map,RoadType.MainRoad,1).Count==1);
        Assert(StampPlan.Create(map,RoadType.LocalRoad,2).All(r=>Math.Abs(r[1].X-r[0].X)*2<=120));
        bool rejected=false;try{StampPlan.Create(map,RoadType.Motorway,1);}catch(InvalidOperationException){rejected=true;}Assert(rejected);
        var reverse=new StreetMap();reverse.Roads.Add(new[]{new Point(60,0),new Point(30,0)});reverse.Roads.Add(new[]{new Point(30,0),new Point(0,0)});
        var directed=StampPlan.Create(reverse,RoadType.LocalRoad,1);Assert(directed[0][0].X==60&&directed[0][1].X==0);
        // Dense rings, shuffled input, seams and entrances from a different category.
        foreach(float scale in new[]{0.5f,1f,2f})
        {
            var ring=new StreetMap();
            var points=Enumerable.Range(0,120).Select(i=>new Point(12*Math.Cos(i*Math.PI/60),12*Math.Sin(i*Math.PI/60))).ToArray();
            for(int i=0;i<120;i++){int k=(i*37)%120;ring.Roads.Add(new[]{points[k],points[(k+1)%120]});ring.Types.Add(RoadType.MainRoad);}
            foreach(int entrance in new[]{0,30,60,90}){ring.Roads.Add(new[]{points[entrance],new Point(points[entrance].X*3,points[entrance].Z*3)});ring.Types.Add(RoadType.LocalRoad);}
            var simplified=StampPlan.Create(ring,RoadType.MainRoad,scale);
            Assert(simplified.Count<120);
            foreach(var edge in simplified)
            {
                Assert(Length(edge)*scale>=2-1e-8);
                Assert(edge[0].X*edge[1].Z-edge[0].Z*edge[1].X>0); // Same circulation direction.
                Assert(simplified.Count(other=>Same(other[0],edge[1]))==1);
                Assert(simplified.Count(other=>Same(other[1],edge[0]))==1);
            }
            foreach(int entrance in new[]{0,30,60,90})Assert(simplified.Any(e=>Same(e[0],points[entrance])));
            foreach(var p in points)Assert(simplified.Min(e=>Deviation(p,e))*scale<=0.250001);
            Assert(ring.Roads.Count==124); // Source template is not altered.
            ring.Roads.RemoveRange(120,4);ring.Types.RemoveRange(120,4);
            var closed=StampPlan.Create(ring,RoadType.MainRoad,scale);
            Assert(closed.All(e=>Length(e)*scale>=2-1e-8));
            Assert(closed.All(e=>closed.Count(other=>Same(other[0],e[1]))==1));
        }
        var shortConnection=new StreetMap();shortConnection.Roads.Add(new[]{new Point(0,0),new Point(1,0)});
        Assert(StampPlan.Create(shortConnection,RoadType.LocalRoad,1).Count==1);
        var tail=new StreetMap();
        for(int i=0;i<6;i++)tail.Roads.Add(new[]{new Point(i*0.8,0),new Point((i+1)*0.8,0)});
        var joined=StampPlan.Create(tail,RoadType.LocalRoad,1);Assert(joined.Count==1&&Math.Abs(Length(joined[0])-4.8)<1e-8);
        Console.WriteLine("PASS: dense roundabouts, shuffled edges, ring closure, entrances, direction, 25 cm tolerance, scale and short connections");
        Console.WriteLine("PASS: stamp graph category selection, direction, junction preservation, collinear merging and size limit");
    }
    static bool Same(Point a,Point b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)<1e-8;
    static double Length(Point[] e)=>Math.Sqrt(Math.Pow(e[1].X-e[0].X,2)+Math.Pow(e[1].Z-e[0].Z,2));
    static double Deviation(Point p,Point[] e)
    {
        double dx=e[1].X-e[0].X,dz=e[1].Z-e[0].Z;
        double t=Math.Max(0,Math.Min(1,((p.X-e[0].X)*dx+(p.Z-e[0].Z)*dz)/(dx*dx+dz*dz)));
        return Math.Sqrt(Math.Pow(p.X-e[0].X-t*dx,2)+Math.Pow(p.Z-e[0].Z-t*dz,2));
    }
}
