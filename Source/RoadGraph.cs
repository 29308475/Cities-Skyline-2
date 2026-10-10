using System;
using System.Collections.Generic;

namespace CityStreetOverlay
{
    public static class RoadGraph
    {
        public static double Coordinate(double value){double rounded=Math.Round(value,2);return rounded==0?0:rounded;}
        public static Point Canonical(Point p)=>new Point(Coordinate(p.X),Coordinate(p.Z));
        static string Key(Point p)=>StreetBuildPlan.Key(p);
        public static List<Point[]> Courses(StreetMap map,RoadType category,float scale,HashSet<string> stops,int limit)
        {
            var at=new Dictionary<string,List<int>>();var edges=new List<Point[]>();
            for(int i=0;i<map.Roads.Count;i++)
            {
                var edge=new[]{Canonical(map.Roads[i][0]),Canonical(map.Roads[i][1])};edges.Add(edge);
                foreach(var p in edge){var k=Key(p);if(!at.ContainsKey(k))at[k]=new List<int>();at[k].Add(i);}
            }
            bool Chosen(int i)=>(map.HasRoadTypes?map.Types[i]:RoadType.LocalRoad)==category;
            bool Reversible(int i)=>map.HasDetails&&map.Details[i].DirectionKnown&&!map.Details[i].OneWay;
            bool Compatible(int a,int b)
            {
                if(!Chosen(a)||!Chosen(b))return false;
                if(!map.HasDetails)return true;
                var x=map.Details[a];var y=map.Details[b];
                return x.OneWay==y.OneWay&&x.Bridge==y.Bridge&&x.Layer==y.Layer&&x.Tunnel==y.Tunnel;
            }
            bool Interior(string k)
            {
                var list=at[k];
                return (stops==null||!stops.Contains(k))&&list.Count==2&&Compatible(list[0],list[1])&&
                    (Reversible(list[0])&&Reversible(list[1])||Key(edges[list[0]][0])!=Key(edges[list[1]][0]));
            }
            var used=new HashSet<int>();var result=new List<Point[]>();
            for(int pass=0;pass<2;pass++)for(int i=0;i<edges.Count;i++)
            {
                if(!Chosen(i)||used.Contains(i))continue;
                var edge=edges[i];bool reverse=Reversible(i)&&Interior(Key(edge[0]))&&!Interior(Key(edge[1]));
                var start=edge[reverse?1:0];var end=edge[reverse?0:1];
                if(pass==0&&Interior(Key(start)))continue;
                used.Add(i);var chain=new List<Point>{start,end};
                while(Interior(Key(end)))
                {
                    int next=-1;foreach(int candidate in at[Key(end)])if(!used.Contains(candidate)){next=candidate;break;}
                    if(next<0)break;
                    var r=edges[next];
                    if(Key(r[0])==Key(end))end=r[1];
                    else if(Reversible(next))end=r[0];else break;
                    used.Add(next);chain.Add(end);
                }
                CurvePlanner.Append(chain,scale,result);
            }
            if(result.Count==0)throw new InvalidOperationException("Keine Straßen in diesem Bereich.");
            if(result.Count>limit)throw new InvalidOperationException("Mehr als 2.000 Bauabschnitte. Bitte weitere Bereiche ausschließen.");
            return result;
        }
    }
}
