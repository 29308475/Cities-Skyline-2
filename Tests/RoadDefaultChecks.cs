using System;
using System.Collections.Generic;
using CityStreetOverlay;

static class RoadDefaultChecks
{
    public static void Run()
    {
        const string highwayName="Dreispurige Einbahnschnellstraße";
        if(!RoadRecognition.StandardRoadLabel(highwayName)||RoadRecognition.LabelLanes(highwayName)!=3||RoadRecognition.StandardRoadLabel(highwayName+" mit Holzdach"))
            throw new Exception("German highway name must be eligible without accepting special variants");
        var options = new List<RoadOption> {
            new RoadOption { Id="highway", Name=highwayName, Width=16, IsHighway=true, IsOneWay=true, DirectionKnown=true, AutomaticEligible=RoadRecognition.StandardRoadLabel(highwayName), Lanes=RoadRecognition.LabelLanes(highwayName) },
            new RoadOption { Id="main", Width=16, DirectionKnown=true, AutomaticEligible=true, Lanes=2 },
            new RoadOption { Id="narrow", Width=8, DirectionKnown=true, AutomaticEligible=true, Lanes=2 },
            new RoadOption { Id="path", Width=3, IsPath=true, AssetName="Pavement Path" },
            new RoadOption { Id="special", Width=4, DirectionKnown=true, Lanes=2 }
        };
        var profiles=RoadWidths.Manual(); profiles[0].Opacity=.4f;
        if(!RoadDefaults.Assign(profiles,options)||profiles[0].PrefabId!="highway"||profiles[1].PrefabId!="main"||profiles[2].PrefabId!="narrow"||profiles[3].PrefabId!="path"||profiles[0].Opacity!=.4f)
            throw new Exception("Category defaults or opacity incorrect");
        profiles[0].PrefabId="user-choice";
        profiles[1]=new RoadWidthProfile {KeepManual=true,Meters=12};
        if(RoadDefaults.Assign(profiles,options)||profiles[0].PrefabId!="user-choice"||profiles[1].PrefabId!=null)
            throw new Exception("User choices overwritten");
        var missing=RoadWidths.Manual();
        if(RoadDefaults.Assign(missing,new List<RoadOption>{options[4]}))throw new Exception("Unsuitable fallback chosen");
        Console.WriteLine("Road default checks passed.");
    }
}
