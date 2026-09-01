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
  Console.WriteLine($"Equation Synth tests passed: {n}");
 }
}
