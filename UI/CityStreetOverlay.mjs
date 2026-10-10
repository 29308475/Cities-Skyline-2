// Uses the game's React and UI controls; no duplicate React runtime.
const R = window.React;
const { createElement: h, useState, useEffect, useRef, useMemo } = R;
const { bindValue, useValue, trigger } = window["cs2/api"];
const { Button, Tooltip, Scrollable } = window["cs2/ui"];
// These controls are registered game modules, not exports of cs2/ui.
// Resolve during rendering so failures stay inside our error boundary.
const TextInput = props => h(window["cs2/modding"].getModule("game-ui/common/input/text/text-input.tsx", "TextInput"), props);
const Slider = props => h(window["cs2/modding"].getModule("game-ui/common/input/slider/slider.tsx", "Slider"), props);
function RoadIcon({ className }) {
    return h("span", { className: "cso-road-icon " + className, "aria-hidden": true },
        h("span", { className: "cso-icon-road cso-icon-road-a" }),
        h("span", { className: "cso-icon-road cso-icon-road-b" }),
        h("span", { className: "cso-icon-road cso-icon-road-c" }),
        h("span", { className: "cso-icon-node cso-icon-node-a" }),
        h("span", { className: "cso-icon-node cso-icon-node-b" }));
}
const open$ = bindValue("CityStreetOverlay", "windowOpen", false);
const state$ = bindValue("CityStreetOverlay", "state", "{}");
const preview$ = bindValue("CityStreetOverlay", "preview", "");
const catalog$ = bindValue("CityStreetOverlay", "catalog", "[]");
const library$ = bindValue("CityStreetOverlay", "library", "[]");
const send = (action, payload = {}) => trigger("CityStreetOverlay", "command", JSON.stringify({ action, ...payload }));
const pointer = value => trigger("CityStreetOverlay", "pointerOverPanel", value);
const groups = ["Autobahnen", "Hauptstraßen", "Wohn-/Nebenstraßen", "Fuß-/Radwege"];
const colors = ["#ff8c37", "#ffdc41", "#46ebff", "#6eeb73"];
const num = (v, d = 1) => Number(v || 0).toFixed(d).replace(".", ",");
const parse = (text, fallback) => { try { return JSON.parse(text); } catch (_) { return fallback; } };
const hint = text => h("div", { className: "cso-hint" }, text);
const btn = (text, onSelect, props = {}) => h(Button, { ...props, className: "cso-button " + (props.className || ""), onSelect }, text);
const swatch = i => h("span", { className: "cso-swatch", style: { backgroundColor: colors[i] } });

export function clampWindow(rect, width, height) {
    const aw = Math.max(280, width - 16), ah = Math.max(280, height - 16);
    const w = Math.max(Math.min(660, aw), Math.min(rect.w, aw));
    const h = Math.max(Math.min(480, ah), Math.min(rect.h, ah));
    return { x: Math.max(8, Math.min(rect.x, width-w-8)), y: Math.max(8, Math.min(rect.y, height-h-8)), w, h };
}

