using System;
using System.Collections.Generic;
using System.Linq;
using CityStreetOverlay;
using Newtonsoft.Json;
static class MosaicChecks
{
    static void Assert(bool ok) { if(!ok)throw new Exception("Mosaic regression"); }
    static StreetMap Tile(double latitude,double longitude) => new StreetMap { Name="Fixture",Latitude=latitude,Longitude=longitude,SizeMeters=1000,
        Roads=new List<Point[]> {new[]{new Point(-500,0),new Point(500,0)}},Types=new List<RoadType>{RoadType.MainRoad} };
    public static void Run()
    {
        var first=Tile(48,11);MapMosaic.Validate(first);
        var center=MapMosaic.Pan(first,500,0);var next=Tile(center.Latitude,center.Longitude);
        var composed=MapMosaic.Compose(new[]{first,next});
        Assert(Math.Abs(composed.Roads.Sum(r=>Math.Abs(r[1].X-r[0].X))-1500)<0.001);
        Assert(composed.Types.All(t=>t==RoadType.MainRoad));
        Assert(first.Roads.Count==1&&next.Roads.Count==1);
        Assert(MapMosaic.Compose(new[]{first,first}).Roads.Count==1);
        var north=MapMosaic.Pan(first,0,1000);var up=Tile(north.Latitude,north.Longitude);
        var p=MapMosaic.ToAnchor(new Point(0,-500),up,first);
        Assert(Math.Abs(p.X)<0.001&&Math.Abs(p.Z-500)<0.001);
        var edge=Geography.Project(first.Latitude,first.Longitude+0.001,up.Latitude,up.Longitude);
        var converted=MapMosaic.ToAnchor(edge,up,first);
        Assert(Math.Abs(converted.X-Geography.Project(first.Latitude,first.Longitude+0.001,first.Latitude,first.Longitude).X)<0.001);
        var saved=new SavedTemplate {Map=next,Tiles=new List<StreetMap>{first,next},Rotation=37.4f,Scale=1.3f};
        var loaded=JsonConvert.DeserializeObject<SavedTemplate>(JsonConvert.SerializeObject(saved));
        Assert(loaded.Tiles.Count==2&&loaded.Rotation==37.4f&&MapMosaic.Compose(loaded.Tiles).Roads.Count==composed.Roads.Count);
        Console.WriteLine("PASS: mosaic seams, overlap removal, projection, source preservation and offline collection roundtrip");
    }
}
