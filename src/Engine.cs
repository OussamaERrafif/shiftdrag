using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ShiftDrag {
 public class Engine {
  readonly Preferences prefs;
  string ffmpeg,office;
  public Preferences Preferences {get{return prefs;}}
  public Engine(Preferences p) { prefs=p;RefreshPaths(); }
  public void RefreshPaths(){ffmpeg=Find(prefs.FFmpeg,"ffmpeg.exe",new[]{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engines","ffmpeg.exe"),@"C:\ffmpeg\bin\ffmpeg.exe"});office=Find(prefs.LibreOffice,"soffice.exe",new[]{@"C:\Program Files\LibreOffice\program\soffice.exe",@"C:\Program Files (x86)\LibreOffice\program\soffice.exe"});}
  public string FFmpeg { get { return ffmpeg; } }
  public string Office { get { return office; } }
  string VideoPreset {get{return new[]{"veryfast","fast","medium"}.Contains(prefs.VideoPreset)?prefs.VideoPreset:"veryfast";}}
  public bool HasFFmpeg { get { return FFmpeg!=null; } } public bool HasOffice { get { return Office!=null; } }
  public bool HasPdf { get { return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engines","python","python.exe")); } }
  [DataContract] class PdfRequest {
   [DataMember] public string[] files;
   [DataMember] public string output;
   [DataMember] public string operation;
   [DataMember] public Dictionary<string,string> options;
  }
  void PdfOperation(string[] files,string output,string operation,Dictionary<string,string> options,CancellationToken ct) {
   string request=Path.Combine(Path.GetTempPath(),"shiftdrag-pdf-"+Guid.NewGuid().ToString("N")+".json");
   try {using(var stream=File.Create(request))new DataContractJsonSerializer(typeof(PdfRequest),new DataContractJsonSerializerSettings{UseSimpleDictionaryFormat=true}).WriteObject(stream,new PdfRequest{files=files,output=output,operation=operation,options=options});
    Run(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"engines","python","python.exe"),new[]{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"pdf_engine.py"),request},ct);
   }finally{if(File.Exists(request))File.Delete(request);}
  }
  static string Find(string configured,string name,string[] known) {
   foreach(var p in new[]{configured}.Concat(known)) if(!String.IsNullOrWhiteSpace(p) && File.Exists(p))return Path.GetFullPath(p);
   foreach(var dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) try { var p=Path.Combine(dir.Trim('"'),name);if(File.Exists(p))return p; }catch{}
   return null;
  }
  public static string Quote(string arg) {
   var b=new StringBuilder("\"");int slashes=0;
   foreach(char c in arg) {if(c=='\\'){slashes++;continue;}if(c=='"'){b.Append('\\',slashes*2+1);b.Append(c);}else {b.Append('\\',slashes);b.Append(c);}slashes=0;}
   b.Append('\\',slashes*2);b.Append('"');return b.ToString();
  }
  static string Run(string exe,IEnumerable<string> args,CancellationToken ct) {
   if(exe==null)throw new InvalidOperationException("The required local engine is unavailable. Set its executable in Settings.");
   using(var p=new Process()) {
    p.StartInfo=new ProcessStartInfo(exe,String.Join(" ",args.Select(Quote))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
    var error=new StringBuilder();p.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(error){if(error.Length>12000)error.Remove(0,6000);error.AppendLine(e.Data);}};
    var stdout=new StringBuilder();p.OutputDataReceived+=(s,e)=>{if(e.Data!=null)lock(stdout){if(stdout.Length<12000)stdout.AppendLine(e.Data);}};p.Start();p.BeginErrorReadLine();p.BeginOutputReadLine();
    while(!p.WaitForExit(100)) { if(ct.IsCancellationRequested){try{p.Kill();}catch{}p.WaitForExit();ct.ThrowIfCancellationRequested();} }
    p.WaitForExit();ct.ThrowIfCancellationRequested();if(p.ExitCode!=0)throw new InvalidOperationException("Local engine failed: "+error.ToString().Trim());return stdout.ToString().Trim();
   }
  }
  string Destination(string source,string suffix,string extension) {
   string dir=String.IsNullOrWhiteSpace(prefs.OutputFolder)?Path.GetDirectoryName(source):prefs.OutputFolder;
   Directory.CreateDirectory(dir);return Path.Combine(dir,Path.GetFileNameWithoutExtension(source)+suffix+"."+extension);
  }
  public static string Publish(string stage,string target,bool directory) {
   string dir=Path.GetDirectoryName(target),name=Path.GetFileNameWithoutExtension(target),ext=Path.GetExtension(target);
   for(int i=0;i<10000;i++) {string p=i==0?target:Path.Combine(dir,name+" ("+i+")"+ext);if(File.Exists(p)||Directory.Exists(p))continue;
    try{if(directory)Directory.Move(stage,p);else File.Move(stage,p);return p;}catch(IOException){if(!File.Exists(p)&&!Directory.Exists(p))throw;}
   }throw new IOException("Could not find an unused output name.");
  }
  public async Task<List<string>> Execute(string[] files,Operation op,Dictionary<string,string> options,CancellationToken ct,Action<string> progress) {
   return await Task.Run(()=>{
    var results=new List<string>();
    if(files.Length==0)throw new ArgumentException("Choose at least one file.");
    foreach(var f in files)if(!File.Exists(f)&&!Directory.Exists(f))throw new FileNotFoundException("The source no longer exists.",f);
    bool group=op.Id=="zip"||op.Id=="pdf"||op.Id=="mergepdf"||op.Id=="joinvideo"||op.Id=="collage"||(op.Id=="to:pdf"&&files.All(f=>Catalog.Kind(f)=="image"));
    for(int i=0;i<(group?1:files.Length);i++) {
     ct.ThrowIfCancellationRequested();var input=files[i];progress("Processing "+(i+1)+" / "+(group?1:files.Length)+" · "+Path.GetFileName(input));
     string ext=OutputExtension(input,op.Id),suffix=op.Id.StartsWith("to:")?"-converted":"-"+op.Id;
     string target=Destination(input,suffix,ext),stage=Path.Combine(Path.GetDirectoryName(target),".shiftdrag-"+Guid.NewGuid().ToString("N")+"."+ext);
     bool isDir=op.Id=="extract"||op.Id=="splitpdf"||op.Id=="splitvideo"||(Catalog.Extension(input)=="pdf"&&(op.Id=="to:jpg"||op.Id=="to:png"));
     if(isDir){target=Path.Combine(Path.GetDirectoryName(target),Path.GetFileNameWithoutExtension(input)+"-"+(op.Id=="extract"?"extracted":op.Id=="splitpdf"?"pages":op.Id=="splitvideo"?"clips":"images"));stage+="-folder";}
     try {
      if(op.Id=="zip") CreateZip(files,stage,ct);
      else if(op.Id=="extract")ExtractZip(input,stage,ct);
      else if(Catalog.Extension(input)=="pdf")PdfOperation(group?files:new[]{input},stage,op.Id,options,ct);
      else if(op.Id=="pdf"||(op.Id=="to:pdf"&&Catalog.Kind(input)=="image"))ImagePdf(files,stage,ct);
      else if(op.Id=="collage")Collage(files,stage,ct);
      else if(Catalog.Kind(input)=="image") ImageOperation(input,stage,op.Id,options,ct);
      else if(op.Id=="joinvideo")JoinVideo(files,stage,ct);
      else if(Catalog.Kind(input)=="audio"||Catalog.Kind(input)=="video")MediaOperation(input,stage,op.Id,options,ct);
      else if(Catalog.Kind(input)=="subtitle")Subtitle(input,stage,ext);
      else if(Catalog.Kind(input)=="text")TextFile(input,stage,ext);
      else if(Catalog.Kind(input)=="document")OfficeConvert(input,stage,ext,ct);
      else throw new NotSupportedException("This operation is unavailable for this file type.");
      ct.ThrowIfCancellationRequested();if(!isDir && (!File.Exists(stage)||new FileInfo(stage).Length==0))throw new IOException("The engine produced no output.");
      results.Add(Publish(stage,target,isDir));
     }catch(Exception ex){ex.Data["CompletedOutputs"]=results.ToArray();throw;}
     finally{if(File.Exists(stage))File.Delete(stage);if(Directory.Exists(stage))Directory.Delete(stage,true);}
    }return results;
   },ct).ConfigureAwait(false);
  }
  static string OutputExtension(string input,string id) {
   if(id.StartsWith("to:"))return id.Substring(3);if(id=="zip")return "zip";if(id=="pdf")return "pdf";
   if(Catalog.Kind(input)=="image")return id=="compress"?"jpg":"png";
   if(id=="frame")return "png";if(id=="audio")return "mp3";if(id=="visualize")return "mp4";
   if(Catalog.Kind(input)=="video")return "mp4";
   if(Catalog.Kind(input)=="audio")return "m4a";return Catalog.Extension(input);
  }
  public static BitmapSource LoadImage(string path) {
   using(var s=File.OpenRead(path)){var decoder=BitmapDecoder.Create(s,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);var b=decoder.Frames[0];b.Freeze();return b;}
  }
  public BitmapSource DecodeImage(string path) {
   try{return LoadImage(path);}catch {if(!HasFFmpeg)throw;string temp=Path.Combine(Path.GetTempPath(),"shiftdrag-image-"+Guid.NewGuid().ToString("N")+".png");try{Run(FFmpeg,new[]{"-nostdin","-v","error","-i",path,"-frames:v","1",temp},CancellationToken.None);return LoadImage(temp);}finally{if(File.Exists(temp))File.Delete(temp);}}
  }
  static BitmapSource Fresh(BitmapSource image) {
   // A native pixel snapshot discards metadata without allocating two managed pixel buffers.
   var b=new WriteableBitmap(image);b.Freeze();return b;
  }
  public static void SaveImage(BitmapSource source,string output,string ext,int quality) {
   BitmapEncoder encoder;
   switch(ext){case "jpg":case "jpeg":encoder=new JpegBitmapEncoder{QualityLevel=quality};break;case "bmp":encoder=new BmpBitmapEncoder();break;case "tif":case "tiff":encoder=new TiffBitmapEncoder{Compression=TiffCompressOption.Zip};break;case "gif":encoder=new GifBitmapEncoder();break;default:encoder=new PngBitmapEncoder();break;}
   encoder.Frames.Add(BitmapFrame.Create(Fresh(source)));using(var s=File.Create(output))encoder.Save(s);
  }
  void ImageOperation(string input,string output,string id,Dictionary<string,string> opts,CancellationToken ct) {
   string ext=Catalog.Extension(output);BitmapSource source;
   if(new[]{"webp","avif"}.Contains(ext)){Run(FFmpeg,new[]{"-nostdin","-v","error","-i",input,"-frames:v","1",output},ct);return;}
   source=DecodeImage(input);
   if(id=="resize") {double width=Value(opts,"width",1600);double scale=Math.Min(1,width/source.PixelWidth);source=new TransformedBitmap(source,new ScaleTransform(scale,scale));}
   if(id=="rotate")source=new TransformedBitmap(source,new RotateTransform(90));
   if(id=="rotate180")source=new TransformedBitmap(source,new RotateTransform(180));
   if(id=="rotate270")source=new TransformedBitmap(source,new RotateTransform(270));
   if(id=="flipx"||id=="flipy")source=new TransformedBitmap(source,new ScaleTransform(id=="flipx"?-1:1,id=="flipy"?-1:1));
   if(id=="half")source=new TransformedBitmap(source,new ScaleTransform(Math.Max(1,source.PixelWidth/2)/(double)source.PixelWidth,Math.Max(1,source.PixelHeight/2)/(double)source.PixelHeight));
   if(id=="grayscale")source=new FormatConvertedBitmap(source,PixelFormats.Gray8,null,0);
   if(id=="crop") {int x=(int)Value(opts,"x",0),y=(int)Value(opts,"y",0),w=(int)Value(opts,"width",source.PixelWidth),h=(int)Value(opts,"height",source.PixelHeight);
    if(w<1||h<1||x<0||y<0||x+w>source.PixelWidth||y+h>source.PixelHeight)throw new ArgumentException("The crop rectangle must fit within the image.");source=new CroppedBitmap(source,new Int32Rect(x,y,w,h));}
   if(id=="background"||id=="watermark")source=Decorate(source,id,opts);
   ct.ThrowIfCancellationRequested();SaveImage(source,output,ext,id=="compress"?Math.Min(75,prefs.Quality):prefs.Quality);
  }
  static BitmapSource Decorate(BitmapSource source,string id,Dictionary<string,string> options){
   var visual=new DrawingVisual();using(var draw=visual.RenderOpen()){
    if(id=="background"){string color;options.TryGetValue("color",out color);draw.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(color??"#FFFFFF")),null,new Rect(0,0,source.PixelWidth,source.PixelHeight));}
    draw.DrawImage(source,new Rect(0,0,source.PixelWidth,source.PixelHeight));
    if(id=="watermark"){string value;options.TryGetValue("text",out value);if(String.IsNullOrWhiteSpace(value))throw new ArgumentException("Enter watermark text.");double size=Math.Max(12,Math.Min(96,source.PixelWidth*.035));
     var text=new FormattedText(value,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,new SolidColorBrush(Color.FromArgb(220,255,255,255)),1);text.MaxTextWidth=Math.Max(1,source.PixelWidth-24);text.MaxTextHeight=Math.Max(1,source.PixelHeight-12);text.Trimming=TextTrimming.CharacterEllipsis;
     Point point=new Point(Math.Max(6,source.PixelWidth-text.Width-16),Math.Max(4,source.PixelHeight-text.Height-12));draw.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(100,0,0,0)),null,new Rect(point.X-5,point.Y-3,text.Width+10,text.Height+6),4,4);draw.DrawText(text,point);
    }
   }var bitmap=new RenderTargetBitmap(source.PixelWidth,source.PixelHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();return bitmap;
  }
  void Collage(string[] files,string output,CancellationToken ct){
   if(files.Length<2||files.Length>64)throw new ArgumentException("Choose between 2 and 64 images for a collage.");int columns=(int)Math.Ceiling(Math.Sqrt(files.Length)),rows=(files.Length+columns-1)/columns;const int tile=500,gap=12;
   int width=columns*tile+(columns+1)*gap,height=rows*tile+(rows+1)*gap;var visual=new DrawingVisual();using(var draw=visual.RenderOpen()){
    draw.DrawRectangle(Brushes.White,null,new Rect(0,0,width,height));for(int i=0;i<files.Length;i++){ct.ThrowIfCancellationRequested();var image=DecodeImage(files[i]);double factor=Math.Min(tile/(double)image.PixelWidth,tile/(double)image.PixelHeight),w=image.PixelWidth*factor,h=image.PixelHeight*factor;
     draw.DrawImage(image,new Rect(gap+(i%columns)*(tile+gap)+(tile-w)/2,gap+(i/columns)*(tile+gap)+(tile-h)/2,w,h));}
   }var result=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);result.Render(visual);SaveImage(result,output,"png",prefs.Quality);
  }
  static double Value(Dictionary<string,string> o,string key,double fallback) {string v;double n;return o.TryGetValue(key,out v)&&Double.TryParse(v,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out n)?n:fallback;}
  void MediaOperation(string input,string output,string id,Dictionary<string,string> opts,CancellationToken ct) {
   var args=new List<string>{"-nostdin","-v","error"};
   if(id=="trim"||id=="frame")args.AddRange(new[]{"-ss",Value(opts,"start",0).ToString(System.Globalization.CultureInfo.InvariantCulture)});
   args.AddRange(new[]{"-i",input});string ext=Catalog.Extension(output);bool audio=Catalog.Kind(input)=="audio"||id=="audio"||ext=="mp3";
   if(id=="trim")args.AddRange(new[]{"-t",Value(opts,"duration",10).ToString(System.Globalization.CultureInfo.InvariantCulture)});
   if((id=="strip"||id=="mute")&&new[]{"mp4","mov","mkv"}.Contains(Catalog.Extension(input))){args.AddRange(new[]{"-map","0","-c","copy"});if(id=="strip")args.AddRange(new[]{"-map_metadata","-1"});else args.Add("-an");args.Add(output);try{Run(FFmpeg,args,ct);return;}catch(InvalidOperationException){if(File.Exists(output))File.Delete(output);args.RemoveRange(5,args.Count-5);}}
   if(id=="strip")args.AddRange(new[]{"-map_metadata","-1"});
   if(id=="channels")args.AddRange(new[]{"-ac",((int)Value(opts,"channels",1)).ToString()});
   if(id=="cropvideo")args.AddRange(new[]{"-vf","crop="+(int)Value(opts,"width",640)+":"+(int)Value(opts,"height",360)+":"+(int)Value(opts,"x",0)+":"+(int)Value(opts,"y",0)});
   if(id=="redactvideo")args.AddRange(new[]{"-vf","drawbox=x="+(int)Value(opts,"x",0)+":y="+(int)Value(opts,"y",0)+":w="+(int)Value(opts,"width",200)+":h="+(int)Value(opts,"height",100)+":color=black:t=fill"});
   if(id=="speed") {string speed=Value(opts,"speed",1).ToString(System.Globalization.CultureInfo.InvariantCulture);bool hasAudio=HasAudio(input,ct);
    if(hasAudio)args.AddRange(new[]{"-filter_complex","[0:v]setpts=PTS/"+speed+"[v];[0:a]atempo="+speed+"[a]","-map","[v]","-map","[a]"});else args.AddRange(new[]{"-vf","setpts=PTS/"+speed});
   }
   if(id=="bleep") {string start=Value(opts,"start",0).ToString(System.Globalization.CultureInfo.InvariantCulture),end=(Value(opts,"start",0)+Value(opts,"duration",1)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    args.AddRange(new[]{"-filter_complex","[0:a]volume=enable='between(t,"+start+","+end+")':volume=0[base];sine=frequency=1000:sample_rate=48000,volume=0.25,afade=t=in:st="+start+":d=0.01,volume=enable='not(between(t,"+start+","+end+"))':volume=0[tone];[base][tone]amix=inputs=2:duration=first:normalize=0[a]","-map","[a]"});
   }
   if(id=="normalize")args.AddRange(new[]{"-af","loudnorm=I=-16:TP=-1.5:LRA=11"});
   if(id=="mono")args.AddRange(new[]{"-ac","1"});
   if(id=="visualize") {args.AddRange(new[]{"-filter_complex","[0:a]showwaves=s=1280x720:mode=line:colors=0xF36B32:rate=30,format=yuv420p[v]","-map","[v]","-map","0:a","-c:v","libx264","-c:a","aac","-shortest"});}
   else if(id=="splitvideo") {double seconds=Value(opts,"duration",30);Directory.CreateDirectory(output);args.AddRange(new[]{"-c:v","libx264","-c:a","aac","-pix_fmt","yuv420p","-force_key_frames","expr:gte(t,n_forced*"+seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+")","-f","segment","-segment_time",seconds.ToString(System.Globalization.CultureInfo.InvariantCulture),"-reset_timestamps","1",Path.Combine(output,"clip-%03d.mp4")});Run(FFmpeg,args,ct);return;}
   else if(id=="frame")args.AddRange(new[]{"-frames:v","1"});
   else if(audio) {args.Add("-vn");if(ext=="mp3")args.AddRange(new[]{"-c:a","libmp3lame","-q:a","2"});if(ext=="m4a")args.AddRange(new[]{"-c:a","aac","-b:a",id=="compress"?"128k":"192k"});if(ext=="opus")args.AddRange(new[]{"-c:a","libopus"});}
   else if(ext=="gif")args.AddRange(new[]{"-vf","fps=12,scale=640:-1:flags=lanczos","-an"});
   else {if(ext=="mp4"||ext=="mov"||ext=="mkv")args.AddRange(new[]{"-c:v","libx264","-preset",VideoPreset,"-crf",id=="compress"?"29":"22","-pix_fmt","yuv420p","-c:a","aac"});
    if(ext=="webm")args.AddRange(new[]{"-c:v","libvpx-vp9","-crf","32","-b:v","0","-c:a","libopus"});if(id=="mute")args.Add("-an");}
   args.Add(output);Run(FFmpeg,args,ct);
  }
  string Probe(string input,string select,string entries,CancellationToken ct) {string probe=Find("","ffprobe.exe",new[]{Path.Combine(Path.GetDirectoryName(FFmpeg),"ffprobe.exe")});return Run(probe,new[]{"-v","error","-select_streams",select,"-show_entries",entries,"-of","csv=p=0",input},ct);}
  bool HasAudio(string input,CancellationToken ct){return Probe(input,"a:0","stream=index",ct).Length>0;}
  void JoinVideo(string[] files,string output,CancellationToken ct) {
   if(files.Length<2)throw new ArgumentException("Select at least two video files to join.");string work=Path.Combine(Path.GetTempPath(),"shiftdrag-join-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
   try{string size=Probe(files[0],"v:0","stream=width,height",ct);var dims=size.Split(',');int w,h;if(dims.Length<2||!Int32.TryParse(dims[0],out w)||!Int32.TryParse(dims[1],out h))throw new IOException("Could not read first video dimensions.");w=w/2*2;h=h/2*2;
    var list=new List<string>();for(int i=0;i<files.Length;i++){ct.ThrowIfCancellationRequested();bool audio=HasAudio(files[i],ct);string clip=Path.Combine(work,"clip-"+i+".mp4");var args=new List<string>{"-nostdin","-v","error","-i",files[i]};if(!audio)args.AddRange(new[]{"-f","lavfi","-i","anullsrc=r=48000:cl=stereo"});args.AddRange(new[]{"-map","0:v:0","-map",audio?"0:a:0":"1:a:0","-vf","scale="+w+":"+h+":force_original_aspect_ratio=decrease,pad="+w+":"+h+":(ow-iw)/2:(oh-ih)/2,setsar=1","-r","30","-c:v","libx264","-pix_fmt","yuv420p","-c:a","aac","-ar","48000","-ac","2","-shortest",clip});Run(FFmpeg,args,ct);list.Add("file 'clip-"+i+".mp4'");}
    string manifest=Path.Combine(work,"clips.txt");File.WriteAllLines(manifest,list,new UTF8Encoding(false));Run(FFmpeg,new[]{"-nostdin","-v","error","-f","concat","-safe","1","-i",manifest,"-c","copy",output},ct);
   }finally{try{Directory.Delete(work,true);}catch{}}
  }
  void OfficeConvert(string input,string output,string ext,CancellationToken ct) {
   string work=Path.Combine(Path.GetTempPath(),"shiftdrag-office-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
   try{string profile=new Uri(Path.Combine(work,"profile")).AbsoluteUri;Run(Office,new[]{"-env:UserInstallation="+profile,"--headless","--convert-to",ext,"--outdir",work,input},ct);
    string result=Path.Combine(work,Path.GetFileNameWithoutExtension(input)+"."+ext);if(!File.Exists(result))throw new IOException("LibreOffice could not convert this file to "+ext+".");File.Move(result,output);
   }finally{try{Directory.Delete(work,true);}catch{}}
  }
  static void TextFile(string input,string output,string ext) {
   string text=File.ReadAllText(input);string from=Catalog.Extension(input);
   if(ext=="html")text="<!doctype html><html><head><meta charset=\"utf-8\"><title>"+System.Net.WebUtility.HtmlEncode(Path.GetFileName(input))+"</title></head><body><pre>"+System.Net.WebUtility.HtmlEncode(text)+"</pre></body></html>";
   else if(from=="html"||from=="htm")text=System.Net.WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(text,@"<(script|style)\b[^>]*>.*?</\1>","",RegexOptions.Singleline|RegexOptions.IgnoreCase),"<[^>]+>",""));
   File.WriteAllText(output,text,new UTF8Encoding(false));
  }
  static void Subtitle(string input,string output,string ext) {
   string source=File.ReadAllText(input).Replace("\r\n","\n");var cues=new List<Tuple<string,string>>();
   foreach(string block in Regex.Split(source,@"\n\s*\n")) {var lines=block.Split('\n');int t=Array.FindIndex(lines,l=>l.Contains("-->"));if(t<0)continue;string timing=Regex.Match(lines[t],@"(?:\d{2}:)?\d{2}:\d{2}[.,]\d{3}\s+-->\s+(?:\d{2}:)?\d{2}:\d{2}[.,]\d{3}").Value;if(timing.Length==0)continue;
    cues.Add(Tuple.Create(timing,String.Join("\n",lines.Skip(t+1))));}
   if(cues.Count==0)throw new FormatException("No valid subtitle cues were found.");
   var b=new StringBuilder();if(ext=="vtt")b.Append("WEBVTT\n\n");int index=0;
   foreach(var cue in cues){if(ext=="txt")b.AppendLine(Regex.Replace(cue.Item2,"<[^>]+>",""));else {if(ext=="srt")b.AppendLine((++index).ToString());b.AppendLine(ext=="srt"?Regex.Replace(cue.Item1.Replace('.',','),@"(?<![\d:])(\d{2}:\d{2},\d{3})", "00:$1"):cue.Item1.Replace(',','.'));b.AppendLine(cue.Item2);b.AppendLine();}}
   File.WriteAllText(output,b.ToString(),new UTF8Encoding(false));
  }
  static void CreateZip(string[] paths,string output,CancellationToken ct) {
   var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   using(var zip=ZipFile.Open(output,ZipArchiveMode.Create)) {
    Action<string,string> add=null;add=(source,name)=>{ct.ThrowIfCancellationRequested();if(String.Equals(Path.GetFullPath(source),Path.GetFullPath(output),StringComparison.OrdinalIgnoreCase))return;if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new IOException("Links and junctions are not included in archives: "+source);
     if(Directory.Exists(source)){zip.CreateEntry(name.TrimEnd('/')+"/");foreach(var p in Directory.GetFileSystemEntries(source))add(p,name+"/"+Path.GetFileName(p));}
     else{var entry=zip.CreateEntry(name,CompressionLevel.Optimal);using(var read=File.OpenRead(source))using(var write=entry.Open()){var buffer=new byte[81920];int n;while((n=read.Read(buffer,0,buffer.Length))>0){ct.ThrowIfCancellationRequested();write.Write(buffer,0,n);}}}};
    foreach(var p in paths){string name=Path.GetFileName(p.TrimEnd(Path.DirectorySeparatorChar));if(!names.Add(name))throw new IOException("Two selected items share the same name. Archive them separately.");add(p,name);}
   }
  }
  static void ExtractZip(string input,string output,CancellationToken ct) {
   if(Catalog.Extension(input)!="zip")throw new NotSupportedException("This build safely extracts ZIP archives. TAR and GZIP extraction are not yet implemented.");
   Directory.CreateDirectory(output);string root=Path.GetFullPath(output)+Path.DirectorySeparatorChar;long total=0,actual=0;
   using(var zip=ZipFile.OpenRead(input)){if(zip.Entries.Count>100000)throw new IOException("Archive has too many entries.");foreach(var entry in zip.Entries){ct.ThrowIfCancellationRequested();total+=entry.Length;if(total>20L*1024*1024*1024)throw new IOException("Archive exceeds the 20 GB extraction limit.");
    string relative=entry.FullName.Replace('/',Path.DirectorySeparatorChar);if(relative.Contains(':'))throw new IOException("Unsafe archive path.");
    string dest=Path.GetFullPath(Path.Combine(output,relative));if(!dest.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Unsafe archive path.");
    if(entry.FullName.EndsWith("/")||entry.FullName.EndsWith("\\")){Directory.CreateDirectory(dest);continue;}Directory.CreateDirectory(Path.GetDirectoryName(dest));
    using(var src=entry.Open())using(var dst=new FileStream(dest,FileMode.CreateNew)){var buffer=new byte[81920];int n;while((n=src.Read(buffer,0,buffer.Length))>0){ct.ThrowIfCancellationRequested();actual+=n;if(actual>20L*1024*1024*1024)throw new IOException("Archive exceeds the 20 GB extraction limit.");dst.Write(buffer,0,n);}}
   }}
  }
  void ImagePdf(string[] images,string output,CancellationToken ct) {
   using(var stream=File.Create(output)) {
    var offsets=new List<long>{0};Action<string> write=s=>{byte[] bytes=Encoding.ASCII.GetBytes(s);stream.Write(bytes,0,bytes.Length);};
    Action<int,string> obj=(n,body)=>{while(offsets.Count<=n)offsets.Add(0);offsets[n]=stream.Position;write(n+" 0 obj\n"+body+"\nendobj\n");};
    write("%PDF-1.4\n");obj(1,"<< /Type /Catalog /Pages 2 0 R >>");obj(2,"<< /Type /Pages /Count "+images.Length+" /Kids [ "+String.Join(" ",Enumerable.Range(0,images.Length).Select(i=>(3+i*3)+" 0 R"))+" ] >>");
    for(int i=0;i<images.Length;i++){ct.ThrowIfCancellationRequested();var image=DecodeImage(images[i]);double w=image.PixelWidth*0.75,h=image.PixelHeight*0.75;int n=3+i*3;
     string ws=w.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture),hs=h.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
     obj(n,"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 "+ws+" "+hs+"] /Resources << /XObject << /Im0 "+(n+2)+" 0 R >> >> /Contents "+(n+1)+" 0 R >>");
     string content="q "+ws+" 0 0 "+hs+" 0 0 cm /Im0 Do Q\n";obj(n+1,"<< /Length "+content.Length+" >>\nstream\n"+content+"endstream");
     var encoder=new JpegBitmapEncoder{QualityLevel=92};encoder.Frames.Add(BitmapFrame.Create(Fresh(image)));byte[] jpeg;using(var ms=new MemoryStream()){encoder.Save(ms);jpeg=ms.ToArray();}
     while(offsets.Count<=n+2)offsets.Add(0);offsets[n+2]=stream.Position;write((n+2)+" 0 obj\n<< /Type /XObject /Subtype /Image /Width "+image.PixelWidth+" /Height "+image.PixelHeight+" /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length "+jpeg.Length+" >>\nstream\n");stream.Write(jpeg,0,jpeg.Length);write("\nendstream\nendobj\n");
    }long xref=stream.Position;write("xref\n0 "+offsets.Count+"\n0000000000 65535 f \n");foreach(long offset in offsets.Skip(1))write(offset.ToString("D10")+" 00000 n \n");write("trailer\n<< /Size "+offsets.Count+" /Root 1 0 R >>\nstartxref\n"+xref+"\n%%EOF\n");
   }
  }
 }
}
