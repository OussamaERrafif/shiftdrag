using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using ShiftDrag;
class BenchmarkRunner {
 [STAThread] static int Main(string[] args){string root=Path.GetFullPath(args[0]);string label=args[1];var prefs=new Preferences{OutputFolder=Path.Combine(root,"output",label)};var engine=new Engine(prefs);var lines=new List<string>{"case,repeat,milliseconds,bytes"};
  foreach(var pair in new[]{new[]{"image-jpeg","photo.png","to:jpg"},new[]{"image-rotate","photo.png","rotate"},new[]{"video-convert","clip.mp4","to:mp4"},new[]{"video-mute","clip.mp4","mute"}}){for(int i=0;i<3;i++){var clock=Stopwatch.StartNew();var outputs=engine.Execute(new[]{Path.Combine(root,pair[1])},new Operation(pair[2],pair[2],""),new Dictionary<string,string>(),CancellationToken.None,s=>{}).GetAwaiter().GetResult();clock.Stop();lines.Add(pair[0]+","+i+","+clock.ElapsedMilliseconds+","+new FileInfo(outputs[0]).Length);}}
  File.WriteAllLines(Path.Combine(root,label+".csv"),lines);Console.WriteLine(String.Join(Environment.NewLine,lines));return 0;
 }
}
