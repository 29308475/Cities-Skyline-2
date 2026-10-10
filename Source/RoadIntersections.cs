using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public static class RoadIntersections
    {
        // Split source geometry before category filtering, retaining source ownership.
        public static StreetMap Split(StreetMap source,ref HashSet<int> selection)
        {
            int count=source.Roads.Count;
            // Use the same node coordinates as RoadGraph before calculating crossings.
            // Recomputing existing joins from unrounded coordinates can create tiny extra cuts.
            var roads=source.Roads.Select(r=>new[]{RoadGraph.Canonical(r[0]),RoadGraph.Canonical(r[1])}).ToArray();
            var cuts=new SortedDictionary<double,Point>[count];
            for(int i=0;i<count;i++)cuts[i]=new SortedDictionary<double,Point>{{0,roads[i][0]},{1,roads[i][1]}};
            var order=Enumerable.Range(0,count).OrderBy(i=>Math.Min(roads[i][0].X,roads[i][1].X)).ToArray();
            bool Compatible(int i,int j)
            {
                if(!source.HasDetails)return true;
                var a=source.Details[i];var b=source.Details[j];
                // Projected crossings of structures must not become ground junctions.
                return !a.Bridge&&!b.Bridge&&!a.Tunnel&&!b.Tunnel&&a.Layer==b.Layer;
            }
            double Cross(Point a,Point b)=>a.X*b.Z-a.Z*b.X;
            Point Sub(Point a,Point b)=>new Point(a.X-b.X,a.Z-b.Z);
            for(int n=0;n<count;n++)
            {
                int i=order[n];var a=roads[i][0];var b=roads[i][1];var r=Sub(b,a);
                for(int m=n+1;m<count;m++)
                {
                    int j=order[m];var c=roads[j][0];var d=roads[j][1];
                    if(Math.Min(c.X,d.X)>Math.Max(a.X,b.X)+1e-8)break;
                    if(StreetBuildPlan.Key(a)==StreetBuildPlan.Key(c)||StreetBuildPlan.Key(a)==StreetBuildPlan.Key(d)||
                        StreetBuildPlan.Key(b)==StreetBuildPlan.Key(c)||StreetBuildPlan.Key(b)==StreetBuildPlan.Key(d))continue;
                    if(Math.Min(c.Z,d.Z)>Math.Max(a.Z,b.Z)||Math.Max(c.Z,d.Z)<Math.Min(a.Z,b.Z)||!Compatible(i,j))continue;
                    var s=Sub(d,c);double denominator=Cross(r,s);
                    if(Math.Abs(denominator)<1e-9)continue;
                    var delta=Sub(c,a);double t=Cross(delta,s)/denominator,u=Cross(delta,r)/denominator;
                    if(t < -1e-9||t > 1+1e-9||u < -1e-9||u > 1+1e-9)continue;
                    t=Math.Max(0,Math.Min(1,t));u=Math.Max(0,Math.Min(1,u));
                    var point=RoadGraph.Canonical(new Point(a.X+t*r.X,a.Z+t*r.Z));
                    cuts[i][t]=point;cuts[j][u]=point;
                }
            }
            var result=new StreetMap{DetailsVersion=source.HasDetails?source.DetailsVersion:0};
            var selected=selection==null?null:new HashSet<int>();
            for(int i=0;i<count;i++)
            {
                var points=cuts[i].Values.ToArray();
                for(int k=1;k<points.Length;k++)
                {
                    if(StreetBuildPlan.Key(points[k-1])==StreetBuildPlan.Key(points[k]))continue;
                    if(selection!=null&&selection.Contains(i))selected.Add(result.Roads.Count);
                    result.Roads.Add(new[]{points[k-1],points[k]});
                    result.Types.Add(source.HasRoadTypes?source.Types[i]:RoadType.LocalRoad);
                    if(source.HasDetails)result.Details.Add(source.Details[i]);
                }
            }
            selection=selected;return result;
        }
    }
}
