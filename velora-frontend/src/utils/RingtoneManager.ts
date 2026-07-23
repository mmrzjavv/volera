// src/utils/RingtoneManager.ts

class RingtoneManager {
  private audioContext: AudioContext | null = null;
  private oscillator: OscillatorNode | null = null;
  private gainNode: GainNode | null = null;
  private intervalId: number | null = null;

  private initAudioContext() {
    if (!this.audioContext) {
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    }
    if (this.audioContext.state === 'suspended') {
      this.audioContext.resume();
    }
  }

  public playIncomingRing() {
    this.stop(); // Stop any existing sound
    this.initAudioContext();
    if (!this.audioContext) return;

    const playTone = () => {
      if (!this.audioContext) return;
      
      const osc = this.audioContext.createOscillator();
      const gain = this.audioContext.createGain();

      osc.type = 'sine';
      osc.frequency.setValueAtTime(440, this.audioContext.currentTime); // A4
      // Modulate pitch slightly for a "phone" sound
      osc.frequency.linearRampToValueAtTime(500, this.audioContext.currentTime + 0.1);
      
      gain.gain.setValueAtTime(0, this.audioContext.currentTime);
      gain.gain.linearRampToValueAtTime(0.5, this.audioContext.currentTime + 0.1);
      gain.gain.linearRampToValueAtTime(0, this.audioContext.currentTime + 1.5);

      osc.connect(gain);
      gain.connect(this.audioContext.destination);

      osc.start();
      osc.stop(this.audioContext.currentTime + 1.5);
    };

    playTone();
    this.intervalId = window.setInterval(playTone, 3000); // Repeat every 3s
  }

  public playOutgoingRing() {
    this.stop();
    this.initAudioContext();
    if (!this.audioContext) return;

    const playTone = () => {
      if (!this.audioContext) return;

      const osc = this.audioContext.createOscillator();
      const gain = this.audioContext.createGain();

      osc.type = 'sine';
      osc.frequency.setValueAtTime(400, this.audioContext.currentTime); 

      gain.gain.setValueAtTime(0, this.audioContext.currentTime);
      gain.gain.linearRampToValueAtTime(0.3, this.audioContext.currentTime + 0.1);
      gain.gain.linearRampToValueAtTime(0, this.audioContext.currentTime + 1.0);

      osc.connect(gain);
      gain.connect(this.audioContext.destination);

      osc.start();
      osc.stop(this.audioContext.currentTime + 1.0);
    };

    playTone();
    this.intervalId = window.setInterval(playTone, 2500); // Repeat every 2.5s
  }

  public playVideoIncomingRing() {
      this.stop();
      this.initAudioContext();
      if (!this.audioContext) return;
  
      const playTone = () => {
        if (!this.audioContext) return;
        
        const osc = this.audioContext.createOscillator();
        const gain = this.audioContext.createGain();
  
        osc.type = 'triangle'; // Different wave for video
        osc.frequency.setValueAtTime(600, this.audioContext.currentTime);
        
        gain.gain.setValueAtTime(0, this.audioContext.currentTime);
        gain.gain.linearRampToValueAtTime(0.5, this.audioContext.currentTime + 0.1);
        gain.gain.linearRampToValueAtTime(0, this.audioContext.currentTime + 1.0);
  
        osc.connect(gain);
        gain.connect(this.audioContext.destination);
  
        osc.start();
        osc.stop(this.audioContext.currentTime + 1.0);
      };
  
      playTone();
      this.intervalId = window.setInterval(playTone, 2000); 
  }

  public stop() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
    if (this.oscillator) {
      try { this.oscillator.stop(); } catch {}
      this.oscillator.disconnect();
      this.oscillator = null;
    }
    if (this.gainNode) {
      this.gainNode.disconnect();
      this.gainNode = null;
    }
  }

  /** Resume AudioContext during a user gesture (required on iOS/Android before call audio).
   * Call ONLY after getUserMedia has already run — resume() can consume the gesture on iOS. */
  public unlock() {
    this.initAudioContext();
  }
}

export const ringtoneManager = new RingtoneManager();
