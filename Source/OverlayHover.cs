using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public sealed partial class OverlayWindow
    {
        public string FocusedGroup;
        public int FocusedCategory=-1,FocusRevision;
        string hoveredRoadGroup;
        List<RoadBatch> hoverBatches;
        StreetMap hoverMap;
        float hoverScale;
        void HoverRoadGroup(string id)
        {
            if(hoveredRoadGroup==id)return;
            hoveredRoadGroup=id;hoverBatches=null;
        }
        public List<RoadBatch> HoverBatches()
        {
            if(!IsOpen||(string.IsNullOrEmpty(hoveredRoadGroup)&&FocusedCategory<0))return new List<RoadBatch>();
            var map=RenderMap??Map;
            if(hoverBatches==null||!ReferenceEquals(map,hoverMap)||hoverScale!=Scale)
            {
                hoverMap=map;hoverScale=Scale;hoverBatches=new List<RoadBatch>();
                var group=Subgroups.Concat(roadSuggestions).FirstOrDefault(g=>g.Id==(hoveredRoadGroup??FocusedGroup));
                if(map==null)return hoverBatches;
                if(group==null&&FocusedCategory>=0&&string.IsNullOrEmpty(hoveredRoadGroup))group=new RoadSubgroup{Id="focus",Category=FocusedCategory,Profile=WidthProfiles[FocusedCategory],Roads=map.Roads.Where((r,i)=>GroupFor(r)==null&&(map.HasRoadTypes?(int)map.Types[i]:2)==FocusedCategory).Select(RoadExclusions.Key).ToList()};
                if(group==null)return hoverBatches;
                var keys=new HashSet<string>(group.Roads);var omitted=new HashSet<string>(ExcludedRoads);
                foreach(var r in map.Roads)if(!keys.Contains(RoadExclusions.Key(r)))omitted.Add(RoadExclusions.Key(r));
                var alignmentGroups=Subgroups.Where(g=>g.Id!=group.Id).Concat(new[]{group}).ToList();
                hoverBatches=RoadSubgroups.Plan(RoadExclusions.Apply(map,omitted),alignmentGroups,WidthProfiles,Scale,null,false,int.MaxValue,RoundaboutProfile,LeftTraffic,RoundaboutApproachProfile,RoadExclusions.Apply(map,ExcludedRoads));
            }
            return hoverBatches;
        }
    }
}
