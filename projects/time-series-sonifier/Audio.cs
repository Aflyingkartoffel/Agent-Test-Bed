using System.Runtime.InteropServices;

namespace TimeSeriesSonifier;

public enum WaveformType { Sine, Triangle, Square, Saw }
public enum AudioEngineState { Stopped, Starting, Running, Stopping, Faulted, Disposed }

public static class PitchMapper
{
    public const double DefaultMinimumFrequency = 110;
    public const double DefaultMaximumFrequency = 1760;
    public const double MaximumSafeFrequency = 20000;
    public static double Map(double normalized, double minimumFrequency = DefaultMinimumFrequency, double maximumFrequency = DefaultMaximumFrequency)
    {
        var min = Sanitize(minimumFrequency, DefaultMinimumFrequency); var max = Sanitize(maximumFrequency, DefaultMaximumFrequency);
        if (min >= max) { min = DefaultMinimumFrequency; max = DefaultMaximumFrequency; }
        return min * Math.Pow(max / min, Math.Clamp(double.IsFinite(normalized) ? normalized : .5, 0, 1));
    }
    static double Sanitize(double value, double fallback) => double.IsFinite(value) && value > 0 ? Math.Min(value, MaximumSafeFrequency) : fallback;
}

public sealed class ParameterSmoother
{
    public double Current { get; private set; }
    public double Target { get; private set; }
    public double GlideSeconds { get; set; } = .05;
    public ParameterSmoother(double initial) { Current = Target = initial; }
    public void SetTarget(double target) => Target = double.IsFinite(target) ? Math.Max(0, target) : Current;
    public double Advance(double deltaSeconds) { deltaSeconds = Math.Clamp(double.IsFinite(deltaSeconds) ? deltaSeconds : 0, 0, 1); var amount = GlideSeconds <= 0 ? 1 : Math.Clamp(deltaSeconds / GlideSeconds, 0, 1); Current += (Target - Current) * amount; return Current; }
}

public sealed class Oscillator
{
    public const int SampleRate = 48000;
    public double Phase { get; private set; }
    public WaveformType Waveform { get; set; } = WaveformType.Sine;
    public float NextSample(double frequency)
    {
        var safe = double.IsFinite(frequency) ? Math.Clamp(frequency, 0, SampleRate * .45) : 0; var sample = Waveform switch { WaveformType.Triangle => 1 - 4 * Math.Abs(Math.Round(Phase) - Phase), WaveformType.Square => Phase < .5 ? 1 : -1, WaveformType.Saw => 2 * Phase - 1, _ => Math.Sin(2 * Math.PI * Phase) }; Phase += safe / SampleRate; Phase -= Math.Floor(Phase); return (float)Math.Clamp(double.IsFinite(sample) ? sample : 0, -1, 1);
    }
}

public interface IAudioBackend : IDisposable
{
    void Start(Action<float[]> render);
    void Stop();
}

public sealed class AudioEngine : IDisposable
{
    readonly object gate = new(); readonly IAudioBackend backend; readonly Oscillator oscillator = new(); readonly ParameterSmoother smoother; double targetFrequency = PitchMapper.Map(.5); bool disposed;
    public AudioEngine(IAudioBackend? backend = null) { this.backend = backend ?? new WaveOutBackend(); smoother = new ParameterSmoother(targetFrequency); }
    public AudioEngineState State { get; private set; } = AudioEngineState.Stopped;
    public string Status { get; private set; } = "Stopped";
    public WaveformType Waveform { get=>oscillator.Waveform; set=>oscillator.Waveform=value; }
    public double TargetFrequency { get=>Volatile.Read(ref targetFrequency); private set=>Volatile.Write(ref targetFrequency, value); }
    public double CurrentFrequency => smoother.Current;
    public double Volume { get; set; } = .25;
    public void SetTargetFrequency(double frequency) => TargetFrequency = PitchMapper.Map(0, frequency, Math.Max(frequency + 1, frequency + 1));
    public void SetTargetFrequencyFromNormalized(double normalized, double minimum, double maximum) => TargetFrequency = PitchMapper.Map(normalized, minimum, maximum);
    public bool Start()
    {
        lock (gate) { if (disposed || State == AudioEngineState.Running || State == AudioEngineState.Starting) return State == AudioEngineState.Running; State = AudioEngineState.Starting; Status = "Starting"; try { backend.Start(Render); State = AudioEngineState.Running; Status = "Running"; return true; } catch (Exception ex) { State = AudioEngineState.Faulted; Status = $"Audio unavailable: {ex.Message}"; return false; } }
    }
    public void Stop()
    {
        lock (gate) { if (disposed || State is AudioEngineState.Stopped or AudioEngineState.Stopping) return; State = AudioEngineState.Stopping; try { backend.Stop(); State = AudioEngineState.Stopped; Status = "Stopped"; } catch (Exception ex) { State = AudioEngineState.Faulted; Status = $"Audio stop fault: {ex.Message}"; } }
    }
    void Render(float[] buffer) { var target = TargetFrequency; for (var i = 0; i < buffer.Length; i++) { var current = smoother.Advance(1.0 / Oscillator.SampleRate); buffer[i] = (float)(oscillator.NextSample(current) * Math.Clamp(double.IsFinite(Volume) ? Volume : 0, 0, 1)); } }
    public void Dispose() { lock (gate) { if (disposed) return; Stop(); backend.Dispose(); disposed = true; State = AudioEngineState.Disposed; Status = "Disposed"; } }
}

