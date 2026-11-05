"""Original deterministic PCM score assets. No licensed samples or plugins.
Soft mallets, restrained percussion and stereo reflection tails; 84 BPM.
"""
from pathlib import Path
import math,random,wave,struct,json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Resources/LoomAudio';OUT.mkdir(parents=True,exist_ok=True)
RATE=32000;BPM=84;BEAT=60/BPM
PI=2*math.pi

def write(name,mono,peak=.55,reflections=True):
 n=len(mono);tail=int(.42*RATE) if reflections else 0
 left=[0.]*(n+tail);right=[0.]*(n+tail)
 for i,v in enumerate(mono):left[i]=right[i]=v
 if reflections:
  for sec,gain,side in [(.083,.17,0),(.137,.14,1),(.211,.09,0),(.307,.065,1)]:
   offset=int(sec*RATE);dest=left if side==0 else right
   for i,v in enumerate(mono):
    if i+offset<len(dest):dest[i+offset]+=v*gain
 maximum=max(max(abs(v) for v in left),max(abs(v) for v in right),1e-9);scale=min(1,peak/maximum)
 payload=bytearray()
 for l,r in zip(left,right):payload.extend(struct.pack('<hh',round(l*scale*32767),round(r*scale*32767)))
 path=OUT/(name+'.wav')
 with wave.open(str(path),'wb') as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(payload)
 return (left,right,scale)

def mallet(frequency,duration=.85,soft=1):
 n=int(duration*RATE);data=[]
 for i in range(n):
  t=i/RATE;attack=1-math.exp(-t/0.007);end=min(1,(duration-t)/.045)
  value=(math.sin(PI*frequency*t)*math.exp(-t*4.2)+.23*math.sin(PI*frequency*2*t)*math.exp(-t*9)+.075*math.sin(PI*frequency*3*t)*math.exp(-t*15))
  data.append(value*.37*attack*end*soft)
 return data

clips={}
def add(key,data,peak=.55,reflect=True):clips[key]=write(key,data,peak,reflect)
add('select',mallet(587.33,.25,.5));add('connect',mallet(440,.45,.5));add('undo',mallet(293.665,.3,.45))
add('mirror',mallet(698.46,.65,.65));add('splitter',mallet(880,.65,.5))
for name,f in [('goal-low',587.33),('goal-mid',698.46),('goal-high',880),('flow',293.665),('wait',1174.66)]:add(name,mallet(f,1.15 if name.startswith('goal') else .48,.7 if name.startswith('goal') else .22))
rng=random.Random(42)
for key,down in [('tick',False),('downbeat',True)]:
 duration=.16 if down else .07;data=[];previous=0
 for i in range(int(duration*RATE)):
  t=i/RATE;noise=rng.uniform(-1,1);hp=noise-previous;previous=noise
  value=math.sin(PI*(67 if down else 132)*t)*math.exp(-t*(32 if down else 65))*.20+hp*.014*math.exp(-t*90)
  data.append(value*min(1,t/.003)*min(1,(duration-t)/.012))
 add(key,data,.24,False)
add('error',mallet(146.83,.36,.35))
# The success phrase resolves D-F-A-E into D; the last note has a long soft tail.
phrase=[0.] * int(2.8*RATE)
for at,f,gain in [(0,293.665,.75),(.12,349.23,.65),(.24,440,.55),(.36,659.25,.40),(.62,587.33,.60)]:
 for i,v in enumerate(mallet(f,1.8,gain)):
  k=int(at*RATE)+i
  if k<len(phrase):phrase[k]+=v
add('win',phrase,.56)
duration=8*BEAT;n=round(duration*RATE);duration=n/RATE;pad=[]
# Whole cycles make a seamless loop without restarting envelopes.
frequencies=[(round(f*duration)/duration,g) for f,g in [(73.416,.045),(110,.026),(146.83,.022),(174.615,.013),(220,.010)]]
for i in range(n):
 t=i/RATE;breath=.78+.22*math.cos(PI*t/duration)
 pad.append(sum(math.sin(PI*f*t)*g for f,g in frequencies)*breath)
add('ambient',pad,.18,False)
# A standalone mix lets a reviewer hear the exact imported timbres.
length=int(10.5*RATE);mix=[[0.]*length,[0.]*length]
def place(key,at,gain=1):
 l,r,scale=clips[key]
 for ch,data in enumerate([l,r]):
  offset=round(at*RATE)
  for i,v in enumerate(data):
   if offset+i<length:mix[ch][offset+i]+=v*scale*gain
for beat in range(8):
 place('downbeat' if beat%4==0 else 'tick',beat*BEAT,.6)
 if beat in [2,3,4]:place('flow',beat*BEAT,.5)
 if beat in [4,5,6]:place('wait',beat*BEAT,.32)
for key in ['goal-low','goal-mid','goal-high']:place(key,7*BEAT,.65)
place('win',7.5*BEAT,.65)
peak=max(max(abs(v) for v in ch) for ch in mix);gain=min(1,.8/max(peak,1e-9));payload=bytearray()
for l,r in zip(*mix):payload.extend(struct.pack('<hh',round(l*gain*32767),round(r*gain*32767)))
preview=ROOT/'Builds/Score092-Preview.wav'
with wave.open(str(preview),'wb') as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(payload)
report={name:{'seconds':len(pair[0])/RATE,'peak':max(max(abs(v) for v in ch) for ch in pair[:2])*pair[2]} for name,pair in clips.items()}
(ROOT/'Builds/Score092-AssetReport.json').write_text(json.dumps({'bpm':BPM,'sampleRate':RATE,'clips':report,'previewPeak':peak*gain},indent=2))
print('Authored',len(clips),'original stereo PCM clips and',preview)
