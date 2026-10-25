using System.Collections.Generic;
using System.Linq;
namespace DoomSNESed {
    public static class NativeWallCatalog {
        // DoomFX WallList / TextureList order. Native faces store the table
        // byte offset (twice the texture number), not an exported PNG number.
        static readonly string[] names=("SKY1 SKY2 BIGDOOR2 BIGDOOR7 BROWN1 COMP2 COMPTILE COMPUTE1 DOOR3 _DOORBLU _DOORRED _DOORTRAK _DOORYEL EXITDOOR EXITSIGN FIREWALL GRAY4 GRAYBIG GSTONE1 GSTSATYR GSTVINE2 ICKWALL1 LITE5 MARBFAC3 MARBLE3 METAL1 PIPE4 ROCKRED3 SKINLOW SKULWAL3 SLADWALL STARG2 STARTAN2 STARTAN3 STEP2 STEPLAD1 STONE2 SUPPORT2 SW1BRN2 SW1BRNGN SW1EXIT SW1GARG SW1GRAY SW1GSTON SW1HOT SW1PIPE SW1SLAD SW1WOOD SW2BRN2 SW2BRNGN SW2EXIT SW2GARG SW2GRAY SW2GSTON SW2HOT SW2PIPE SW2SLAD SW2WOOD TEKWALL5 WOOD3 WOOD4 WOOD5 WOODSKUL").Split(' ');
        public static IEnumerable<int> Ids {get{return Enumerable.Range(0,names.Length).Select(i=>i*2);}}
        public static string Name(int id){return id>=0&&id%2==0&&id/2<names.Length?names[id/2]:"Unknown";}
        public static string Label(int id){string name=Name(id);return id+" / "+name+(name.StartsWith("SW1")?" (switch off)":name.StartsWith("SW2")?" (switch on)":"");}
    }
}
