using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
#if DOOM_CONVERTER
[assembly: AssemblyTitle("DoomSNESconv")]
[assembly: AssemblyProduct("DoomSNESconv")]
[assembly: AssemblyDescription("PWAD optimization and native SNES Doom campaign converter")]
#else
[assembly: AssemblyTitle("DoomSNESed")]
[assembly: AssemblyProduct("DoomSNESed")]
[assembly: AssemblyDescription("SNES Doom Reality Engine level editor - test build")]
#endif
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.2")]
namespace DoomSNESed {
    public static class Program {
        [STAThread]public static int Main(string[] args) {
            bool automated=args.Length>0 && args[0]=="--ui-smoke-test";
            try {
                if(args.Length>0 && args[0]=="--self-test")return SelfTest.Run(args.Skip(1).ToArray());
                bool capture=args.Length>=3 && args[0]=="--ui-smoke-test";
                string path=args.Length>=2 && (args[0]=="--rom" || capture)?args[1]:args.FirstOrDefault();
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                var form=new EditorForm(path);
                if(capture)form.Shown+=delegate {
                    form.PrepareSmoke(args.Length>3?args[3]:"2d");
                    var timer=new Timer {Interval=500};timer.Tick+=delegate {
                        timer.Stop();try {using(var bitmap=new System.Drawing.Bitmap(form.Width,form.Height)) {form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save(args[2]);}form.Close();}finally{timer.Dispose();}
                    };timer.Start();
                };
                Application.Run(form);return 0;
            } catch(Exception e) {File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"DoomSNESed-error.log"),e.ToString());if(!automated)MessageBox.Show(e.ToString(),"DoomSNESed");return 1;}
        }
    }
}




