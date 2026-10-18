using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace ShiftDrag {
 public class CropEditor : Window {
  public Dictionary<string,string> Values;readonly Canvas canvas;readonly Rectangle selection;readonly TextBlock dimensions;Point origin;Rect crop;readonly double scale;readonly int iw,ih;
  public CropEditor(string path){Title="ShiftDrag · Crop image";WindowStartupLocation=WindowStartupLocation.CenterScreen;ResizeMode=ResizeMode.NoResize;SizeToContent=SizeToContent.WidthAndHeight;Background=App.Brush("#FAFAF8");FontFamily=new FontFamily("Segoe UI");var source=Engine.LoadImage(path);iw=source.PixelWidth;ih=source.PixelHeight;scale=Math.Min(1,Math.Min(680.0/iw,440.0/ih));var panel=new StackPanel{Margin=new Thickness(24)};Content=panel;panel.Children.Add(new TextBlock{Text="Keep the good part.",FontSize=24,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,8)});panel.Children.Add(new TextBlock{Text="Drag across the image to select a crop. Saves a new PNG.",Margin=new Thickness(0,0,0,18),Foreground=App.Brush("#77826C")});canvas=new Canvas{Width=iw*scale,Height=ih*scale,Background=Brushes.Black,ClipToBounds=true};canvas.Children.Add(new Image{Source=source,Width=canvas.Width,Height=canvas.Height});selection=new Rectangle{Stroke=App.Brush("#F36B32"),StrokeThickness=2,Fill=new SolidColorBrush(Color.FromArgb(25,243,107,50)),IsHitTestVisible=false};canvas.Children.Add(selection);panel.Children.Add(canvas);dimensions=new TextBlock{Margin=new Thickness(0,12,0,12)};panel.Children.Add(dimensions);crop=new Rect(0,0,canvas.Width,canvas.Height);Draw();canvas.MouseLeftButtonDown+=(s,e)=>{origin=Clamp(e.GetPosition(canvas));canvas.CaptureMouse();crop=new Rect(origin,origin);Draw();};canvas.MouseMove+=(s,e)=>{if(canvas.IsMouseCaptured){crop=new Rect(origin,Clamp(e.GetPosition(canvas)));Draw();}};canvas.MouseLeftButtonUp+=(s,e)=>canvas.ReleaseMouseCapture();panel.Children.Add(App.NewButton("Save cropped copy",()=>{int x=(int)Math.Round(crop.X/scale),y=(int)Math.Round(crop.Y/scale),w=Math.Min(iw-x,(int)Math.Round(crop.Width/scale)),h=Math.Min(ih-y,(int)Math.Round(crop.Height/scale));if(w<1||h<1){MessageBox.Show(this,"Select an area to keep.");return;}Values=new Dictionary<string,string>{{"x",x.ToString()},{"y",y.ToString()},{"width",w.ToString()},{"height",h.ToString()}};DialogResult=true;}));}
  Point Clamp(Point p){return new Point(Math.Max(0,Math.Min(canvas.Width,p.X)),Math.Max(0,Math.Min(canvas.Height,p.Y)));}
  void Draw(){Canvas.SetLeft(selection,crop.X);Canvas.SetTop(selection,crop.Y);selection.Width=crop.Width;selection.Height=crop.Height;dimensions.Text=Math.Round(crop.Width/scale)+" × "+Math.Round(crop.Height/scale)+" px";}
 }
}
