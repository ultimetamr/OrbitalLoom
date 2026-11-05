"""Offline, deterministic level authoring. No runtime randomness or dependencies.

The first six lesson profiles are explicit; later layouts are searched under
chapter constraints and accepted only when their reachable orientation is unique.
Device playtesting, rather than this certificate, decides experiential difficulty.
"""
import json
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NAMES = [
    ["第一束光", "跨过星隙", "双星初遇", "一动两变", "回望终点", "织成归途", "错位入口", "共同方向", "短暂分离", "初光成图"],
    ["齿轮共鸣", "相反方向", "同进一格", "镜中星轨", "牵一发动两环", "逆向思考", "保留另一束光", "相位转移", "两难之间", "双生共振"],
    ["封锁刻度", "穿过间隙", "固定桥梁", "侧身观察", "有限的连接", "从背面读光", "断桥重织", "绕过禁区", "空间交错", "深空归航"],
    ["等待另一束光", "一刻之差", "先后有序", "金光稍候", "蓝光稍候", "同时抵达", "时序与镜像", "共同起点", "最后的脉冲", "星轨重生"],
]
LESSONS = ["用 + / − 调整后环 B，再按发射", "点 A 出口，再点 B 上闪亮的同号入口", "转动前环 A，让两路光一起接入", "一次转环影响两路光，先观察再试射", "从同形终点沿箭头倒推入口", "试射会保留断点；每次失败都能缩小范围"]

def required(d, a, b):
    needed = []
    for beam in range(d["beamCount"]):
        ca = next((c for c in d["channels"][0] if (c["input"]+a)%8 == d["sourceSlots"][beam]), None)
        if ca is None: return None
        slot = (ca["output"]+a)%8
        cb = next((c for c in d["channels"][1] if (c["input"]+b)%8 == slot), None)
        if cb is None or (cb["output"]+b)%8 != d["goalSlots"][beam] or slot in d["forbiddenSlots"]: return None
        needed.append(slot)
    if d["fixedBridges"] and not set(needed) <= set(d["initialBridges"]): return None
    return sorted(set(needed))

def reachable(d, a, b):
    if any(d["lockedRings"][i] and [a,b][i] != d["initialSteps"][i] for i in range(2)): return False
    c = d["coupling"]
    return c == 0 or (b-d["initialSteps"][1]-c*(a-d["initialSteps"][0])) % 8 == 0

def create(index):
    chapter, number = index//10+1, index%10+1
    for attempt in range(10000):
        rng = random.Random(6831 + index*10007 + attempt*71)
        a,b = rng.randrange(8),rng.randrange(8)
        ai,ao = rng.sample(range(8),3),rng.sample(range(8),3)
        slots = [(ao[i]+a)%8 for i in range(2)]
        bi = [(s-b)%8 for s in slots]
        bi += [rng.choice([s for s in range(8) if s not in bi])]
        bo = rng.sample(range(8),3)
        coupling = (1 if number%2 else -1) if chapter == 2 or chapter == 4 and number >= 7 else 0
        beam_count = 1 if index < 2 else 2
        initial = [(a+rng.choice([1,2,3,5,6]))%8, (b+rng.choice([1,2,3,5,6]))%8]
        if coupling: initial[1] = (b+coupling*(initial[0]-a))%8
        locked = [index == 0, index == 2]
        if locked[0]: initial[0] = a
        if locked[1]: initial[1] = b
        fixed = index in (0,2) or chapter == 3 and number in (3,6,9)
        forbidden = rng.sample([s for s in range(8) if s not in slots],2) if chapter >= 3 else []
        d = dict(version=1,id=f"r{chapter}_{number:02}",chapter=chapter,number=number,title=NAMES[chapter-1][number-1],
                 lesson=LESSONS[index] if index<6 else "两环同向联动，光桥位置保持不变" if coupling==1 else
                 "镜像联动：转一环，另一环反向转动" if coupling==-1 else
                 "带叉的刻度禁止建桥，沿空间通道寻找替代连接" if chapter==3 else
                 "让两路抵达时间相差不超过 0.2 秒" if chapter==4 else "同时照顾两路光，从终点倒推朝向",
                 initialSteps=initial,sourceSlots=[(ai[i]+a)%8 for i in range(2)],goalSlots=[(bo[i]+b)%8 for i in range(2)],
                 channels=[[dict(input=x,output=y) for x,y in zip(ai,ao)], [dict(input=x,output=y) for x,y in zip(bi,bo)]],
                 beamCount=beam_count,bridgeCapacity=beam_count,lockedRings=locked,coupling=coupling,
                 initialBridges=slots[:beam_count] if fixed else [],fixedBridges=fixed,forbiddenSlots=forbidden,
                 syncTolerance=.2 if chapter==4 else 0,recommendedMoves=16 if chapter==4 else 12)
        solutions = [(x,y,required(d,x,y)) for x in range(8) for y in range(8) if reachable(d,x,y) and required(d,x,y) is not None]
        if len(solutions) == 1 and solutions[0][:2] == (a,b):
            return d,dict(id=d['id'],steps=[a,b],bridges=solutions[0][2],orientationSolutions=1)
    raise RuntimeError(f"Could not author level {index}")

if __name__ == '__main__':
    authored = [create(i) for i in range(40)]
    dest = ROOT/'Assets/Resources/RingCampaign.json'
    dest.write_text(json.dumps(dict(version=1,levels=[d for d,_ in authored]),ensure_ascii=False,indent=2),encoding='utf-8')
    (ROOT/'Builds/RingCampaign-Certificates.json').write_text(json.dumps([c for _,c in authored],ensure_ascii=False,indent=2),encoding='utf-8')
    print(f"40 authored configurations; 40 reachable unique-orientation certificates -> {dest}")
