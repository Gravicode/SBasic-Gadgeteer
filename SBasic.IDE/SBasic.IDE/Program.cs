using Gadgeteer.Modules.GHIElectronics;
using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;
using Skewworks.Labs;
using GHI.Glide;
using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using System.IO;
using System.Text;

namespace System.Diagnostics
{
    public enum DebuggerBrowsableState
    {
        Collapsed,
        Never,
        RootHidden
    }
}
namespace SBasic.IDE
{
    #region Forms
    public class Screen
    {
        public enum ScreenTypes { Splash = 0, Editor, Executor };
        public delegate void GoToFormEventHandler(ScreenTypes form, params string[] Param);
        public event GoToFormEventHandler FormRequestEvent;
        protected void CallFormRequestEvent(ScreenTypes form, params string[] Param)
        {
            // Event will be null if there are no subscribers
            if (FormRequestEvent != null)
            {
                FormRequestEvent(form, Param);
            }
        }
        protected GHI.Glide.Display.Window MainWindow { set; get; }
        public virtual void Init(params string[] Param)
        {
            //do nothing
        }

        public Screen(ref GHI.Glide.Display.Window window)
        {
            MainWindow = window;
        }
    }

    public class ExecutorForm : Screen
    {
        int LineCounter
        {
            set; get;
        }
        const int WidthScreen = 320;
        const int HeightScreen = 170;
        const int LineSpacing = 20;
        private Gadgeteer.Modules.GHIElectronics.DisplayTE35 displayTE35;
        GHI.Glide.UI.Image imgCode { set; get; }
        ArrayList LinesOfCode;
        Bitmap bmp = null;
        Font myFont;
        GHI.Glide.UI.Button btnBack { set; get; }

        public ExecutorForm(ref GHI.Glide.Display.Window window, ref Gadgeteer.Modules.GHIElectronics.DisplayTE35 displayTE35) : base(ref window)
        {
            this.displayTE35 = displayTE35;
        }
        public override void Init(params string[] Param)
        {
            LinesOfCode = new ArrayList();
            LineCounter = 0;
            bmp = new Bitmap(WidthScreen, HeightScreen);
            MainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.ExecuteForm));

            imgCode = (GHI.Glide.UI.Image)MainWindow.GetChildByName("imgCode");
            btnBack = (GHI.Glide.UI.Button)MainWindow.GetChildByName("btnBack");

            Glide.MainWindow = MainWindow;

            myFont = Resources.GetFont(Resources.FontResources.NinaB);
            SBASIC s = new SBASIC();

            Thread.Sleep(500);

