using System;
using System.Collections.Generic;
using Game.Rendering;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CityStreetOverlay
{
    public sealed partial class OverlayWindow
    {
        public bool Selecting, SelectionOnly;
        bool selectionDragging,selectionRemoving;
        Vector2 selectionStart,selectionEnd;
        public string SelectionBoxJson => Selecting&&selectionDragging ? Newtonsoft.Json.JsonConvert.SerializeObject(new {
            x0=selectionStart.x,y0=selectionStart.y,x1=selectionEnd.x,y1=selectionEnd.y }) : "null";
        void UpdateSelectionInput()
        {
            var input=Game.Input.InputManager.instance;
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(!IsOpen||!Visible||mouse==null||input==null||!Application.isFocused||
                !input.controlOverWorld||input.hasInputFieldFocus||input.overlayActive||StreetStampTool.Instance?.Building==true)
            {selectionDragging=false;return;}
            if(!Selecting&&!(World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Tools.ToolSystem>().activeTool is Game.Tools.DefaultToolSystem)){selectionDragging=false;return;}
            if(mouse.rightButton.wasPressedThisFrame)
            {Selecting=false;selectionDragging=false;return;}
            var screen=input.mousePosition;
            var point=new Vector2(Mathf.Clamp01(screen.x/Screen.width),1-Mathf.Clamp01(screen.y/Screen.height));
            if(mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame){selectionStart=point;selectionDragging=true;selectionRemoving=mouse.rightButton.wasPressedThisFrame;}
            if(!selectionDragging)return;
            selectionEnd=point;
            if(selectionRemoving?mouse.rightButton.wasReleasedThisFrame:mouse.leftButton.wasReleasedThisFrame)
            {
                selectionDragging=false;
                var keyboard=UnityEngine.InputSystem.Keyboard.current;
                bool remove=selectionRemoving||(keyboard!=null&&(keyboard.leftAltKey.isPressed||keyboard.rightAltKey.isPressed));
                if(Selecting||Vector2.Distance(Vector2.Scale(selectionStart,new Vector2(Screen.width,Screen.height)),Vector2.Scale(selectionEnd,new Vector2(Screen.width,Screen.height)))<6)
                    SelectScreen(selectionStart.x,selectionStart.y,selectionEnd.x,selectionEnd.y,remove,!Selecting);
            }
            else if(!(selectionRemoving?mouse.rightButton.isPressed:mouse.leftButton.isPressed))selectionDragging=false;
        }
        public readonly HashSet<int> SelectedRoads=new HashSet<int>();
        public readonly HashSet<string> ExcludedRoads=new HashSet<string>();
        public void RemoveMarkedRoads()
        {
            CheckSelection();
            if(selectionMap==null||StreetStampTool.Instance?.Building==true)return;
            foreach(int i in SelectedRoads)ExcludedRoads.Add(RoadExclusions.Key(selectionMap.Roads[i]));
            SelectedRoads.Clear();SelectionOnly=false;InvalidateRoadPlans();
            StampStatus=ExcludedRoads.Count+" Abschnitte aus der Vorlage entfernt.";
        }
        StreetMap selectionMap;
        public void CheckSelection()
        {
            var current=RenderMap??Map;
            if(ReferenceEquals(current,selectionMap))return;
            selectionMap=current;FocusedGroup=null;FocusedCategory=-1;hoverBatches=null;SelectedRoads.Clear();SelectionOnly=false;Selecting=false;
        }
        public void SelectScreen(float x0,float y0,float x1,float y1,bool remove,bool inspect=false)
        {
            CheckSelection();
            if((!Selecting&&!inspect)||!Visible||Opacity<=0||selectionMap==null||StreetStampTool.Instance?.Building==true)return;
            var world=World.DefaultGameObjectInjectionWorld;
            var camera=world.GetOrCreateSystemManaged<CameraUpdateSystem>().activeCamera;
            var heights=world.GetOrCreateSystemManaged<TerrainSystem>().GetHeightData();
            if(camera==null||!heights.heights.IsCreated)return;
            var from=new Vector2(x0*Screen.width,(1-y0)*Screen.height);
            var to=new Vector2(x1*Screen.width,(1-y1)*Screen.height);
            bool click=Vector2.Distance(from,to)<6;
            float nearest=12;int hit=-1;var hits=new HashSet<int>();
            var lo=Vector2.Min(from,to);var hi=Vector2.Max(from,to);
            float radians=Rotation*Mathf.Deg2Rad,sn=Mathf.Sin(radians),cs=Mathf.Cos(radians);
            var aligned=HighwayAlignment.Prepare(selectionMap,Subgroups,WidthProfiles,Scale,LeftTraffic,RoadExclusions.Apply(selectionMap,ExcludedRoads));
            for(int i=0;i<selectionMap.Roads.Count;i++)
            {
                if(ExcludedRoads.Contains(RoadExclusions.Key(selectionMap.Roads[i])))continue;
                var group=GroupFor(selectionMap.Roads[i]);
                var type=group!=null?(RoadType)group.Category:selectionMap.HasRoadTypes?selectionMap.Types[i]:RoadType.LocalRoad;
                if(!RoadTypes.IsVisible(Filter,type)||WidthProfiles[(int)type].Opacity<=0)continue;
                Vector3 Project(Point p)
                {
                    var v=new float3(OffsetX+(float)(p.X*cs-p.Z*sn)*Scale,0,OffsetZ+(float)(p.X*sn+p.Z*cs)*Scale);
                    v.y=TerrainUtils.SampleHeight(ref heights,v)+0.5f;
                    return camera.WorldToScreenPoint((Vector3)v);
                }
                var a=Project(aligned.Roads[i][0]);var b=Project(aligned.Roads[i][1]);
                if(a.z<=0||b.z<=0)continue;
                if(click)
                {
                    Vector2 av=a,bv=b,delta=bv-av;
                    float t=delta.sqrMagnitude<0.001f?0:Mathf.Clamp01(Vector2.Dot(from-av,delta)/delta.sqrMagnitude);
                    float distance=Vector2.Distance(from,av+t*delta);
                    if(distance<nearest){nearest=distance;hit=i;}
                }
                else
                {
                    // Exact line/rectangle clipping, including lines crossing both sides.
                    var p=new Point(a.x-(lo.x+hi.x)/2,a.y-(lo.y+hi.y)/2);
                    var q=new Point(b.x-(lo.x+hi.x)/2,b.y-(lo.y+hi.y)/2);
                    double sx=Math.Max(1,hi.x-lo.x),sy=Math.Max(1,hi.y-lo.y);
                    p.X/=sx;p.Z/=sy;q.X/=sx;q.Z/=sy;
                    if(Geography.Clip(ref p,ref q,0.5))hits.Add(i);
                }
            }
            if(inspect)
            {
                if(hit>=0){var group=GroupFor(selectionMap.Roads[hit]);FocusedGroup=group?.Id;FocusedCategory=group?.Category??(selectionMap.HasRoadTypes?(int)selectionMap.Types[hit]:2);FocusRevision++;hoverBatches=null;}
                else {FocusedGroup=null;FocusedCategory=-1;hoverBatches=null;}
                return;
            }
            if(hit>=0)hits.Add(hit);
            RoadSelection.Toggle(SelectedRoads,RoadSelection.Units(selectionMap,Subgroups,ExcludedRoads),hits,remove);
            SelectionOnly=true;
            StampStatus=SelectedRoads.Count+" Overlay-Abschnitte ausgewählt.";
        }
    }
}
