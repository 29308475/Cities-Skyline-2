using System;
using System.Collections.Generic;
using CityStreetOverlay;

static class IsolatedRoadChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Add(StreetMap map,double x0,double z0,double x1,double z1,RoadType type=RoadType.LocalRoad)
    {map.Roads.Add(new[]{new Point(x0,z0),new Point(x1,z1)});map.Types.Add(type);}
    public static void Run()
    {
        var map=new StreetMap();Add(map,0,0,6,0);Add(map,6,0,12,0);
        Check(IsolatedRoads.ShortComponentNodes(map,1).Count==3,"Multi-segment isolated fragment is filtered as a component");
        Check(IsolatedRoads.ShortComponentNodes(map,2).Count==0,"Threshold is measured in world meters");
        Add(map,12,0,80,0,RoadType.MainRoad);
        Check(IsolatedRoads.ShortComponentNodes(map,1).Count==0,"Short attached cross-category driveway is retained");
        var cross=new StreetMap();Add(cross,-40,0,40,0,RoadType.MainRoad);Add(cross,0,0,0,8);
        HashSet<int> selection=null;cross=RoadIntersections.Split(cross,ref selection);
        Check(IsolatedRoads.ShortComponentNodes(cross,1).Count==0,"Interior T-connection retained after splitting");
        var ring=new StreetMap();Add(ring,0,0,3,0);Add(ring,3,0,0,3);Add(ring,0,3,0,0);
        Check(IsolatedRoads.ShortComponentNodes(ring,1).Count==0,"Closed tiny ring is not mistaken for disconnected fragment");
        var exact=new StreetMap();Add(exact,0,0,16,0);
        Check(IsolatedRoads.ShortComponentNodes(exact,1).Count==0,"Sixteen-meter component retained");
        Console.WriteLine("PASS: isolated component filtering, cross-category/T connections, scale, rings and length boundary");
    }
}
