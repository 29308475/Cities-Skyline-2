using System;
using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CityStreetOverlay
{
    public struct StampDefinition : IComponentData { }
    public partial class StreetStampTool : ToolBaseSystem
    {
        public static StreetStampTool Instance;
        public override string toolID => "CityStreetOverlay.Stamp";
        ToolOutputBarrier output;
        TerrainSystem terrain;
        EntityQuery definitions, allDefinitions, errors, temporary, prefabQuery, nodes,edges;
        readonly Dictionary<int2,List<Entity>> nearbyEdges=new Dictionary<int2,List<Entity>>();
        readonly List<NetCourse> courses=new List<NetCourse>();
        readonly List<Entity> coursePrefabs=new List<Entity>();
        readonly Dictionary<float3,CoursePos> endpointCache=new Dictionary<float3,CoursePos>();
        readonly Dictionary<Entity,float3> snappedSources=new Dictionary<Entity,float3>();
        readonly List<KeyValuePair<Entity,ObjectDefinition>> islands=new List<KeyValuePair<Entity,ObjectDefinition>>();
        readonly Dictionary<int2,List<KeyValuePair<Entity,float3>>> nearbyNodes=new Dictionary<int2,List<KeyValuePair<Entity,float3>>>();
        Entity prefabEntity;
        PrefabBase roadPrefab;
        int frames, finishing;
        int skippedIsolated;
        bool requested, cancelRequested;
        public bool Building => requested || finishing>0;
        public override PrefabBase GetPrefab() => roadPrefab;
        public override bool TrySetPrefab(PrefabBase prefab) => false;
        protected override void OnCreate()
        {
            base.OnCreate();Instance=this;
            output=World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            terrain=World.GetOrCreateSystemManaged<TerrainSystem>();
            definitions=GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(),ComponentType.ReadOnly<StampDefinition>(),ComponentType.Exclude<Updated>());
            allDefinitions=GetEntityQuery(ComponentType.ReadOnly<StampDefinition>());
            errors=GetEntityQuery(ComponentType.ReadOnly<Game.Tools.Error>());
            temporary=GetEntityQuery(ComponentType.ReadOnly<Temp>(),ComponentType.ReadOnly<Edge>(),ComponentType.Exclude<Deleted>());
            prefabQuery=GetEntityQuery(ComponentType.ReadOnly<PrefabData>(),ComponentType.ReadOnly<NetData>(),ComponentType.ReadOnly<NetGeometryData>());
            nodes=GetEntityQuery(ComponentType.ReadOnly<Node>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Deleted>());
            edges=GetEntityQuery(ComponentType.ReadOnly<Edge>(),ComponentType.ReadOnly<Curve>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Deleted>());
        }
        public void Cancel() { cancelRequested=true; }
        public void Begin(OverlayWindow ui,int category)
        {
            if(Building)throw new InvalidOperationException("Ein Bauauftrag läuft bereits.");
            if(!ui.Visible||!RoadTypes.IsVisible(ui.Filter,(RoadType)category))throw new InvalidOperationException("Vorlage und Kategorie zuerst einblenden.");
            ui.CheckSelection();
            if(ui.SelectedRoads.Count>0)throw new InvalidOperationException("Markierte Abschnitte zuerst entfernen oder Markierung aufheben.");
            var batches=RoadSubgroups.Plan(RoadExclusions.Apply(ui.RenderMap??ui.Map,ui.ExcludedRoads),ui.Subgroups,ui.WidthProfiles,ui.Scale,category,true,2000,ui.RoundaboutProfile,ui.LeftTraffic,ui.RoundaboutApproachProfile);
            var heights=terrain.GetHeightData();
            if(!heights.heights.IsCreated)throw new InvalidOperationException("Geländedaten noch nicht bereit.");
            float angle=math.radians(ui.Rotation),sn=math.sin(angle),cs=math.cos(angle);
            courses.Clear();
            int isolatedSkipped=0;
            var isolatedCandidates=new List<bool>();
            islands.Clear();coursePrefabs.Clear();endpointCache.Clear();snappedSources.Clear();
            foreach(var batch in batches)
            {
            var plan=batch.Plan;
            var id=batch.Profile.PrefabId;
            if(string.IsNullOrEmpty(id))throw new InvalidOperationException("Zuerst eine Spielstraße auswählen.");
            prefabEntity=Entity.Null;roadPrefab=null;
            using(var entities=prefabQuery.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
            {
                // TryGetPrefab reports a valid index even when its typed cast returns null.
                if(m_PrefabSystem.TryGetPrefab<NetGeometryPrefab>(entity,out var prefab)&&prefab!=null&&prefab.GetPrefabID().ToString()==id&&prefab.active&&m_PrefabSystem.IsAvailable(prefab))
                { prefabEntity=entity;roadPrefab=prefab;break; }
            }
            if(prefabEntity==Entity.Null)throw new InvalidOperationException("Die gewählte Spielstraße ist nicht verfügbar.");

            foreach(var circle in plan.Circles)
            {
                string name=(circle.Radius<10?"Small":circle.Radius<15?"Medium":circle.Radius<20?"Large":"XL")+"Roundabout01";
                if(!m_PrefabSystem.TryGetPrefab(new PrefabID("StaticObjectPrefab",name),out var island)||island==null||!m_PrefabSystem.IsAvailable(island))
                    throw new InvalidOperationException("Spiel-Kreisverkehr nicht verfügbar: "+name);
                var position=Position(circle.Center,ui,sn,cs);position.y=TerrainUtils.SampleHeight(ref heights,position);
                islands.Add(new KeyValuePair<Entity,ObjectDefinition>(m_PrefabSystem.GetEntity(island),new ObjectDefinition{
                    m_Position=position,m_LocalPosition=position,m_Rotation=quaternion.identity,m_LocalRotation=quaternion.identity,
                    m_Scale=new float3(1),m_Intensity=1,m_Probability=100,m_PrefabSubIndex=-1,m_ParentMesh=-1}));
            }
            nearbyNodes.Clear();
            var selectedNet=EntityManager.GetComponentData<NetData>(prefabEntity);
            nearbyEdges.Clear();
            using(var existing=edges.ToEntityArray(Allocator.Temp))foreach(var entity in existing)
            {
                var reference=EntityManager.GetComponentData<PrefabRef>(entity);
                if(!EntityManager.HasComponent<NetData>(reference.m_Prefab)||!NetUtils.CanConnect(selectedNet,EntityManager.GetComponentData<NetData>(reference.m_Prefab)))continue;
                var curve=EntityManager.GetComponentData<Curve>(entity).m_Bezier;
                var min=math.min(math.min(curve.a.xz,curve.b.xz),math.min(curve.c.xz,curve.d.xz));
                var max=math.max(math.max(curve.a.xz,curve.b.xz),math.max(curve.c.xz,curve.d.xz));
                var lo=(int2)math.floor((min-2)/64);var hi=(int2)math.floor((max+2)/64);
                for(int x=lo.x;x<=hi.x;x++)for(int z=lo.y;z<=hi.y;z++)
                {var key=new int2(x,z);if(!nearbyEdges.TryGetValue(key,out var bucket))nearbyEdges[key]=bucket=new List<Entity>();bucket.Add(entity);}
            }
            using(var existing=nodes.ToEntityArray(Allocator.Temp))foreach(var entity in existing)
            {
                var reference=EntityManager.GetComponentData<PrefabRef>(entity);
                if(!EntityManager.HasComponent<NetData>(reference.m_Prefab)||!NetUtils.CanConnect(selectedNet,EntityManager.GetComponentData<NetData>(reference.m_Prefab)))continue;
                var position=EntityManager.GetComponentData<Node>(entity).m_Position;
                var key=(int2)math.floor(position.xz/2);
                if(!nearbyNodes.TryGetValue(key,out var bucket))nearbyNodes[key]=bucket=new List<KeyValuePair<Entity,float3>>();
                bucket.Add(new KeyValuePair<Entity,float3>(entity,position));
            }
            bool reverseRing=false;
            if((batch.IsRing||batch.IsApproach)&&EntityManager.HasComponent<RoadData>(prefabEntity))
            {
                var flags=EntityManager.GetComponentData<RoadData>(prefabEntity).m_Flags;
                reverseRing=(flags&Game.Prefabs.RoadFlags.DefaultIsBackward)!=0&&(flags&Game.Prefabs.RoadFlags.DefaultIsForward)==0;
            }
            for(int lineIndex=0;lineIndex<plan.Lines.Count;lineIndex++)
            {
                var plannedLine=plan.Lines[lineIndex];
                var line=reverseRing?(plannedLine.Length==4?new[]{plannedLine[1],plannedLine[0],plannedLine[3],plannedLine[2]}:new[]{plannedLine[1],plannedLine[0]}):plannedLine;
                var a=Position(line[0],ui,sn,cs);var b=Position(line[1],ui,sn,cs);
                float ea=plan.Elevated.Contains(StreetBuildPlan.Key(line[0]))?6.25f:0;
                float eb=plan.Elevated.Contains(StreetBuildPlan.Key(line[1]))?6.25f:0;
                a.y=TerrainUtils.SampleHeight(ref heights,a)+ea;b.y=TerrainUtils.SampleHeight(ref heights,b)+eb;
                var start=Endpoint(a);var end=Endpoint(b);
                bool isolated=plan.ShortIsolatedNodes.Contains(StreetBuildPlan.Key(line[0]))&&plan.ShortIsolatedNodes.Contains(StreetBuildPlan.Key(line[1]));
                // Do not collapse a short connection by snapping both ends to one node.
                if(math.distance(start.m_Position.xz,end.m_Position.xz)<0.01f)
                {
                    start=new CoursePos{m_Position=a,m_ParentMesh=-1};
                    end=new CoursePos{m_Position=b,m_ParentMesh=-1};
                }
                var curve=NetUtils.StraightCurve(start.m_Position,end.m_Position);
                if(line.Length==4)
                {
                    curve.b=Position(line[2],ui,sn,cs);curve.c=Position(line[3],ui,sn,cs);
                    curve.b.y=math.lerp(start.m_Position.y,end.m_Position.y,1f/3);
                    curve.c.y=math.lerp(start.m_Position.y,end.m_Position.y,2f/3);
                    // Keep endpoint tangents when snapping to an existing compatible node.
                    curve.b.xz+=start.m_Position.xz-a.xz;curve.c.xz+=end.m_Position.xz-b.xz;
                }
                if(plan.GeometryLines!=null)
                {
                    // Keep CoursePos at the common topological node. Native non-StrictNodes
                    // roads preserve separate X/Z curve endpoints (GenerateEdgesSystem).
                    if((EntityManager.GetComponentData<NetGeometryData>(prefabEntity).m_Flags&GeometryFlags.StrictNodes)!=0)
                        throw new InvalidOperationException("Straßendaten geändert: Auswahl neu laden, bevor seitliche Anschlüsse gebaut werden.");
                    var physical=plan.GeometryLines[lineIndex];
                    curve.a=Position(physical[0],ui,sn,cs);curve.d=Position(physical[1],ui,sn,cs);
                    curve.b=physical.Length==4?Position(physical[2],ui,sn,cs):math.lerp(curve.a,curve.d,1f/3);
                    curve.c=physical.Length==4?Position(physical[3],ui,sn,cs):math.lerp(curve.a,curve.d,2f/3);
                    curve.a.xz+=start.m_Position.xz-a.xz;curve.b.xz+=start.m_Position.xz-a.xz;
                    curve.c.xz+=end.m_Position.xz-b.xz;curve.d.xz+=end.m_Position.xz-b.xz;
                    curve.a.y=start.m_Position.y;curve.d.y=end.m_Position.y;
                    curve.b.y=math.lerp(curve.a.y,curve.d.y,1f/3);curve.c.y=math.lerp(curve.a.y,curve.d.y,2f/3);
                }
                start.m_Rotation=NetUtils.GetNodeRotation(curve.b-curve.a);end.m_Rotation=NetUtils.GetNodeRotation(curve.d-curve.c);
                start.m_CourseDelta=0;end.m_CourseDelta=1;
                start.m_Flags=CoursePosFlags.IsFirst|CoursePosFlags.IsLeft|CoursePosFlags.IsRight;
                end.m_Flags=CoursePosFlags.IsLast|CoursePosFlags.IsLeft|CoursePosFlags.IsRight;
                start.m_Elevation=new float2(ea);end.m_Elevation=new float2(eb);
                if(ea>0)start.m_Flags|=CoursePosFlags.FreeHeight|CoursePosFlags.ForceElevatedNode;
                if(eb>0)end.m_Flags|=CoursePosFlags.FreeHeight|CoursePosFlags.ForceElevatedNode;
                if(ea>0&&eb>0){start.m_Flags|=CoursePosFlags.ForceElevatedEdge;end.m_Flags|=CoursePosFlags.ForceElevatedEdge;}
                float courseLength=Colossal.Mathematics.MathUtils.Length(curve);
                coursePrefabs.Add(prefabEntity);
                isolatedCandidates.Add(isolated);
                courses.Add(new NetCourse{m_StartPosition=start,m_EndPosition=end,m_Curve=curve,m_Length=courseLength,m_FixedIndex=-1});
            }
            }
            // Protect the entire component when any one of its courses snaps to existing roads.
            var at=new Dictionary<float3,List<int>>();
            for(int i=0;i<courses.Count;i++)foreach(var p in new[]{courses[i].m_StartPosition.m_Position,courses[i].m_EndPosition.m_Position})
            {if(!at.TryGetValue(p,out var list))at[p]=list=new List<int>();list.Add(i);}
            var visited=new HashSet<int>();var omit=new HashSet<int>();
            for(int i=0;i<courses.Count;i++)
            {
                if(!visited.Add(i))continue;
                var queue=new Queue<int>();queue.Enqueue(i);var component=new List<int>();bool discard=true;
                while(queue.Count>0)
                {
                    int j=queue.Dequeue();component.Add(j);var c=courses[j];
                    discard&=isolatedCandidates[j]&&c.m_StartPosition.m_Entity==Entity.Null&&c.m_EndPosition.m_Entity==Entity.Null;
                    foreach(var p in new[]{c.m_StartPosition.m_Position,c.m_EndPosition.m_Position})foreach(int next in at[p])if(visited.Add(next))queue.Enqueue(next);
                }
                if(discard)foreach(int j in component)omit.Add(j);
            }
            isolatedSkipped=omit.Count;
            for(int i=courses.Count-1;i>=0;i--)if(omit.Contains(i)){courses.RemoveAt(i);coursePrefabs.RemoveAt(i);}
            if(courses.Count==0)throw new InvalidOperationException(isolatedSkipped>0?"Nur kurze, unverbundene Reststücke vorhanden – nicht gebaut.":"Keine verbleibenden Straßen dieser Kategorie.");
            frames=0;finishing=0;cancelRequested=false;requested=true;
            skippedIsolated=isolatedSkipped;
            ui.Selecting=false;
            ui.StampStatus="Bauprüfung: "+courses.Count+" Abschnitte mit "+ui.WidthProfiles[category].Name;
            m_ToolSystem.activeTool=this;
        }
        CoursePos Endpoint(float3 position)
        {
            if(endpointCache.TryGetValue(position,out var cached))return cached;
            var result=new CoursePos{m_Position=position,m_ParentMesh=-1};
            float nearest=2;
            var key=(int2)math.floor(position.xz/2);
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                if(!nearbyNodes.TryGetValue(key+new int2(x,z),out var bucket))continue;
                foreach(var node in bucket){float distance=math.distance(position,node.Value);
                    if(snappedSources.TryGetValue(node.Key,out var source)&&!source.Equals(position))continue;
                    if(distance<nearest){nearest=distance;result.m_Entity=node.Key;result.m_Position=node.Value;}}
            }
            if(result.m_Entity!=Entity.Null)snappedSources[result.m_Entity]=position;
            else if(nearbyEdges.TryGetValue((int2)math.floor(position.xz/64),out var edgeBucket))
            {
                foreach(var entity in edgeBucket)
                {
                    var curve=EntityManager.GetComponentData<Curve>(entity).m_Bezier;
                    float distance=Colossal.Mathematics.MathUtils.Distance(curve.xz,position.xz,out float t);
                    if(distance>=nearest||t<=0.001f||t>=0.999f)continue;
                    var point=Colossal.Mathematics.MathUtils.Position(curve,t);
                    if(math.abs(point.y-position.y)>1)continue; // Do not connect a bridge to the road below.
                    nearest=distance;result.m_Entity=entity;result.m_Position=point;result.m_SplitPosition=t;
                }
            }
            endpointCache[position]=result;return result;
        }
        static float3 Position(Point p,OverlayWindow ui,float sn,float cs) => new float3(ui.OffsetX+(float)(p.X*cs-p.Z*sn)*ui.Scale,0,ui.OffsetZ+(float)(p.X*sn+p.Z*cs)*ui.Scale);
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();applyMode=ApplyMode.Clear;
            var ui=OverlayWindow.Current;
            if(finishing>0){if(--finishing==0)m_ToolSystem.activeTool=m_DefaultToolSystem;return inputDeps;}
            if(!requested){m_ToolSystem.activeTool=m_DefaultToolSystem;return inputDeps;}
            if(cancelRequested||ui==null||cancelAction.WasPressedThisFrame())return Finish(inputDeps,"Bau abgebrochen.",false);
            try
            {
                frames++;
                // Allow the native temporary-network and validation systems to settle.
                if(frames>=8&&definitions.CalculateEntityCount()==courses.Count+islands.Count&&!temporary.IsEmptyIgnoreFilter&&errors.IsEmptyIgnoreFilter&&GetAllowApply())
                    return Finish(inputDeps,"Bauauftrag an das Spiel übergeben: "+courses.Count+" Abschnitte.",true);
                if(frames>=90)return Finish(inputDeps,"Nicht gebaut: Das Spiel meldet Platzierungsfehler (z. B. Kollision, Steigung oder fehlendes Geld).",false);
                var dependency=DestroyDefinitions(definitions,output,inputDeps);
                var commands=output.CreateCommandBuffer();
                for(int i=0;i<courses.Count;i++)
                {
                    var entity=commands.CreateEntity();
                    commands.AddComponent(entity,new CreationDefinition{m_Prefab=coursePrefabs[i],m_RandomSeed=1000+i});
                    commands.AddComponent(entity,new Updated());commands.AddComponent(entity,new StampDefinition());commands.AddComponent(entity,courses[i]);
                }
                foreach(var island in islands)
                {
                    var entity=commands.CreateEntity();
                    commands.AddComponent(entity,new CreationDefinition{m_Prefab=island.Key,m_RandomSeed=2001});
                    commands.AddComponent(entity,new Updated());commands.AddComponent(entity,new StampDefinition());commands.AddComponent(entity,island.Value);
                }
                return dependency;
            }
            catch(Exception error){Colossal.Logging.LogManager.GetLogger("CityStreetOverlay").Error("Stamp update failed: "+error.ToString());return Finish(inputDeps,"Nicht gebaut: "+error.Message,false);}
        }
        JobHandle Finish(JobHandle deps,string message,bool apply)
        {
            if(apply&&skippedIsolated>0)message+=" "+skippedIsolated+" kurze isolierte Abschnitte ausgelassen.";
            WriteConnectionAudit(apply,message);
            applyMode=apply?ApplyMode.Apply:ApplyMode.Clear;
            requested=false;finishing=2;
            if(OverlayWindow.Current!=null)OverlayWindow.Current.StampStatus=message;
            return DestroyDefinitions(definitions,output,deps);
        }
        void WriteConnectionAudit(bool apply,string message)
        {
            try
            {
                object PositionData(float3 p)=>new {x=p.x,y=p.y,z=p.z};
                var planned=new List<object>();
                for(int i=0;i<courses.Count;i++)
                {
                    var c=courses[i];planned.Add(new {prefab=coursePrefabs[i].ToString(),start=PositionData(c.m_StartPosition.m_Position),end=PositionData(c.m_EndPosition.m_Position),
                        prefabId=m_PrefabSystem.TryGetPrefab<NetGeometryPrefab>(coursePrefabs[i],out var p)&&p!=null?p.GetPrefabID().ToString():null,
                        startEntity=c.m_StartPosition.m_Entity.ToString(),endEntity=c.m_EndPosition.m_Entity.ToString(),length=c.m_Length,
                        curveStart=PositionData(c.m_Curve.a),curveEnd=PositionData(c.m_Curve.d),control1=PositionData(c.m_Curve.b),control2=PositionData(c.m_Curve.c)});
                }
                var generated=new List<object>();
                var junctions=new HashSet<Entity>();
                using(var entities=temporary.ToEntityArray(Allocator.Temp))foreach(var e in entities)
                {
                    var edge=EntityManager.GetComponentData<Edge>(e);
                    if(!EntityManager.HasComponent<Node>(edge.m_Start)||!EntityManager.HasComponent<Node>(edge.m_End))continue;
                    junctions.Add(edge.m_Start);junctions.Add(edge.m_End);
                    generated.Add(new {entity=e.ToString(),startNode=edge.m_Start.ToString(),endNode=edge.m_End.ToString(),
                        start=PositionData(EntityManager.GetComponentData<Node>(edge.m_Start).m_Position),
                        end=PositionData(EntityManager.GetComponentData<Node>(edge.m_End).m_Position),
                        curve=EntityManager.HasComponent<Curve>(e)?(object)new{start=PositionData(EntityManager.GetComponentData<Curve>(e).m_Bezier.a),end=PositionData(EntityManager.GetComponentData<Curve>(e).m_Bezier.d),control1=PositionData(EntityManager.GetComponentData<Curve>(e).m_Bezier.b),control2=PositionData(EntityManager.GetComponentData<Curve>(e).m_Bezier.c)}:null,
                        flags=EntityManager.GetComponentData<Temp>(e).m_Flags.ToString()});
                }
                var laneConnections=new List<object>();
                foreach(var node in junctions)
                {
                    if(!EntityManager.HasBuffer<Game.Net.SubLane>(node))continue;
                    foreach(var entry in EntityManager.GetBuffer<Game.Net.SubLane>(node,true))
                    {
                        var laneEntity=entry.m_SubLane;
                        if(!EntityManager.HasComponent<Game.Net.CarLane>(laneEntity)||!EntityManager.HasComponent<Lane>(laneEntity)||!EntityManager.HasComponent<Curve>(laneEntity))continue;
                        var lane=EntityManager.GetComponentData<Lane>(laneEntity);var curve=EntityManager.GetComponentData<Curve>(laneEntity).m_Bezier;
                        laneConnections.Add(new{node=node.ToString(),lane=laneEntity.ToString(),
                            fromOwner=lane.m_StartNode.GetOwnerIndex(),fromLane=lane.m_StartNode.GetLaneIndex(),
                            toOwner=lane.m_EndNode.GetOwnerIndex(),toLane=lane.m_EndNode.GetLaneIndex(),
                            start=PositionData(curve.a),end=PositionData(curve.d)});
                    }
                }
                var directory=System.IO.Path.Combine(MapService.DataDirectory,"Diagnostics");System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"last-connections.json"),Newtonsoft.Json.JsonConvert.SerializeObject(
                    new {time=DateTime.UtcNow,apply,message,stage="native temporary network before apply; may include affected existing edges",planned,generated,laneConnections},Newtonsoft.Json.Formatting.Indented));
            }
            catch(Exception error){Colossal.Logging.LogManager.GetLogger("CityStreetOverlay").Warn("Connection audit failed: "+error.Message);}
        }
        protected override void OnStopRunning()
        {
            if(requested&&OverlayWindow.Current!=null)OverlayWindow.Current.StampStatus="Bau durch Werkzeugwechsel abgebrochen.";
            Dependency.Complete();
            if(!allDefinitions.IsEmptyIgnoreFilter)EntityManager.DestroyEntity(allDefinitions);
            requested=false;finishing=0;courses.Clear();base.OnStopRunning();
        }
        protected override void OnDestroy(){if(Instance==this)Instance=null;base.OnDestroy();}
    }
}
