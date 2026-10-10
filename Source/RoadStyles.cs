using UnityEngine;

namespace CityStreetOverlay
{
    internal static class RoadStyles
    {
        // Shared by the terrain overlay, preview and legend.
        public static Color32 Color(RoadType type)
        {
            switch (type)
            {
                case RoadType.Motorway: return new Color32(255, 140, 55, 255);
                case RoadType.MainRoad: return new Color32(255, 220, 65, 255);
                case RoadType.LocalRoad: return new Color32(70, 235, 255, 255);
                default: return new Color32(110, 235, 115, 255);
            }
        }
    }
}
