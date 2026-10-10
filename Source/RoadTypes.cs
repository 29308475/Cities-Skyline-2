using System;
using System.Collections.Generic;

namespace CityStreetOverlay
{
    public enum RoadType { Motorway, MainRoad, LocalRoad, FootCyclePath }

    [Flags]
    public enum RoadFilter
    {
        None = 0, Motorway = 1, MainRoad = 2, LocalRoad = 4, FootCyclePath = 8,
        All = Motorway | MainRoad | LocalRoad | FootCyclePath
    }

    public static class RoadTypes
    {
        static readonly Dictionary<string, RoadType> Types = new Dictionary<string, RoadType>(StringComparer.Ordinal)
        {
            { "motorway", RoadType.Motorway }, { "motorway_link", RoadType.Motorway },
            { "trunk", RoadType.MainRoad }, { "trunk_link", RoadType.MainRoad },
            { "primary", RoadType.MainRoad }, { "primary_link", RoadType.MainRoad },
            { "secondary", RoadType.MainRoad }, { "secondary_link", RoadType.MainRoad },
            { "tertiary", RoadType.MainRoad }, { "tertiary_link", RoadType.MainRoad },
            { "unclassified", RoadType.LocalRoad }, { "residential", RoadType.LocalRoad },
            { "living_street", RoadType.LocalRoad }, { "service", RoadType.LocalRoad },
            { "pedestrian", RoadType.FootCyclePath }, { "footway", RoadType.FootCyclePath },
            { "cycleway", RoadType.FootCyclePath }, { "path", RoadType.FootCyclePath },
            { "steps", RoadType.FootCyclePath }
        };

        public static string HighwayPattern => "^(" + string.Join("|", Types.Keys) + ")$";
        public static bool TryClassify(string highway, out RoadType type)
        {
            type = default;
            return highway != null && Types.TryGetValue(highway, out type);
        }

        public static RoadFilter Flag(RoadType type) => (RoadFilter)(1 << (int)type);
        public static bool IsVisible(RoadFilter filter, RoadType type) => (filter & Flag(type)) != 0;

        public static string Label(RoadType type)
        {
            switch (type)
            {
                case RoadType.Motorway: return "Autobahnen · Orange";
                case RoadType.MainRoad: return "Hauptstraßen · Gelb";
                case RoadType.LocalRoad: return "Wohn-/Nebenstraßen · Türkis";
                default: return "Fuß-/Radwege · Grün";
            }
        }
    }
}
