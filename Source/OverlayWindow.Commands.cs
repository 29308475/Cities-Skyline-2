using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CityStreetOverlay
{
    public sealed partial class OverlayWindow
    {
        public string PreviewDataUrl { get; private set; } = "";
        public string CatalogJson { get; private set; } = "[]";
        bool previewDirty;
        float previewDue;

        public string GetStateJson()
        {
            CheckSelection();
            return JsonConvert.SerializeObject(new
            {
                focusGroup=FocusedGroup,focusCategory=FocusedCategory,focusRevision=FocusRevision,
                selecting=Selecting, selectionOnly=SelectionOnly, selectionCount=SelectedRoads.Count,
                excludedCount=ExcludedRoads.Count,
                roadSuggestions=roadSuggestions.Select(g=>new {id=g.Id,name=RoadRecognition.DisplayName(g.Name),road=g.Profile.Name,width=g.Profile.Meters,count=g.Roads.Count}).ToArray(),
                roundabout=RoundaboutProfile==null?null:new {name=RoundaboutProfile.Name,width=RoundaboutProfile.Meters,id=RoundaboutProfile.PrefabId},
                roundaboutApproach=RoundaboutApproachProfile==null?null:new {name=RoundaboutApproachProfile.Name,id=RoundaboutApproachProfile.PrefabId},
                leftTraffic=LeftTraffic,
                subgroups=Subgroups.Select(g=>new {id=g.Id,name=RoadRecognition.DisplayName(g.Name),category=g.Category,count=g.Roads.Count,roadName=g.Profile.Name,prefabId=g.Profile.PrefabId,width=g.Profile.Meters,available=roadOptions.Exists(o=>o.Id==g.Profile.PrefabId)}).ToArray(),
                categoryReady=Enumerable.Range(0,4).Select(c=>DisplayBatches().Any(b=>b.Category==c)&&DisplayBatches().Where(b=>b.Category==c).All(b=>roadOptions.Exists(o=>o.Id==b.Profile.PrefabId))).ToArray(),
                selectionCounts=Enumerable.Range(0,4).Select(c=>SelectedRoads.Count(i=>((RenderMap??Map).HasRoadTypes?(int)(RenderMap??Map).Types[i]:2)==c)).ToArray(),
                detailsReady=(RenderMap??Map)?.HasDetails==true, removedDriveways=(RenderMap??Map)?.RemovedDriveways??0,
                busy, status, query, sizeKm, eastKm, northKm, tileCount = Tiles.Count, canPan = service != null,
                stampBusy = StreetStampTool.Instance != null && StreetStampTool.Instance.Building, stampStatus = StampStatus,
                selected = selected?.Name,
                places = places.Select(p => p.Name).ToArray(),
                canSearch = service != null,
                canRestore = library != null, libraryOpen = LibraryOpen, savedCount,
                map = Map == null ? null : new { name = Map.Name, sizeMeters = Map.SizeMeters, typed = Map.HasRoadTypes },
                visible = Visible, locked = Locked, filter = (int)Filter,
                offsetX = OffsetX, offsetZ = OffsetZ, rotation = Rotation, scale = Scale, opacity = Opacity,
                catalogStatus = CatalogStatus,
                profiles = WidthProfiles.Select(p => new {
                    name = p.Name, width = p.Meters, opacity = p.Opacity, assigned = !string.IsNullOrEmpty(p.PrefabId),
                    available = roadOptions.Exists(o => o.Id == p.PrefabId)
                }).ToArray()
            });
        }

        public void SetRoadCatalog(List<RoadOption> options)
        {
            roadOptions = options;
            if(RoadDefaults.Assign(WidthProfiles,options))InvalidateRoadPlans();
            RefreshRoundaboutDefault();
            CatalogJson = JsonConvert.SerializeObject(options.Select(o => new {
                id = o.Id, name = o.Name, width = o.Width, isPath = o.IsPath, isHighway = o.IsHighway
            }).ToArray());
            CatalogStatus = options.Count == 0 ? "Noch keine verfügbaren Spielstraßen gefunden." : options.Count + " Spielstraßen und Wege verfügbar.";
            if (RoadWidths.Refresh(WidthProfiles, options)) InvalidateRoadPlans();
            foreach(var group in Subgroups)if(RoadWidths.Refresh(new[]{group.Profile},options))InvalidateRoadPlans();
        }

        void RequestPreview()
        {
            // Coalesce slider events; the terrain overlay itself updates immediately.
            if (!previewDirty) previewDue = Time.realtimeSinceStartup + 0.15f;
            previewDirty = true;
        }

        static float Number(JObject command, string field, float min, float max)
        {
            var value = command[field];
            if (value == null || (value.Type != JTokenType.Float && value.Type != JTokenType.Integer))
                throw new ArgumentException("Ungültiger Zahlenwert.");
            float result = (float)value;
            if (float.IsNaN(result) || float.IsInfinity(result)) throw new ArgumentException("Ungültiger Zahlenwert.");
            return Mathf.Clamp(result, min, max);
        }

        static int Category(JObject command)
        {
            if (command["type"]?.Type != JTokenType.Integer) throw new ArgumentException("Ungültiger Straßentyp.");
            int type = (int)command["type"];
            if (type < 0 || type > 3) throw new ArgumentException("Ungültiger Straßentyp.");
            return type;
        }

        public void HandleCommand(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json) || json.Length > 4096) throw new ArgumentException("Ungültige Eingabe.");
                var command = JObject.Parse(json);
                switch ((string)command["action"])
                {
                    case "hoverRoadGroup": HoverRoadGroup((string)command["group"]);break;
                    case "recognizeRoads": if(!busy&&StreetStampTool.Instance?.Building!=true)AnalyzeRoads();break;
                    case "applyRecognition": if(!busy&&StreetStampTool.Instance?.Building!=true)ApplyRoadRecognition();break;
                    case "roundaboutApproachRoad":
                        if(busy||StreetStampTool.Instance?.Building==true)break;
                        RoundaboutApproachProfile=RoadWidths.Select(roadOptions.Find(o=>o.Id==(string)command["id"]&&!o.IsPath));InvalidateRoadPlans();break;
                    case "roundaboutRoad":
                        if(busy||StreetStampTool.Instance?.Building==true)break;
                        RoundaboutProfile=RoadWidths.Select(roadOptions.Find(o=>o.Id==(string)command["id"]&&!o.IsPath));InvalidateRoadPlans();break;
                    case "createSubgroup":
                        if(busy||Map==null||SelectedRoads.Count==0||StreetStampTool.Instance?.Building==true)return;
                        if(Subgroups.Count>=64)throw new InvalidOperationException("Maximal 64 Untergruppen.");
                        var groupOption=roadOptions.Find(o=>o.Id==(string)command["id"]);
                        if(groupOption==null)throw new InvalidOperationException("Bitte eine verfügbare Spielstraße wählen.");
                        var name=((string)command["name"]??"Unterstraße").Trim();
                        if(name.Length==0)name="Unterstraße";if(name.Length>60)name=name.Substring(0,60);
                        var keys=new HashSet<string>(SelectedRoads.Select(i=>RoadExclusions.Key((RenderMap??Map).Roads[i])));
                        foreach(var g in Subgroups)g.Roads.RemoveAll(k=>keys.Contains(k));
                        Subgroups.RemoveAll(g=>g.Roads.Count==0);
                        Subgroups.Add(new RoadSubgroup{Id=Guid.NewGuid().ToString("N"),Name=name,Category=Category(command),Profile=RoadWidths.Select(groupOption),Roads=keys.ToList()});
                        SelectedRoads.Clear();SelectionOnly=false;InvalidateRoadPlans();StampStatus="Untergruppe angelegt: "+name;break;
                    case "subgroupRoad":
                        if(busy||StreetStampTool.Instance?.Building==true)return;
                        var editGroup=Subgroups.Find(g=>g.Id==(string)command["group"]);
                        var editOption=roadOptions.Find(o=>o.Id==(string)command["id"]);
                        if(editGroup!=null&&editOption!=null){editGroup.Profile=RoadWidths.Select(editOption);InvalidateRoadPlans();}break;
                    case "deleteSubgroup":
                        if(busy||StreetStampTool.Instance?.Building==true)return;
                        Subgroups.RemoveAll(g=>g.Id==(string)command["group"]);InvalidateRoadPlans();break;
                    case "selectionMode":
                        CheckSelection();
                        if(Map!=null && !busy && StreetStampTool.Instance?.Building!=true){Selecting=!Selecting;Visible=true;}
                        break;
                    case "clearSelection": SelectedRoads.Clear();SelectionOnly=false; break;
                    case "removeMarked": if(!busy)RemoveMarkedRoads(); break;
                    case "restoreExcluded":
                        if(busy||StreetStampTool.Instance?.Building==true)return;
                        ExcludedRoads.Clear();SelectedRoads.Clear();SelectionOnly=false;InvalidateRoadPlans();break;
                    case "selectScreen": SelectScreen(Number(command,"x0",0,1),Number(command,"y0",0,1),Number(command,"x1",0,1),Number(command,"y1",0,1),(bool?)command["remove"]==true); break;
                    case "stampRoad":
                        if (busy || Map == null) return;
                        try {
                            if (StreetStampTool.Instance == null) throw new InvalidOperationException("Bauwerkzeug noch nicht bereit.");
                            StreetStampTool.Instance.Begin(this,Category(command));
                        } catch(Exception error) {
                            Colossal.Logging.LogManager.GetLogger("CityStreetOverlay").Error("Stamp start failed: "+error.ToString());
                            StampStatus="Nicht gebaut: "+error.Message;
                        }
                        break;
                    case "cancelStamp": StreetStampTool.Instance?.Cancel(); break;
                    case "close": CloseWindow(); break;
                    case "search":
                        if (busy) return;
                        query = ((string)command["query"] ?? "").Trim();
                        if (query.Length > 120) query = query.Substring(0,120);
                        StartRequest(() => service.Search(query, cancellation.Token), results => {
                            places = results;
                            status = results.Count == 0 ? "Keine Stadt gefunden." : "Bitte einen Treffer auswählen.";
                        });
                        break;
                    case "selectPlace":
                        if (busy) return;
                        int index = (int?)command["index"] ?? -1;
                        if (index < 0 || index >= places.Count) return;
                        selected = places[index]; eastKm = northKm = 0;
                        break;
                    case "area":
                        if (busy) return;
                        switch ((string)command["field"])
                        {
                            case "sizeKm": sizeKm = Number(command,"value",0.5f,10); break;
                            case "eastKm": eastKm = Number(command,"value",-10,10); break;
                            case "northKm": northKm = Number(command,"value",-10,10); break;
                        }
                        break;
                    case "loadPreview":
                        if (selected == null || busy) return;
                        var center = new Place { Name = selected.Name,
                            Latitude = selected.Latitude + northKm*1000/111319.4908,
                            Longitude = selected.Longitude + eastKm*1000/(111319.4908*Math.Cos(selected.Latitude*Math.PI/180)) };
                        double size = sizeKm * 1000;
                        StartRequest(() => service.Load(center, size, cancellation.Token), map => {
                            ExcludedRoads.Clear();
                            Subgroups.Clear();InvalidateRoadPlans();
                            Map = map; Tiles = new List<StreetMap> { map }; RenderMap = map; Visible = false; Locked = false;
                            OffsetX = OffsetZ = Rotation = 0; Scale = 1;
                            BuildPreview();
                            status = "Vorschau bereit. Mit „Auf Karte anzeigen“ platzieren.";
                        });
                        break;
                    case "visible": if (Map != null) Visible = (bool?)command["value"] ?? false; break;
                    case "locked": if (Map != null) Locked = (bool?)command["value"] ?? false; break;
                    case "filter":
                        if (Map == null || !Map.HasRoadTypes) return;
                        var flag = RoadTypes.Flag((RoadType)Category(command));
                        if ((bool?)command["value"] == true) Filter |= flag; else Filter &= ~flag;
                        RequestPreview();
                        break;
                    case "selectRoad":
                        if (Map == null) return;
                        int category = Category(command);
                        var option = roadOptions.Find(o => o.Id == (string)command["id"]);
                        if (option == null) return;
                        var oldOpacity = WidthProfiles[category].Opacity;
                        WidthProfiles[category] = RoadWidths.Select(option);
                        WidthProfiles[category].Opacity = oldOpacity;
                        InvalidateRoadPlans();
                        break;
                    case "categoryOpacity":
                        if (Map == null) return;
                        WidthProfiles[Category(command)].Opacity = Number(command,"value",0,1);
                        RequestPreview();
                        break;
                    case "manualWidth":
                        if (Map == null) return;
                        var profile = WidthProfiles[Category(command)];
                        float width = Number(command,"value",0.5f,128);
                        profile.PrefabId = null; profile.Name = null; profile.Meters = width; profile.KeepManual = true; profile.DrivingLaneOffsets=null;
                        InvalidateRoadPlans();
                        break;
                    case "placement":
                        if (Map == null) return;
                        var field = (string)command["field"];
                        if (field == "opacity") { Opacity = Number(command,"value",0.05f,1); break; }
                        if (Locked) return;
                        switch (field)
                        {
                            case "offsetX": OffsetX = Number(command,"value",-14000,14000); break;
                            case "offsetZ": OffsetZ = Number(command,"value",-14000,14000); break;
                            case "rotation": Rotation = Number(command,"value",-180,180); break;
                            case "scale": Scale = Number(command,"value",0.25f,2); RequestPreview(); break;
                        }
                        break;
                    case "nudge":
                        if (Map == null || Locked) return;
                        OffsetX = Mathf.Clamp(OffsetX + Number(command,"x",-100,100),-14000,14000);
                        OffsetZ = Mathf.Clamp(OffsetZ + Number(command,"z",-100,100),-14000,14000);
                        break;
                    case "panPreview":
                        if(Map == null || busy || service == null) return;
                        double east = Number(command,"east",-(float)Map.SizeMeters*2,(float)Map.SizeMeters*2);
                        double north = Number(command,"north",-(float)Map.SizeMeters*2,(float)Map.SizeMeters*2);
                        if(Math.Abs(east)+Math.Abs(north)<1)return;
                        var target=MapMosaic.Pan(Map,east,north);
                        double tileSize=Map.SizeMeters;
                        Geography.Bounds(target.Latitude,target.Longitude,tileSize);
                        var existing=Tiles.Find(t=>Math.Abs(t.Latitude-target.Latitude)<0.0000001&&Math.Abs(t.Longitude-target.Longitude)<0.0000001&&t.SizeMeters==tileSize);
                        if(existing!=null){Map=existing;BuildPreview();break;}
                        if(Tiles.Count>=MapMosaic.MaxTiles)throw new InvalidOperationException("Maximal 24 Ausschnitte pro Sammlung. Bitte speichern.");
                        var previousTiles=new List<StreetMap>(Tiles);
                        StartRequest(async ()=>{
                            var map=await service.Load(target,tileSize,cancellation.Token);
                            MapMosaic.Validate(map);
                            var expanded=new List<StreetMap>(previousTiles){map};
                            var render=MapMosaic.Compose(expanded);
                            return new { map,expanded,render };
                        },result=>{
                            Tiles=result.expanded;RenderMap=result.render;Map=result.map;
                            Visible=true;BuildPreview();
                            status=Tiles.Count+" Ausschnitte verbunden. Vorherige Ausschnitte bleiben erhalten.";
                        });
                        break;
                    case "save": if (Map != null && !busy) Save(); break;
                    case "restore":
                        if (busy || library == null) return;
                        RefreshLibrary(); LibraryOpen = true;
                        if (library.UnreadableCount > 0) status = library.UnreadableCount + " gespeicherte Vorlagen konnten nicht gelesen werden.";
                        break;
                    case "closeLibrary": LibraryOpen = false; break;
                    case "deleteTemplate":
                        if(busy||library==null)return;
                        library.Delete((string)command["id"]);RefreshLibrary();
                        status="Gespeicherte Vorlage gelöscht. Die aktuell geladene Karte bleibt erhalten.";
                        break;
                    case "loadTemplate": if (!busy && library != null) Restore((string)command["id"]); break;
                    case "license": Application.OpenURL("https://www.openstreetmap.org/copyright"); break;
                }
            }
            catch (Exception error) { status = "Fehler: " + error.Message; }
        }
    }
}
