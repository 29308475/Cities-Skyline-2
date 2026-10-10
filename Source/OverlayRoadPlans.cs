using System.Collections.Generic;

namespace CityStreetOverlay
{
    public sealed partial class OverlayWindow
    {
        public struct PreviewStroke { public Point[] Points; public int Category; public RoadWidthProfile Profile; }
        public IEnumerable<PreviewStroke> PreviewStrokes()
        {
            foreach(var batch in DisplayBatches())foreach(var course in batch.Plan.VisibleLines)
            {
                int samples=course.Length==2?1:System.Math.Max(1,(int)System.Math.Ceiling(CurvePlanner.Length(course)/5));
                for(int i=0;i<samples;i++)yield return new PreviewStroke{Category=batch.Category,Profile=batch.Profile,
                    Points=new[]{CurvePlanner.Evaluate(course,(double)i/samples),CurvePlanner.Evaluate(course,(double)(i+1)/samples)}};
            }
        }
        public List<RoadSubgroup> Subgroups=new List<RoadSubgroup>();
        List<RoadBatch> displayBatches;
        StreetMap displayMap;
        float displayScale;
        Dictionary<string,RoadSubgroup> groupLookup;
        HashSet<int> highlighted=new HashSet<int>();
        List<RoadBatch> selectedBatches;
        StreetMap selectedMap;
        float selectedScale;
        public List<RoadBatch> SelectedBatches()
        {
            var map=RenderMap??Map;
            if(selectedBatches==null||!ReferenceEquals(map,selectedMap)||selectedScale!=Scale||!highlighted.SetEquals(SelectedRoads))
            {
                selectedMap=map;selectedScale=Scale;highlighted=new HashSet<int>(SelectedRoads);
                var omitted=new HashSet<string>(ExcludedRoads);
                if(map!=null)for(int i=0;i<map.Roads.Count;i++)if(!highlighted.Contains(i))omitted.Add(RoadExclusions.Key(map.Roads[i]));
                selectedBatches=map==null?new List<RoadBatch>():RoadSubgroups.Plan(RoadExclusions.Apply(map,omitted),Subgroups,WidthProfiles,Scale,null,false,int.MaxValue,RoundaboutProfile,LeftTraffic,RoundaboutApproachProfile,RoadExclusions.Apply(map,ExcludedRoads));
            }
            return selectedBatches;
        }
        public void InvalidateRoadPlans(){displayBatches=null;selectedBatches=null;hoverBatches=null;groupLookup=null;RequestPreview();}
        public RoadSubgroup GroupFor(Point[] road)
        {
            if(groupLookup==null)groupLookup=RoadSubgroups.Lookup(Subgroups);
            groupLookup.TryGetValue(RoadExclusions.Key(road),out var group);return group;
        }
        public List<RoadBatch> DisplayBatches()
        {
            var map=RenderMap??Map;
            if(displayBatches==null||!ReferenceEquals(map,displayMap)||displayScale!=Scale)
            {
                displayMap=map;displayScale=Scale;
                displayBatches=map==null?new List<RoadBatch>():RoadSubgroups.Plan(RoadExclusions.Apply(map,ExcludedRoads),Subgroups,WidthProfiles,Scale,null,false,int.MaxValue,RoundaboutProfile,LeftTraffic,RoundaboutApproachProfile);
            }
            return displayBatches;
        }
    }
}
