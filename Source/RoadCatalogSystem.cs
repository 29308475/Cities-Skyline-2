using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace CityStreetOverlay
{
    public partial class RoadCatalogSystem : GameSystemBase
    {
        EntityQuery roads;
        PrefabSystem prefabs;
        float nextScan;
        string lastLaneAudit;

        protected override void OnCreate()
        {
            base.OnCreate();
            prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
            roads = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<NetGeometryData>(),
                ComponentType.ReadOnly<UIObjectData>());
        }

        // Road prefabs normally have geometry sections, but no DefaultNetLane buffer.
        // Reconstruct the default composition through the same helpers as the game.
        float[] ReadLaneOffsets(Entity entity,bool backward)
        {
            if(!EntityManager.HasBuffer<NetGeometrySection>(entity))return null;
            using(var pieces=new NativeList<NetCompositionPiece>(Allocator.Temp))
            using(var lanes=new NativeList<NetCompositionLane>(Allocator.Temp))
            {
                NetCompositionHelpers.GetCompositionPieces(pieces,
                    EntityManager.GetBuffer<NetGeometrySection>(entity,true).AsNativeArray(),default(CompositionFlags),
                    GetBufferLookup<NetSubSection>(true),GetBufferLookup<NetSectionPiece>(true));
                var composition=default(NetCompositionData);
                NetCompositionHelpers.CalculateCompositionData(ref composition,pieces.AsArray(),
                    GetComponentLookup<NetPieceData>(true),GetComponentLookup<NetVertexMatchData>(true));
                NetCompositionHelpers.AddCompositionLanes(Entity.Null,ref composition,pieces,lanes,
                    default(DynamicBuffer<NetCompositionCarriageway>),GetComponentLookup<NetLaneData>(true),
                    GetBufferLookup<NetPieceLane>(true));
                var positions=new List<float>();
                foreach(var lane in lanes)
                {
                    var flags=lane.m_Flags;
                    if((flags&LaneFlags.Road)==0||(flags&(LaneFlags.Master|LaneFlags.Virtual|LaneFlags.Parking|LaneFlags.BicyclesOnly|LaneFlags.Secondary))!=0)continue;
                    if((flags&LaneFlags.Twoway)!=0||((flags&LaneFlags.Invert)!=0)!=backward)return null;
                    positions.Add(lane.m_Position.x*(backward?-1:1));
                }
                return positions.Count==0?null:positions.Distinct().OrderBy(x=>x).ToArray();
            }
        }
        protected override void OnUpdate()
        {
            var ui = OverlayWindow.Current;
            if (ui == null || (!ui.IsOpen && ui.Map == null) || UnityEngine.Time.realtimeSinceStartup < nextScan) return;
            nextScan = UnityEngine.Time.realtimeSinceStartup + 5;
            if (GameManager.instance == null || GameManager.instance.isGameLoading) return;
            var options = new List<RoadOption>();
            try
            {
                using (var entities = roads.ToEntityArray(Allocator.Temp))
                {
                    foreach (var entity in entities)
                    {
                        if (!prefabs.TryGetPrefab<NetGeometryPrefab>(entity, out var prefab) || prefab == null || !prefab.active) continue;
                        var road = prefab as RoadPrefab;
                        bool path = prefab is PathwayPrefab;
                        if (road == null && !path) continue;
                        if (!prefab.TryGet<UIObject>(out var uiObject) || !uiObject.active || uiObject.m_IsDebugObject || uiObject.m_Group == null) continue;
                        if (!prefabs.IsAvailable(prefab)) continue;
                        var width = EntityManager.GetComponentData<NetGeometryData>(entity).m_DefaultWidth;
                        if (!RoadWidths.Valid(width)) continue;
                        string name = prefab.name;
                        var dictionary = GameManager.instance.localizationManager?.activeDictionary;
                        if (dictionary != null && dictionary.TryGetValue("Assets.NAME[" + prefab.name + "]", out var translated) && !string.IsNullOrEmpty(translated))
                            name = translated;
                        var flags=EntityManager.HasComponent<RoadData>(entity)?EntityManager.GetComponentData<RoadData>(entity).m_Flags:0;
                        bool forward=(flags&Game.Prefabs.RoadFlags.DefaultIsForward)!=0,backward=(flags&Game.Prefabs.RoadFlags.DefaultIsBackward)!=0;
                        var laneOffsets=forward!=backward?ReadLaneOffsets(entity,backward):null;
                        options.Add(new RoadOption { Id = prefab.GetPrefabID().ToString(), Name = name, AssetName=prefab.name, Width = width, IndependentCurveEnds=(EntityManager.GetComponentData<NetGeometryData>(entity).m_Flags&Game.Net.GeometryFlags.StrictNodes)==0,
                            Lanes=laneOffsets?.Length??RoadRecognition.LabelLanes(prefab.name+" "+name),DirectionKnown=road!=null,IsOneWay=forward!=backward,DrivingLaneOffsets=laneOffsets,
                            AutomaticEligible=prefab.isBuiltin&&(RoadRecognition.StandardRoadLabel(prefab.name)||RoadRecognition.StandardRoadLabel(name)),
                            IsPath = path, IsHighway = road != null && road.m_HighwayRules,
                            DefaultRoundabout = road!=null && (prefab.name=="One-Lane One-Way Road" || prefab.name=="OneLaneOnewayRoad" || prefab.name=="Small Road OneWay 1 Lane" ||
                                (name.IndexOf("einspurig",StringComparison.OrdinalIgnoreCase)>=0&&name.IndexOf("Einbahn",StringComparison.OrdinalIgnoreCase)>=0)) });
                    }
                }
                options.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name));
                ui.SetRoadCatalog(options);
                var audit=Newtonsoft.Json.JsonConvert.SerializeObject(options.Where(o=>o.IsHighway).Select(o=>new{o.Id,o.Name,o.Width,o.Lanes,o.IsOneWay,o.IndependentCurveEnds,o.DrivingLaneOffsets}),Newtonsoft.Json.Formatting.Indented);
                if(audit!=lastLaneAudit)
                {
                    var directory=System.IO.Path.Combine(MapService.DataDirectory,"Diagnostics");
                    System.IO.Directory.CreateDirectory(directory);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"highway-lanes.json"),audit);
                    lastLaneAudit=audit;
                }
            }
            catch (Exception error)
            {
                // No road/terrain mutation; the saved line widths remain usable on failure.
                ui.CatalogStatus = "Straßenauswahl konnte nicht gelesen werden. Gespeicherte Breiten bleiben erhalten.";
                Colossal.Logging.LogManager.GetLogger("CityStreetOverlay").Warn(error.Message);
                nextScan = UnityEngine.Time.realtimeSinceStartup + 30;
            }
        }
    }
}
