using System;
using System.Collections.Generic;

namespace CityStreetOverlay
{
    // All tiles share the first tile's meter projection and one placement transform.
    public static class MapMosaic
    {
        public const int MaxTiles = 24;
        public static void Validate(StreetMap map)
        {
            if(map?.Roads==null||map.Roads.Count>80000||double.IsNaN(map.SizeMeters)||map.SizeMeters<500||map.SizeMeters>10000)throw new FormatException("Ungültiger Ausschnitt.");
            Geography.Bounds(map.Latitude,map.Longitude,map.SizeMeters);
            map.ValidateRoadTypes();
            foreach(var road in map.Roads)
            {
                if(road==null||road.Length!=2)throw new FormatException("Ungültiger Straßenabschnitt.");
                foreach(var p in road)if(double.IsNaN(p.X+p.Z)||double.IsInfinity(p.X+p.Z)||Math.Abs(p.X)>5001||Math.Abs(p.Z)>5001)throw new FormatException("Ungültige Koordinaten.");
            }
        }
        public static Point ToAnchor(Point p, StreetMap tile, StreetMap anchor)
        {
            var center=Geography.Project(tile.Latitude,tile.Longitude,anchor.Latitude,anchor.Longitude);
            return new Point(center.X+p.X*Math.Cos(anchor.Latitude*Math.PI/180)/Math.Cos(tile.Latitude*Math.PI/180),center.Z+p.Z);
        }
        public static Place Pan(StreetMap tile, double east, double north)
        {
            return new Place { Name=tile.Name, Latitude=tile.Latitude+north/111319.49079327358,
                Longitude=tile.Longitude+east/(111319.49079327358*Math.Cos(tile.Latitude*Math.PI/180)) };
        }
        // Remove segments covered by earlier tiles to avoid brighter overlap bands.
        public static StreetMap Compose(IList<StreetMap> tiles)
        {
            if(tiles==null||tiles.Count==0||tiles.Count>MaxTiles)throw new ArgumentException("Ungültige Ausschnittsammlung.");
            var anchor=tiles[0];
            int total=0;
            foreach(var tile in tiles) { total+=tile.Roads.Count; if(total>320000)throw new InvalidOperationException("Sammlung erreicht die Grenze von 320.000 Straßenabschnitten. Bisherige Ausschnitte bleiben erhalten."); }
            var result=new StreetMap { Name=anchor.Name,Latitude=anchor.Latitude,Longitude=anchor.Longitude,SizeMeters=anchor.SizeMeters };
            var boxes=new List<double[]>();
            foreach(var tile in tiles)
            {
                result.RemovedDriveways += tile.RemovedDriveways;
                for(int roadIndex=0;roadIndex<tile.Roads.Count;roadIndex++)
                {
                    var road=tile.Roads[roadIndex];
                    var parts=new List<Point[]> { new[]{ToAnchor(road[0],tile,anchor),ToAnchor(road[1],tile,anchor)} };
                    foreach(var box in boxes)
                    {
                        var next=new List<Point[]>();
                        foreach(var part in parts) Subtract(part,box,next);
                        parts=next;
                    }
                    result.Roads.AddRange(parts);
                    while(result.Types.Count<result.Roads.Count)result.Types.Add(tile.HasRoadTypes?tile.Types[roadIndex]:RoadType.LocalRoad);
                    while(result.Details.Count<result.Roads.Count)result.Details.Add(tile.HasDetails?tile.Details[roadIndex]:new RoadDetails());
                }
                var lo=ToAnchor(new Point(-tile.SizeMeters/2,-tile.SizeMeters/2),tile,anchor);
                var hi=ToAnchor(new Point(tile.SizeMeters/2,tile.SizeMeters/2),tile,anchor);
                boxes.Add(new[]{lo.X,lo.Z,hi.X,hi.Z});
            }
            result.DetailsVersion = 1;
            foreach(var tile in tiles)if(!tile.HasDetails)result.DetailsVersion=0;
            return result;
        }
        static void Subtract(Point[] line,double[] box,List<Point[]> output)
        {
            var a=line[0];var b=line[1];double dx=b.X-a.X,dz=b.Z-a.Z,lo=0,hi=1;
            var p=new[]{-dx,dx,-dz,dz};var q=new[]{a.X-box[0],box[2]-a.X,a.Z-box[1],box[3]-a.Z};
            for(int i=0;i<4;i++)
            {
                if(Math.Abs(p[i])<1e-12){if(q[i]<0){output.Add(line);return;}}
                else {double r=q[i]/p[i];if(p[i]<0)lo=Math.Max(lo,r);else hi=Math.Min(hi,r);if(lo>hi){output.Add(line);return;}}
            }
            if(lo>1e-8)output.Add(new[]{a,new Point(a.X+dx*lo,a.Z+dz*lo)});
            if(hi<1-1e-8)output.Add(new[]{new Point(a.X+dx*hi,a.Z+dz*hi),b});
        }
    }
}
