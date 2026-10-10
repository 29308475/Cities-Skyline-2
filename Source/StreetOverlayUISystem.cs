using Colossal.UI.Binding;
using Game.UI;
using Game.Input;

namespace CityStreetOverlay
{
    // The native HUD button controls the existing window through the game's UI bindings.
    public partial class StreetOverlayUISystem : UISystemBase
    {
        const string Group = "CityStreetOverlay";
        ValueBinding<bool> windowOpen;
        ValueBinding<string> state, preview, catalog, library, selectionBox;
        float nextRefresh;
        InputBarrier cameraBarrier, toolBarrier;
        bool pointerOverPanel;
        static StreetOverlayUISystem instance;

        protected override void OnCreate()
        {
            base.OnCreate();
            instance = this;
            cameraBarrier = InputManager.instance.CreateMapBarrier("Camera", "CityStreetOverlay");
            toolBarrier = InputManager.instance.CreateMapBarrier("Tool", "CityStreetOverlay");
            AddBinding(windowOpen = new ValueBinding<bool>(Group, "windowOpen", false));
            AddBinding(state = new ValueBinding<string>(Group, "state", "{}"));
            AddBinding(selectionBox = new ValueBinding<string>(Group, "selectionBox", "null"));
            AddBinding(preview = new ValueBinding<string>(Group, "preview", ""));
            AddBinding(library = new ValueBinding<string>(Group, "library", "[]"));
            AddBinding(catalog = new ValueBinding<string>(Group, "catalog", "[]"));
            AddBinding(new TriggerBinding<string>(Group, "command", json => {
                OverlayWindow.Current?.HandleCommand(json);
                UpdateInputBarrier();
            }));
            AddBinding(new TriggerBinding<bool>(Group, "pointerOverPanel", value => {
                pointerOverPanel = value;
                UpdateInputBarrier();
            }));
            AddBinding(new TriggerBinding(Group, "toggleWindow", () =>
            {
                var window = OverlayWindow.Current;
                if (window == null) return;
                window.ToggleWindow();
                windowOpen.Update(window.IsOpen);
            }));
        }

        protected override void OnUpdate()
        {
            UpdateInputBarrier();
            // Keep the selected state in sync when the window's close button is used.
            windowOpen.Update(OverlayWindow.Current != null && OverlayWindow.Current.IsOpen);
            var model = OverlayWindow.Current;
            selectionBox.Update(model!=null&&model.IsOpen?model.SelectionBoxJson:"null");
            if (model != null && model.IsOpen && UnityEngine.Time.realtimeSinceStartup >= nextRefresh)
            {
                nextRefresh = UnityEngine.Time.realtimeSinceStartup + 0.1f;
                state.Update(model.GetStateJson());
                preview.Update(model.PreviewDataUrl);
                catalog.Update(model.CatalogJson);
                library.Update(model.LibraryJson);
            }
            base.OnUpdate();
        }

        void UpdateInputBarrier()
        {
            if (OverlayWindow.Current == null || !OverlayWindow.Current.IsOpen) pointerOverPanel = false;
            bool selecting=OverlayWindow.Current?.IsOpen==true && OverlayWindow.Current.Selecting;
            // Selection owns tool clicks, not camera navigation on the map.
            cameraBarrier.blocked = pointerOverPanel;
            toolBarrier.blocked = pointerOverPanel || selecting;
        }

        internal static void ReleaseInput()
        {
            if (instance == null) return;
            instance.pointerOverPanel = false;
            instance.cameraBarrier.blocked = false;
            instance.toolBarrier.blocked = false;
        }

        protected override void OnDestroy()
        {
            cameraBarrier?.Dispose();
            toolBarrier?.Dispose();
            if (instance == this) instance = null;
            base.OnDestroy();
        }
    }
}
