using Game;
using Game.Modding;
using Game.Rendering;
using Game.Simulation;
using Colossal.Mathematics;
using Unity.Mathematics;
using UnityEngine;

namespace CityStreetOverlay
{
    public sealed class Mod : IMod
    {
        GameObject ui;
        public void OnLoad(UpdateSystem updateSystem)
        {
            ui = new GameObject("CityStreetOverlay");
            Object.DontDestroyOnLoad(ui);
            ui.AddComponent<OverlayWindow>();
            // Submit lines before the rendering phase consumes the overlay buffer.
            updateSystem.UpdateAt<StreetOverlaySystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<StreetStampTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<StreetOverlayUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<RoadCatalogSystem>(SystemUpdatePhase.UIUpdate);
            Colossal.Logging.LogManager.GetLogger("CityStreetOverlay").Info("Loaded. Open the street template using the map button in the top-left mod toolbar.");
        }
        public void OnDispose()
        {
            StreetOverlayUISystem.ReleaseInput();
            OverlayWindow.Current = null;
            if (ui != null) Object.Destroy(ui);
        }
    }

    public partial class StreetOverlaySystem : GameSystemBase
    {
        TerrainSystem terrain;
        OverlayRenderSystem overlay;
        protected override void OnCreate()
        {
            base.OnCreate();
            terrain = World.GetOrCreateSystemManaged<TerrainSystem>();
            overlay = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
        }

        protected override void OnUpdate()
        {
            var ui = OverlayWindow.Current;
            if (ui == null || ui.Map == null || !ui.Visible) return;
            var heights = terrain.GetHeightData();
            if (!heights.heights.IsCreated) return;
            var buffer = overlay.GetBuffer(out var dependency);
            dependency.Complete();
            var angle = math.radians(ui.Rotation);
            var sin = math.sin(angle); var cos = math.cos(angle);
            var map = ui.RenderMap ?? ui.Map;
            ui.CheckSelection();
            foreach(var batch in ui.DisplayBatches())
            {
                if(!RoadTypes.IsVisible(ui.Filter,(RoadType)batch.Category))continue;
                Color color=RoadStyles.Color((RoadType)batch.Category);
                color.a=ui.Opacity*ui.WidthProfiles[batch.Category].Opacity;
                foreach(var line in batch.Plan.VisibleLines)
                {
                    var a=Position(line[0],ui,sin,cos);var b=Position(line[1],ui,sin,cos);
                    a.y=TerrainUtils.SampleHeight(ref heights,a)+0.5f;b.y=TerrainUtils.SampleHeight(ref heights,b)+0.5f;
                    if(line.Length==2)buffer.DrawLine(color,color,0,OverlayRenderSystem.StyleFlags.Projected,new Line3.Segment(a,b),batch.Profile.Meters,new float2(1f));
                    else
                    {
                        var c=Position(line[2],ui,sin,cos);var d=Position(line[3],ui,sin,cos);
                        c.y=math.lerp(a.y,b.y,1f/3);d.y=math.lerp(a.y,b.y,2f/3);
                        buffer.DrawCurve(color,color,0,OverlayRenderSystem.StyleFlags.Projected,new Bezier4x3(a,c,d,b),batch.Profile.Meters,new float2(1f));
                    }
                }
            }
            // Highlight the same fitted geometry as the normal overlay.
            foreach(var batch in ui.HoverBatches())foreach(var line in batch.Plan.VisibleLines)
            {
                if(!RoadTypes.IsVisible(ui.Filter,(RoadType)batch.Category))continue;
                var a=Position(line[0],ui,sin,cos);var b=Position(line[1],ui,sin,cos);
                a.y=TerrainUtils.SampleHeight(ref heights,a)+0.7f;b.y=TerrainUtils.SampleHeight(ref heights,b)+0.7f;
                var red=new Color(1,1,1,1);
                if(line.Length==2)buffer.DrawLine(red,red,0,OverlayRenderSystem.StyleFlags.Projected,new Line3.Segment(a,b),batch.Profile.Meters,new float2(1f));
                else
                {
                    var c=Position(line[2],ui,sin,cos);var d=Position(line[3],ui,sin,cos);
                    c.y=math.lerp(a.y,b.y,1f/3);d.y=math.lerp(a.y,b.y,2f/3);
                    buffer.DrawCurve(red,red,0,OverlayRenderSystem.StyleFlags.Projected,new Bezier4x3(a,c,d,b),batch.Profile.Meters,new float2(1f));
                }
            }
            // Highlight the same fitted geometry as the normal overlay.
            foreach(var batch in ui.SelectedBatches())foreach(var line in batch.Plan.VisibleLines)
            {
                if(!RoadTypes.IsVisible(ui.Filter,(RoadType)batch.Category))continue;
                var a=Position(line[0],ui,sin,cos);var b=Position(line[1],ui,sin,cos);
                a.y=TerrainUtils.SampleHeight(ref heights,a)+0.6f;b.y=TerrainUtils.SampleHeight(ref heights,b)+0.6f;
                var red=new Color(1,0.2f,0.2f,1);
                if(line.Length==2)buffer.DrawLine(red,red,0,OverlayRenderSystem.StyleFlags.Projected,new Line3.Segment(a,b),batch.Profile.Meters,new float2(1f));
                else
                {
                    var c=Position(line[2],ui,sin,cos);var d=Position(line[3],ui,sin,cos);
                    c.y=math.lerp(a.y,b.y,1f/3);d.y=math.lerp(a.y,b.y,2f/3);
                    buffer.DrawCurve(red,red,0,OverlayRenderSystem.StyleFlags.Projected,new Bezier4x3(a,c,d,b),batch.Profile.Meters,new float2(1f));
                }
            }
        }
        static float3 Position(Point point, OverlayWindow ui, float sin, float cos)
        {
            return new float3(ui.OffsetX + (float)(point.X*cos-point.Z*sin)*ui.Scale, 0,
                ui.OffsetZ + (float)(point.X*sin+point.Z*cos)*ui.Scale);
        }
    }
}