            s.Print += S_Print;
            s.ClearScreen += S_ClearScreen;
            btnBack.PressEvent += (sender) =>
            {
                s.Print -= S_Print;
                s.ClearScreen -= S_ClearScreen;
                CallFormRequestEvent(ScreenTypes.Editor);
            };
            //execute the code
            s.Run(Param[0]);
            //MainWindow.Invalidate();
        }

        private void S_ClearScreen(SBASIC sender)
        {
            bmp.DrawRectangle(Color.Black, 0, 0, 0, WidthScreen, HeightScreen, 0, 0, Color.Black, 0, 0, Color.Black, 0, 0, 100);
            imgCode.Bitmap = bmp;
            imgCode.Invalidate();
        }

        private void S_Print(SBASIC sender, string value)
        {

            bmp.Clear();
            //clean screen
            bmp.DrawRectangle(Color.Black, 0, 0, 0, WidthScreen, HeightScreen, 0, 0, Color.Black, 0, 0, Color.Black, 0, 0, 100);

            if (LineCounter > 7)
            {
                //reset
                LineCounter = 0;
                LinesOfCode.Clear();
            }
            LinesOfCode.Add(value);
            for (int i = 0; i <= LineCounter; i++)
            {
                bmp.DrawText(LinesOfCode[i].ToString(), myFont, GT.Color.Green, 5, 5 + (i * LineSpacing));
            }
            bmp.Flush();
            imgCode.Bitmap = bmp;
            imgCode.Invalidate();
            LineCounter++;
            Thread.Sleep(200);
        }
    }
    public class EditorForm : Screen
    {
        private Gadgeteer.Modules.GHIElectronics.SDCard sdCard;
        private Gadgeteer.Modules.GHIElectronics.DisplayTE35 displayTE35;
        GHI.Glide.UI.Dropdown cmbFile { set; get; }
        GHI.Glide.UI.TextBox txtCode { set; get; }
        GHI.Glide.UI.List listFile { set; get; }
        GHI.Glide.UI.Button btnExec { set; get; }
        GHI.Glide.UI.Button btnClear { set; get; }
        bool VerifySDCard()
        {
            if (!sdCard.IsCardInserted || !sdCard.IsCardMounted)
            {
                Glide.MessageBoxManager.Show("Insert SD card!", "Error", ModalButtons.Ok);
                return false;
            }

            return true;
        }
        public void PopulateList()
        {
            ArrayList options = new ArrayList();
            if (VerifySDCard())
            {
                try
                {
                    sdCard.StorageDevice.CreateDirectory(@"Code");
                }
                catch { }
                GT.StorageDevice storage = sdCard.StorageDevice;

                foreach (string s in storage.ListRootDirectorySubdirectories())
                {
                    Debug.Print(s);
                    if (s == "Code")
                    {
                        foreach (string f in storage.ListFiles("\\SD\\Code\\"))
                        {
                            //var x = f.Substring(f.LastIndexOf("\\")+1);
                            var namafile = Path.GetFileNameWithoutExtension(f);
                            options.Add(new object[2] { namafile, f });
                        }
                    }
                }
                if (options.Count <= 0)
                {
                    options.Add(new object[2] { "--kosong--", null });
                }
                listFile = new GHI.Glide.UI.List(options, 300);

            }
        }
        public EditorForm(ref GHI.Glide.Display.Window window, ref Gadgeteer.Modules.GHIElectronics.SDCard sdCard, ref Gadgeteer.Modules.GHIElectronics.DisplayTE35 displayTE35) : base(ref window)
        {

            this.sdCard = sdCard;
            this.displayTE35 = displayTE35;
        }
        public override void Init(params string[] Param)
        {
            sdCard.Mounted += (SDCard sender, GT.StorageDevice device)=>
            {
                PopulateList();
            };
            MainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.EditorForm));
            //populate Code data
            PopulateList();

            txtCode = (GHI.Glide.UI.TextBox)MainWindow.GetChildByName("txtCode");
            btnExec = (GHI.Glide.UI.Button)MainWindow.GetChildByName("btnExec");
            btnClear = (GHI.Glide.UI.Button)MainWindow.GetChildByName("btnClear");
            txtCode.TapEvent += new OnTap(Glide.OpenKeyboard);

            listFile.CloseEvent += (object sender) =>
            {
                Glide.CloseList();
            };

            cmbFile = (GHI.Glide.UI.Dropdown)MainWindow.GetChildByName("cmbFile");
            cmbFile.TapEvent += (object sender) =>
            {
                Glide.OpenList(sender, listFile);
            };
            cmbFile.ValueChangedEvent += (object sender) =>
            {
                var dropdown = (GHI.Glide.UI.Dropdown)sender;
                if (dropdown.Value == null) return;
                var data = sdCard.StorageDevice.ReadFile(dropdown.Value.ToString());
                txtCode.Text = new string(Encoding.UTF8.GetChars(data));
                txtCode.Invalidate();
                //Debug.Print("Dropdown value: " + dropdown.Text + " : " + dropdown.Value.ToString());
            };

            btnExec.PressEvent += (sender) =>
            {
                CallFormRequestEvent(ScreenTypes.Executor, txtCode.Text);
            };

            btnClear.PressEvent += (sender) =>
            {
                txtCode.Text = string.Empty;
            };
            Glide.MainWindow = MainWindow;
            //MainWindow.Invalidate();
        }

        
    }
    public class SplashForm : Screen
    {
        public SplashForm(ref GHI.Glide.Display.Window window) : base(ref window)
        {

        }
        public override void Init(params string[] Param)
        {

            MainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.SplashForm));
            var img = (GHI.Glide.UI.Image)MainWindow.GetChildByName("ImgLogo");

            GT.Picture pic = new GT.Picture(Resources.GetBytes(Resources.BinaryResources.logo), GT.Picture.PictureEncoding.JPEG);
            img.Bitmap = pic.MakeBitmap();

            Glide.MainWindow = MainWindow;
            //MainWindow.Invalidate();
            Thread.Sleep(2000);
            CallFormRequestEvent(ScreenTypes.Editor);

        }
    }

    #endregion

    public partial class Program
    {
        private static GHI.Glide.Display.Window MainWindow;
        private static Screen.ScreenTypes ActiveWindow { set; get; }
        Hashtable Screens { set; get; }
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            Screens = new Hashtable();
            //populate all form
            var F1 = new SplashForm(ref MainWindow);
            F1.FormRequestEvent += General_FormRequestEvent;
            Screens.Add(Screen.ScreenTypes.Splash, F1);

            var F2 = new EditorForm(ref MainWindow, ref sdCard, ref displayTE35);
            F2.FormRequestEvent += General_FormRequestEvent;
            Screens.Add(Screen.ScreenTypes.Editor, F2);

            var F3 = new ExecutorForm(ref MainWindow, ref displayTE35);
            F3.FormRequestEvent += General_FormRequestEvent;
            Screens.Add(Screen.ScreenTypes.Executor, F3);

            Glide.FitToScreen = true;
            GlideTouch.Initialize();

            //load splash
            LoadForm(Screen.ScreenTypes.Splash);
        }
        void LoadForm(Screen.ScreenTypes form, params string[] Param)
        {
            ActiveWindow = form;
            switch (form)
            {
                case Screen.ScreenTypes.Splash:
                case Screen.ScreenTypes.Editor:
                case Screen.ScreenTypes.Executor:
                    (Screens[form] as Screen).Init(Param);
                    break;
                default:
                    return;
                    //throw new Exception("Belum diterapkan");
            }

        }
        void General_FormRequestEvent(Screen.ScreenTypes form, params string[] Param)
        {
            LoadForm(form, Param);
        }
    }
}
