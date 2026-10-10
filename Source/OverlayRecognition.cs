using System;
using System.Collections.Generic;
using System.Linq;

namespace CityStreetOverlay
{
    public sealed partial class OverlayWindow
    {
        List<RoadSubgroup> roadSuggestions=new List<RoadSubgroup>();
        StreetMap recognitionMap;
        void AnalyzeRoads()
        {
            recognitionMap=RenderMap??Map;
            roadSuggestions=recognitionMap==null?new List<RoadSubgroup>():RoadRecognition.Suggest(recognitionMap,roadOptions,WidthProfiles,Subgroups,ExcludedRoads);
            StampStatus=roadSuggestions.Count==0?"Keine neuen Vorschläge. Benötigt Straßendetails aus einer neu geladenen Vorschau und verfügbare Spielstraßen.":roadSuggestions.Count+" Vorschläge bereit. Vor dem Übernehmen prüfen.";
        }
        void ApplyRoadRecognition()
        {
            if(!ReferenceEquals(recognitionMap,RenderMap??Map))throw new InvalidOperationException("Vorlage geändert. Erkennung erneut starten.");
            var used=RoadSubgroups.Lookup(Subgroups);
            if(roadSuggestions.Any(g=>g.Roads.Any(k=>used.ContainsKey(k)||ExcludedRoads.Contains(k))))throw new InvalidOperationException("Zuordnung geändert. Erkennung erneut starten.");
            if(Subgroups.Count+roadSuggestions.Count>64)throw new InvalidOperationException("Mehr als 64 Untergruppen. Kleineren Ausschnitt verwenden.");
            Subgroups.AddRange(roadSuggestions);roadSuggestions=new List<RoadSubgroup>();InvalidateRoadPlans();
            StampStatus="Vorschläge übernommen. Untergruppen vor dem Bauen prüfen und bei Bedarf ändern.";
        }
    }
}
