using System.Linq;
using Unity.Entities;

namespace CityStreetOverlay
{
    public sealed partial class OverlayWindow
    {
        public RoadWidthProfile RoundaboutProfile;
        public RoadWidthProfile RoundaboutApproachProfile;
        public bool LeftTraffic=>World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>().leftHandTraffic??false;
        void RefreshRoundaboutDefault()
        {
            if(RoundaboutApproachProfile==null)
            {
                var approach=roadOptions.FirstOrDefault(o=>o.DefaultRoundabout);
                if(approach!=null){RoundaboutApproachProfile=RoadWidths.Select(approach);InvalidateRoadPlans();}
            }
            else if(RoadWidths.Refresh(new[]{RoundaboutApproachProfile},roadOptions))InvalidateRoadPlans();
            if(RoundaboutProfile==null)
            {
                var option=roadOptions.FirstOrDefault(o=>o.DefaultRoundabout);
                if(option!=null){RoundaboutProfile=RoadWidths.Select(option);InvalidateRoadPlans();}
            }
            else if(RoadWidths.Refresh(new[]{RoundaboutProfile},roadOptions))InvalidateRoadPlans();
        }
    }
}
