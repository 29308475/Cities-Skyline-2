using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public static class RoadDefaults
    {
        public static bool Assign(RoadWidthProfile[] profiles,List<RoadOption> options)
        {
            bool changed=false;
            for(int category=0;category<4;category++)
            {
                var current=profiles[category];
                if(current.KeepManual||!string.IsNullOrEmpty(current.PrefabId))continue;
                IEnumerable<RoadOption> choices;
                if(category==3)choices=options.Where(o=>o.IsPath&&(o.AssetName=="Pavement Path"||o.AssetName=="Asphalt Path"||o.Name=="Asphaltweg"||o.Name=="Asphaltierter Weg"));
                else choices=options.Where(o=>!o.IsPath&&o.DirectionKnown&&o.AutomaticEligible&&o.IsHighway==(category==0)&&o.IsOneWay==(category==0)&&o.Lanes==(category==0?3:2));
                var choice=(category==1?choices.OrderBy(o=>Math.Abs(o.Width-16)):choices.OrderBy(o=>o.Width)).ThenBy(o=>o.Id,StringComparer.Ordinal).FirstOrDefault();
                if(choice==null)continue;
                profiles[category]=RoadWidths.Select(choice);profiles[category].Opacity=current.Opacity;changed=true;
            }
            return changed;
        }
    }
}
