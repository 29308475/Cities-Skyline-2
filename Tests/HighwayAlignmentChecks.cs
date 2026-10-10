using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;

static class HighwayAlignmentChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var map=new StreetMap{DetailsVersion=1};
        map.Roads.Add(new[]{new Point(0,0),new Point(100,0)});
        map.Roads.Add(new[]{new Point(100,0),new Point(160,0)});
        map.Roads.Add(new[]{new Point(100,0),new Point(140,-35)});
        foreach(int i in Enumerable.Range(0,3)){map.Types.Add(RoadType.Motorway);map.Details.Add(new RoadDetails{Highway=i==2?"motorway_link":"motorway",DirectionKnown=true,OneWay=true});}
        var profiles=RoadWidths.Manual(16);
        var wide=new RoadWidthProfile{PrefabId="wide",Meters=24};
        var groups=new List<RoadSubgroup>{new RoadSubgroup{Id="wide",Category=0,Profile=wide,Roads=new List<string>{RoadExclusions.Key(map.Roads[0])}}};
        foreach(bool left in new[]{false,true})foreach(float scale in new[]{.5f,1f,2f})
        {
            var moved=HighwayAlignment.Prepare(map,groups,profiles,scale,left);
            double z=(left?4:-4)/scale;
            Check(Math.Abs(moved.Roads[0][0].Z-z)<.001,"Wider carriageway offsets half width difference in world meters");
            Check(Math.Abs((moved.Roads[0][0].Z*scale+(left?-12:12))-(left?-8:8))<.001,"Median-side edge stays aligned");
            Check(moved.Roads[0][1].Z==moved.Roads[1][0].Z&&moved.Roads[1][0].Z==moved.Roads[2][0].Z,"Ramp and width transition share moved junction");
            Check(moved.Roads[2][1].Z==-35,"Far end of ramp stays fixed");
            var subset=new StreetMap{Roads=new List<Point[]>{map.Roads[2]},Types=new List<RoadType>{RoadType.Motorway},Details=new List<RoadDetails>{map.Details[2]},DetailsVersion=1};
            Check(HighwayAlignment.Prepare(subset,groups,profiles,scale,left,map).Roads[0][0].Z==z,"Partial highlight uses full alignment context");
            var plans=RoadSubgroups.Plan(map,groups,profiles,scale,0,false,2000,null,left);
            Check(plans.Any(b=>b.Profile==wide),"Source keys retain subgroup assignment after movement");
        }
        Check(map.Roads[0][0].Z==0&&map.Roads[2][0].Z==0,"Source and saved selection keys remain unchanged");
        map.Roads[0]=new[]{map.Roads[0][1],map.Roads[0][0]};
        Check(HighwayAlignment.Prepare(map,groups,profiles,1,false).Roads[0][0].Z==4,"Opposite carriageway widens towards opposite shoulder");
        map.Details[0].OneWay=false;
        Check(HighwayAlignment.Prepare(map,groups,profiles,1,false).Roads[0][0].Z==0,"Two-way road is not shifted");
        map.Details[0].OneWay=true;
        map.Roads.Add(new[]{new Point(0,20),new Point(100,20)});map.Types.Add(RoadType.Motorway);
        map.Details.Add(new RoadDetails{Highway="motorway",OneWay=true,DirectionKnown=true});
        foreach(bool left in new[]{false,true})
            Check(HighwayAlignment.Prepare(map,groups,profiles,1,left).Roads[0][0].Z==-4,"Neighbor carriageway overrides misleading source direction and traffic-side fallback");
        map.Roads[2]=new[]{new Point(100,0),new Point(110,-5)};
        map.Roads.Add(new[]{new Point(110,-5),new Point(160,-40)});map.Types.Add(RoadType.Motorway);
        map.Details.Add(new RoadDetails{Highway="motorway_link",OneWay=true,DirectionKnown=true});
        var rampMoved=HighwayAlignment.Prepare(map,groups,profiles,1,false);
        Check(rampMoved.Roads[2][1].Z< -5&&rampMoved.Roads[2][1].Z> -9,"Ramp displacement tapers through intermediate samples");
        Check(rampMoved.Roads[4][1].Z==-40,"Far ramp connection remains fixed");
        Console.WriteLine("PASS: wider motorway median alignment, neighbor carriageway, ramp taper, traffic side, scale and immutable source keys");
    }
}
