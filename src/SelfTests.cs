using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace ShiftDrag {
 public static class SelfTests {
  public static int Run(){string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","test-results",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);var report=new List<string>();int failures=0;
   Action<string,Action> check=(name,action)=>{try{action();report.Add("PASS  "+name);}catch(Exception e){failures++;report.Add("FAIL  "+name+"\n"+e);}};
   var engine=new Engine(new Preferences());var empty=new Dictionary<string,string>();
   check("Held shortcut key is handled once until key-up",()=>{var gate=new ShortcutGate();Assert(gate.Accept(160,true));for(int i=0;i<100;i++)Assert(!gate.Accept(160,true));Assert(!gate.Accept(160,false));Assert(gate.Accept(160,true));});
   check("Wheel has readable pages and a non-committing center",()=>{Assert(WheelLayout.PageCount(16)==2&&WheelLayout.PageCount(17)==3);Assert(WheelLayout.Hit(new System.Windows.Point(190,190),8)==-1);Assert(WheelLayout.Hit(new System.Windows.Point(190,67),8)==0);});
   check("Repeated shortcut callbacks do not rebuild the active drag menu",()=>{var wheel=new RadialWindow(engine,"Convert",null,(f,o)=>{});try{int initial=wheel.Builds;for(int i=0;i<100;i++)wheel.Update("Convert");Assert(wheel.Builds==initial);wheel.Update("Tools");Assert(wheel.Builds==initial+1);Assert(((System.Windows.Controls.Canvas)wheel.Content).Children.Count==1);}finally{wheel.Close();}});
   Func<string[],string,Dictionary<string,string>,List<string>> run=(files,id,opts)=>engine.Execute(files,new Operation(id,id,""),opts,CancellationToken.None,s=>{}).GetAwaiter().GetResult();
   string image=Path.Combine(dir,"sample with space & quote.png");byte[] pixels=new byte[80*60*4];for(int i=0;i<pixels.Length;i+=4){pixels[i]=100;pixels[i+1]=180;pixels[i+2]=240;pixels[i+3]=255;}var bitmap=BitmapSource.Create(80,60,96,96,PixelFormats.Bgra32,null,pixels,80*4);Engine.SaveImage(bitmap,image,"png",90);
   check("PNG to JPEG writes a readable file and preserves source",()=>{var p=run(new[]{image},"to:jpg",empty)[0];var b=Engine.LoadImage(p);Assert(b.PixelWidth==80&&b.PixelHeight==60&&File.Exists(image));});
   check("Repeated conversions preserve previous outputs",()=>{var a=run(new[]{image},"to:jpg",empty)[0];var b=run(new[]{image},"to:jpg",empty)[0];Assert(a!=b&&File.Exists(a)&&File.Exists(b));});
   check("Resize preserves aspect ratio",()=>{var p=run(new[]{image},"resize",new Dictionary<string,string>{{"width","40"}})[0];var b=Engine.LoadImage(p);Assert(b.PixelWidth==40&&b.PixelHeight==30);});
   check("Crop exports exact pixel dimensions",()=>{var p=run(new[]{image},"crop",new Dictionary<string,string>{{"x","5"},{"y","7"},{"width","20"},{"height","15"}})[0];var b=Engine.LoadImage(p);Assert(b.PixelWidth==20&&b.PixelHeight==15);});
   check("Invalid crop fails without partial output",()=>{bool failed=false;try{run(new[]{image},"crop",new Dictionary<string,string>{{"width","9999"}});}catch(ArgumentException){failed=true;}Assert(failed&&!Directory.GetFiles(dir,".shiftdrag-*").Any());});
   check("Rotation swaps dimensions",()=>{var b=Engine.LoadImage(run(new[]{image},"rotate",empty)[0]);Assert(b.PixelWidth==60&&b.PixelHeight==80);});
   check("Half-size tool exports exact dimensions",()=>{var b=Engine.LoadImage(run(new[]{image},"half",empty)[0]);Assert(b.PixelWidth==40&&b.PixelHeight==30);});
   check("Left rotation swaps dimensions",()=>{var b=Engine.LoadImage(run(new[]{image},"rotate270",empty)[0]);Assert(b.PixelWidth==60&&b.PixelHeight==80);});
   check("Horizontal flip reverses pixels",()=>{string p=Path.Combine(dir,"two-pixels.png");var b=BitmapSource.Create(2,1,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,255,255,255,0,0,255},8);Engine.SaveImage(b,p,"png",90);var flipped=new FormatConvertedBitmap(Engine.LoadImage(run(new[]{p},"flipx",empty)[0]),PixelFormats.Bgra32,null,0);byte[] data=new byte[8];flipped.CopyPixels(data,8,0);Assert(data[0]==255&&data[2]==0&&data[4]==0&&data[6]==255);});
   check("Background flattens alpha onto requested color",()=>{string p=Path.Combine(dir,"transparent.png");Engine.SaveImage(BitmapSource.Create(2,1,96,96,PixelFormats.Bgra32,null,new byte[8],8),p,"png",90);var b=new FormatConvertedBitmap(Engine.LoadImage(run(new[]{p},"background",new Dictionary<string,string>{{"color","#0000FF"}})[0]),PixelFormats.Bgra32,null,0);var data=new byte[8];b.CopyPixels(data,8,0);Assert(data[0]==255&&data[1]==0&&data[2]==0&&data[3]==255);});
   check("Watermark preserves image dimensions",()=>{var b=Engine.LoadImage(run(new[]{image},"watermark",new Dictionary<string,string>{{"text","Test"}})[0]);Assert(b.PixelWidth==80&&b.PixelHeight==60);});
   check("Collage combines both selected images",()=>{var b=Engine.LoadImage(run(new[]{image,image},"collage",empty)[0]);Assert(b.PixelWidth==1036&&b.PixelHeight==524);});
   check("Optimized image encoding removes author metadata",()=>{string tagged=Path.Combine(dir,"tagged.jpg");var meta=new BitmapMetadata("jpg");meta.SetQuery("/app1/ifd/{ushort=315}","ShiftDrag Test Author");var encoder=new JpegBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap,null,meta,null));using(var stream=File.Create(tagged))encoder.Save(stream);var result=run(new[]{tagged},"strip",empty)[0];using(var stream=File.OpenRead(result)){var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);var metadata=decoder.Frames[0].Metadata as BitmapMetadata;Assert(metadata==null||!metadata.ContainsQuery("/app1/ifd/{ushort=315}"));}});
   check("Multi-image PDF writes page tree and EOF",()=>{var p=run(new[]{image,image},"pdf",empty)[0];var text=Encoding.ASCII.GetString(File.ReadAllBytes(p));Assert(text.StartsWith("%PDF-1.4")&&text.Contains("/Count 2")&&text.EndsWith("%%EOF\n"));});
   string subtitle=Path.Combine(dir,"captions.srt");File.WriteAllText(subtitle,"1\n00:00:01,000 --> 00:00:03,500\nHello world\n\n2\n00:00:04,000 --> 00:00:05,000\nSecond line\n");
   check("SRT to VTT and back preserves cues",()=>{var vtt=run(new[]{subtitle},"to:vtt",empty)[0];Assert(File.ReadAllText(vtt).StartsWith("WEBVTT")&&File.ReadAllText(vtt).Contains("00:00:01.000"));var srt=run(new[]{vtt},"to:srt",empty)[0];Assert(File.ReadAllText(srt).Contains("00:00:03,500"));});
   string textInput=Path.Combine(dir,"notes.txt");File.WriteAllText(textInput,"<hello> & café");
   check("HTML export escapes content and preserves Unicode",()=>{var text=File.ReadAllText(run(new[]{textInput},"to:html",empty)[0]);Assert(text.Contains("&lt;hello&gt; &amp;")&&System.Net.WebUtility.HtmlDecode(text).Contains("<hello> & café"));});
   check("ZIP creation and extraction round trip",()=>{var zip=run(new[]{image,textInput},"zip",empty)[0];var folder=run(new[]{zip},"extract",empty)[0];Assert(File.ReadAllBytes(Path.Combine(folder,Path.GetFileName(image))).SequenceEqual(File.ReadAllBytes(image)));});
   string malicious=Path.Combine(dir,"unsafe.zip");using(var z=ZipFile.Open(malicious,ZipArchiveMode.Create)){using(var s=new StreamWriter(z.CreateEntry("../escaped.txt").Open()))s.Write("unsafe");}
   check("ZIP traversal is rejected and stage cleaned",()=>{bool failed=false;try{run(new[]{malicious},"extract",empty);}catch(IOException){failed=true;}Assert(failed&&!File.Exists(Path.Combine(dir,"escaped.txt"))&&!Directory.GetDirectories(dir,".shiftdrag-*").Any());});
   check("Canceled jobs do not publish outputs",()=>{using(var cancel=new CancellationTokenSource()){cancel.Cancel();bool canceled=false;try{engine.Execute(new[]{image},new Operation("to:bmp","",""),empty,cancel.Token,s=>{}).GetAwaiter().GetResult();}catch(OperationCanceledException){canceled=true;}Assert(canceled);}});
   check("Actions support folders and mixed selections",()=>Assert(Catalog.Get(new[]{image,dir},"Actions",engine).Count==6));
   check("Mixed document families do not advertise incompatible conversion formats",()=>Assert(Catalog.Get(new[]{Path.Combine(dir,"file.pdf"),Path.Combine(dir,"file.docx")},"Convert",engine).Single().Id=="zip"));
   check("Windows argument quoting handles trailing slash and quotes",()=>{Assert(Engine.Quote("a b")=="\"a b\"");Assert(Engine.Quote("a\\")=="\"a\\\\\"");Assert(Engine.Quote("a\"b")=="\"a\\\"b\"");});
   if(engine.HasPdf){string pdf=Path.Combine(dir,"readable.pdf");TextPdf(pdf);
    check("PDF renders every page at 300 DPI and output can be cropped",()=>{var folder=run(new[]{pdf},"to:png",empty)[0];var page=Directory.GetFiles(folder,"*.png").Single();var b=Engine.LoadImage(page);Assert(b.PixelWidth==600&&b.PixelHeight==400);var cropped=run(new[]{page},"crop",new Dictionary<string,string>{{"x","0"},{"y","0"},{"width","300"},{"height","200"}})[0];Assert(Engine.LoadImage(cropped).PixelHeight==200);});
    check("PDF selectable text exports locally",()=>Assert(File.ReadAllText(run(new[]{pdf},"to:txt",empty)[0]).Contains("ShiftDrag local test")));
    check("PDF merge and split preserve page counts",()=>{var merged=run(new[]{pdf,pdf},"mergepdf",empty)[0];var split=run(new[]{merged},"splitpdf",empty)[0];Assert(Directory.GetFiles(split,"*.pdf").Length==2);});
    check("PDF organize supports duplication and rotation",()=>{var result=run(new[]{pdf},"organizepdf",new Dictionary<string,string>{{"pages","1,1"},{"rotation","90"}})[0];string data=Python("from pypdf import PdfReader;import sys;r=PdfReader(sys.argv[1]);print(str(len(r.pages))+':'+str(r.pages[0].rotation))",result);Assert(data.Trim()=="2:90");});
    check("PDF invalid page order fails cleanly",()=>{bool failed=false;try{run(new[]{pdf},"organizepdf",new Dictionary<string,string>{{"pages","999"}});}catch(InvalidOperationException){failed=true;}Assert(failed&&!Directory.GetFiles(dir,".shiftdrag-*").Any());});
    check("PDF to DOCX produces a valid text document package",()=>{var doc=run(new[]{pdf},"to:docx",empty)[0];using(var zip=ZipFile.OpenRead(doc)){using(var reader=new StreamReader(zip.GetEntry("word/document.xml").Open()))Assert(reader.ReadToEnd().Contains("ShiftDrag local test"));}});
    check("PDF to EPUB produces content and navigation",()=>{var epub=run(new[]{pdf},"to:epub",empty)[0];using(var zip=ZipFile.OpenRead(epub))Assert(zip.GetEntry("content.xhtml")!=null&&zip.GetEntry("nav.xhtml")!=null);});
    check("PDF compression remains readable",()=>{var result=run(new[]{pdf},"compress",empty)[0];Assert(Python("from pypdf import PdfReader;import sys;print(PdfReader(sys.argv[1]).pages[0].extract_text())",result).Contains("ShiftDrag local test"));});
   }
   if(engine.HasFFmpeg){string wave=Path.Combine(dir,"tone.wav");using(var w=new BinaryWriter(File.Create(wave))){w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+16000);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(8000);w.Write(16000);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(16000);for(int i=0;i<8000;i++)w.Write((short)(Math.Sin(i*2*Math.PI*440/8000)*5000));}
    check("FFmpeg WAV to MP3 local conversion",()=>Assert(new FileInfo(run(new[]{wave},"to:mp3",empty)[0]).Length>100));
    check("FFmpeg WebP export",()=>Assert(new FileInfo(run(new[]{image},"to:webp",empty)[0]).Length>10));
    check("FFmpeg audio trim",()=>Assert(new FileInfo(run(new[]{wave},"trim",new Dictionary<string,string>{{"start","0.2"},{"duration","0.4"}})[0]).Length>100));
    check("FFmpeg normalization",()=>Assert(new FileInfo(run(new[]{wave},"normalize",empty)[0]).Length>100));
    check("FFmpeg stereo conversion",()=>Assert(new FileInfo(run(new[]{wave},"channels",new Dictionary<string,string>{{"channels","2"}})[0]).Length>100));
    check("FFmpeg waveform visualization",()=>Assert(new FileInfo(run(new[]{wave},"visualize",empty)[0]).Length>100));
    check("FFmpeg timed bleep",()=>Assert(new FileInfo(run(new[]{wave},"bleep",new Dictionary<string,string>{{"start","0.2"},{"duration","0.4"}})[0]).Length>100));
    string video=Path.Combine(dir,"silent.mp4");ProcessRun(engine.FFmpeg,new[]{"-nostdin","-v","error","-f","lavfi","-i","color=c=orange:s=160x120:d=1.2:r=10","-c:v","libx264","-pix_fmt","yuv420p",video});
    check("FFmpeg silent video speed preserves video without requiring audio",()=>Assert(new FileInfo(run(new[]{video},"speed",new Dictionary<string,string>{{"speed","1.5"}})[0]).Length>100));
    check("FFmpeg video crop",()=>Assert(new FileInfo(run(new[]{video},"cropvideo",new Dictionary<string,string>{{"x","10"},{"y","10"},{"width","100"},{"height","80"}})[0]).Length>100));
    check("FFmpeg video rectangle redaction",()=>Assert(new FileInfo(run(new[]{video},"redactvideo",new Dictionary<string,string>{{"x","0"},{"y","0"},{"width","20"},{"height","20"}})[0]).Length>100));
    check("Fast mute preserves the compressed video stream",()=>{string result=run(new[]{video},"mute",empty)[0];string probe=Path.Combine(Path.GetDirectoryName(engine.FFmpeg),"ffprobe.exe");string a=ProcessRun(probe,new[]{"-v","error","-select_streams","v:0","-show_entries","packet=data_hash","-show_data_hash","sha256","-of","csv=p=0",video});string b=ProcessRun(probe,new[]{"-v","error","-select_streams","v:0","-show_entries","packet=data_hash","-show_data_hash","sha256","-of","csv=p=0",result});Assert(a.Length>0&&a==b);});
    check("FFmpeg split produces multiple clips",()=>Assert(Directory.GetFiles(run(new[]{video},"splitvideo",new Dictionary<string,string>{{"duration","0.5"}})[0],"*.mp4").Length>=2));
    check("FFmpeg joins silent clips and synthesizes audio",()=>Assert(new FileInfo(run(new[]{video,video},"joinvideo",empty)[0]).Length>100));
   }
   report.Add("\n"+(report.Count-failures)+" passed; "+failures+" failed.");Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","test-results"));File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","test-results","latest.txt"),report);return failures==0?0:1;
  }
  static void Assert(bool condition){if(!condition)throw new Exception("Assertion failed.");}
  static string Python(string code,string arg){return ProcessRun(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engines","python","python.exe"),new[]{"-c",code,arg});}
  static string ProcessRun(string exe,string[] args){using(var p=Process.Start(new ProcessStartInfo(exe,String.Join(" ",args.Select(Engine.Quote))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})){string output=p.StandardOutput.ReadToEnd(),error=p.StandardError.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0)throw new Exception(error);return output;}}
  static void TextPdf(string path){var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 144 96] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",""};string content="BT /F1 10 Tf 10 50 Td (ShiftDrag local test) Tj ET\n";objects[4]="<< /Length "+content.Length+" >>\nstream\n"+content+"endstream";var b=new StringBuilder("%PDF-1.4\n");var offsets=new List<int>();for(int i=0;i<objects.Length;i++){offsets.Add(b.Length);b.Append((i+1)+" 0 obj\n"+objects[i]+"\nendobj\n");}int start=b.Length;b.Append("xref\n0 6\n0000000000 65535 f \n");foreach(int offset in offsets)b.Append(offset.ToString("D10")+" 00000 n \n");b.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n"+start+"\n%%EOF\n");File.WriteAllText(path,b.ToString(),Encoding.ASCII);}
 }
}
