using System.Web.Script.Serialization;
namespace DoomSNESed {
    public static class ProjectJson {
        // A ROM can fit while decimal JSON pixel/sample arrays exceed 8 MiB.
        // Use one limit for saving, reopening, cloning and optimization.
        public static JavaScriptSerializer Create(){return new JavaScriptSerializer{MaxJsonLength=128*1024*1024,RecursionLimit=128};}
    }
}
