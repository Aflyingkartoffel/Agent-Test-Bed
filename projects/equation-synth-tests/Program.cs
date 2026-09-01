using EquationSynth;
static class T {
 static int n; static readonly Dictionary<string,double> None=[];
 static void Ok(bool b,string m){if(!b)throw new Exception(m);n++;}
 static void Main(){
  Ok(Expression.Parse("2").Evaluate(0,0,None).Value==2,"literal");
  Ok(Expression.Parse("2+3-1").Evaluate(0,0,None).Value==4,"add/subtract");
  Ok(Expression.Parse("2*3/2").Evaluate(0,0,None).Value==3,"multiply/divide");
  Ok(Expression.Parse("2+3*4").Evaluate(0,0,None).Value==14,"precedence");
  Ok(Expression.Parse("(2+3)*4").Evaluate(0,0,None).Value==20,"parentheses");
  Ok(Expression.Parse("2^3").Evaluate(0,0,None).Value==8,"power");
  Ok(Expression.Parse("-2").Evaluate(0,0,None).Value==-2,"unary");
  Ok(Math.Abs(Expression.Parse("sin(pi/2)").Evaluate(0,0,None).Value-1)<1e-9,"sin");
  Ok(Math.Abs(Expression.Parse("cos(0)").Evaluate(0,0,None).Value-1)<1e-9,"cos");
  Ok(Math.Abs(Expression.Parse("abs(sin(-pi/2))").Evaluate(0,0,None).Value-1)<1e-9,"nested functions");
  Ok(Expression.Parse("e").Evaluate(0,0,None).Value>2.7,"e");
  Ok(Expression.Parse("x").Evaluate(3,0,None).Value==3,"x");
  Ok(Expression.Parse("t").Evaluate(0,3,None).Value==3,"t");
  var e=Expression.Parse("sin(x*a)+b*cos(t)"); Ok(e.Parameters.SequenceEqual(new[]{"a","b"}),"parameters");
  var values=new Dictionary<string,double>{{"a",2},{"b",1}}; Ok(e.Evaluate(1,0,values).Status==ValueStatus.Valid,"multiple params");
  try{Expression.Parse("sin(");Ok(false,"malformed");}catch(ParseException){Ok(true,"malformed");}
  try{Expression.Parse("unknown(x)");Ok(false,"unknown function");}catch(ParseException){Ok(true,"unknown function");}
  Ok(Expression.Parse("1/0").Evaluate(0,0,None).Status is ValueStatus.NaN or ValueStatus.Invalid,"division safety");
  Ok(Expression.Parse("sqrt(-1)").Evaluate(0,0,None).Status==ValueStatus.NaN,"NaN safety");
  Ok(Expression.Parse("exp(100000)").Evaluate(0,0,None).Status==ValueStatus.Infinity,"Infinity safety");
  var wave=new WaveformEngine();var w=wave.Generate(Expression.Parse("sin(x)"),0,None);Ok(w.Length==WaveformEngine.SampleCount,"wavetable size");Ok(w.Max()<=1&&w.Min()>=-1,"normalization");
  Ok(wave.Generate(Expression.Parse("sqrt(-1)"),0,None).All(v=>v==0),"invalid audio sample");
  var p=Expression.Parse("sin(x*a)");Ok(p.Evaluate(1,0,new Dictionary<string,double>{{"a",1}}).Value!=p.Evaluate(1,0,new Dictionary<string,double>{{"a",2}}).Value,"parameter changes output");
  var timed=Expression.Parse("sin(x+t)");Ok(timed.Evaluate(1,0,None).Value!=timed.Evaluate(1,1,None).Value,"time changes output");
  var staticE=Expression.Parse("sin(x)");Ok(Math.Abs(staticE.Evaluate(1,0,None).Value-staticE.Evaluate(1,1,None).Value)<1e-12,"static ignores time");
  var te=new TimeEngine();te.Play();te.Update(2);Ok(te.Time==2,"time update");te.Reset();Ok(te.Time==0,"time reset");
  var dir=Path.Combine(Path.GetTempPath(),"equation-synth-test.json");PresetService.Save(dir,new PresetState{Equation="sin(x)",Frequency=220});var loaded=PresetService.Load(dir);Ok(loaded.Equation=="sin(x)"&&loaded.Frequency==220,"preset round trip");File.WriteAllText(dir,"{\"Equation\":\"cos(x)\"}");Ok(PresetService.Load(dir).Volume>.1,"legacy defaults");File.Delete(dir);
  var doc=EquationDocument.CreateDefault();var first=doc.Selected!;var second=doc.Add("cos(x*a)");Ok(doc.Equations.Count==2&&first.Id!=second.Id,"add equations");Ok(doc.Parameters.Items.Count==1&&doc.Parameters.Items[0].Name=="a","shared parameters");doc.Select(first);doc.RemoveSelected();Ok(doc.Equations.Count==1&&doc.Selected==second,"delete/select equations");second.IsVisible=false;Ok(!second.IsVisible,"visibility");second.ColorHex="#FF00AA";Ok(second.ColorHex=="#FF00AA","equation color");
  doc.Add("sin(x*b)");doc.Parameters.Items[0].Value=3.5;doc.ReconcileParameters();Ok(doc.Parameters.Items.Any(p=>p.Name=="b"),"parameter coexist");Ok(doc.Parameters.Items.First(p=>p.Name=="a").Value==3.5,"parameter survives reconciliation");doc.Equations.RemoveAll(x=>x.ExpressionText.Contains("a"));doc.ReconcileParameters();Ok(!doc.Parameters.Items.Any(p=>p.Name=="a"),"unused parameter removed");
  var metadata=new Parameter("m"){Minimum=5,Maximum=2,Step=0};Ok(!metadata.IsMetadataValid,"metadata validation");metadata.Normalize();Ok(metadata.IsMetadataValid&&metadata.Value>=metadata.Minimum&&metadata.Value<=metadata.Maximum,"metadata normalization");var cam=new GraphCamera();var screen=cam.WorldToScreen(2,-1,800,400);var world=cam.ScreenToWorld(screen.X,screen.Y,800,400);Ok(Math.Abs(world.X-2)<1e-9&&Math.Abs(world.Y+1)<1e-9,"camera round trip");var oldCenter=cam.CenterX;cam.Pan(2,1);Ok(cam.CenterX==oldCenter+2,"camera pan");var anchor=cam.ScreenToWorld(300,150,800,400);cam.ZoomAt(.5,anchor.X,anchor.Y);var anchorAfter=cam.ScreenToWorld(300,150,800,400);Ok(Math.Abs(anchor.X-anchorAfter.X)<1e-9&&Math.Abs(anchor.Y-anchorAfter.Y)<1e-9,"cursor zoom anchor");cam.Reset();Ok(cam.XMin==-10&&cam.XMax==10&&cam.YMin==-5&&cam.YMax==5,"camera reset");Ok(!cam.Set(1,1,0,1),"invalid ranges rejected");
  Ok(GraphSampler.NiceSpacing(.4)==.05&&GraphSampler.NiceSpacing(40)==5,"nice grid spacing");Ok(GraphSampler.FormatLabel(.3,.1)=="0.3","label format");Ok(GraphSampler.Sample(EquationDocument.CreateDefault().Selected!,cam,0,None,500).Count==750,"viewport sampling");var discontinuity=doc.Add("1/x");Ok(discontinuity.ParsedExpression!.Evaluate(0,0,None).Status!=ValueStatus.Valid,"discontinuity samples break");
  var multiPath=Path.Combine(Path.GetTempPath(),"equation-synth-multi.json");PresetService.Save(multiPath,new PresetState{Equation="cos(x)",Frequency=220});Ok(PresetService.Load(multiPath).Equation=="cos(x)","legacy single equation migration source");File.Delete(multiPath);
  Console.WriteLine($"Equation Synth tests passed: {n}");
 }
}
