using System;
using System.Collections.Generic;
using System.Globalization;

namespace CityStreetOverlay
{
    public static class StampPlan
    {
        static string Key(Point p) => Math.Round(p.X,2).ToString(CultureInfo.InvariantCulture)+":"+Math.Round(p.Z,2).ToString(CultureInfo.InvariantCulture);
        public static List<Point[]> Create(StreetMap map,RoadType category,float scale,HashSet<string> stops=null,bool nativeCurves=false,int limit=2000)
        {
            if(map?.Roads==null)throw new ArgumentException("Keine Vorlage geladen.");
            if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<=0)throw new ArgumentException("Ungültiger Maßstab.");
            if(nativeCurves)return RoadGraph.Courses(map,category,scale,stops,limit);
            var degree=new Dictionary<string,int>();
            var outgoing=new Dictionary<string,List<int>>();
            var candidates=new List<Point[]>();
            for(int i=0;i<map.Roads.Count;i++)
            {
                var r=map.Roads[i];
                foreach(var p in r){var k=Key(p);degree[k]=degree.TryGetValue(k,out var n)?n+1:1;}
                if((map.HasRoadTypes?map.Types[i]:RoadType.LocalRoad)!=category)continue;
                var key=Key(r[0]);if(!outgoing.ContainsKey(key))outgoing[key]=new List<int>();
                outgoing[key].Add(candidates.Count);candidates.Add(r);
            }
            var incoming=new Dictionary<string,int>();
            foreach(var r in candidates){var k=Key(r[1]);incoming[k]=incoming.TryGetValue(k,out var n)?n+1:1;}
            bool Interior(string key) => (stops==null||!stops.Contains(key))&&degree[key]==2&&incoming.TryGetValue(key,out var n)&&n==1&&outgoing.TryGetValue(key,out var list)&&list.Count==1;
            var used=new HashSet<int>();var result=new List<Point[]>();
            // Start at junctions/endpoints first, then process remaining closed rings.
            // This makes simplification independent of the order of source edges.
            for(int pass=0;pass<2;pass++)for(int i=0;i<candidates.Count;i++)
            {
                if(pass==0&&Interior(Key(candidates[i][0])))continue;
                if(!used.Add(i))continue;
                var chain=new List<Point>{candidates[i][0],candidates[i][1]};
                var b=chain[1];
                while(Interior(Key(b)))
                {
                    int next=outgoing[Key(b)][0];
                    if(!used.Add(next))break;
                    b=candidates[next][1];chain.Add(b);
                }
                if(nativeCurves)CurvePlanner.Append(chain,scale,result);else Simplify(chain,scale,result);
            }
            if(result.Count==0)throw new InvalidOperationException("Keine Straßen dieser Kategorie in der Vorlage.");
            // 16 m is a planning preference; preserve unavoidable short connections.
            if(result.Count>2000)throw new InvalidOperationException("Mehr als 2.000 Bauabschnitte. Bitte auf der Karte weniger Abschnitte auswählen.");
            return result;
        }

        static double Distance(Point a,Point b) => Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));

        static void Simplify(List<Point> points,float scale,List<Point[]> result)
        {
            int count=points.Count;
            var shortCount=new int[count];var segments=new int[count];var next=new int[count];
            for(int i=count-2;i>=0;i--)
            {
                // Always retain the original edge as a fallback. An unavoidable short
                // junction connection belongs to native validation, not silent deletion.
                next[i]=i+1;segments[i]=1+segments[i+1];
                shortCount[i]=shortCount[i+1]+(Distance(points[i],points[i+1])*scale<2?1:0);
                double arc=Distance(points[i],points[i+1])*scale;
                for(int j=i+2;j<count;j++)
                {
                    arc+=Distance(points[j-1],points[j])*scale;
                    if(arc>120||j-i>400)break;
                    double length=Distance(points[i],points[j])*scale;
                    if(length<2||!FollowsShape(points,i,j,scale))continue;
                    int shortEdges=shortCount[j],edges=1+segments[j];
                    if(shortEdges<shortCount[i]||(shortEdges==shortCount[i]&&edges<=segments[i]))
                    {shortCount[i]=shortEdges;segments[i]=edges;next[i]=j;}
                }
            }
            for(int i=0;i<count-1;i=next[i])result.Add(new[]{points[i],points[next[i]]});
        }

        static bool FollowsShape(List<Point> points,int start,int end,float scale)
        {
            var a=points[start];var b=points[end];double dx=b.X-a.X,dz=b.Z-a.Z;
            double squared=dx*dx+dz*dz,previous=0;
            for(int k=start+1;k<end;k++)
            {
                double x=points[k].X-a.X,z=points[k].Z-a.Z;
                double t=(x*dx+z*dz)/squared;
                // Preserve forward direction, avoid cutting across loops, and allow
                // at most 25 cm of world-space deviation from the source polyline.
                if(t<previous||t>1||Math.Abs(x*dz-z*dx)/Math.Sqrt(squared)*scale>0.25)return false;
                previous=t;
            }
            return true;
        }
    }
}
