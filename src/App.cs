using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms=System.Windows.Forms;

namespace ShiftDrag {
 public class App : Application {
  public Preferences Prefs;public Engine Engine;Window main;Forms.NotifyIcon tray;DragWatcher watcher;RadialWindow wheel;Window shelf;
  StackPanel body;UIElement[] home;CancellationTokenSource cancellation;bool quitting;readonly List<string> shelfFiles=new List<string>();readonly List<Tuple<string,string,bool>> history=new List<Tuple<string,string,bool>>();
  static Mutex mutex;
  [STAThread] public static int Main(string[] args) {
   if(args.Contains("--self-test"))return SelfTests.Run();
   bool first;mutex=new Mutex(true,"Local\\ShiftDrag.Windows.Application",out first);if(!first){var existing=FindWindow(null,"ShiftDrag");if(existing!=IntPtr.Zero){ShowWindowAsync(existing,9);SetForegroundWindow(existing);}mutex.Dispose();return 0;}
   var app=new App();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
   app.DispatcherUnhandledException+=(s,e)=>{app.Error(e.Exception);e.Handled=true;};app.Startup+=(s,e)=>app.Start(args);app.Exit+=(s,e)=>{if(app.watcher!=null)app.watcher.Dispose();if(app.tray!=null)app.tray.Dispose();mutex.Dispose();};app.Run();return 0;
  }
  T Get<T>(string name) where T:class{return main.FindName(name) as T;}
  public static Brush Brush(string hex){return (Brush)new BrushConverter().ConvertFromString(hex);}
  void Start(string[] args) {
   Prefs=Preferences.Load();Engine=new Engine(Prefs);
   using(var reader=System.Xml.XmlReader.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Main.xaml")))main=(Window)XamlReader.Load(reader);
   Resources=main.Resources;MainWindow=main;body=Get<StackPanel>("PageBody");home=body.Children.Cast<UIElement>().ToArray();
   main.PreviewKeyDown+=(s,e)=>{if(wheel!=null&&wheel.IsVisible&&!DragWatcher.LeftDown)wheel.HandleKey(e);};
   main.Closing+=(s,e)=>{if(!quitting){e.Cancel=true;main.Hide();}};
   main.StateChanged+=(s,e)=>{if(main.WindowState==WindowState.Minimized)main.Hide();};
   Button("HomeNav",()=>Page("Workspace"));Button("ShelfNav",()=>{Page("Shelf");ShowShelf();});Button("ActivityNav",()=>Page("Activity"));Button("AllActivityButton",()=>Page("Activity"));Button("SettingsNav",()=>Page("Settings"));Button("HelpNav",()=>Page("Help"));
   Button("BrowseButton",Browse);Button("EnabledButton",()=>{Prefs.Enabled=!Prefs.Enabled;Prefs.Save();if(watcher!=null)watcher.Enabled=Prefs.Enabled;RefreshEnabled();});
   foreach(var tool in new[]{new[]{"compress","Compress"},new[]{"resize","Resize"},new[]{"watermark","Watermark"},new[]{"collage","Collage"}}){string id=tool[0];var quick=NewButton(tool[1],()=>ChooseTool(id));quick.Padding=new Thickness(14,7,14,7);quick.FontSize=11;Get<WrapPanel>("QuickTools").Children.Add(quick);}
   Get<WrapPanel>("QuickTools").Children.Add(NewButton("Test the drag",()=>new DragLab(Engine,Apply).Show()));
   var zone=Get<Border>("DropZone");zone.DragOver+=(s,e)=>{e.Handled=true;e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;};zone.Drop+=(s,e)=>{e.Handled=true;if(e.Data.GetDataPresent(DataFormats.FileDrop))OpenWheel("Convert",(string[])e.Data.GetData(DataFormats.FileDrop));};
   var demo=new WheelDrawing{Width=380,Height=380,Preview=true,Glass=true,Selected=0,Items=new List<Operation>{new Operation("","PNG",""),new Operation("","WEBP",""),new Operation("","TIFF",""),new Operation("","PDF",""),new Operation("","JPG","")},Center="Convert"};
   demo.RenderTransform=new ScaleTransform(.64,.64);Canvas.SetLeft(demo,7);Canvas.SetTop(demo,-7);Get<Canvas>("HeroWheel").Children.Add(demo);
   tray=new Forms.NotifyIcon{Text="ShiftDrag · drag a file, tap Shift",Visible=true,Icon=MakeIcon()};var menu=new Forms.ContextMenuStrip();
   main.Icon=System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(tray.Icon.Handle,Int32Rect.Empty,System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
   menu.Items.Add("Open ShiftDrag",null,(s,e)=>ShowMain());menu.Items.Add("Choose files…",null,(s,e)=>{ShowMain();Browse();});menu.Items.Add("Floating shelf",null,(s,e)=>ShowShelf());menu.Items.Add("Pause / resume",null,(s,e)=>Get<Button>("EnabledButton").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)));menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("Quit ShiftDrag",null,(s,e)=>Quit());tray.ContextMenuStrip=menu;tray.MouseClick+=(s,e)=>{if(e.Button==Forms.MouseButtons.Left)ShowMain();};
   try{watcher=new DragWatcher(mode=>{if(Prefs.Enabled)OpenWheel(mode,null);},()=>{if(wheel!=null)wheel.Close();});watcher.Enabled=Prefs.Enabled;}catch(Exception ex){Prefs.Enabled=false;Status("Drag shortcuts unavailable: "+ex.Message);}
   RefreshEnabled();if(!args.Contains("--tray"))main.Show();
   var initialFiles=args.Where(p=>File.Exists(p)||Directory.Exists(p)).Select(Path.GetFullPath).ToArray();if(initialFiles.Length>0)Dispatcher.BeginInvoke(new Action(()=>OpenWheel("Convert",initialFiles)));
  }
  static System.Drawing.Icon MakeIcon(){using(var b=new System.Drawing.Bitmap(32,32))using(var g=System.Drawing.Graphics.FromImage(b)){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.Clear(System.Drawing.Color.Transparent);using(var brush=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(243,107,50)))g.FillEllipse(brush,1,1,30,30);using(var pen=new System.Drawing.Pen(System.Drawing.Color.White,3)){g.DrawLine(pen,10,23,23,10);g.DrawLine(pen,13,10,23,10);g.DrawLine(pen,23,10,23,20);}IntPtr h=b.GetHicon();try{return (System.Drawing.Icon)System.Drawing.Icon.FromHandle(h).Clone();}finally{DestroyIcon(h);}}}
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr h);
  [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr FindWindow(string cls,string title);
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ShowWindowAsync(IntPtr window,int command);
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
  void Button(string name,Action action){Get<Button>(name).Click+=(s,e)=>action();}
  void ShowMain(){main.WindowState=WindowState.Normal;main.Show();main.Activate();}
  void RefreshEnabled(){Get<Button>("EnabledButton").Content=Prefs.Enabled?"●  Running in the tray":"○  Shortcuts paused";}
  void Quit(){if(cancellation!=null){if(MessageBox.Show(main,"A job is running. Cancel it and quit?","ShiftDrag",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;cancellation.Cancel();}quitting=true;if(wheel!=null)wheel.Close();Shutdown();}
  void Browse(){var d=new OpenFileDialog{Title="Choose files for ShiftDrag",Multiselect=true,CheckFileExists=true};if(d.ShowDialog(main)==true)OpenWheel("Convert",d.FileNames);}
  void OpenWheel(string mode,string[] files){if(mode=="NextPage"||mode=="PreviousPage"){if(wheel!=null)wheel.ChangePage(mode=="NextPage"?1:-1);return;}if(wheel!=null){if(files==null){wheel.Update(mode);return;}wheel.Close();}wheel=new RadialWindow(Engine,mode,files,Apply);wheel.Closed+=(s,e)=>wheel=null;wheel.Show();}
  void Status(string text){Get<TextBlock>("StatusText").Text=text;}
  void Error(Exception ex){Directory.CreateDirectory(Preferences.Home);File.AppendAllText(Path.Combine(Preferences.Home,"errors.log"),DateTime.Now.ToString("s")+" "+ex+Environment.NewLine);MessageBox.Show(main,ex.Message,"ShiftDrag",MessageBoxButton.OK,MessageBoxImage.Warning);}
  async void Apply(string[] files,Operation operation) {
   try {
    if(new[]{"copy","path","shelf","reveal","copyto","moveto"}.Contains(operation.Id)){Actions(files,operation.Id);return;}
    if(cancellation!=null){MessageBox.Show(main,"A conversion is already running. Finish or cancel it before starting another.","ShiftDrag");return;}
    var opts=new Dictionary<string,string>();
    if(operation.Id=="crop"){var crop=new CropEditor(files[0]);if(crop.ShowDialog()!=true)return;opts=crop.Values;}
    if(new[]{"resize","trim","frame","speed","cropvideo","redactvideo","splitvideo","channels","bleep","organizepdf","background","watermark"}.Contains(operation.Id)) {opts=Options(operation.Id);if(opts==null)return;}
    cancellation=new CancellationTokenSource();var token=cancellation.Token;string label=operation.Label+" · "+(files.Length==1?Path.GetFileName(files[0]):files.Length+" files");
    Status("Processing · "+label);Get<Button>("BrowseButton").IsEnabled=false;AddHistory(label,"Processing locally…",true);Page("Activity");
    try {var outputs=await Engine.Execute(files,operation,opts,token,text=>Dispatcher.BeginInvoke(new Action(()=>Status(text))));
     history.RemoveAt(0);foreach(var p in outputs)AddHistory(operation.Label+" · "+Path.GetFileName(p),p,true);Status("✓  Saved "+outputs.Count+" "+(outputs.Count==1?"file":"files"));tray.ShowBalloonTip(3500,"ShiftDrag",outputs.Count+" output(s) saved. Originals kept.",Forms.ToolTipIcon.Info);
    }catch(OperationCanceledException){history.RemoveAt(0);AddHistory(label,"Canceled. Already completed outputs are kept.",false);Status("Job canceled");}
    catch(Exception ex){history.RemoveAt(0);var completed=ex.Data["CompletedOutputs"] as string[];if(completed!=null)foreach(var p in completed)AddHistory("Saved · "+Path.GetFileName(p),p,true);AddHistory(label,ex.Message,false);Status("Could not finish · see Activity");tray.ShowBalloonTip(5000,"ShiftDrag could not finish",ex.Message.Length>200?ex.Message.Substring(0,200):ex.Message,Forms.ToolTipIcon.Warning);}
    finally{cancellation.Dispose();cancellation=null;Get<Button>("BrowseButton").IsEnabled=true;Page("Activity");}
   }catch(Exception ex){Error(ex);}
  }
  void AddHistory(string title,string detail,bool ok){history.Insert(0,Tuple.Create(title,detail,ok));if(history.Count>100)history.RemoveAt(history.Count-1);RefreshRecent();}
  void RefreshRecent(){var list=Get<StackPanel>("RecentList");list.Children.Clear();foreach(var item in history.Take(2))list.Children.Add(HistoryCard(item));}
  Border HistoryCard(Tuple<string,string,bool> item){var row=new StackPanel();row.Children.Add(new TextBlock{Text=(item.Item3?"↗  ":"!  ")+item.Item1,FontSize=13,FontWeight=FontWeights.SemiBold});row.Children.Add(new TextBlock{Text=item.Item2,FontSize=11,Foreground=Brush("#939A8A"),Margin=new Thickness(0,6,0,0),TextWrapping=TextWrapping.Wrap});if(File.Exists(item.Item2)||Directory.Exists(item.Item2)){var button=NewButton("Show in Explorer",()=>Reveal(item.Item2));button.HorizontalAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,10,0,0);row.Children.Add(button);}return Card(row);}
  public static Button NewButton(string label,Action action){var b=new Button{Content=label,Margin=new Thickness(0,4,10,4)};b.Click+=(s,e)=>action();return b;}
  static Border Card(UIElement child){return new Border{Background=Brush("#FFFFFF"),BorderBrush=Brush("#E5E7DF"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(20),Margin=new Thickness(0,0,0,12),Child=child};}
  static TextBlock Text(string text,int size=13){return new TextBlock{Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap,Foreground=Brush("#68725D"),Margin=new Thickness(0,0,0,12),LineHeight=size*1.6};}
  void Page(string page) {
   foreach(var item in new[]{new[]{"HomeNav","Workspace"},new[]{"ShelfNav","Shelf"},new[]{"ActivityNav","Activity"},new[]{"SettingsNav","Settings"}}){var nav=Get<Button>(item[0]);nav.Background=item[1]==page?Brush("#FFFFFF"):Brushes.Transparent;nav.Foreground=Brush(item[1]==page?"#6240D7":"#60708B");nav.FontWeight=item[1]==page?FontWeights.SemiBold:FontWeights.Normal;}
   body.Children.Clear();Get<TextBlock>("PageTitle").Text=page=="Workspace"?"Drag. Drop. Done.":page=="Shelf"?"A place between places.":page=="Activity"?"Made, right here.":page=="Settings"?"Make it yours.":"Find your flow.";
   Get<TextBlock>("PageSubtitle").Text=page=="Workspace"?"A shortcut from what you have to what you need.":page=="Activity"?"Conversions and file actions from this session.":page=="Shelf"?"Park files here. Drag them into whatever comes next.":page=="Settings"?"Local engines, output preferences, and shortcuts.":"Small gestures. A much shorter workflow.";
   if(page=="Workspace"){foreach(var child in home)body.Children.Add(child);return;}
   if(page=="Activity"){if(cancellation!=null)body.Children.Add(NewButton("Cancel current job",()=>{if(cancellation!=null)cancellation.Cancel();}));if(history.Count==0)body.Children.Add(Card(Text("Nothing here yet. Drop files in the workspace to begin.")));foreach(var item in history)body.Children.Add(HistoryCard(item));return;}
   if(page=="Shelf"){body.Children.Add(Card(Text("The floating shelf keeps references to your files. It never moves your originals. Drag a row from the floating window into Explorer or another app.")));body.Children.Add(NewButton("Open floating shelf",ShowShelf));if(shelfFiles.Count==0)body.Children.Add(Text("Your shelf is empty. Drag files and tap Ctrl, then drop on Float."));foreach(var path in shelfFiles)body.Children.Add(Card(Text(path)));return;}
   if(page=="Settings"){Settings();body.Children.Insert(0,AppearanceCard());return;}
   body.Children.Add(Card(Text("01   Start dragging a file from File Explorer or your desktop.\n02   While holding the mouse, tap Shift for formats, Alt for tools, or Ctrl for actions.\n03   Release the key, move onto a segment, then release the mouse.\n\nTap Escape to cancel. The wheel stays open after you release the key. Near screen edges, the menu shifts inward so every option stays reachable.",14)));
   body.Children.Add(Card(Text("Prefer browsing? Choose files in Workspace, then click a segment. You can also use arrow keys and Enter. Shift, Alt, and Ctrl switch menus. File actions accept folders and mixed selections.\n\nMinimizing or closing this window hides it from the taskbar and keeps ShiftDrag in the system tray. Click its tray icon to reopen it, or right-click to quit. Dragging between apps with different administrator privileges is limited by Windows.")));
   body.Children.Add(Card(Text("Images: conversion, PDF creation, compression, visual cropping, resize, rotate, grayscale and metadata removal. Media: conversion, trim, compression, normalize, mute, frames, crop, speed, join, split, rectangle redaction, channels, bleep and visualizer. PDFs: 300 DPI PNG/JPG, text/Markdown, basic DOCX/EPUB, merge, split, reorder, rotate, compression and document tags. Also: text, subtitles, ZIP archives and six file actions.\n\nPhoto annotation, automatic face/text redaction, advanced photo adjustment, OCR, PDF-to-PowerPoint/Excel, HEIC export, TAR/GZIP and Mac-only formats/actions are not implemented. PDF text exports preserve text rather than page layout; scanned PDFs need OCR elsewhere. Animated images convert to a single frame. Office-file conversion needs LibreOffice. Available media codecs depend on your FFmpeg build.")));
  }
  void Settings(){var panel=new StackPanel();panel.Children.Add(Text("CONVERSION ENGINES",11));panel.Children.Add(Text("PDF engine  ·  "+(Engine.HasPdf?"Bundled engine ready":"Not bundled — PDF tools unavailable")));panel.Children.Add(Text("FFmpeg  ·  "+(Engine.HasFFmpeg?Engine.FFmpeg:"Not found — media conversions unavailable")));var ff=new TextBox{Text=Prefs.FFmpeg,ToolTip="Optional full path to ffmpeg.exe"};panel.Children.Add(Text("Override FFmpeg executable (leave blank for auto-detect)",11));panel.Children.Add(ff);
   panel.Children.Add(Text("LibreOffice  ·  "+(Engine.HasOffice?Engine.Office:"Not found — Office conversions unavailable")));var office=new TextBox{Text=Prefs.LibreOffice};panel.Children.Add(Text("Override LibreOffice soffice.exe",11));panel.Children.Add(office);
   panel.Children.Add(Text("Output folder (leave blank to save beside each original)",11));var output=new TextBox{Text=Prefs.OutputFolder};panel.Children.Add(output);
   panel.Children.Add(NewButton("Choose output folder…",()=>{using(var d=new Forms.FolderBrowserDialog()){if(d.ShowDialog()==Forms.DialogResult.OK)output.Text=d.SelectedPath;}}));
   panel.Children.Add(Text("JPEG export quality (1–100)",11));var quality=new TextBox{Text=Prefs.Quality.ToString(),Width=90,HorizontalAlignment=HorizontalAlignment.Left};panel.Children.Add(quality);
   panel.Children.Add(NewButton("Save preferences",()=>{int q;if(!Int32.TryParse(quality.Text,out q)||q<1||q>100){MessageBox.Show(main,"JPEG quality must be between 1 and 100.");return;}foreach(string p in new[]{ff.Text,office.Text})if(!String.IsNullOrWhiteSpace(p)&&(!File.Exists(p)||Catalog.Extension(p)!="exe")){MessageBox.Show(main,"Engine overrides must point to an existing .exe file.");return;}
    if(!String.IsNullOrWhiteSpace(output.Text)&&(!Path.IsPathRooted(output.Text)||!Directory.Exists(output.Text))){MessageBox.Show(main,"Choose an existing absolute output folder.");return;}Prefs.FFmpeg=ff.Text.Trim();Prefs.LibreOffice=office.Text.Trim();Prefs.OutputFolder=output.Text.Trim();Prefs.Quality=q;Prefs.Save();Engine.RefreshPaths();Status("✓  Preferences saved");Page("Settings");}));body.Children.Add(Card(panel));body.Children.Add(Card(Text("Shortcuts: Shift → Convert   ·   Alt → File tools   ·   Ctrl → File actions\nSettings are stored locally in "+Preferences.Home+". No telemetry, accounts, uploads, or automatic startup registration. Place portable engines in an engines folder next to ShiftDrag.exe to use them on another PC.")));}
  Border AppearanceCard(){var panel=new StackPanel();panel.Children.Add(Text("WHEEL & PERFORMANCE",11));var glass=new CheckBox{Content="Translucent glass surfaces",IsChecked=Prefs.Glass,Margin=new Thickness(0,5,0,15),FontSize=13};panel.Children.Add(glass);panel.Children.Add(Text("Video encoding speed · faster presets may create larger files",11));var speed=new ComboBox{ItemsSource=new[]{"veryfast","fast","medium"},SelectedItem=String.IsNullOrWhiteSpace(Prefs.VideoPreset)?"veryfast":Prefs.VideoPreset,Padding=new Thickness(9),Margin=new Thickness(0,0,0,10)};panel.Children.Add(speed);panel.Children.Add(NewButton("Apply appearance & speed",()=>{Prefs.Glass=glass.IsChecked==true;Prefs.VideoPreset=(string)speed.SelectedItem??"veryfast";Prefs.Save();if(wheel!=null)wheel.Close();Status("Preferences saved · applies to the next wheel and conversion");}));panel.Children.Add(Text("Glass adds translucent gradients and highlights. Native backdrop blur is disabled after visual testing. Scroll the wheel, use Page Up / Page Down, or click its arrows for more tools.",11));return Card(panel);}
  void ChooseTool(string id){var dialog=new OpenFileDialog{Title="Choose images",Multiselect=true,Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif;*.webp;*.avif;*.heic"};if(dialog.ShowDialog(main)==true){var op=Catalog.Get(dialog.FileNames,"Tools",Engine).FirstOrDefault(x=>x.Id==id);if(op!=null)Apply(dialog.FileNames,op);else MessageBox.Show(main,"Select at least two images for a collage.");}}
  Dictionary<string,string> Options(string id) {
   var titles=new Dictionary<string,string>{{"resize","Resize image"},{"trim","Trim media"},{"frame","Save video frame"},{"speed","Playback speed"},{"cropvideo","Crop video"},{"redactvideo","Redact video"},{"splitvideo","Split video"},{"channels","Audio channels"},{"bleep","Bleep audio"},{"organizepdf","Organize PDF pages"},{"background","Image background"},{"watermark","Text watermark"}};
   var dialog=new Window{Title=titles[id],Width=430,SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterScreen,ResizeMode=ResizeMode.NoResize,Background=Brush("#FAFAF8"),FontFamily=new FontFamily("Segoe UI")};
   var panel=new StackPanel{Margin=new Thickness(26)};dialog.Content=panel;panel.Children.Add(Text(dialog.Title,22));var fields=new Dictionary<string,TextBox>();
   Action<string,string,string> field=(key,label,value)=>{panel.Children.Add(Text(label,12));var t=new TextBox{Text=value,Margin=new Thickness(0,0,0,15)};fields[key]=t;panel.Children.Add(t);};
   if(id=="resize")field("width","Maximum width in pixels (keeps aspect ratio)","1600");
   if(id=="watermark")field("text","Watermark text","My photo");
   if(id=="background")field("color","Background color (#RRGGBB)","#FFFFFF");
   if(id=="trim"||id=="frame"||id=="bleep"){field("start","Start time in seconds","0");if(id!="frame")field("duration","Duration in seconds",id=="bleep"?"1":"10");}
   if(id=="speed")field("speed","Playback multiplier (0.5 to 2; audio pitch is preserved)","1.5");
   if(id=="cropvideo"||id=="redactvideo"){field("x","Left offset in pixels","0");field("y","Top offset in pixels","0");field("width","Rectangle width in pixels",id=="cropvideo"?"640":"200");field("height","Rectangle height in pixels",id=="cropvideo"?"360":"100");}
   if(id=="splitvideo")field("duration","Seconds per clip (re-encodes for precise boundaries)","30");
   if(id=="channels")field("channels","1 for mono, 2 for stereo","1");
   if(id=="organizepdf"){field("pages","Page order, separated by commas (duplicates allowed)","1");field("rotation","Rotate every selected page: 0, 90, 180, or 270 degrees","0");}
   panel.Children.Add(Text("A new copy will be saved. Your original stays untouched.",11));
   panel.Children.Add(NewButton("Apply",()=>{foreach(var pair in fields){if(pair.Key=="text"){if(String.IsNullOrWhiteSpace(pair.Value.Text)||pair.Value.Text.Length>200){MessageBox.Show(dialog,"Enter 1–200 characters for the watermark.");return;}continue;}if(pair.Key=="color"){if(!System.Text.RegularExpressions.Regex.IsMatch(pair.Value.Text,@"^#[0-9a-fA-F]{6}$")){MessageBox.Show(dialog,"Enter a color such as #FFFFFF.");return;}continue;}if(pair.Key=="pages"){if(!System.Text.RegularExpressions.Regex.IsMatch(pair.Value.Text,@"^\s*[1-9]\d*(\s*,\s*[1-9]\d*)*\s*$")){MessageBox.Show(dialog,"Enter page numbers, for example 3,1,2.");return;}continue;}
    double n;bool valid=Double.TryParse(pair.Value.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out n)&&!Double.IsInfinity(n)&&!Double.IsNaN(n)&&n>=0;
    if(new[]{"width","height","duration","speed","channels"}.Contains(pair.Key)&&n<=0)valid=false;
    if(new[]{"width","height","x","y","channels","rotation"}.Contains(pair.Key)&&(n%1!=0||n>65535))valid=false;
    if(pair.Key=="speed"&&(n<0.5||n>2))valid=false;if(pair.Key=="channels"&&n!=1&&n!=2)valid=false;
    if(pair.Key=="rotation"&&!new[]{0.0,90,180,270}.Contains(n))valid=false;
    if(id=="cropvideo"&&new[]{"width","height"}.Contains(pair.Key)&&n%2!=0)valid=false;
    if(!valid){MessageBox.Show(dialog,"Check "+pair.Key+". Use valid numbers in the range shown; video crop dimensions must be even.");return;}}
    dialog.DialogResult=true;}));
   return dialog.ShowDialog()==true?fields.ToDictionary(x=>x.Key,x=>x.Value.Text):null;
  }
  void Actions(string[] files,string id){if(id=="path")Clipboard.SetText(String.Join(Environment.NewLine,files));if(id=="copy"){var paths=new StringCollection();paths.AddRange(files);Clipboard.SetFileDropList(paths);}if(id=="reveal")Reveal(files[0]);if(id=="shelf"){foreach(var file in files)if(!shelfFiles.Contains(file))shelfFiles.Add(file);ShowShelf();}if(id=="copyto"||id=="moveto"){using(var dialog=new Forms.FolderBrowserDialog()){dialog.Description=id=="moveto"?"Move originals to…":"Copy files to…";if(dialog.ShowDialog()!=Forms.DialogResult.OK)return;foreach(string source in files){string dest=Path.Combine(dialog.SelectedPath,Path.GetFileName(source.TrimEnd('\\')));if(File.Exists(dest)||Directory.Exists(dest))throw new IOException("A destination already exists. Nothing is overwritten: "+dest);if(Directory.Exists(source)&&Path.GetFullPath(dest).StartsWith(Path.GetFullPath(source).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new IOException("A folder cannot be placed inside itself.");}foreach(var source in files){var dest=Path.Combine(dialog.SelectedPath,Path.GetFileName(source.TrimEnd('\\')));if(id=="moveto"){if(Directory.Exists(source))Directory.Move(source,dest);else File.Move(source,dest);}else CopyTree(source,dest);}}}AddHistory(id+" · "+files.Length+" item(s)","File action completed",true);Status("✓  File action completed");}
  static void CopyTree(string source,string dest){if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new IOException("Copying links and junctions is not supported.");if(File.Exists(source)){File.Copy(source,dest,false);return;}Directory.CreateDirectory(dest);foreach(var p in Directory.GetFileSystemEntries(source))CopyTree(p,Path.Combine(dest,Path.GetFileName(p)));}
  static void Reveal(string path){Process.Start(new ProcessStartInfo("explorer.exe","/select,"+Engine.Quote(path)){UseShellExecute=true});}
  void ShowShelf(){if(shelf!=null){RefreshShelf();shelf.Show();shelf.Activate();return;}shelf=new Window{Title="ShiftDrag · Floating shelf",Width=360,Height=430,MinWidth=300,MinHeight=200,Topmost=true,AllowDrop=true,Background=Brush("#F4F5EF"),FontFamily=new FontFamily("Segoe UI")};shelf.Closed+=(s,e)=>shelf=null;shelf.DragOver+=(s,e)=>{e.Handled=true;e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;};shelf.Drop+=(s,e)=>{e.Handled=true;if(e.Data.GetDataPresent(DataFormats.FileDrop)){foreach(var p in (string[])e.Data.GetData(DataFormats.FileDrop))if(!shelfFiles.Contains(p))shelfFiles.Add(p);RefreshShelf();}};RefreshShelf();shelf.Show();}
  void RefreshShelf(){if(shelf==null)return;var p=new StackPanel{Margin=new Thickness(18)};p.Children.Add(Text("A little holding space.",21));p.Children.Add(Text("Drop files here. Drag a row into another app.",11));foreach(string file in shelfFiles.ToArray()){var row=new StackPanel();var b=NewButton(Path.GetFileName(file),()=>{});Point start=new Point();b.PreviewMouseLeftButtonDown+=(s,e)=>start=e.GetPosition(b);b.PreviewMouseMove+=(s,e)=>{if(e.LeftButton==System.Windows.Input.MouseButtonState.Pressed&&(e.GetPosition(b)-start).Length>8){if(File.Exists(file)||Directory.Exists(file))DragDrop.DoDragDrop(b,new DataObject(DataFormats.FileDrop,new[]{file}),DragDropEffects.Copy);}};row.Children.Add(b);row.Children.Add(NewButton("Remove from shelf",()=>{shelfFiles.Remove(file);RefreshShelf();}));p.Children.Add(Card(row));}shelf.Content=new ScrollViewer{Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};}
 }
}