function AngleInput({ value, disabled, onChange }) {
    const [text,setText] = useState(num(value,1));
    useEffect(()=>setText(num(value,1)),[value]);
    const commit=()=>{
        const normalized=text.trim().replace(",", ".");
        if(!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(normalized)||!Number.isFinite(Number(normalized))){setText(num(value,1));return;}
        const angle=Math.max(-180,Math.min(180,Math.round(Number(normalized)*10)/10));
        setText(num(angle,1));if(angle!==value)onChange(angle);
    };
    return h("span",{className:"cso-angle-value"},h(TextInput,{className:"cso-angle-input",value:text,disabled,"aria-label":"Drehung in Grad",maxLength:12,
        onChange:e=>setText(e.target.value),onBlur:commit,onKeyDown:e=>{if(e.key==="Enter"||e.keyCode===13)e.currentTarget.blur();}}),h("span",null,"°"));
}
function Range({ label, value, min, max, step = 1, unit = "", decimals = 1, disabled = false, editableAngle = false, onChange }) {
    const [draft, setDraft] = useState(value);
    useEffect(() => setDraft(value), [value]);
    return h("div", { className: "cso-range" + (disabled ? " cso-disabled" : "") },
        h("div", { className: "cso-label-row" }, h("span", null, label), editableAngle?h(AngleInput,{value:draft,disabled,onChange:v=>{setDraft(v);onChange(v);}}):h("span", { className: "cso-value" }, num(draft,decimals)+unit)),
        h(Slider, { className: "cso-slider", value: draft, start: min, end: max, disabled,
            valueTransformer: (start,end,f) => Math.max(start,Math.min(end,Math.round((start+f*(end-start))/step)*step)),
            onChange: v => { const rounded=Math.max(min,Math.min(max,Math.round(v/step)*step));setDraft(rounded);onChange(rounded); } }));
}
function Section({ title, detail, expanded, onToggle, children }) {
    return h("section", { className: "cso-section" },
        h(Button, { className: "cso-section-toggle", onSelect: onToggle, "aria-expanded": expanded },
            h("span", { className: "cso-chevron" + (expanded ? " cso-chevron-open" : ""), "aria-hidden": true }),
            h("span", { className: "cso-section-title" }, title), h("span", { className: "cso-section-detail" }, detail)),
        expanded && h("div", { className: "cso-section-content" }, children));
}
function NestedScrollable(props) {
    const boundary = useRef(null);
    useEffect(() => {
        const element = boundary.current;
        // Game Scrollable handles wheel on its content with a native listener.
        // Stop bubbling AFTER that handler, BEFORE the outer Scrollable sees it.
        const contain = event => { event.preventDefault(); event.stopPropagation(); };
        element.addEventListener("wheel", contain, { passive: false });
        return () => element.removeEventListener("wheel", contain);
    }, []);
    return h("div", { ref: boundary }, h(Scrollable, props));
}
function CityControls({ s }) {
    const [query,setQuery] = useState(s.query || "");
    const [offsets,setOffsets] = useState(false);
    useEffect(() => setQuery(s.query || ""), [s.query]);
    const disabled = s.busy || !s.canSearch;
    const search = () => { if (!disabled && query.trim().length >= 2) send("search", { query }); };
    return h("div", { className: "cso-city" },
        h("div", { className: "cso-search-row" },
            h(TextInput, { className: "cso-input", value: query, placeholder: "Stadt oder Ort suchen …", maxLength: 120, disabled,
                onChange: e => setQuery(e.target.value), onKeyDown: e => { if (e.key === "Enter" || e.keyCode === 13) search(); } }),
            btn("Suchen", search, { disabled: disabled || query.trim().length < 2 })),
        (s.places || []).length > 0 && h(s.places.length > 4 ? NestedScrollable : "div", { className: "cso-results", style: s.places.length > 4 ? { height: "144px" } : undefined },
            s.places.map((place,index) => h(Button, { key:index, className:"cso-place"+(s.selected===place?" cso-active":""), disabled:s.busy,
                onSelect:() => send("selectPlace",{index}) },place))),
        s.selected && h(R.Fragment,null,
            !(s.places || []).includes(s.selected) && h("div",{className:"cso-selected-place"},s.selected),
            h(Range,{label:"Ausschnitt",value:s.sizeKm,min:0.5,max:10,step:0.1,unit:" km",disabled:s.busy,onChange:value=>send("area",{field:"sizeKm",value})}),
            btn(h(R.Fragment,null,h("span",null,offsets?"Mittelpunkt ausblenden":"Mittelpunkt verschieben"),h("span",{className:"cso-chevron"+(offsets?" cso-chevron-open":"")})),()=>setOffsets(!offsets),{className:"cso-subtle cso-disclosure"}),
            offsets && h(R.Fragment,null,
                h(Range,{label:"Ost / West",value:s.eastKm,min:-10,max:10,step:0.1,unit:" km",disabled:s.busy,onChange:value=>send("area",{field:"eastKm",value})}),
                h(Range,{label:"Nord / Süd",value:s.northKm,min:-10,max:10,step:0.1,unit:" km",disabled:s.busy,onChange:value=>send("area",{field:"northKm",value})})),
            btn(s.busy?"Wird geladen …":s.tileCount>1?"Neue Sammlung laden":"Straßenvorschau laden",()=>send("loadPreview"),{className:"cso-primary cso-full",disabled:s.busy})));
}
function RecognitionControls({s}) {
    useEffect(()=>()=>send("hoverRoadGroup",{group:null}),[]);
    return h("div",{className:"cso-recognition"},
        btn("Straßen erkennen",()=>send("recognizeRoads"),{disabled:!s.map||s.busy||s.stampBusy}),
        (s.roadSuggestions||[]).length>0&&h(R.Fragment,null,
            hint("Nur passende Standardstraßen werden vorgeschlagen. Nicht zugeordnete Abschnitte behalten ihre Kategorienstraße und müssen manuell geprüft werden. Spurzahl aus Straßenbezeichnung; Rampen ohne Spurangabe werden als einspurig geschätzt."),
            h(NestedScrollable,{style:{height:"180px"}},s.roadSuggestions.map((g,i)=>h("div",{key:i,className:"cso-subgroup",onMouseEnter:()=>send("hoverRoadGroup",{group:g.id}),onMouseLeave:()=>send("hoverRoadGroup",{group:null})},h("strong",null,g.name),hint(g.road+" · "+num(g.width,1)+" m · "+g.count+" Abschnitte")))),
            btn("Vorschläge übernehmen",()=>send("applyRecognition"),{disabled:s.busy||s.stampBusy})));
}
function RoundaboutControls({s,catalog}) {
    const [open,setOpen]=useState(false),[query,setQuery]=useState(""),[approach,setApproach]=useState(false);
    const options=catalog.filter(o=>!o.isPath&&o.name.toLowerCase().includes(query.toLowerCase()));
    return h("div",{className:"cso-roundabout-controls"},
        btn("Kreisverkehr: "+(s.roundabout?s.roundabout.name:"Straße auswählen"),()=>{setApproach(false);setOpen(!open||approach);},{disabled:s.busy||s.stampBusy,className:"cso-road-choice"}),
        open&&btn("Ein-/Ausfahrten: "+(s.roundaboutApproach?s.roundaboutApproach.name:"Straße auswählen"),()=>{setApproach(true);setOpen(true);},{disabled:s.busy||s.stampBusy,className:"cso-road-choice"}),
        open&&h("div",{className:"cso-picker"},
            hint(s.leftTraffic?"Linksverkehr · im Uhrzeigersinn":"Rechtsverkehr · gegen den Uhrzeigersinn"),
            hint("Einspurige Einbahnstraße empfohlen. Freie Auswahl; Zweirichtungsstraßen behalten Gegenverkehr. Getrennte Ein-/Ausfahrten erhalten eine eigene Straße bis zur nächsten Verzweigung. Diese Auswahl hat bei erkannten getrennten Ein-/Ausfahrten Vorrang vor Unterkategorien. Zweirichtungszufahrten bleiben unverändert."),
            h(TextInput,{className:"cso-input",value:query,placeholder:"Straße suchen …",onChange:e=>setQuery(e.target.value)}),
            h(NestedScrollable,{className:"cso-road-list",style:{height:"170px"}},options.map(o=>h(Button,{key:o.id,className:"cso-road-option",onSelect:()=>{send(approach?"roundaboutApproachRoad":"roundaboutRoad",{id:o.id});setOpen(false);}},o.name)))));
}
function RoadControls({ s, catalog, onlyType }) {
    const [picker,setPicker] = useState(-1), [query,setQuery] = useState(""), [all,setAll] = useState(false);
    if (!s.map) return hint("Lade zuerst einen Stadtausschnitt, um Straßen auszuwählen.");
    return h(R.Fragment,null,
        !s.map.typed && hint("Ältere Vorlage ohne Straßentypen. Die Breite gilt für alle Linien."),
        (onlyType!==undefined?[onlyType]:s.map.typed?[0,1,2,3]:[2]).map(type => {
            const p=s.profiles[type], visible=(s.filter&(1<<type))!==0;
            const options=catalog.filter(o=>(all||(type===3?o.isPath:type===0?o.isHighway:!o.isPath&&!o.isHighway))&&o.name.toLowerCase().includes(query.toLowerCase()));
            return h("div",{className:"cso-road",key:type},
                h("div",{className:"cso-road-header"},
                    h(Button,{className:"cso-road-toggle",disabled:!s.map.typed,onSelect:()=>send("filter",{type,value:!visible})},
                        h("span",{className:"cso-check"+(visible?" cso-checked":"")},visible&&h("span",{className:"cso-tick"})),swatch(type),h("span",null,s.map.typed?groups[type]:"Alle Straßen")),
                    h("span",{className:"cso-value"},num(p.width,2)+" m")),
                btn(h(R.Fragment,null,h("span",null,p.assigned?p.name:"Spielstraße auswählen"),h("span",{className:"cso-chevron"+(picker===type?" cso-chevron-open":"")})),()=>{setPicker(picker===type?-1:type);setQuery("");setAll(false);},{className:"cso-road-choice"}),
                onlyType!==undefined&&h(Range,{label:"Deckkraft",value:(p.opacity===undefined?1:p.opacity)*100,min:0,max:100,step:1,unit:" %",decimals:0,onChange:value=>send("categoryOpacity",{type,value:value/100})}),
                onlyType!==undefined&&btn("Bauen",()=>send("stampRoad",{type}),{className:"cso-primary cso-stamp-button",disabled:s.busy||s.stampBusy||(s.categoryReady?!s.categoryReady[type]:!p.assigned||!p.available)||!s.visible||!visible||s.selectionCount>0}),
                p.assigned&&!p.available&&hint("Nicht verfügbar · gespeicherte Breite bleibt erhalten."),
                picker===type&&h("div",{className:"cso-picker"},
                    h(TextInput,{className:"cso-input",value:query,placeholder:"Spielstraße suchen …",maxLength:100,onChange:e=>setQuery(e.target.value)}),
                    btn("Alle Straßen und Wege",()=>setAll(!all),{className:"cso-subtle"+(all?" cso-active":""),"aria-pressed":all}),
                    h(NestedScrollable,{className:"cso-road-list",style:{height:"170px"}},options.length===0?hint("Keine Treffer. Suche ändern oder alle Straßen anzeigen."):
                        options.map(o=>h(Button,{className:"cso-road-option",key:o.id,onSelect:()=>{send("selectRoad",{type,id:o.id});setPicker(-1);}},h("span",null,o.name),h("span",{className:"cso-value"},num(o.width,2)+" m")))),
                    p.assigned?btn("Breite manuell einstellen",()=>send("manualWidth",{type,value:p.width}),{className:"cso-subtle"}):
                        h(Range,{label:"Manuelle Breite",value:p.width,min:0.5,max:128,step:0.5,unit:" m",onChange:value=>send("manualWidth",{type,value})})));
        }),
        s.map.typed&&s.filter===0&&hint("Alle Straßentypen sind ausgeblendet."),
        onlyType===undefined&&h(RoundaboutControls,{s,catalog}),

        onlyType===undefined&&h(Tooltip,{tooltip:"Spielstraße auswählen, um ihre Breite zu übernehmen. Für getrennte Autobahnlinien eine Fahrtrichtung wählen."},h("div",{className:"cso-catalog-info"},s.catalogStatus)));
}
const selectionBox$ = bindValue("CityStreetOverlay", "selectionBox", "null");
function SelectionSurface() {
    const box=parse(useValue(selectionBox$),null);
    return h("div",{className:"cso-selection-surface","aria-hidden":true},box&&h("div",{className:"cso-selection-box",style:{
        left:Math.min(box.x0,box.x1)*100+"%",top:Math.min(box.y0,box.y1)*100+"%",
        width:Math.abs(box.x1-box.x0)*100+"%",height:Math.abs(box.y1-box.y0)*100+"%"}}));
}
function SubgroupEditor({s,catalog,group,close}) {
    const [name,setName]=useState(group?.name||"Auf-/Abfahrten"),[type,setType]=useState(group?.category||0);
    const [query,setQuery]=useState(""),[id,setId]=useState(group?.prefabId||"");
    const options=catalog.filter(o=>o.name.toLowerCase().includes(query.toLowerCase()));
    return h("div",{className:"cso-subgroup-editor"},h("strong",null,group?"Spielstraße der Untergruppe":"Markierung als Untergruppe"),
        !group&&h(R.Fragment,null,h(TextInput,{className:"cso-input",value:name,placeholder:"Name der Untergruppe",maxLength:60,onChange:e=>setName(e.target.value)}),
            h("div",{className:"cso-subgroup-categories"},groups.map((label,i)=>btn(label,()=>setType(i),{key:i,className:type===i?"cso-active":"","aria-pressed":type===i})))),
        h(TextInput,{className:"cso-input",value:query,placeholder:"Spielstraße für Untergruppe suchen",onChange:e=>setQuery(e.target.value)}),
        h(NestedScrollable,{className:"cso-subgroup-options",style:{height:"220px"}},options.map(o=>btn(o.name+" · "+num(o.width,2)+" m",()=>setId(o.id),{key:o.id,className:id===o.id?"cso-active":"","aria-pressed":id===o.id}))),
        btn(group?"Spielstraße übernehmen":"Untergruppe anlegen",()=>{send(group?"subgroupRoad":"createSubgroup",{group:group?.id,id,name,type});close();},{className:"cso-primary",disabled:s.busy||s.stampBusy||!id||(!group&&!s.selectionCount)}),
        btn("Abbrechen",close));
}
function SubgroupRoadPicker({s,catalog,group}) {
    const [open,setOpen]=useState(false),[query,setQuery]=useState(""),[all,setAll]=useState(false);
    const type=group.category;
    const options=catalog.filter(o=>(all||(type===3?o.isPath:type===0?o.isHighway:!o.isPath&&!o.isHighway))&&o.name.toLowerCase().includes(query.toLowerCase()));
    return h("div",{className:"cso-subgroup-road-picker"},
        btn(h(R.Fragment,null,h("span",null,group.roadName||"Spielstraße auswählen"),h("span",{className:"cso-chevron"+(open?" cso-chevron-open":"")})),()=>{setOpen(!open);setQuery("");setAll(false);},{className:"cso-road-choice",disabled:s.busy||s.stampBusy,"aria-expanded":open}),
        !group.available&&hint("Spielstraße nicht verfügbar"),
        open&&h("div",{className:"cso-picker"},
            h(TextInput,{className:"cso-input",value:query,placeholder:"Spielstraße suchen …",maxLength:100,onChange:e=>setQuery(e.target.value)}),
            btn("Alle Straßen und Wege",()=>setAll(!all),{className:"cso-subtle"+(all?" cso-active":""),"aria-pressed":all}),
            h(NestedScrollable,{className:"cso-road-list",style:{height:"170px"}},options.length===0?hint("Keine Treffer. Suche ändern oder alle Straßen anzeigen."):
                options.map(o=>h(Button,{key:o.id,className:"cso-road-option"+(group.prefabId===o.id?" cso-active":""),disabled:s.busy||s.stampBusy,"aria-pressed":group.prefabId===o.id,onSelect:()=>{send("subgroupRoad",{group:group.id,id:o.id});setOpen(false);}},h("span",null,o.name),h("span",{className:"cso-value"},num(o.width,2)+" m"))))));
}
function SubgroupTree({s,type,catalog}) {
    useEffect(()=>()=>send("hoverRoadGroup",{group:null}),[]);
    const [open,setOpen]=useState(false),[expanded,setExpanded]=useState(null);
    const treeRef=useRef(null);
    useEffect(()=>{if(s.focusCategory===type&&s.focusGroup){setOpen(true);setExpanded(s.focusGroup);}},[s.focusRevision]);
    useEffect(()=>{if(open&&expanded===s.focusGroup){try{treeRef.current?.querySelector(".cso-tree-focused")?.scrollIntoView({block:"nearest"});}catch(e){}}},[open,expanded,s.focusRevision]);
    const items=(s.subgroups||[]).filter(g=>g.category===type);
    useEffect(()=>{if(!open)send("hoverRoadGroup",{group:null});},[open]);
    if(!items.length)return null;
    return h("div",{className:"cso-subgroup-tree",ref:treeRef},
        btn(h(R.Fragment,null,h("span",{className:"cso-chevron"+(open?" cso-chevron-open":"")}),"Unterkategorien ("+items.length+")"),()=>setOpen(!open),{className:"cso-tree-heading","aria-expanded":open}),
        open&&h("div",{className:"cso-tree-children"},items.map(g=>h("div",{key:g.id,className:"cso-tree-item",onMouseEnter:()=>send("hoverRoadGroup",{group:g.id}),onMouseLeave:()=>send("hoverRoadGroup",{group:null})},
            btn(h(R.Fragment,null,h("span",{className:"cso-chevron"+(expanded===g.id?" cso-chevron-open":"")}),h("span",{className:"cso-tree-name",title:g.name},g.name),h("span",{className:"cso-value"},g.count)),()=>setExpanded(expanded===g.id?null:g.id),{className:"cso-tree-row"+(s.focusGroup===g.id?" cso-tree-focused":""),"aria-expanded":expanded===g.id}),
            expanded===g.id&&h("div",{className:"cso-tree-details"},h(SubgroupRoadPicker,{s,catalog,group:g}),
                btn("Zuordnung aufheben",()=>send("deleteSubgroup",{group:g.id}),{disabled:s.busy||s.stampBusy}))))));
}
function MiniRoadWindows({s,catalog,restore,block}) {
    const [editing,setEditing]=useState(null),[actions,setActions]=useState(false),[settings,setSettings]=useState(false);
    useEffect(()=>{if(s.focusCategory>=0&&!s.focusGroup){try{document.querySelector(".cso-category-focused")?.scrollIntoView({block:"nearest"});}catch(e){}}},[s.focusRevision]);
    return h("div",{className:"cso-input-root cso-mini-dock",onMouseEnter:()=>block(true),onMouseMove:()=>block(true),onMouseLeave:()=>block(false),
        onMouseDown:e=>e.stopPropagation(),onClick:e=>e.stopPropagation(),onDoubleClick:e=>e.stopPropagation(),onWheel:e=>{block(true);e.stopPropagation();},onContextMenu:e=>{e.preventDefault();e.stopPropagation();}},
        h("div",{className:"cso-mini-toolbar"},btn("Großes Fenster",restore),btn("×",()=>{block(false);send("close");},{className:"cso-mini-close","aria-label":"Detailfenster schließen"})),
        h(Scrollable,{className:"cso-mini-scroll",style:{flex:"1 1 0px",minHeight:0}},groups.map((name,type)=>h("div",{key:type,"data-category":type,className:"cso-mini-card"+(s.focusCategory===type?" cso-category-focused":""),style:{borderTopColor:colors[type]}},
            !s.map||!s.profiles||(!s.map.typed&&type!==2)?h(R.Fragment,null,h("strong",null,name),hint(s.map?"Keine eigenen Typdaten in dieser älteren Vorlage.":"Zuerst eine Vorlage im großen Fenster laden.")):
            h(R.Fragment,null,h(RoadControls,{s,catalog,onlyType:type}),
                h(SubgroupTree,{s,type,catalog}))))),
        h("div",{className:"cso-mini-footer"},
            btn("Weitere Einstellungen",()=>setSettings(!settings),{"aria-expanded":settings}),
            settings&&h(RoundaboutControls,{s,catalog}),
            settings&&h(RecognitionControls,{s}),
            btn(s.selecting?"Auswahl beenden":"Bereiche markieren",()=>send("selectionMode"),{disabled:!s.map||s.busy||s.stampBusy}),
            s.selectionCount>0&&btn(s.selectionCount+" markiert · Aktionen",()=>setActions(!actions),{"aria-expanded":actions}),
            s.selectionCount>0&&actions&&btn("Als Untergruppe zuordnen",()=>setEditing({create:true}),{disabled:s.busy||s.stampBusy}),
            s.selecting&&actions&&hint("Klick / Rahmen: markieren oder abwählen · Alt: nur abwählen"),
            s.selectionCount>0&&actions&&h("div",{className:"cso-selection-actions"},btn("Markierte entfernen",()=>send("removeMarked"),{disabled:s.busy||s.stampBusy}),btn("Markierung aufheben",()=>send("clearSelection"))),
            settings&&s.excludedCount>0&&h(R.Fragment,null,hint(s.excludedCount+" Abschnitte entfernt"),btn("Entfernte wiederherstellen",()=>send("restoreExcluded"),{disabled:s.busy||s.stampBusy})),
            settings&&s.removedDriveways>0&&hint(s.removedDriveways+" markierte Zufahrten herausgefiltert"),
            btn(s.visible?"Alle ausblenden":"Vorlage anzeigen",()=>send("visible",{value:!s.visible}),{disabled:!s.map}),
            s.stampBusy&&btn("Bau abbrechen",()=>send("cancelStamp"),{className:"cso-full"}),
            s.stampStatus&&h("div",{className:"cso-stamp-status"},s.stampStatus),
            settings&&hint("Gesamtdeckkraft: "+num(s.opacity*100,0)+" %. Die Kategorie-Werte wirken zusätzlich.")),
        editing&&h(SubgroupEditor,{key:editing.id||"new",s,catalog,group:editing.create?null:editing,close:()=>setEditing(null)}));
}
function PlacementControls({ s }) {
    if(!s.map)return hint("Lade zuerst eine Vorlage. Danach kannst du sie hier ausrichten.");
    const change=field=>value=>send("placement",{field,value});
    return h("div",{className:"cso-placement"},
        s.locked&&h("div",{className:"cso-notice"},"Position fixiert. Zum Bearbeiten unten entsperren."),
        h("div",{className:"cso-nudge"},[["← 10 m",-10,0],["10 m →",10,0],["↓ 10 m",0,-10],["10 m ↑",0,10]].map(([label,x,z])=>btn(label,()=>send("nudge",{x,z}),{key:label,disabled:s.locked}))),
        h("div",{className:"cso-placement-grid"},
        h(Range,{label:"Ost / West",value:s.offsetX,min:-14000,max:14000,step:10,unit:" m",decimals:0,disabled:s.locked,onChange:change("offsetX")}),
        h(Range,{label:"Nord / Süd",value:s.offsetZ,min:-14000,max:14000,step:10,unit:" m",decimals:0,disabled:s.locked,onChange:change("offsetZ")}),
        h(Range,{label:"Drehung",value:s.rotation,min:-180,max:180,step:1,unit:"°",decimals:1,editableAngle:true,disabled:s.locked,onChange:change("rotation")}),
        h(Range,{label:"Maßstab",value:s.scale,min:0.25,max:2,step:0.01,unit:"×",decimals:2,disabled:s.locked,onChange:change("scale")}),
        h(Range,{label:"Deckkraft",value:s.opacity*100,min:5,max:100,unit:" %",decimals:0,onChange:value=>change("opacity")(value/100)}),
        h("div",{className:"cso-scale-reset"},h(Tooltip,{tooltip:"Reale Abstände wiederherstellen. Die Straßenbreite bleibt unabhängig vom Maßstab gleich."},btn("Abstände 1:1",()=>change("scale")(1),{disabled:s.locked})))));
}

