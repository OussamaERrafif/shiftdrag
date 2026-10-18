using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Threading;

namespace ShiftDrag {
 // A normal draggable sample file exercises the same OLE target as an Explorer drag.
 public class DragLab : Window {
  public DragLab(Engine engine,Action<string[],Operation> apply){
   Title="ShiftDrag · Drag test";Width=820;Height=600;WindowStartupLocation=WindowStartupLocation.CenterScreen;ResizeMode=ResizeMode.NoResize;Background=App.Brush("#F1F4FB");FontFamily=new FontFamily("Segoe UI");
   var canvas=new Canvas();Content=canvas;
   var title=new TextBlock{Text="Try the glass wheel",FontSize=28,FontWeight=FontWeights.SemiBold,Foreground=App.Brush("#26334C")};Canvas.SetLeft(title,35);Canvas.SetTop(title,28);canvas.Children.Add(title);
   var help=new TextBlock{Text="Drag the sample onto JPG at the top of the wheel. No personal files are used.",FontSize=12,Foreground=App.Brush("#74819B")};Canvas.SetLeft(help,35);Canvas.SetTop(help,77);canvas.Children.Add(help);
   var ghost=new System.Windows.Shapes.Ellipse{Width=342,Height=342,Stroke=App.Brush("#CFD5E8"),StrokeThickness=1,StrokeDashArray=new DoubleCollection{4,4},Fill=App.Brush("#E7E8F6")};Canvas.SetLeft(ghost,349);Canvas.SetTop(ghost,99);canvas.Children.Add(ghost);
   var target=new TextBlock{Text="JPG target",Foreground=App.Brush("#9A8ABF"),FontSize=12};Canvas.SetLeft(target,485);Canvas.SetTop(target,140);canvas.Children.Add(target);
   var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){dc.DrawRectangle(new LinearGradientBrush(Colors.MediumPurple,Colors.LightSkyBlue,35),null,new Rect(0,0,600,400));dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140,255,255,255)),null,new Point(440,130),95,95);dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(150,255,255,255)),null,new Rect(70,170,300,150),35,35);}
   var bitmap=new RenderTargetBitmap(600,400,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();
   string folder=Path.Combine(Preferences.Home,"samples");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"glass-sample.png");if(!File.Exists(path))Engine.SaveImage(bitmap,path,"png",90);
   var source=new Border{Width=170,Height=165,Background=Brushes.White,CornerRadius=new CornerRadius(14),Padding=new Thickness(10),Cursor=Cursors.Hand};var stack=new StackPanel();source.Child=stack;stack.Children.Add(new Image{Source=bitmap,Width=150,Height=100});stack.Children.Add(new TextBlock{Text="Drag this image →",FontSize=13,FontWeight=FontWeights.SemiBold,Margin=new Thickness(5,15,0,0)});Canvas.SetLeft(source,45);Canvas.SetTop(source,222);canvas.Children.Add(source);
   bool dragging=false;source.MouseLeftButtonDown+=(s,e)=>{if(dragging)return;dragging=true;e.Handled=true;
    var wheel=new RadialWindow(engine,"Convert",null,apply,PointToScreen(new Point(520,270)));wheel.Show();
    try{DragDrop.DoDragDrop(source,new DataObject(DataFormats.FileDrop,new[]{path}),DragDropEffects.Copy);}finally{dragging=false;if(wheel.IsVisible)wheel.Close();}
   };
  }
 }
}
