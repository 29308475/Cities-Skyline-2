using System;
using System.IO;
using System.Linq;
using CityStreetOverlay;
using Newtonsoft.Json;

static class TemplateLibraryChecks
{
    static void Assert(bool value) { if (!value) throw new Exception("Template library regression"); }
    public static void Run()
    {
        // Isolated test data only; no access to the player's library.
        var root=Path.Combine(Path.GetTempPath(),"CityStreetOverlay-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var library=new TemplateLibrary(root);
            Assert(library.List().Count==0);
            var saved=new SavedTemplate { Map=new StreetMap { Name="München, Bayern",SizeMeters=2000 },Rotation=-59.4f,Scale=1.2f,Filter=RoadFilter.All,WidthProfiles=RoadWidths.Manual(8),Preview="data:image/png;base64,test" };
            var first=library.Save(saved);var second=library.Save(saved);
            Assert(JsonConvert.DeserializeObject<RoadWidthProfile>("{\"Meters\":3}").Opacity==1);
            saved.WidthProfiles[0].Opacity=0.35f;
            var profilesCopy=JsonConvert.DeserializeObject<RoadWidthProfile[]>(JsonConvert.SerializeObject(saved.WidthProfiles));
            RoadWidths.Validate(profilesCopy);
            Assert(profilesCopy[0].Opacity==0.35f&&profilesCopy[1].Opacity==1);
            profilesCopy[0].Opacity=float.NaN;
            bool opacityRejected=false;try{RoadWidths.Validate(profilesCopy);}catch(FormatException){opacityRejected=true;}Assert(opacityRejected);
            Assert(first.City=="München"&&first.Version==1&&second.Version==2&&first.Id!=second.Id);
            var restart=new TemplateLibrary(root);
            Assert(restart.List().Count==2&&restart.List()[0].Version==2);
            Assert(restart.Load(first.Id).Version==1&&restart.Load(first.Id).Rotation==-59.4f&&restart.Load(first.Id).WidthProfiles[0].Meters==8);
            Assert(restart.Load(first.Id).Preview==saved.Preview);
            saved.Map.Name="Berlin, Deutschland";Assert(restart.Save(saved).Version==1);
            saved.Map.Name="MÜNCHEN, Bayern";Assert(restart.Save(saved).Version==3);
            bool rejected=false;try { restart.Load("../last-template.json"); } catch(ArgumentException) { rejected=true; }Assert(rejected);
            var legacy=Path.Combine(root,"last-template.json");
            File.WriteAllText(legacy,JsonConvert.SerializeObject(new SavedTemplate { Map=new StreetMap {Name="Odelzhausen, Bayern",SizeMeters=2000} }));
            var legacyText=File.ReadAllText(legacy);
            Assert(restart.List().Any(e=>e.Id=="legacy"&&e.Version==1));
            var old=restart.Load("legacy");Assert(restart.Save(old).Version==2);Assert(File.ReadAllText(legacy)==legacyText);
            File.WriteAllText(Path.Combine(root,"Templates",Guid.NewGuid().ToString("N")+".json"),"broken");
            Assert(restart.List().Count==6&&restart.UnreadableCount==1);
            var untouched=File.ReadAllText(Path.Combine(root,"Templates",second.Id+".json"));
            restart.Delete(first.Id);
            Assert(!restart.List().Any(e=>e.Id==first.Id));
            Assert(File.ReadAllText(Path.Combine(root,"Templates",second.Id+".json"))==untouched);
            rejected=false;try{restart.Delete("../last-template.json");}catch(ArgumentException){rejected=true;}Assert(rejected&&File.Exists(legacy));
            restart.Delete("legacy");Assert(!File.Exists(legacy));
            Assert(restart.List().Count==4);
            Console.WriteLine("PASS: template library versions, independent cities, restart, immutable snapshots, legacy preservation, path validation and corrupt entry isolation");
        }
        finally { Directory.Delete(root,true); }
    }
}