export function previewPan(dx,dy,pixels,size,rotation) {
    const angle=rotation*Math.PI/180, c=Math.cos(angle),sn=Math.sin(angle);
    const meters=size*(Math.abs(c)+Math.abs(sn))/pixels;
    const x=-dx*meters,z=dy*meters;
    return {east:x*c+z*sn,north:-x*sn+z*c};
}
function InteractivePreview({ preview, s, imageSize, onDragging }) {
    const drag=useRef(null),frame=useRef(null);
    const [shift,setShift]=useState({x:0,y:0});
    const radians=(s.rotation||0)*Math.PI/180;
    const fit=1/(Math.abs(Math.cos(radians))+Math.abs(Math.sin(radians)));
    useEffect(()=>{
        const move=e=>{if(!drag.current)return;setShift({x:e.clientX-drag.current.x,y:e.clientY-drag.current.y});e.preventDefault();};
        const end=e=>{
            const d=drag.current;if(!d)return;drag.current=null;setShift({x:0,y:0});
            const dx=e.clientX-d.x,dy=e.clientY-d.y;
            if(Math.abs(dx)+Math.abs(dy)>5)send("panPreview",previewPan(dx,dy,d.width,d.size,d.rotation));
            onDragging(false,!!(e.target&&e.target.closest&&e.target.closest(".cso-input-root")));
        };
        const cancel=()=>{drag.current=null;setShift({x:0,y:0});onDragging(false,false);};
        window.addEventListener("mousemove",move);window.addEventListener("mouseup",end);window.addEventListener("blur",cancel);
        return()=>{window.removeEventListener("mousemove",move);window.removeEventListener("mouseup",end);window.removeEventListener("blur",cancel);if(drag.current)pointer(false);};
    },[]);
    const start=e=>{
        if(e.button!==0||s.busy||!s.map||s.canPan===false)return;
        drag.current={x:e.clientX,y:e.clientY,width:frame.current.getBoundingClientRect().width,size:s.map.sizeMeters,rotation:s.rotation||0};
        onDragging(true);pointer(true);e.preventDefault();e.stopPropagation();
    };
    return h("div",{ref:frame,className:"cso-preview-frame cso-interactive-preview",style:{width:imageSize+"px",height:imageSize+"px"},onMouseDown:start},
        preview?h("img",{className:"cso-preview-image",src:preview,draggable:false,alt:"Straßennetz-Vorschau",style:{transform:"translate("+shift.x+"px,"+shift.y+"px) rotate("+(-(s.rotation||0))+"deg) scale("+fit+")"}}):
            h("div",{className:"cso-empty"},h("strong",null,"Hier beginnt deine Vorlage"),hint("Suche eine Stadt und lade ihren Straßenausschnitt.")),
        h("div",{className:"cso-north",style:{transform:"rotate("+(-(s.rotation||0))+"deg)"}},"↑ N"),
        s.map&&h("div",{className:"cso-pan-caption"},(s.tileCount||1)+" Ausschnitt"+((s.tileCount||1)>1?"e":"")+" · Ziehen zum Erweitern"),
        s.busy&&h("div",{className:"cso-busy"},"Kartendaten werden geladen …"));
}
function TemplatePicker({ entries, busy, status }) {
    const [id,setId]=useState(entries.length?entries[0].Id:"");
    const [deleting,setDeleting]=useState(null);
    const entry=entries.find(e=>e.Id===id)||entries[0];
    return h("div",{className:"cso-library"},
        h("div",{className:"cso-library-heading"},h("div",null,h("div",{className:"cso-title"},"Gespeicherte Vorlagen"),hint("Jeder Speicherstand bleibt als eigene Version erhalten.")),btn("Zurück",()=>send("closeLibrary"))),
        entries.length===0?h("div",{className:"cso-loading"},"Noch keine Vorlagen gespeichert. Lade eine Stadt und wähle Speichern."):
        h("div",{className:"cso-library-body"},
            h(Scrollable,{className:"cso-library-list",style:{height:"100%"}},entries.map(e=>h(Button,{key:e.Id,className:"cso-template-entry"+(entry.Id===e.Id?" cso-active":""),onSelect:()=>setId(e.Id)},
                h("strong",null,e.City),h("span",null,"Version "+e.Version),h("span",{className:"cso-hint"},new Date(e.SavedUtc).toLocaleString("de-DE"))))),
            h("div",{className:"cso-library-detail"},h("h3",null,entry.City+" – Version "+entry.Version),
                entry.Preview?h("img",{className:"cso-saved-preview",src:entry.Preview,alt:"Gespeicherte Straßenvorschau"}):hint("Für diese ältere Vorlage ist noch kein Vorschaubild gespeichert."),
                hint(entry.Location),hint((entry.TileCount||1)+" Ausschnitt(e) mit gemeinsamer Platzierung. Die Vorlage bleibt zunächst ausgeblendet."),
                btn("Diese Vorlage laden",()=>send("loadTemplate",{id:entry.Id}),{className:"cso-primary cso-full",disabled:busy}),
                deleting===entry.Id?h("div",null,hint(entry.City+" – Version "+entry.Version+" endgültig löschen?"),
                    btn("Endgültig löschen",()=>{send("deleteTemplate",{id:entry.Id});setDeleting(null);},{disabled:busy}),btn("Abbrechen",()=>setDeleting(null))):
                    btn("Vorlage löschen",()=>setDeleting(entry.Id),{className:"cso-full",disabled:busy}))),hint(status));
}
function ToolbarButton() {
    const open=useValue(open$);
    return h(Tooltip,{tooltip:"Straßen-Vorlage · City Street Overlay"},h(Button,{
        variant:"floating",selected:open,onSelect:()=>trigger("CityStreetOverlay","toggleWindow")
    },h(RoadIcon,{className:"cso-toolbar-icon"})));
}
function WindowPanel() {
    const open=useValue(open$), json=useValue(state$), preview=useValue(preview$), catalogJson=useValue(catalog$), libraryJson=useValue(library$);
    const entries=useMemo(()=>parse(libraryJson,[]),[libraryJson]);
    const s=useMemo(()=>parse(json,{}),[json]), catalog=useMemo(()=>parse(catalogJson,[]),[catalogJson]);
    const [sections,setSections]=useState({city:true,roads:false,placement:false});
    const [minimized,setMinimized]=useState(false);
    useEffect(()=>{if(s.focusRevision>0)setMinimized(true);},[s.focusRevision]);
    const windowHeight=()=>Math.max(784,window.innerHeight-84-180);
    const initial=()=>clampWindow({x:32,y:84,w:1060,h:windowHeight()},window.innerWidth,window.innerHeight);
    const [rect,setRect]=useState(initial), [moving,setMoving]=useState(false);
    const drag=useRef(null), panDragging=useRef(false), root=useRef(null), rectRef=useRef(rect), over=useRef(false);
    rectRef.current=rect;
    const block=value=>{if(over.current!==value){over.current=value;pointer(value);}};
    useEffect(()=>{
        const move=e=>{
            if(!drag.current)return;
            const d=drag.current, dx=e.clientX-d.x, dy=e.clientY-d.y;
            setRect(clampWindow({...d.rect,x:d.rect.x+dx,y:d.rect.y+dy},window.innerWidth,window.innerHeight));
            e.preventDefault();
        };
        const end=e=>{
            if(!drag.current)return;
            drag.current=null;setMoving(false);
            const hit=e&&typeof e.clientX==="number"?document.elementFromPoint(e.clientX,e.clientY):null;
            block(!!(hit&&root.current&&root.current.contains(hit)));
        };
        const blur=()=>{drag.current=null;setMoving(false);block(false);};
        const resize=()=>setRect(r=>clampWindow({...r,h:windowHeight()},window.innerWidth,window.innerHeight));
        window.addEventListener("mousemove",move);window.addEventListener("mouseup",end);window.addEventListener("blur",blur);window.addEventListener("resize",resize);
        return()=>{window.removeEventListener("mousemove",move);window.removeEventListener("mouseup",end);window.removeEventListener("blur",blur);window.removeEventListener("resize",resize);pointer(false);};
    },[]);
    useEffect(()=>{if(!open){drag.current=null;panDragging.current=false;setMoving(false);block(false);}},[open]);
    const start=e=>{
        if(e.button!==0)return;
        drag.current={x:e.clientX,y:e.clientY,rect:rectRef.current};setMoving(true);block(true);e.preventDefault();e.stopPropagation();
    };
    if(!open)return null;
    if(minimized)return h(R.Fragment,null,s.selecting&&h(SelectionSurface,{s}),h(MiniRoadWindows,{s,catalog,block,restore:()=>{if(s.selecting)send("selectionMode");block(false);setMinimized(false);}}));
    const toggle=key=>setSections(prev=>({city:false,roads:false,placement:false,[key]:!prev[key]}));
    const narrow=rect.w<840;
    const imageSize=Math.max(64,Math.min(rect.w*(narrow?0.40:0.51)-38,rect.h-(narrow?460:440)));
    const close=()=>send("close");
    const header=h("div",{className:"cso-header",onMouseDown:start},
        h(RoadIcon,{className:"cso-app-icon"}),
        h("div",{className:"cso-heading"},h("div",{className:"cso-eyebrow"},"STRASSENVORLAGE"),h("div",{className:"cso-title"},"City Street Overlay")),
        h("div",{className:"cso-header-actions",onMouseDown:e=>e.stopPropagation()},
            h(Tooltip,{tooltip:"Vier Detailfenster am linken Rand"},btn(h("span",{className:"cso-minimize-mark"}),()=>{block(false);send("closeLibrary");setMinimized(true);},{className:"cso-icon-button cso-minimize","aria-label":"Minimieren"})),
            h(Tooltip,{tooltip:"Fenster schließen"},btn("×",close,{className:"cso-icon-button cso-close"}))));
    return h(R.Fragment,null,
        moving&&h("div",{className:"cso-drag-shield",onWheel:e=>{e.preventDefault();e.stopPropagation();}}),
        h("div",{ref:root,className:"cso-input-root"+(narrow?" cso-narrow":"")+(rect.h>=900?" cso-spacious":""),style:{left:rect.x+"px",top:rect.y+"px",width:rect.w+"px",height:rect.h+"px"},
            onMouseEnter:()=>block(true),onMouseMove:()=>block(true),onMouseLeave:()=>{if(!drag.current&&!panDragging.current)block(false);},
            onMouseDown:e=>e.stopPropagation(),onClick:e=>e.stopPropagation(),
            onDoubleClick:e=>e.stopPropagation(),onContextMenu:e=>{e.preventDefault();e.stopPropagation();},
            onWheel:e=>{block(true);e.stopPropagation();}},
            h("div",{className:"cso-root cso-window"+(s.libraryOpen?" cso-library-open":"")},
                s.libraryOpen&&h(TemplatePicker,{entries,busy:s.busy,status:s.status}),
                header, h("div",{className:"cso-panel-content"},
                !s.profiles?h("div",{className:"cso-loading"},"Fenster wird geladen …"):h(R.Fragment,null,
                    h("div",{className:"cso-body"},
                        h("div",{className:"cso-preview-column",style:{width:(imageSize+(narrow?12:18))+"px"}},
                            h("div",{className:"cso-preview-heading"},h("div",{className:"cso-eyebrow"},"VORSCHAU"),h("div",{className:"cso-map-name"},s.map?s.map.name:"Deine nächste Stadt")),
                            h(InteractivePreview,{preview,s,imageSize,onDragging:(value,inside=true)=>{panDragging.current=value;setMoving(value);block(value||inside);}}),
                            h("div",{className:"cso-preview-meta"},h("span",null,s.map?num(s.map.sizeMeters*s.scale/1000,2)+" × "+num(s.map.sizeMeters*s.scale/1000,2)+" km":"Noch kein Ausschnitt"),
                                h("span",{className:"cso-badge"},s.map?"Maßstab "+num(s.scale,2)+"×":"1:1 in Spielmetern")),
                            h("div",{className:"cso-legend"},groups.map((label,i)=>h("div",{key:i,className:s.map&&!(s.filter&(1<<i))?"cso-muted":""},swatch(i),label)))),
                        h(Scrollable,{className:"cso-controls"+(sections.placement?" cso-placement-open":""),style:{height:"100%"}},
                            h(Section,{title:"Stadt & Ausschnitt",detail:s.selected?"Ausgewählt":"Hier starten",expanded:sections.city,onToggle:()=>toggle("city")},h(CityControls,{s})),
                            h(Section,{title:"Straßentypen & Breiten",detail:s.map?"4 Typen":"",expanded:sections.roads,onToggle:()=>toggle("roads")},h(RoadControls,{s,catalog})),
                            h(Section,{title:"Platzierung & Darstellung",detail:s.locked?"Fixiert":"",expanded:sections.placement,onToggle:()=>toggle("placement")},h(PlacementControls,{s})))),
                    h("div",{className:"cso-footer"},
                        h("div",{className:"cso-preview-note"},"Vorschau folgt der Drehung. Ziehen erweitert die Stadt; vorhandene Ausschnitte bleiben erhalten."),
                        h("div",{className:"cso-status-row"},h("span",{className:s.status&&s.status.startsWith("Fehler")?"cso-error":"cso-status"},s.status),h("span",{className:"cso-visibility"},s.visible?"● Auf Karte sichtbar":"○ Vorlage ausgeblendet")),
                        h("div",{className:"cso-actions"},
                            btn(s.visible?"Vorlage ausblenden":"Auf Karte anzeigen",()=>send("visible",{value:!s.visible}),{className:"cso-primary",disabled:!s.map}),
                            btn(s.locked?"Position entsperren":"Position fixieren",()=>send("locked",{value:!s.locked}),{disabled:!s.map,className:s.locked?"cso-active":""}),
                            h("div",{className:"cso-action-space"}),btn("Offline laden",()=>send("restore"),{disabled:s.busy||!s.canRestore}),btn("Speichern",()=>send("save"),{disabled:!s.map||s.busy})),
                        h("div",{className:"cso-bottom-row"},btn("© OpenStreetMap-Mitwirkende · ODbL",()=>send("license"),{className:"cso-attribution"}))))))));
}
class WindowErrorBoundary extends R.Component {
    constructor(props) { super(props); this.state = { failed: false }; }
    static getDerivedStateFromError() { return { failed: true }; }
    componentDidUpdate() {
        if (this.state.failed && !this.props.open) this.setState({ failed: false });
    }
    componentDidCatch(error, info) {
        console.error("[CityStreetOverlay] Window render failed", error, info.componentStack);
        pointer(false);
        send("close");
    }
    render() { return this.state.failed ? null : this.props.children; }
}
function SafeWindow() {
    const open = useValue(open$);
    return h(WindowErrorBoundary, { open }, h(WindowPanel));
}
export default function register(registry) {
    registry.append("GameTopLeft",ToolbarButton);
    registry.append("Game",SafeWindow);
}
export const hasCSS=true;
