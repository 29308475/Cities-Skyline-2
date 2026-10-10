using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public sealed class RoadWidthProfile
    {
        public string PrefabId;
        public string Name;
        public float Meters = 3;
        public float Opacity = 1;
        public bool KeepManual;
        public float[] DrivingLaneOffsets;
        public bool IndependentCurveEnds;
    }

    public sealed class RoadOption
    {
        public string Id, Name, AssetName;
        public float Width;
        public bool IsPath, IsHighway, DefaultRoundabout;
        public bool IsOneWay, DirectionKnown;
        public int Lanes;
        public bool AutomaticEligible;
        public float[] DrivingLaneOffsets;
        public bool IndependentCurveEnds;
    }

    public static class RoadWidths
    {
        public static bool Valid(float meters) => !float.IsNaN(meters) && !float.IsInfinity(meters) && meters > 0 && meters <= 512;

        public static RoadWidthProfile[] Manual(float meters = 3)
        {
            if (!Valid(meters)) throw new FormatException("Ungültige Straßenbreite.");
            return new[] { new RoadWidthProfile { Meters = meters }, new RoadWidthProfile { Meters = meters },
                new RoadWidthProfile { Meters = meters }, new RoadWidthProfile { Meters = meters } };
        }

        public static void Validate(RoadWidthProfile[] profiles)
        {
            if (profiles == null || profiles.Length != 4) throw new FormatException("Vier Straßenbreiten werden benötigt.");
            foreach (var profile in profiles)
                if (profile == null || !Valid(profile.Meters) || float.IsNaN(profile.Opacity) || float.IsInfinity(profile.Opacity) || profile.Opacity < 0 || profile.Opacity > 1) throw new FormatException("Ungültige Straßenbreite oder Deckkraft.");
        }

        public static RoadWidthProfile Select(RoadOption option)
        {
            if (option == null || string.IsNullOrEmpty(option.Id) || !Valid(option.Width)) throw new ArgumentException("Ungültige Spielstraße.");
            return new RoadWidthProfile { PrefabId = option.Id, Name = option.Name, Meters = option.Width, IndependentCurveEnds=option.IndependentCurveEnds, DrivingLaneOffsets=option.DrivingLaneOffsets?.ToArray() };
        }

        // A removed mod must not silently map to a different street; keep the saved width.
        public static bool Refresh(RoadWidthProfile[] profiles, IReadOnlyList<RoadOption> options)
        {
            bool changed = false;
            foreach (var profile in profiles)
            {
                if (string.IsNullOrEmpty(profile.PrefabId)) continue;
                foreach (var option in options)
                {
                    if (option.Id != profile.PrefabId || !Valid(option.Width)) continue;
                    changed |= profile.Meters != option.Width;
                    changed |= !(profile.DrivingLaneOffsets??new float[0]).SequenceEqual(option.DrivingLaneOffsets??new float[0]);
                    changed |= profile.IndependentCurveEnds != option.IndependentCurveEnds;
                    profile.IndependentCurveEnds=option.IndependentCurveEnds;
                    profile.DrivingLaneOffsets=option.DrivingLaneOffsets?.ToArray();
                    profile.Meters = option.Width;
                    profile.Name = option.Name;
                    break;
                }
            }
            return changed;
        }

        // Scale changes centerline spacing. The chosen game street stays the same width.
        public static double PreviewPixels(float widthMeters, double mapMeters, float scale, int pixels)
            => widthMeters / (mapMeters * scale) * (pixels - 1);
    }
}
