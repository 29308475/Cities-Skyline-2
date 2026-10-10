using System;
using System.Linq;

namespace CityStreetOverlay
{
    public static class LaneAlignment
    {
        // Offsets are measured in meters to the right of the road's travel direction.
        // Align all common lanes from the median side, leaving added lanes on the outside.
        public static bool TryShift(RoadWidthProfile baseline,RoadWidthProfile wider,double outside,out double shift)
        {
            shift=0;var a=baseline.DrivingLaneOffsets;var b=wider.DrivingLaneOffsets;
            if(a==null||b==null||a.Length==0||b.Length<=a.Length||outside==0)return false;
            if(a.Concat(b).Any(v=>float.IsNaN(v)||float.IsInfinity(v)))return false;
            a=a.OrderBy(v=>v*outside).ToArray();b=b.OrderBy(v=>v*outside).ToArray();
            double offset=Enumerable.Range(0,a.Length).Average(i=>(double)a[i]-b[i]);
            if(offset*outside<0||Enumerable.Range(0,a.Length).Any(i=>Math.Abs(a[i]-b[i]-offset)>.25))return false;
            shift=offset*outside;return true;
        }
    }
}
