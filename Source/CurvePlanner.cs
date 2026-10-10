using System;
using System.Collections.Generic;

namespace CityStreetOverlay
{
    // Native courses: [start, end] for lines; [start, end, control1, control2] for cubics.
    public static class CurvePlanner
    {
        public const double MinimumLength=16;
        static double Distance(Point a,Point b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
        static Point Add(Point a,Point b)=>new Point(a.X+b.X,a.Z+b.Z);
        static Point Mul(Point a,double s)=>new Point(a.X*s,a.Z*s);
        static Point Sub(Point a,Point b)=>new Point(a.X-b.X,a.Z-b.Z);
        public static Point Evaluate(Point[] c,double t)
        {
            if(c.Length==2)return Add(Mul(c[0],1-t),Mul(c[1],t));
            double u=1-t;
            return Add(Add(Mul(c[0],u*u*u),Mul(c[2],3*u*u*t)),Add(Mul(c[3],3*u*t*t),Mul(c[1],t*t*t)));
        }
        public static double Length(Point[] c)
        {
            if(c.Length==2)return Distance(c[0],c[1]);
            double length=0;var previous=c[0];
            for(int k=1;k<=64;k++){var p=Evaluate(c,k/64.0);length+=Distance(previous,p);previous=p;}
            return length;
        }
        static bool Straight(List<Point> p,int first,int last,float scale)
        {
            var a=p[first];var d=Sub(p[last],a);double length=Distance(a,p[last]),previous=0;
            if(length<0.001)return false;
            for(int k=first+1;k<last;k++)
            {
                var v=Sub(p[k],a);double t=(v.X*d.X+v.Z*d.Z)/(length*length);
                if(t<previous||t>1||Math.Abs(v.X*d.Z-v.Z*d.X)/length*scale>0.1)return false;
                previous=t;
            }
            return true;
        }
        static Point[] Fit(List<Point> p,int first,int last,float scale)
        {
            if(Straight(p,first,last,scale))return new[]{p[first],p[last]};
            if(last-first<2||Distance(p[first],p[last])*scale<2)return null;
            var times=new double[last-first+1];
            for(int k=1;k<times.Length;k++)times[k]=times[k-1]+Distance(p[first+k-1],p[first+k]);
            double arc=times[times.Length-1];if(arc*scale>1000||arc<0.001)return null;
            for(int k=1;k<times.Length;k++)times[k]/=arc;
            double aa=0,ab=0,bb=0;Point ra=new Point(),rb=new Point();
            for(int k=1;k<times.Length-1;k++)
            {
                double t=times[k],u=1-t,a=3*u*u*t,b=3*u*t*t;
                var residual=Sub(p[first+k],Add(Mul(p[first],u*u*u),Mul(p[last],t*t*t)));
                aa+=a*a;ab+=a*b;bb+=b*b;ra=Add(ra,Mul(residual,a));rb=Add(rb,Mul(residual,b));
            }
            double determinant=aa*bb-ab*ab;
            Point c1,c2;
            if(times.Length==3)
            {
                double t=times[1],u=1-t;
                var q=Mul(Sub(p[first+1],Add(Mul(p[first],u*u),Mul(p[last],t*t))),1/(2*u*t));
                c1=Add(p[first],Mul(Sub(q,p[first]),2.0/3));
                c2=Add(p[last],Mul(Sub(q,p[last]),2.0/3));
            }
            else
            {
                if(determinant<1e-12)return null;
                c1=Mul(Sub(Mul(ra,bb),Mul(rb,ab)),1/determinant);
                c2=Mul(Sub(Mul(rb,aa),Mul(ra,ab)),1/determinant);
            }
            if(Distance(c1,p[first])>arc||Distance(c2,p[last])>arc)return null;
            var curve=new[]{p[first],p[last],c1,c2};
            // Chord-length parameters are only an initial estimate. A cubic's speed
            // varies along a bend; compare geometry, not an arbitrary equal-time sample.
            for(int k=1;k<times.Length-1;k++)times[k]=NearestParameter(curve,p[first+k],times[k]);
            // Check both source vertices and interval midpoints; do not round off a sharp corner.
            for(int k=0;k<times.Length;k++)
            {
                if(Distance(Evaluate(curve,times[k]),p[first+k])*scale>0.25)return null;
                if(k>0&&times[k]<=times[k-1])return null;
                if(k>0)
                {
                    double t=(times[k-1]+times[k])/2;
                    // Sparse OSM chords sit inside a smooth bend. Allow the sagitta implied
                    // by adjacent headings, while retaining the strict vertex constraint.
                    double turn=Math.Max(Turn(p,first+k-1,first,last),Turn(p,first+k,first,last));
                    double allowance=0.25+Distance(p[first+k-1],p[first+k])*scale*0.55*Math.Tan(turn/4);
                    if(Distance(Evaluate(curve,t),Mul(Add(p[first+k-1],p[first+k]),0.5))*scale>allowance)return null;
                    var tangent=Sub(Evaluate(curve,Math.Min(1,t+0.0001)),Evaluate(curve,Math.Max(0,t-0.0001)));
                    var edge=Sub(p[first+k],p[first+k-1]);if(tangent.X*edge.X+tangent.Z*edge.Z<=0)return null;
                }
                if(k>0&&k<times.Length-1)
                {
                    var a=Sub(p[first+k],p[first+k-1]);var b=Sub(p[first+k+1],p[first+k]);
                    double denominator=Distance(p[first+k],p[first+k-1])*Distance(p[first+k+1],p[first+k]);
                    if(denominator>1e-9&&(a.X*b.X+a.Z*b.Z)/denominator<0.866)return null;
                }
            }
            return curve;
        }
        public static void Append(List<Point> points,float scale,List<Point[]> output)
        {
            // Graph junctions are already chain boundaries. Remove only collinear
            // sampling points inside a chain, never a junction or a direction reversal.
            var reduced=new List<Point>();
            foreach(var p in points)
            {
                while(reduced.Count>=2)
                {
                    var a=reduced[reduced.Count-2];var b=reduced[reduced.Count-1];
                    var d=Sub(p,a);var v=Sub(b,a);double length=Distance(p,a);
                    if(length<1e-9)break;
                    double t=(v.X*d.X+v.Z*d.Z)/(length*length);
                    if(t<0||t>1||Math.Abs(v.X*d.Z-v.Z*d.X)/length*scale>0.02)break;
                    reduced.RemoveAt(reduced.Count-1);
                }
                if(reduced.Count==0||Distance(reduced[reduced.Count-1],p)>1e-9)reduced.Add(p);
            }
            points=reduced;
            if(points.Count<2)return;
            if(Straight(points,0,points.Count-1,scale)){output.Add(new[]{points[0],points[points.Count-1]});return;}
            int count=points.Count;
            var shorts=new int[count];var costs=new int[count];var next=new int[count];var curves=new Point[count][];
            for(int i=count-2;i>=0;i--)
            {
                curves[i]=new[]{points[i],points[i+1]};next[i]=i+1;costs[i]=costs[i+1]+1;
                shorts[i]=shorts[i+1]+(Length(curves[i])*scale<MinimumLength?1:0);
                // A long straight inside a mixed chain is not limited by the curve fitting window.
                var direction=Sub(points[i+1],points[i]);double baseLength=Distance(points[i+1],points[i]);
                int straightEnd=i+1;double previous=baseLength;
                if(baseLength>1e-9)for(int j=i+2;j<count;j++)
                {
                    var v=Sub(points[j],points[i]);double along=(v.X*direction.X+v.Z*direction.Z)/baseLength;
                    if(along<previous||Math.Abs(v.X*direction.Z-v.Z*direction.X)/baseLength*scale>0.05)break;
                    previous=along;straightEnd=j;
                }
                if(straightEnd>i+1&&Straight(points,i,straightEnd,scale))
                {
                    var line=new[]{points[i],points[straightEnd]};
                    int shortCount=shorts[straightEnd]+(Length(line)*scale<MinimumLength?1:0),cost=costs[straightEnd]+1;
                    if(shortCount<shorts[i]||(shortCount==shorts[i]&&cost<costs[i]))
                    {shorts[i]=shortCount;costs[i]=cost;curves[i]=line;next[i]=straightEnd;}
                }
                for(int j=i+2;j<count&&j<=i+128;j++)
                {
                    var curve=Fit(points,i,j,scale);if(curve==null)continue;
                    int shortCount=shorts[j]+(Length(curve)*scale<MinimumLength?1:0),cost=costs[j]+1;
                    if(shortCount<shorts[i]||(shortCount==shorts[i]&&cost<costs[i]))
                    {shorts[i]=shortCount;costs[i]=cost;curves[i]=curve;next[i]=j;}
                }
            }
            for(int i=0;i<count-1;i=next[i])output.Add(curves[i]);
        }
        static double Turn(List<Point> p,int k,int first,int last)
        {
            if(k<=first||k>=last)return 0;
            var a=Sub(p[k],p[k-1]);var b=Sub(p[k+1],p[k]);
            double denominator=Distance(p[k],p[k-1])*Distance(p[k+1],p[k]);
            return denominator<1e-9?0:Math.Acos(Math.Max(-1,Math.Min(1,(a.X*b.X+a.Z*b.Z)/denominator)));
        }
        static double NearestParameter(Point[] curve,Point point,double initial)
        {
            double lo=Math.Max(0,initial-0.15),hi=Math.Min(1,initial+0.15);
            for(int i=0;i<24;i++)
            {
                double a=lo+(hi-lo)/3,b=hi-(hi-lo)/3;
                if(Distance(Evaluate(curve,a),point)<Distance(Evaluate(curve,b),point))hi=b;else lo=a;
            }
            return (lo+hi)/2;
        }
    }
}
