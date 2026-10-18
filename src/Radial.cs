using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;

namespace ShiftDrag {
 public sealed class ShortcutGate {
  readonly HashSet<int> held=new HashSet<int>();
  public bool Accept(int key,bool pressed){if(!pressed){held.Remove(key);return false;}return held.Add(key);}
 }
 public class DragWatcher : IDisposable {
  [StructLayout(LayoutKind.Sequential)] struct POINT {public int X,Y;}
  [DllImport("user32.dll")]static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
  [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,Hook proc,IntPtr module,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
  [DllImport("kernel32.dll")]static extern IntPtr GetModuleHandle(string name);
  delegate IntPtr Hook(int code,IntPtr w,IntPtr l);
  readonly Hook callback;IntPtr handle;readonly DispatcherTimer timer;
  readonly Action<string> trigger;readonly Action cancel;readonly ShortcutGate gate=new ShortcutGate();bool down;POINT origin;
  public bool Enabled=true;
  public DragWatcher(Action<string> onTrigger,Action onCancel){
   trigger=onTrigger;cancel=onCancel;callback=Keyboard;
   using(var p=Process.GetCurrentProcess())using(var m=p.MainModule)handle=SetWindowsHookEx(13,callback,GetModuleHandle(m.ModuleName),0);
   if(handle==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();
   timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(30)};
   timer.Tick+=(s,e)=>{POINT pos;GetCursorPos(out pos);bool held=LeftDown;if(held&&!down)origin=pos;down=held;};timer.Start();
  }
  IntPtr Keyboard(int code,IntPtr w,IntPtr l){
   long message=w.ToInt64();
   if(code>=0&&(message==0x100||message==0x104||message==0x101||message==0x105)){
    int key=Marshal.ReadInt32(l);bool pressed=message==0x100||message==0x104;
    if(gate.Accept(key,pressed)){
     if(key==27)Application.Current.Dispatcher.BeginInvoke(cancel);
     else if(Enabled&&LeftDown&&down){POINT pos;GetCursorPos(out pos);
      if(Math.Abs(pos.X-origin.X)+Math.Abs(pos.Y-origin.Y)>8){
       string mode=new[]{16,160,161}.Contains(key)?"Convert":new[]{18,164,165}.Contains(key)?"Tools":new[]{17,162,163}.Contains(key)?"Actions":key==34?"NextPage":key==33?"PreviousPage":null;
       if(mode!=null)Application.Current.Dispatcher.BeginInvoke(new Action(()=>trigger(mode)));
      }
     }
    }
   }
   return CallNextHookEx(handle,code,w,l);
  }
  public static bool LeftDown {get{return (GetAsyncKeyState(1)&0x8000)!=0;}}
  public static Point Cursor {get{POINT p;GetCursorPos(out p);return new Point(p.X,p.Y);}}
  public void Dispose(){timer.Stop();if(handle!=IntPtr.Zero){UnhookWindowsHookEx(handle);handle=IntPtr.Zero;}}
 }
 public static class WheelLayout {
  public const int PageSize=8;
  public static int PageCount(int count){return Math.Max(1,(count+PageSize-1)/PageSize);}
  public static int Hit(Point p,int count){double x=p.X-190,y=p.Y-190,r=Math.Sqrt(x*x+y*y);if(r<78||r>170||count==0)return -1;double angle=(Math.Atan2(y,x)*180/Math.PI+90+180.0/count+360)%360;return (int)(angle/(360.0/count))%count;}
 }
 public class WheelDrawing : FrameworkElement {
  public List<Operation> Items=new List<Operation>();public int Selected=-1;public bool Preview;public bool Glass;
  public string Caption="Choose a format",Center="Convert",FileLabel="FILE CONVERTER";public int Page,Pages=1,Paints;
  readonly Dictionary<int,Geometry[]> geometries=new Dictionary<int,Geometry[]>();
  static Brush B(string hex){var b=(Brush)new BrushConverter().ConvertFromString(hex);b.Freeze();return b;}
  static readonly Brush Ink=B("#15232D"),Muted=B("#556A79"),White=B("#FFFFFF"),Edge=B("#99FFFFFF");
  static readonly Brush Pane=Gradient("#D9FFFFFF","#A9E9F2F8"),Active=Gradient("#F57C5AFF","#CF553DE7");
  static readonly Brush Core=Gradient("#F0FFFFFF","#D9E8EFF8");
  static Brush Gradient(string a,string b){var brush=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(a),(Color)ColorConverter.ConvertFromString(b),90);brush.Freeze();return brush;}
  static Point At(double r,double angle){double a=angle*Math.PI/180;return new Point(190+r*Math.Cos(a),190+r*Math.Sin(a));}
  static void Text(DrawingContext dc,string value,Point center,double size,Brush color,FontWeight weight,double width=0){
   var t=new FormattedText(value??"",System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,weight,FontStretches.Normal),size,color,1);
   if(width>0){t.MaxTextWidth=width;t.MaxTextHeight=34;t.TextAlignment=TextAlignment.Center;t.Trimming=TextTrimming.CharacterEllipsis;dc.DrawText(t,new Point(center.X-width/2,center.Y-t.Height/2));}
   else dc.DrawText(t,new Point(center.X-t.Width/2,center.Y-t.Height/2));
  }
  Geometry[] Shapes(int count){Geometry[] cached;if(geometries.TryGetValue(count,out cached))return cached;cached=new Geometry[count];
   for(int i=0;i<count;i++){double step=360.0/count,a=-90-step/2+i*step+1.1,b=a+step-2.2;var g=new StreamGeometry();
    using(var c=g.Open()){c.BeginFigure(At(83,a),true,true);c.LineTo(At(165,a),true,false);c.ArcTo(At(165,b),new Size(165,165),0,b-a>180,SweepDirection.Clockwise,true,false);c.LineTo(At(83,b),true,false);c.ArcTo(At(83,a),new Size(83,83),0,b-a>180,SweepDirection.Counterclockwise,true,false);}g.Freeze();cached[i]=g;
   }geometries[count]=cached;return cached;
  }
  protected override void OnRender(DrawingContext dc){
   base.OnRender(dc);Paints++;
   dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(18,15,32,60)),null,new Point(190,194),173,173);
   dc.DrawEllipse(Gradient("#47FFFFFF","#19A4BCD0"),new Pen(B("#60FFFFFF"),1),new Point(190,190),171,171);
   var shapes=Shapes(Items.Count);
   for(int i=0;i<Items.Count;i++){
    bool selected=Selected==i;dc.DrawGeometry(selected?Active:Glass?Pane:B("#D8F3F6FC"),new Pen(selected?B("#D6C6FF"):Edge,1),shapes[i]);
    Point label=At(123,-90+i*360.0/Items.Count);string name=Items[i].Label.Replace(" selection","");
    Text(dc,name,label,name.Length>12?10:12,selected?White:Ink,FontWeights.SemiBold,92);
   }
   dc.DrawEllipse(Core,new Pen(Edge,1.3),new Point(190,190),72,72);
   Text(dc,Center.ToUpperInvariant(),new Point(190,157),8,Muted,FontWeights.SemiBold);
   string chosen=Selected>=0&&Selected<Items.Count?Items[Selected].Label:Preview?"PNG":"Drop to apply";
   Text(dc,chosen,new Point(190,187),chosen.Length>13?12:15,Ink,FontWeights.SemiBold,125);
   Text(dc,Preview?"LOCAL & PRIVATE":FileLabel,new Point(190,220),8,Muted,FontWeights.Normal,124);
   if(Preview)return;
   dc.DrawRoundedRectangle(Gradient("#F2FFFFFF","#E0EBF2F9"),new Pen(Edge,1),new Rect(0,370,400,101),18,18);
   Text(dc,Selected>=0&&Selected<Items.Count?Items[Selected].Detail:Caption,new Point(200,394),11,Ink,FontWeights.Normal,368);
   string[] modes={"Convert","Tools","Actions"};string[] keys={"⇧","Alt","Ctrl"};
   for(int i=0;i<3;i++){double x=8+i*107;bool active=Center==modes[i];if(active)dc.DrawRoundedRectangle(B("#246A4BFF"),null,new Rect(x,420,103,26),8,8);Text(dc,keys[i]+"  "+modes[i],new Point(x+51,433),10,active?B("#6240D7"):Muted,active?FontWeights.SemiBold:FontWeights.Normal);}
   Text(dc,"Esc",new Point(372,433),9,Muted,FontWeights.Normal);
   if(Pages>1){Text(dc,"‹",new Point(30,457),17,Ink,FontWeights.SemiBold);Text(dc,(Page+1)+" / "+Pages+"   ·   PgUp / PgDn for more",new Point(200,457),9,Muted,FontWeights.Normal);Text(dc,"›",new Point(370,457),17,Ink,FontWeights.SemiBold);}
  }
  public int Hit(Point p){return WheelLayout.Hit(p,Items.Count);}
 }
 public class RadialWindow : Window {
  readonly Engine engine;readonly Action<string[],Operation> apply;readonly WheelDrawing drawing;readonly Canvas root;readonly DispatcherTimer timer;
  readonly DateTime opened=DateTime.UtcNow;readonly bool external;string[] files;List<Operation> all;int page;bool finished,initialized;
  public string Mode;public int Builds,DragEvents,SelectionChanges,EffectChanges,InvalidData;
  DragDropEffects lastEffect=DragDropEffects.None;
  public RadialWindow(Engine e,string mode,string[] input,Action<string[],Operation> onApply,Point? anchor=null){
   engine=e;files=input;apply=onApply;external=input==null;
   Width=400;Height=480;WindowStyle=WindowStyle.None;
   AllowsTransparency=true;Background=new SolidColorBrush(Color.FromArgb(1,255,255,255));ShowInTaskbar=false;ShowActivated=!external;Topmost=true;ResizeMode=ResizeMode.NoResize;AllowDrop=true;
   drawing=new WheelDrawing{Width=400,Height=480,Glass=e.Preferences.Glass,IsHitTestVisible=false};root=new Canvas{Width=400,Height=480,Background=Brushes.Transparent};root.Children.Add(drawing);Content=root;
   Update(mode);
   Point cursor=anchor??DragWatcher.Cursor;var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)cursor.X,(int)cursor.Y));
   double scale=1;var main=Application.Current==null?null:Application.Current.MainWindow;var source=main==null?null:PresentationSource.FromVisual(main);if(source!=null)scale=source.CompositionTarget.TransformToDevice.M11;
   Left=Math.Max(screen.WorkingArea.Left/scale,Math.Min(cursor.X/scale-190,screen.WorkingArea.Right/scale-Width));Top=Math.Max(screen.WorkingArea.Top/scale,Math.Min(cursor.Y/scale-190,screen.WorkingArea.Bottom/scale-Height));
   PreviewDragEnter+=OnDrag;PreviewDragOver+=OnDrag;PreviewDrop+=OnDrop;
   MouseMove+=(s,a)=>{if(!external)Select(drawing.Hit(a.GetPosition(drawing)));};
   MouseLeftButtonUp+=(s,a)=>{if(!external){var p=a.GetPosition(drawing);if(p.Y>=420){Footer(p);return;}Commit(drawing.Hit(p));}};
   PreviewKeyDown+=(s,a)=>HandleKey(a);MouseWheel+=(s,a)=>{ChangePage(a.Delta<0?1:-1);a.Handled=true;};
   timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(s,a)=>{if(external&&!DragWatcher.LeftDown&&(DateTime.UtcNow-opened).TotalMilliseconds>700)Close();};timer.Start();
   Closed+=(s,a)=>{timer.Stop();WriteDiagnostics();};
  }
  public void Update(string mode){if(initialized&&Mode==mode)return;Mode=mode;page=0;all=Catalog.Get(files??new string[0],mode,engine);initialized=true;BuildPage();}
  void BindFiles(string[] incoming){files=incoming;all=Catalog.Get(files,Mode,engine);page=0;BuildPage();}
  void BuildPage(){
   Builds++;Title="ShiftDrag · "+Mode;drawing.Items=all.Skip(page*WheelLayout.PageSize).Take(WheelLayout.PageSize).ToList();drawing.Selected=-1;drawing.Center=Mode;drawing.Page=page;drawing.Pages=WheelLayout.PageCount(all.Count);
   drawing.FileLabel=files==null?"RELEASE KEY · KEEP DRAGGING":files.Length==1?Path.GetExtension(files[0]).TrimStart('.').ToUpperInvariant()+" FILE":files.Length+" FILES";
   drawing.Caption=external?"Release the key. Drop onto a segment. Center cancels.":"Choose a segment · arrow keys + Enter · Esc to cancel";drawing.InvalidateVisual();
   while(root.Children.Count>1)root.Children.RemoveAt(1);
   if(external)return;
   for(int i=0;i<drawing.Items.Count;i++){int index=i;double angle=(-90+i*360.0/drawing.Items.Count)*Math.PI/180;
    var b=new Button{Width=78,Height=42,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Focusable=false};var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.BackgroundProperty,Brushes.Transparent);b.Template=new ControlTemplate(typeof(Button)){VisualTree=border};System.Windows.Automation.AutomationProperties.SetName(b,drawing.Items[i].Label);b.Click+=(s,a)=>Commit(index);Canvas.SetLeft(b,190+123*Math.Cos(angle)-39);Canvas.SetTop(b,190+123*Math.Sin(angle)-21);root.Children.Add(b);
   }
  }
  void Select(int index){if(drawing.Selected==index)return;drawing.Selected=index;SelectionChanges++;drawing.InvalidateVisual();}
  public void ChangePage(int direction){int count=WheelLayout.PageCount(all.Count);if(count<2)return;page=(page+direction+count)%count;BuildPage();}
  void Footer(Point p){if(p.Y>446){if(p.X<80)ChangePage(-1);else if(p.X>320)ChangePage(1);return;}if(p.X>=340){Close();return;}int index=Math.Max(0,Math.Min(2,(int)((p.X-8)/107)));Update(new[]{"Convert","Tools","Actions"}[index]);}
  public void HandleKey(KeyEventArgs a){if(a.IsRepeat&&new[]{Key.LeftShift,Key.RightShift,Key.LeftCtrl,Key.RightCtrl,Key.System}.Contains(a.Key)){a.Handled=true;return;}
   if(a.Key==Key.Escape){Close();a.Handled=true;}else if(a.Key==Key.LeftShift||a.Key==Key.RightShift){Update("Convert");a.Handled=true;}else if(a.Key==Key.LeftCtrl||a.Key==Key.RightCtrl){Update("Actions");a.Handled=true;}else if(a.Key==Key.System){Update("Tools");a.Handled=true;}
   else if(a.Key==Key.PageDown||a.Key==Key.PageUp){ChangePage(a.Key==Key.PageDown?1:-1);a.Handled=true;}
   else if(a.Key==Key.Right||a.Key==Key.Down){Select((drawing.Selected+1)%drawing.Items.Count);a.Handled=true;}
   else if(a.Key==Key.Left||a.Key==Key.Up){Select((drawing.Selected+drawing.Items.Count-1)%drawing.Items.Count);a.Handled=true;}
   else if(a.Key==Key.Enter){Commit(drawing.Selected);a.Handled=true;}
  }
  void OnDrag(object sender,DragEventArgs e){e.Handled=true;DragEvents++;
   if(files==null){try{if(e.Data.GetDataPresent(DataFormats.FileDrop))BindFiles((string[])e.Data.GetData(DataFormats.FileDrop));}catch(COMException){InvalidData++;}}
   if(files==null){InvalidData++;e.Effects=DragDropEffects.None;return;}
   Select(drawing.Hit(e.GetPosition(drawing)));
   e.Effects=(e.AllowedEffects&DragDropEffects.Copy)!=0?DragDropEffects.Copy:DragDropEffects.None;
   if(e.Effects!=lastEffect){EffectChanges++;lastEffect=e.Effects;}
  }
  void OnDrop(object sender,DragEventArgs e){e.Handled=true;e.Effects=DragDropEffects.None;if(files==null&&e.Data.GetDataPresent(DataFormats.FileDrop))BindFiles((string[])e.Data.GetData(DataFormats.FileDrop));int index=drawing.Hit(e.GetPosition(drawing));if(files!=null&&index>=0&&(e.AllowedEffects&DragDropEffects.Copy)!=0){e.Effects=DragDropEffects.Copy;Commit(index);}else Close();}
  void Commit(int index){if(finished||files==null||index<0||index>=drawing.Items.Count)return;finished=true;var op=drawing.Items[index];var selected=files;Close();Dispatcher.BeginInvoke(new Action(()=>apply(selected,op)));}
  void WriteDiagnostics(){try{Directory.CreateDirectory(Preferences.Home);string line=DateTime.UtcNow.ToString("s")+" external="+external+" glass="+drawing.Glass+" builds="+Builds+" dragEvents="+DragEvents+" selectionChanges="+SelectionChanges+" paints="+drawing.Paints+" effectChanges="+EffectChanges+" invalidData="+InvalidData+" applied="+finished+Environment.NewLine;File.AppendAllText(Path.Combine(Preferences.Home,"wheel-diagnostics.log"),line);}catch{}}
 }
}
