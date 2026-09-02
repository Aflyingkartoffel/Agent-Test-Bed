namespace EquationSynth;

public enum EnvelopeStage { Idle, Attack, Decay, Sustain, Release }

public sealed class EnvelopeSettings {
    public double Attack { get; set; } = .01;
    public double Decay { get; set; } = .10;
    public double Sustain { get; set; } = 1;
    public double Release { get; set; } = .10;
    public bool IsValid => double.IsFinite(Attack) && double.IsFinite(Decay) && double.IsFinite(Sustain) && double.IsFinite(Release) && Attack >= 0 && Decay >= 0 && Release >= 0 && Sustain >= 0 && Sustain <= 1;
    public void Normalize() { Attack=Math.Max(0,double.IsFinite(Attack)?Attack:.01); Decay=Math.Max(0,double.IsFinite(Decay)?Decay:.1); Release=Math.Max(0,double.IsFinite(Release)?Release:.1); Sustain=Math.Clamp(double.IsFinite(Sustain)?Sustain:1,0,1); }
}

public sealed class AdsrEnvelope {
    readonly EnvelopeSettings settings; public EnvelopeStage Stage { get; private set; } = EnvelopeStage.Idle; public double Level { get; private set; }
    public AdsrEnvelope(EnvelopeSettings settings) => this.settings=settings;
    public void GateOn() { Stage=EnvelopeStage.Attack; if(settings.Attack<=0){Level=1;Stage=settings.Decay<=0?EnvelopeStage.Sustain:EnvelopeStage.Decay;} }
    public void GateOff() { if(Stage!=EnvelopeStage.Idle) Stage=EnvelopeStage.Release; }
    public double Next(double seconds) { if(!double.IsFinite(seconds)||seconds<0)seconds=0;switch(Stage){case EnvelopeStage.Attack: if(settings.Attack<=0){Level=1;Stage=EnvelopeStage.Decay;}else{Level+=seconds/settings.Attack;if(Level>=1){Level=1;Stage=settings.Decay<=0?EnvelopeStage.Sustain:EnvelopeStage.Decay;}}break;case EnvelopeStage.Decay:if(settings.Decay<=0){Level=settings.Sustain;Stage=EnvelopeStage.Sustain;}else{Level-=seconds*(1-settings.Sustain)/settings.Decay;if(Level<=settings.Sustain){Level=settings.Sustain;Stage=EnvelopeStage.Sustain;}}break;case EnvelopeStage.Sustain:Level=settings.Sustain;break;case EnvelopeStage.Release:if(settings.Release<=0){Level=0;Stage=EnvelopeStage.Idle;}else{Level-=seconds/settings.Release;if(Level<=0){Level=0;Stage=EnvelopeStage.Idle;}}break;case EnvelopeStage.Idle:Level=0;break;}return double.IsFinite(Level)?Math.Clamp(Level,0,1):0; }
}

public readonly record struct StereoSample(double Left,double Right);
public static class MixerMath {
    public static bool IsAudible(EquationEntry layer, bool anySolo) => layer.IsAudioEnabled && !layer.IsMuted && (!anySolo || layer.IsSolo);
    public static (double Left,double Right) EqualPowerPan(double pan) { var angle=(Math.Clamp(pan,-1,1)+1)*Math.PI/4;return (Math.Cos(angle),Math.Sin(angle)); }
    public static StereoSample Mix(IReadOnlyList<(double Sample,EquationEntry Layer,double Envelope)> voices,double masterGain){var anySolo=voices.Any(v=>v.Layer.IsSolo);var l=0d;var r=0d;var count=0;foreach(var voice in voices){if(!IsAudible(voice.Layer,anySolo))continue;var pan=EqualPowerPan(voice.Layer.AudioPan);l+=voice.Sample*voice.Layer.AudioVolume*voice.Envelope*pan.Left;r+=voice.Sample*voice.Layer.AudioVolume*voice.Envelope*pan.Right;count++;}var attenuation=count>1?1/Math.Sqrt(count):1;l*=attenuation*masterGain;r*=attenuation*masterGain;return new StereoSample(Math.Tanh(l*1.4),Math.Tanh(r*1.4));}
}
