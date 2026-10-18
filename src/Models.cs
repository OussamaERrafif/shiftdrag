using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ShiftDrag {
 [DataContract] public class Preferences {
  [DataMember] public bool Enabled = true;
  [DataMember] public string FFmpeg = "";
  [DataMember] public string LibreOffice = "";
  [DataMember] public string OutputFolder = "";
  [DataMember] public int Quality = 85;
  [DataMember] public bool Glass = true;
  [DataMember] public string VideoPreset = "veryfast";
  [OnDeserializing] void Defaults(StreamingContext context){Glass=true;VideoPreset="veryfast";}
  public static string Home { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShiftDrag"); } }
  public static Preferences Load() { try { using(var s=File.OpenRead(Path.Combine(Home,"settings.json"))) return (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(s); } catch { return new Preferences(); } }
  public void Save() { Directory.CreateDirectory(Home); using(var s=File.Create(Path.Combine(Home,"settings.json"))) new DataContractJsonSerializer(typeof(Preferences)).WriteObject(s,this); }
 }
 public class Operation {
  public string Id, Label, Detail;
  public Operation(string id,string label,string detail) { Id=id;Label=label;Detail=detail; }
 }
 public static class Catalog {
  public static readonly string[] Images={"jpg","jpeg","png","bmp","tif","tiff","gif","webp","avif","heic","heif","ico"};
  public static readonly string[] Audio={"mp3","m4a","wav","flac","ogg","opus","aiff","aif","wma","aac"};
  public static readonly string[] Video={"mp4","mov","mkv","webm","avi","wmv","m4v"};
  public static string Extension(string p) { return Path.GetExtension(p).TrimStart('.').ToLowerInvariant(); }
  public static string Kind(string p) {
   if(Directory.Exists(p)) return "folder";
   var e=Extension(p);
   if(Images.Contains(e))return "image"; if(Audio.Contains(e))return "audio"; if(Video.Contains(e))return "video";
   if(new[]{"zip","tar","gz","tgz"}.Contains(e))return "archive";
   if(new[]{"srt","vtt"}.Contains(e))return "subtitle";
   if(new[]{"txt","md","markdown","html","htm"}.Contains(e))return "text";
   if(new[]{"doc","docx","rtf","odt","ppt","pptx","odp","xls","xlsx","ods","csv","tsv","epub","pdf"}.Contains(e))return "document";
   return "file";
  }
  public static List<Operation> Get(string[] files,string mode,Engine engine) {
   var list=new List<Operation>(); var kind=files.Length==0?"image":Kind(files[0]);
   bool mixed=files.Select(p=>Kind(p)=="document"?"document:"+Extension(p):Kind(p)).Distinct().Count()>1;
   Action<string,string,string> add=(id,label,detail)=>list.Add(new Operation(id,label,detail));
   if(mode=="Actions") {
    add("copy","Copy","Copy files to the Windows clipboard"); add("path","Copy path","Copy full paths, one per line");
    add("shelf","Float","Keep files in a floating, draggable shelf"); add("reveal","Show folder","Reveal the original in File Explorer");
    add("copyto","Copy to…","Choose a folder for duplicate files"); add("moveto","Move to…","Choose a folder and move the originals"); return list;
   }
   if(mixed || kind=="folder" || kind=="file") { add("zip","ZIP","Pack the selection into a ZIP archive");return list; }
   if(mode=="Convert") {
    string[] formats=new string[0];
    if(kind=="image")formats=engine.HasFFmpeg?new[]{"png","jpg","webp","tiff","bmp","gif","avif","pdf"}:new[]{"png","jpg","tiff","bmp","gif","pdf"};
    if(kind=="audio" && engine.HasFFmpeg)formats=new[]{"mp3","m4a","wav","flac","ogg","opus","aiff","wma"};
    if(kind=="video" && engine.HasFFmpeg)formats=new[]{"mp4","mov","mkv","webm","avi","wmv","gif","mp3"};
    if(kind=="subtitle")formats=new[]{"srt","vtt","txt"};
    if(kind=="document" && Extension(files[0])=="pdf" && engine.HasPdf)formats=new[]{"png","jpg","txt","md","docx","epub"};
    if(kind=="text")formats=new[]{"txt","md","html"};
    if(kind=="document" && engine.HasOffice && Extension(files[0])!="pdf") {
     var e=Extension(files[0]);
     formats=new[]{"xls","xlsx","ods","csv","tsv"}.Contains(e)?new[]{"xlsx","ods","csv","tsv","pdf"}:new[]{"ppt","pptx","odp"}.Contains(e)?new[]{"pptx","odp","pdf"}:new[]{"docx","odt","rtf","txt","html","pdf","epub"};
    }
    foreach(var f in formats) if(files.Length!=1 || Extension(files[0])!=f) add("to:"+f,f.ToUpperInvariant(),"Save a new ."+f+" copy beside the original");
    if(kind=="archive" && Extension(files[0])=="zip")add("extract","Extract","Safely unpack into a new folder");
    if(list.Count==0)add("zip","ZIP","Create a ZIP copy; additional formats may need a local engine");
   } else {
    if(kind=="image") {
     add("compress","Compress","Save a smaller JPEG copy");add("resize","Resize","Set a maximum width in pixels");
     add("crop","Crop","Choose the area to keep in a visual editor");add("rotate","Rotate 90°","Rotate clockwise into a new PNG");
     add("grayscale","Grayscale","Create a black-and-white PNG");add("strip","Strip metadata","Re-encode pixels without embedded metadata");
     add("pdf","Create PDF","Combine selected images, one page per image");
     add("flipx","Flip horizontal","Mirror the image left to right");add("flipy","Flip vertical","Mirror the image top to bottom");
     add("rotate180","Rotate 180°","Turn the image upside down");add("rotate270","Rotate left","Rotate counterclockwise into a new PNG");
     add("half","Half size","Resize to 50% of the original dimensions");add("background","Background","Flatten transparency onto a color you choose");
     add("watermark","Watermark","Add a text watermark to a new image copy");if(files.Length>1)add("collage","Make collage","Arrange selected images into a grid on a new canvas");
    }
    if((kind=="audio" || kind=="video") && engine.HasFFmpeg) {
     add("compress","Compress","Create a smaller media copy");add("trim","Trim","Enter a start time and duration");
     add("strip","Strip metadata","Remove metadata into a new copy");
     if(kind=="audio") {add("normalize","Normalize","Normalize loudness using EBU R128");add("channels","Channels","Convert to mono or stereo");add("visualize","Visualizer","Create an orange waveform MP4");add("bleep","Bleep","Replace a time range with a 1 kHz tone");}
     else {add("mute","Mute","Create a video without its audio track");add("audio","Extract audio","Export the audio as MP3");add("frame","Save frame","Export the frame at a chosen time as PNG");add("cropvideo","Crop","Enter the video region to keep");add("speed","Speed","Change playback speed and preserve audio pitch");add("splitvideo","Split","Split into clips of a chosen duration");add("redactvideo","Redact","Cover a rectangle throughout the video");if(files.Length>1)add("joinvideo","Join videos","Normalize to the first video canvas and join in selection order");}
    }
    if(kind=="document" && Extension(files[0])=="pdf" && engine.HasPdf){add("compress","Compress","Losslessly compress PDF content streams");add("strip","Strip tags","Remove PDF document metadata");add("splitpdf","Split pages","Save one PDF per page in a new folder");add("organizepdf","Organize","Reorder, duplicate, remove, or rotate pages");if(files.Length>1)add("mergepdf","Merge PDFs","Combine PDFs in selection order");}
    if(kind=="archive" && Extension(files[0])=="zip") add("extract","Extract","Safely unpack into a new folder");
    add("zip","ZIP selection","Pack files and folders into a ZIP archive");
   }
   return list;
  }
 }
}
