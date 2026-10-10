using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public static class RoundaboutGeometry
    {
        // Find isolated simple cycles, not arbitrary circular-looking fragments.
        // Iterative biconnected decomposition avoids recursion on large city graphs.
        public static StreetMap Infer(StreetMap source,float scale)
        {
            var at=new Dictionary<string,List<int>>();
            for(int i=0;i<source.Roads.Count;i++)foreach(var p in source.Roads[i])
            {var k=StreetBuildPlan.Key(p);if(!at.ContainsKey(k))at[k]=new List<int>();at[k].Add(i);}
            var found=new HashSet<int>();var depth=new Dictionary<string,int>();var low=new Dictionary<string,int>();
            var parent=new Dictionary<string,int>();var next=new Dictionary<string,int>();var stack=new Stack<string>();var edges=new Stack<int>();int clock=0;
            string Other(int i,string k)=>StreetBuildPlan.Key(source.Roads[i][StreetBuildPlan.Key(source.Roads[i][0])==k?1:0]);
            void Inspect(List<int> component)
            {
                if(component.Count<6)return;
                var counts=new Dictionary<string,int>();var points=new Dictionary<string,Point>();
                foreach(int i in component)
                {
                    if(source.HasDetails&&(source.Details[i].Bridge||source.Details[i].Tunnel))return;
                    foreach(var p in source.Roads[i]){var k=StreetBuildPlan.Key(p);points[k]=p;counts[k]=counts.TryGetValue(k,out var n)?n+1:1;}
                }
                if(counts.Values.Any(n=>n!=2)||points.Count!=component.Count)return;
                var values=points.Values.ToArray();
                var center=new Point((values.Min(p=>p.X)+values.Max(p=>p.X))/2,(values.Min(p=>p.Z)+values.Max(p=>p.Z))/2);
                var radii=values.Select(p=>Math.Sqrt((p.X-center.X)*(p.X-center.X)+(p.Z-center.Z)*(p.Z-center.Z))).ToArray();
                double radius=radii.Average();
                if(radius*scale<4||radius*scale>30||radii.Any(r=>Math.Abs(r-radius)>radius*0.12))return;
                // Reject squares/polygons with subdivided sides and require real approaches.
                double length=component.Sum(i=>CurvePlanner.Length(source.Roads[i]));
                if(Math.Abs(length-2*Math.PI*radius)>radius*0.3||points.Keys.Count(k=>at[k].Count>2)<2)return;
                foreach(int i in component)found.Add(i);
            }
            foreach(var root in at.Keys)
            {
                if(depth.ContainsKey(root))continue;
                depth[root]=low[root]=++clock;parent[root]=-1;next[root]=0;stack.Push(root);
                while(stack.Count>0)
                {
                    var v=stack.Peek();
                    if(next[v]<at[v].Count)
                    {
                        int e=at[v][next[v]++];if(e==parent[v])continue;var w=Other(e,v);
                        if(!depth.ContainsKey(w)){edges.Push(e);parent[w]=e;depth[w]=low[w]=++clock;next[w]=0;stack.Push(w);}
                        else if(depth[w]<depth[v]){edges.Push(e);low[v]=Math.Min(low[v],depth[w]);}
                    }
                    else
                    {
                        stack.Pop();int e=parent[v];if(e<0)continue;var p=Other(e,v);low[p]=Math.Min(low[p],low[v]);
                        if(low[v]>=depth[p]){var component=new List<int>();int item;do{item=edges.Pop();component.Add(item);}while(item!=e);Inspect(component);}
                    }
                }
            }
            if(found.Count==0)return source;
            var result=new StreetMap{Roads=source.Roads,Types=source.Types,DetailsVersion=1};
            for(int i=0;i<source.Roads.Count;i++)
            {
                var d=source.HasDetails?source.Details[i]:new RoadDetails();
                result.Details.Add(new RoadDetails{WayId=d.WayId,Highway=d.Highway,Lanes=d.Lanes,WidthMeters=d.WidthMeters,Bridge=d.Bridge,Tunnel=d.Tunnel,Layer=d.Layer,
                    DirectionKnown=d.DirectionKnown,OneWay=d.OneWay,Roundabout=d.Roundabout||found.Contains(i)});
            }
            return result;
        }
    }
}