public sealed class AudioLifecycle
{
    readonly IAudioBackend backend; public AudioEngineState State { get; private set; } = AudioEngineState.Stopped;
    public AudioLifecycle(IAudioBackend backend) => this.backend = backend;
    public bool Start() { if (State is AudioEngineState.Disposed or AudioEngineState.Running) return State == AudioEngineState.Running; try { State = AudioEngineState.Starting; backend.Start(_ => { }); State = AudioEngineState.Running; return true; } catch { State = AudioEngineState.Faulted; return false; } }
    public void Stop() { if (State is AudioEngineState.Stopped or AudioEngineState.Disposed or AudioEngineState.Stopping) return; State = AudioEngineState.Stopping; try { backend.Stop(); State = AudioEngineState.Stopped; } catch { State = AudioEngineState.Faulted; } }
    public void Dispose() { if (State == AudioEngineState.Disposed) return; Stop(); backend.Dispose(); State = AudioEngineState.Disposed; }
}

sealed class WaveOutBackend : IAudioBackend
{
    const int BufferCount=3, Samples=512, CallbackFunction=0x00030000, WomDone=0x3BD, FloatFormat=3; readonly object gate=new(); readonly byte[][] buffers=new byte[BufferCount][]; readonly GCHandle[] pins=new GCHandle[BufferCount]; readonly Header[] headers=new Header[BufferCount]; readonly WaveOutProc callback; IntPtr device; Action<float[]>? render; int callbacksInFlight; bool running;
    public WaveOutBackend() => callback = OnMessage;
    public void Start(Action<float[]> renderer) { lock(gate){ if(running)return; render=renderer; var format=new Format{Tag=FloatFormat,Channels=1,Rate=Oscillator.SampleRate,BytesPerSecond=Oscillator.SampleRate*4,BlockAlign=4,Bits=32}; if(waveOutOpen(out device,-1,ref format,callback,IntPtr.Zero,CallbackFunction)!=0)throw new InvalidOperationException("audio device could not be opened"); try { running=true; for(var i=0;i<BufferCount;i++){buffers[i]=new byte[Samples*4];pins[i]=GCHandle.Alloc(buffers[i],GCHandleType.Pinned);headers[i]=new Header{Data=pins[i].AddrOfPinnedObject(),Length=(uint)buffers[i].Length};Check(waveOutPrepareHeader(device,ref headers[i],Marshal.SizeOf<Header>()),"prepare buffer");Fill(i);Check(waveOutWrite(device,ref headers[i],Marshal.SizeOf<Header>()),"queue buffer");} }catch{ Stop(); throw; } } }
    void Fill(int index){var floats=new float[Samples];render?.Invoke(floats);for(var i=0;i<Samples;i++)BitConverter.TryWriteBytes(buffers[index].AsSpan(i*4,4),float.IsFinite(floats[i])?Math.Clamp(floats[i],-1,1):0);}
    void OnMessage(IntPtr h,uint message,IntPtr a,IntPtr b,IntPtr c){if(message!=WomDone||!Volatile.Read(ref running))return;Interlocked.Increment(ref callbacksInFlight);try{if(!Volatile.Read(ref running))return;var index=(Environment.TickCount&int.MaxValue)%BufferCount;Fill(index);if(Volatile.Read(ref running)&&device!=IntPtr.Zero)waveOutWrite(device,ref headers[index],Marshal.SizeOf<Header>());}finally{Interlocked.Decrement(ref callbacksInFlight);}}
    public void Stop(){lock(gate){if(device==IntPtr.Zero){running=false;return;}running=false;var handle=device;try{waveOutReset(handle);var deadline=Environment.TickCount64+1000;while(Volatile.Read(ref callbacksInFlight)>0&&Environment.TickCount64<deadline)Thread.Yield();for(var i=0;i<BufferCount;i++){if(handle!=IntPtr.Zero)waveOutUnprepareHeader(handle,ref headers[i],Marshal.SizeOf<Header>());if(pins[i].IsAllocated)pins[i].Free();}}finally{waveOutClose(handle);device=IntPtr.Zero;render=null;}}}
    public void Dispose()=>Stop(); static void Check(uint code,string operation){if(code!=0)throw new InvalidOperationException($"audio {operation} failed (code {code})");}
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void WaveOutProc(IntPtr h,uint msg,IntPtr a,IntPtr b,IntPtr c); [StructLayout(LayoutKind.Sequential)]struct Format{public ushort Tag,Channels;public uint Rate,BytesPerSecond;public ushort BlockAlign,Bits;} [StructLayout(LayoutKind.Sequential)]struct Header{public IntPtr Data;public uint Length,Recorded,User,Flags,Loops;public IntPtr Next,Reserved;}
    [DllImport("winmm.dll")]static extern uint waveOutOpen(out IntPtr h,int device,ref Format format,WaveOutProc callback,IntPtr instance,uint flags);[DllImport("winmm.dll")]static extern uint waveOutPrepareHeader(IntPtr h,ref Header header,int size);[DllImport("winmm.dll")]static extern uint waveOutUnprepareHeader(IntPtr h,ref Header header,int size);[DllImport("winmm.dll")]static extern uint waveOutWrite(IntPtr h,ref Header header,int size);[DllImport("winmm.dll")]static extern uint waveOutReset(IntPtr h);[DllImport("winmm.dll")]static extern uint waveOutClose(IntPtr h);
}
