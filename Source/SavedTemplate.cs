namespace CityStreetOverlay
{
    public sealed class SavedTemplate
    {
        public int Version;
        public System.DateTime SavedUtc;
        public string Preview;
        public StreetMap Map;
        public System.Collections.Generic.List<StreetMap> Tiles;
        public System.Collections.Generic.List<string> ExcludedRoads;
        public System.Collections.Generic.List<RoadSubgroup> Subgroups;
        public float OffsetX, OffsetZ, Rotation, Scale, Opacity, Width;
        // Missing in older JSON: keep all categories visible by default.
        public RoadFilter Filter = RoadFilter.All;
        public RoadWidthProfile[] WidthProfiles;
        public RoadWidthProfile RoundaboutProfile;
        public RoadWidthProfile RoundaboutApproachProfile;
    }
}
