using System;
using System.Collections.Generic;

namespace CityStreetOverlay
{
    public static class RoadExclusions
    {
        public static string Key(Point[] road)
        {
            string a=StreetBuildPlan.Key(road[0]),b=StreetBuildPlan.Key(road[1]);
            return string.CompareOrdinal(a,b)<=0?a+"/"+b:b+"/"+a;
        }
        public static StreetMap Apply(StreetMap source,HashSet<string> excluded)
        {
            if(excluded.Count==0)return source;
            var result=new StreetMap{Name=source.Name,Latitude=source.Latitude,Longitude=source.Longitude,SizeMeters=source.SizeMeters,DetailsVersion=source.DetailsVersion};
            for(int i=0;i<source.Roads.Count;i++)
            {
                if(excluded.Contains(Key(source.Roads[i])))continue;
                result.Roads.Add(source.Roads[i]);
                result.Types.Add(source.HasRoadTypes?source.Types[i]:RoadType.LocalRoad);
                if(source.HasDetails)result.Details.Add(source.Details[i]);
            }
            return result;
        }
    }
}
