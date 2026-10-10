using System;
using System.Linq;
using System.Collections.Generic;
using CityStreetOverlay;
using Newtonsoft.Json;

static class LaneAlignmentChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var baseline=new RoadWidthProfile{Meters=16,DrivingLaneOffsets=new[]{-4f,0f,4f}};
        var wider=new RoadWidthProfile{Meters=24,DrivingLaneOffsets=new[]{-6f,-2f,2f,6f}};
        foreach(double side in new[]{-1.0,1.0})
        {
            Check(LaneAlignment.TryShift(baseline,wider,side,out var shift)&&shift==2,"Align actual lanes, not half the eight-meter surface-width difference");
            var a=baseline.DrivingLaneOffsets.OrderBy(v=>v*side).ToArray();var b=wider.DrivingLaneOffsets.OrderBy(v=>v*side).ToArray();
            Check(Enumerable.Range(0,3).All(i=>Math.Abs(a[i]-(b[i]+side*shift))<.001),"All three through lanes align; added fourth lane is outside");
        }
        wider.Meters=16;
        Check(LaneAlignment.TryShift(baseline,wider,1,out var sameWidth)&&sameWidth==2,"Equal total width still requires lane alignment");
        wider.DrivingLaneOffsets=new[]{-7f,-2f,2f,6f};
        Check(!LaneAlignment.TryShift(baseline,wider,1,out _),"Unequal spacing cannot be advertised as exact lane match");
        var option=new RoadOption{Id="highway",Width=16,DrivingLaneOffsets=new[]{-6f,-2f,2f,6f}};
        var profile=RoadWidths.Select(option);option.DrivingLaneOffsets[0]=-8;
        Check(profile.DrivingLaneOffsets[0]==-6,"Selected layout is an independent snapshot");
        Check(RoadWidths.Refresh(new[]{profile},new List<RoadOption>{option}),"Changed lane layout invalidates cached plan even at unchanged width");
        Check(JsonConvert.DeserializeObject<RoadWidthProfile>(JsonConvert.SerializeObject(profile)).DrivingLaneOffsets.SequenceEqual(profile.DrivingLaneOffsets),"Offline template retains lane geometry");
        Console.WriteLine("PASS: physical through-lane alignment, equal-width prefabs, incompatible lane spacing and refresh/persistence");
    }
}
