using System;
using System.Collections.Generic;

namespace CityStreetOverlay
{
    // Nearby parallel main carriageways identify the median independently of source ordering.
    public sealed class CarriagewaySides
    {
        readonly StreetMap map; readonly double cell;
        readonly Dictionary<(int,int),List<int>> grid=new Dictionary<(int,int),List<int>>();
        static bool IsMain(RoadDetails d)=>d.DirectionKnown&&d.OneWay&&!d.Roundabout&&(d.Highway=="motorway"||d.Highway=="trunk");
        public CarriagewaySides(StreetMap map,float scale)
        {
            this.map=map;cell=80/scale;
            for(int i=0;i<map.Roads.Count;i++)
            {
                if(!IsMain(map.Details[i]))continue;
                var r=map.Roads[i];
                for(int x=Bucket(Math.Min(r[0].X,r[1].X));x<=Bucket(Math.Max(r[0].X,r[1].X));x++)
                for(int z=Bucket(Math.Min(r[0].Z,r[1].Z));z<=Bucket(Math.Max(r[0].Z,r[1].Z));z++)
                {var key=(x,z);if(!grid.TryGetValue(key,out var list))grid[key]=list=new List<int>();list.Add(i);}
            }
        }
        int Bucket(double value)=>(int)Math.Floor(value/cell);
        public double Sign(int index,double fallback,float scale)
        {
            var r=map.Roads[index];var d=map.Details[index];
            double dx=r[1].X-r[0].X,dz=r[1].Z-r[0].Z,len=Math.Sqrt(dx*dx+dz*dz);
            if(len<.001)return fallback;
            double mx=(r[0].X+r[1].X)/2,mz=(r[0].Z+r[1].Z)/2;
            double positive=double.MaxValue,negative=double.MaxValue;
            var seen=new HashSet<int>();
            for(int x=Bucket(mx)-1;x<=Bucket(mx)+1;x++)for(int z=Bucket(mz)-1;z<=Bucket(mz)+1;z++)
            {
                if(!grid.TryGetValue((x,z),out var list))continue;
                foreach(int j in list)
                {
                    if(j==index||!seen.Add(j))continue;
                    var other=map.Details[j];var s=map.Roads[j];
                    if(d.Layer!=other.Layer||d.Bridge!=other.Bridge||d.Tunnel!=other.Tunnel)continue;
                    if(StreetBuildPlan.Key(r[0])==StreetBuildPlan.Key(s[0])||StreetBuildPlan.Key(r[0])==StreetBuildPlan.Key(s[1])||StreetBuildPlan.Key(r[1])==StreetBuildPlan.Key(s[0])||StreetBuildPlan.Key(r[1])==StreetBuildPlan.Key(s[1]))continue;
                    double ux=s[1].X-s[0].X,uz=s[1].Z-s[0].Z,sl=Math.Sqrt(ux*ux+uz*uz);
                    if(sl<.001||Math.Abs((dx*ux+dz*uz)/(len*sl))<.95)continue;
                    double t=((mx-s[0].X)*ux+(mz-s[0].Z)*uz)/(sl*sl);
                    if(t<0||t>1)continue;
                    double vx=s[0].X+t*ux-mx,vz=s[0].Z+t*uz-mz;
                    double distance=Math.Sqrt(vx*vx+vz*vz)*scale;
                    if(distance<4||distance>80)continue;
                    double side=(vx*dz-vz*dx)/len;
                    if(side>0)positive=Math.Min(positive,distance);else negative=Math.Min(negative,distance);
                }
            }
            if(positive==double.MaxValue&&negative==double.MaxValue)return fallback;
            if(Math.Max(positive,negative)<Math.Min(positive,negative)*1.5)return 0; // ambiguous median
            return positive<negative?-1:1; // move away from the nearby carriageway
        }
    }
}
