using System;
using System.Drawing;
using System.Reflection;
namespace DoomSNESed {
    public static class ConverterBranding {
        public static Icon LoadIcon(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("DoomSNESconv.Icon.ico")){if(stream!=null)using(var icon=new Icon(stream))return (Icon)icon.Clone();}return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);}
        public static Image LoadLogo(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("DoomSNESconv.Logo.png")){if(stream==null)return null;using(var image=Image.FromStream(stream))return new Bitmap(image);}}
    }
}
