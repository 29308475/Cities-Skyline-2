using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace CityStreetOverlay
{
    // Main-thread model for the native game UI and terrain overlay.
    public sealed partial class OverlayWindow : MonoBehaviour
    {
        public static OverlayWindow Current;
        public StreetMap Map;
        public List<StreetMap> Tiles = new List<StreetMap>();
        public StreetMap RenderMap;
        public bool Visible, Locked;
        public RoadFilter Filter = RoadFilter.All;
        public RoadWidthProfile[] WidthProfiles = RoadWidths.Manual();
        public string CatalogStatus = "Spielstraßen werden geladen …";
        public string StampStatus = "";
        List<RoadOption> roadOptions = new List<RoadOption>();




        public float OffsetX, OffsetZ, Rotation, Scale = 1, Opacity = 0.7f, Width = 3;
        public bool IsOpen { get; private set; }

        public void ToggleWindow() { IsOpen = !IsOpen; }
        public void CloseWindow() { IsOpen = false; }


        string query = "", status = "Stadt suchen und einen Ausschnitt laden.";
        List<Place> places = new List<Place>();
        Place selected;
        float sizeKm = 2, eastKm, northKm;
        MapService service;
        Task<object> pending;
        Action<object> completed;
        readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        TemplateLibrary library;
        public string LibraryJson { get; private set; } = "[]";
        public bool LibraryOpen { get; private set; }
        int savedCount;
        void RefreshLibrary() { var entries=library.List(); savedCount=entries.Count; LibraryJson=JsonConvert.SerializeObject(entries); }
        Texture2D preview;
        bool busy => pending != null;

        void Awake()
        {
            Current = this;
            try
            {
                library = new TemplateLibrary(MapService.DataDirectory);
                RefreshLibrary();
                service = new MapService();
            }
            catch (Exception e) { status = "Initialisierung fehlgeschlagen: " + e.Message; }
        }

        void Update()
        {
            UpdateSelectionInput();
            if (previewDirty && Map != null && Time.realtimeSinceStartup >= previewDue)
            {
                previewDirty = false;
                try { BuildPreview(); }
                catch (Exception error) { status = "Vorschaufehler: " + error.Message; }
            }
            if (pending == null || !pending.IsCompleted) return;
            var task = pending;
            var callback = completed;
            pending = null;
            try { callback(task.GetAwaiter().GetResult()); }
            catch (OperationCanceledException) { status = "Anfrage abgebrochen oder Zeitlimit erreicht."; }
            catch (Exception e) { status = "Fehler: " + e.Message; }
        }

        void StartRequest<T>(Func<Task<T>> action, Action<T> result)
        {
            if (busy || service == null) return;
            status = "Daten werden geladen …";
            completed = value => result((T)value);
            pending = Task.Run(async () => (object)await action());
        }

        void BuildPreview()
        {
            if (preview != null) Destroy(preview);
            const int size = 512;
            preview = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size*size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(15, 25, 35, 255);
            var previewMap=RenderMap??Map;
            var typed = previewMap.HasRoadTypes;
            var center=Geography.Project(Map.Latitude,Map.Longitude,previewMap.Latitude,previewMap.Longitude);
            double factor=Math.Cos(Map.Latitude*Math.PI/180)/Math.Cos(previewMap.Latitude*Math.PI/180);
            foreach(var stroke in PreviewStrokes())
            {
                var type = (RoadType)stroke.Category;
                if (typed && !RoadTypes.IsVisible(Filter, type)) continue;
                var sourceRoad=stroke.Points;
                if(ExcludedRoads.Contains(RoadExclusions.Key(sourceRoad)))continue;
                var p0=new Point((sourceRoad[0].X-center.X)*factor,sourceRoad[0].Z-center.Z);
                var p1=new Point((sourceRoad[1].X-center.X)*factor,sourceRoad[1].Z-center.Z);
                if(!Geography.Clip(ref p0,ref p1,Map.SizeMeters/2))continue;
                var road=new[]{p0,p1};
                var a = new Vector2((float)(road[0].X/Map.SizeMeters+0.5)*(size-1), (float)(road[0].Z/Map.SizeMeters+0.5)*(size-1));
                var b = new Vector2((float)(road[1].X/Map.SizeMeters+0.5)*(size-1), (float)(road[1].Z/Map.SizeMeters+0.5)*(size-1));
                float radius = Mathf.Max(0.5f, (float)RoadWidths.PreviewPixels(stroke.Profile.Meters, Map.SizeMeters, Scale, size) * 0.5f);
                var delta = b - a;
                float lengthSquared = delta.sqrMagnitude;
                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius), 0, size - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius), 0, size - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius), 0, size - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius), 0, size - 1);
                var color = RoadStyles.Color(type);
                color.a = (byte)Mathf.RoundToInt(255 * WidthProfiles[(int)type].Opacity);
                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x, y);
                    float t = lengthSquared > 0 ? Mathf.Clamp01(Vector2.Dot(p-a, delta) / lengthSquared) : 0;
                    if ((p - (a + t * delta)).sqrMagnitude <= radius * radius) pixels[y*size+x] = color;
                }
            }
            preview.SetPixels32(pixels); preview.Apply();
            PreviewDataUrl = "data:image/png;base64," + Convert.ToBase64String(preview.EncodeToPNG());
        }

        void Save()
        {
            try
            {
                BuildPreview();
                var saved = new SavedTemplate { Map=Map, Tiles=Tiles, OffsetX=OffsetX, OffsetZ=OffsetZ, Rotation=Rotation, Scale=Scale, Opacity=Opacity, Width=Width, Filter=Filter, WidthProfiles=WidthProfiles, RoundaboutProfile=RoundaboutProfile, RoundaboutApproachProfile=RoundaboutApproachProfile, Preview=PreviewDataUrl };
                saved.ExcludedRoads=new List<string>(ExcludedRoads); saved.Subgroups=Subgroups;
                var entry = library.Save(saved);
                RefreshLibrary();
                status = entry.City + " - Version " + entry.Version + " gespeichert.";

            }
            catch (Exception e) { status = "Speichern fehlgeschlagen: " + e.Message; }
        }

        void Restore(string id)
        {
            try
            {
                var saved = library.Load(id);
                MapMosaic.Validate(saved.Map);
                var restoredTiles = saved.Tiles ?? new List<StreetMap> { saved.Map };
                if(restoredTiles.Count==0||restoredTiles.Count>MapMosaic.MaxTiles)throw new FormatException("Ungültige Ausschnittsammlung.");
                foreach(var tile in restoredTiles)MapMosaic.Validate(tile);
                var restoredRender = MapMosaic.Compose(restoredTiles);
                var restoredGroups=saved.Subgroups??new List<RoadSubgroup>();RoadSubgroups.Validate(restoredGroups);
                if (saved?.Map?.Roads == null || saved.Map.SizeMeters < 500 || saved.Map.SizeMeters > 10000 || saved.Map.Roads.Count > 80000)
                    throw new FormatException("Ungültige Vorlage.");
                saved.Map.ValidateRoadTypes();
                if ((saved.Filter & ~RoadFilter.All) != 0) throw new FormatException("Ungültige Straßenfilter.");
                foreach (var road in saved.Map.Roads)
                {
                    if (road == null || road.Length != 2) throw new FormatException("Ungültiger Straßenabschnitt.");
                    foreach (var p in road) if (double.IsNaN(p.X+p.Z) || double.IsInfinity(p.X+p.Z) || Math.Abs(p.X)>5001 || Math.Abs(p.Z)>5001)
                        throw new FormatException("Ungültige Koordinaten.");
                }
                var values = new[] { saved.OffsetX, saved.OffsetZ, saved.Rotation, saved.Scale, saved.Opacity, saved.Width };
                foreach (var value in values) if (float.IsNaN(value) || float.IsInfinity(value)) throw new FormatException("Ungültige Platzierung.");
                var profiles = saved.WidthProfiles ?? RoadWidths.Manual(Mathf.Clamp(saved.Width, 1, 12));
                RoadWidths.Validate(profiles);
                RoadWidths.Refresh(profiles, roadOptions);
                Map = saved.Map; OffsetX = Mathf.Clamp(saved.OffsetX,-14000,14000); OffsetZ = Mathf.Clamp(saved.OffsetZ,-14000,14000);
                Rotation = saved.Rotation; Scale = Mathf.Clamp(saved.Scale,0.25f,2); Opacity = Mathf.Clamp01(saved.Opacity); Width = Mathf.Clamp(saved.Width,1,12);
                Filter = saved.Filter;
                WidthProfiles = profiles;
                RoundaboutProfile=saved.RoundaboutProfile;
                RoundaboutApproachProfile=saved.RoundaboutApproachProfile;
                if(RoundaboutApproachProfile!=null)RoadWidths.Validate(new[]{RoundaboutApproachProfile,RoundaboutApproachProfile,RoundaboutApproachProfile,RoundaboutApproachProfile});
                if(RoundaboutProfile!=null)RoadWidths.Validate(new[]{RoundaboutProfile,RoundaboutProfile,RoundaboutProfile,RoundaboutProfile});
                RefreshRoundaboutDefault();
                Tiles = restoredTiles; RenderMap = restoredRender; Subgroups=restoredGroups;InvalidateRoadPlans();
                ExcludedRoads.Clear();if(saved.ExcludedRoads!=null)foreach(var key in saved.ExcludedRoads)ExcludedRoads.Add(key);

                Visible = false; Locked = true; BuildPreview(); LibraryOpen = false;
                status = "Offline geladen. Anzeige bei Bedarf einschalten.";
            }
            catch (Exception e) { status = "Laden fehlgeschlagen: " + e.Message; }
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            cancellation.Cancel();
            if (pending != null) pending.ContinueWith(t => { var observed = t.Exception; service?.Dispose(); });
            else service?.Dispose();
            if (preview != null) Destroy(preview);
        }
    }
}
