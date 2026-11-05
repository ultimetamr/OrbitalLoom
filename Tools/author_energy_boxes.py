"""Six hand-authored constraint graphs. No random generation.

References document intended routes; the Unity solver independently enumerates
all legal layouts. Coordinates are local meters; visual rails are real 3D lines.
"""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
def node(id,kind,title,x,y,z=0,units=0,outputs=(),choices=(),rune='',code=''):
    return dict(id=id,kind=kind,title=title,x=x,y=y,z=z,units=units,outputs=list(outputs),choices=list(choices),rune=rune,code=code)
def turn(name,target):return dict(kind='turn',name=name,outputs=[target])
def split(name,a,b):return dict(kind='split',name=name,outputs=[a,b])
def level(i,title,lesson,turns,splitters,nodes,reference,hints):
    return dict(id=f'box_{i:02}',title=title,lesson=lesson,turns=turns,splitters=splitters,nodes=nodes,reference=reference,hints=hints)
levels=[
level(1,'第一道弯','点插槽 A → 装转向件 → 选通往终点的出口 → 开始',1,0,[
node('s','source','能源',-.62,.08,units=1,outputs=['a']),
node('a','socket','插槽 A',-.15,.08,choices=[turn('向下 · 断口','d'),turn('向右上 · 终点','g')]),
node('g','goal','终点 Ⅰ',.40,.28,units=1),node('d','blocked','封闭断口',.26,-.20)], [2],
['先看终点的入口导轨，它只通向插槽 A。','转向件的亮色箭头表示当前出口；灰色轨道只是可选方向。','在 A 安装转向件，再切到“向右上 · 终点”。']),
level(2,'一分为二','一件等分器，必须同时点亮两个一格终点',0,1,[
node('s','source','能源',-.62,0,units=2,outputs=['a']),node('a','socket','插槽 A',-.12,0,choices=[split('上下各一格','g1','g2'),turn('只送上方','g1'),turn('只送下方','g2')]),
node('g1','goal','终点 Ⅰ',.45,.26,units=1),node('g2','goal','终点 Ⅱ',.45,-.22,units=1)], [1],
['两个终点都只需要 1 格，能源一共 2 格。','转向只改变方向，不减少能量；这里要等分。','在 A 安装等分件，让两个出口分别通往上下终点。']),
level(3,'留住另一半','三个终点要 2、1、1 格，决定第二次分流的位置',1,2,[
node('s','source','能源',-.70,.10,units=4,outputs=['a']),node('a','socket','插槽 A',-.32,.10,choices=[split('上路保留 · 下路再分','g2','b'),split('先送一格目标','g1','b'),turn('全部送入 B','b')]),
node('b','socket','插槽 B',.04,-.16,choices=[split('分给两个一格目标','g1','g3'),split('分给上下终点','g2','g3'),turn('全部送二格目标','g2')]),
node('g2','goal','终点 Ⅰ',.36,.32,units=2),node('g1','goal','终点 Ⅱ',.57,.04,units=1),node('g3','goal','终点 Ⅲ',.44,-.31,units=1)], [1,1],
['从需要 2 格的终点倒推：它不能经过两次等分。','第一次分出 2＋2，一路保留，另一路再分成 1＋1。','A 选择上路保留、下路再分；B 选择两个一格目标。']),
level(4,'近路的代价','短路口不能合流：为另一半能量留下通道',1,2,[
node('s','source','能源',-.70,.08,units=4,outputs=['a']),
node('a','socket','插槽 A',-.38,.08,choices=[split('上方短路口 ＋ B','j','b'),split('上方短路口 ＋ C','j','c'),split('B ＋ C','b','c')]),
node('j','relay','共享短路口',-.02,.32,outputs=['g2']),
node('b','socket','插槽 B',-.03,-.13,choices=[turn('走上方近路','j'),split('一格直送 · 一格经 C','g1','c'),split('短路口 ＋ C','j','c')]),
node('c','socket','插槽 C',.31,-.16,choices=[turn('送下方终点','g3'),turn('送右方终点','g1')]),
node('g2','goal','终点 Ⅰ',.40,.32,units=2),node('g1','goal','终点 Ⅱ',.63,.05,units=1),node('g3','goal','终点 Ⅲ',.62,-.32,units=1)], [1,2,1],
['上方短路口只容一路能量；两路进入会争用。','二格目标需要保留完整的一半；B 的一半应分给两个一格目标。','A 分向短路口与 B；B 分向右方终点与 C；C 转向下方终点。']),
level(5,'上下错层','两路不能挤同一接点，寻找前方上层绕行',2,1,[
node('s','source','能源',-.72,.02,units=2,outputs=['a']),node('a','socket','插槽 A',-.42,.02,choices=[split('分向 B 与 C','b','c'),turn('全部走 B','b')]),
node('b','socket','插槽 B',-.12,.19,-.08,choices=[turn('后方共享通道','j'),turn('前方上层桥','h')]),
node('c','socket','插槽 C',-.08,-.22,.14,choices=[turn('后方共享通道','j'),turn('封闭断口','d')]),
node('h','relay','上层桥',.19,.36,-.34,outputs=['g1']),node('j','relay','后方接点',.23,-.08,.28,outputs=['g2']),
node('g1','goal','终点 Ⅰ',.61,.22,-.31,units=1),node('g2','goal','终点 Ⅱ',.62,-.24,.28,units=1),node('d','blocked','封闭断口',-.22,-.38,.24)], [1,2,1],
['后方接点会把两路挤在一起，它只通向下方终点。','C 没有别的有效出口，所以后方通道必须留给 C。','A 等分；B 转向前方上层桥；C 转向后方共享通道。']),
level(6,'星轨织机','库存刚好：保留二格、二次分流，并避开共享通道',3,2,[
node('s','source','能源',-.78,.02,units=4,outputs=['a']),node('a','socket','插槽 A',-.49,.02,choices=[split('B ＋ C','b','c'),split('B ＋ D','b','d'),turn('全部走 C','c')]),
node('b','socket','插槽 B',-.18,.28,.10,choices=[turn('二格专用通道','j'),turn('一格终点近路','g1')]),
node('c','socket','插槽 C',-.18,-.23,-.16,choices=[turn('占用二格通道','j'),split('D ＋ E','d','e'),split('共享通道 ＋ E','j','e')]),
node('d','socket','插槽 D',.18,.06,-.30,choices=[turn('前方一格终点','g1'),turn('共享通道','j')]),
node('e','socket','插槽 E',.23,-.29,.14,choices=[turn('下方一格终点','g3'),turn('共享通道','j')]),
node('j','relay','共享通道',.20,.34,.28,outputs=['g2']),
node('g2','goal','终点 Ⅰ',.67,.34,.28,units=2),node('g1','goal','终点 Ⅱ',.62,.02,-.30,units=1),node('g3','goal','终点 Ⅲ',.68,-.29,.14,units=1)], [1,1,2,1,1],
['先标出二格终点的必经接点，哪条路不能再被等分？','B 必须带着完整 2 格走共享通道；其余两格从 C 分给 D、E。','A 等分到 B、C；B 转向二格通道；C 等分到 D、E；D、E 各送一个一格终点。'])]
def find(level,id):return next(n for n in level['nodes'] if n['id']==id)

# Authored rune puzzles: alternatives carry identical energy to the same
# receiver, so checking rune order is necessary to distinguish legal routes.
l=levels[2];l['title']='月印之门';l['lesson']='先点蓝色终点读锁印，再找紫色检查点；光会按经过顺序记下符文。'
find(l,'g1')['code']='月';find(l,'g1')['x']=.68
find(l,'b')['choices']=[split('经前方印记台','rm','g3'),split('经后方印记台','rs','g3'),turn('全部送二格目标','g2')]
l['nodes'] += [node('rm','relay','前方印记台',.31,.02,-.30,outputs=['g1'],rune='月'),node('rs','relay','后方印记台',.32,-.15,.32,outputs=['g1'],rune='星')]
l['hints']=['先点击带锁的终点，比较它要的符文与两个紫色检查点。','能量正确还不够。二格目标保留一半；另一半等分，其中一路必须先经过月印。','A 保留二格并送 B；B 走前方月印台与下方终点。']

l=levels[3];l['title']='颠倒的锁印';l['lesson']='相同的两个印记，顺序不同也打不开门。先倒读终点，再沿导轨找顺序。'
find(l,'g1')['code']='月星';find(l,'g1')['x']=.74
find(l,'b')['choices'][1]=split('前层串联通道','r1','c')
find(l,'b')['choices'].append(split('后层串联通道','q1','c'))
l['nodes'] += [node('r1','relay','前层印记一',.12,.02,-.32,outputs=['r2'],rune='月'),node('r2','relay','前层印记二',.43,.12,-.32,outputs=['g1'],rune='星'),node('q1','relay','后层印记一',.13,-.34,.32,outputs=['q2'],rune='星'),node('q2','relay','后层印记二',.47,-.31,.32,outputs=['g1'],rune='月')]
l['hints']=['两条印记路的能量都一样，目标却只接受一种先后顺序。','终点要求月→星。转动视角，沿箭头确认每条串联路先遇到什么。','A 分向短路口与 B；B 选择前层串联通道与 C；C 送下方终点。']

l=levels[4];l['title']='背面的真相';l['lesson']='读清前后层的符文顺序；给没有备用出口的支路留下通道。'
find(l,'h')['rune']='月';find(l,'h')['outputs']=['hx'];find(l,'h')['x']=.10
find(l,'j')['rune']='日';find(l,'g1')['code']='月星';find(l,'g2')['code']='日'
find(l,'b')['choices'].append(turn('后层印记桥','hy'))
l['nodes'] += [node('hx','relay','前层第二印',.39,.35,-.34,outputs=['g1'],rune='星'),node('hy','relay','后层第一印',.10,.30,.32,outputs=['hz'],rune='星'),node('hz','relay','后层第二印',.40,.22,.32,outputs=['g1'],rune='月')]
l['hints']=['点两个终点分别读锁印；上方有两条供能相同的桥。','下方目标要日印，只能从后方接点走；上方目标要先月后星。','A 等分；B 走前方上层桥（月→星），C 经过后方日印接点。']

l=levels[5];l['title']='双锁星匣';l['lesson']='同时满足能量、锁印先后和通道独占；先从最受限制的终点倒推。'
find(l,'j')['rune']='月';find(l,'j')['outputs']=['jx'];find(l,'j')['x']=.08
find(l,'g2')['code']='月星';find(l,'g2')['x']=.76
find(l,'d')['choices']=[turn('前层单印路','dr'),turn('共享通道','j'),turn('前层双印路','dx')]
find(l,'e')['choices']=[turn('下方印记路','er'),turn('共享通道','j')]
find(l,'g1')['code']='日';find(l,'g1')['x']=.76;find(l,'g3')['code']='星'
find(l,'b')['choices'].append(turn('后层逆序通道','by'))
l['nodes'] += [node('jx','relay','二格第二印',.41,.36,.28,outputs=['g2'],rune='星'),node('by','relay','后层第一印',.03,.12,.42,outputs=['bz'],rune='星'),node('bz','relay','后层第二印',.39,.13,.42,outputs=['g2'],rune='月'),node('dr','relay','前层单印',.46,.02,-.30,outputs=['g1'],rune='日'),node('dx','relay','前层双印入口',.18,-.08,-.44,outputs=['dr'],rune='月'),node('er','relay','下方印记',.45,-.32,.14,outputs=['g3'],rune='星')]
l['hints']=['先看二格锁与日印锁：分别要求月→星和只有一个日印，多印也会拒绝。','完整的二格必须留在 B；C 分出的两格分别走 D、E。B 的后层路顺序相反，D 的双印路多了月。','A 分 B、C；B 走月→星二格通道；C 分 D、E；D 选前层单印路，E 走下方星印。']

def gate(id,title,x,y,target,require,z=0):
    n=node(id,'relay',title,x,y,z,outputs=[target]);n['gate']=require;return n
def swap(id,title,x,y,target,a,b,z=0):
    n=node(id,'relay',title,x,y,z,outputs=[target]);n.update(swapFrom=a,swapTo=b);return n

# Each new level isolates one deduction before combining it with prior rules.
# None is a rotated copy of another graph. The C# solver certifies independently.
levels += [
level(7,'守门的月光','先读条件门；完整印记吻合才可通过。',1,0,[
 node('s','source','能源',-.70,0,units=1,outputs=['a']),
 node('a','socket','插槽 A',-.38,0,choices=[turn('经过星印','rs'),turn('经过月印','rm')]),
 node('rs','relay','星印',-.04,.25,-.22,outputs=['door'],rune='星'),
 node('rm','relay','月印',-.04,-.23,.20,outputs=['door'],rune='月'),
 gate('door','月门',.31,.03,'g','月'),node('g','goal','终点 Ⅰ',.70,.10,units=1,code='月')],[2],
 ['条件门检查整串符文；先看门要什么。','月门要月印，星印即使能量正确也会被挡住。','选择月印路线。']),
level(8,'分岔的通行证','两个门各认一种印记。先等分，再决定每一路经过什么。',2,1,[
 node('s','source','能源',-.74,0,units=2,outputs=['a']),
 node('a','socket','插槽 A',-.48,0,choices=[split('分给 B 与 C','b','c'),turn('全部给 B','b')]),
 node('b','socket','插槽 B',-.20,.27,-.12,choices=[turn('上路星印','bs'),turn('上路月印','bm')]),
 node('c','socket','插槽 C',-.20,-.25,.12,choices=[turn('下路星印','cs'),turn('下路月印','cm')]),
 node('bs','relay','星印',.06,.36,-.30,outputs=['db'],rune='星'),node('bm','relay','月印',.06,.15,-.12,outputs=['db'],rune='月'),
 node('cs','relay','星印',.07,-.14,.12,outputs=['dc'],rune='星'),node('cm','relay','月印',.07,-.36,.30,outputs=['dc'],rune='月'),
 gate('db','月门',.39,.27,'g1','月',-.12),gate('dc','星门',.39,-.25,'g2','星',.12),
 node('g1','goal','终点 Ⅰ',.74,.27,-.12,units=1,code='月'),node('g2','goal','终点 Ⅱ',.74,-.25,.12,units=1,code='星')],[1,2,1],
 ['两个门要求不同，不能在分流前盖同一个印。','A 分为两路；B 留月，C 留星。','沿月门和星门分别倒推。']),
level(9,'借月换星','能源已经带月印；终点却要星印。留意换印器。',1,0,[
 node('s','source','能源',-.73,0,units=1,outputs=['rm']),node('rm','relay','月印',-.46,.09,-.10,outputs=['a'],rune='月'),
 node('a','socket','插槽 A',-.12,.02,choices=[turn('直接送终点','g'),turn('经过换印器','cv')]),
 swap('cv','换印器',.24,-.25,'g','月','星',.20),node('g','goal','终点 Ⅰ',.68,.10,units=1,code='星')],[2],
 ['追加星印会变月→星，仍不等于只有星。','换印器可以替换末印，不会追加一个印。','选择经过换印器的出口。']),
level(10,'保留的前印','换印只动末尾，先后顺序决定能否过门。',2,1,[
 node('s','source','能源',-.77,.02,units=2,outputs=['rd']),node('rd','relay','日印',-.55,.10,outputs=['a'],rune='日'),
 node('a','socket','插槽 A',-.32,.02,choices=[split('分给 B 与 C','b','c'),turn('全部送 B','b')]),
 node('b','socket','插槽 B',-.08,.26,-.20,choices=[turn('先追加星印','bs'),turn('先替换日印','cv')]),
 swap('cv','日换月',.15,.27,'door','日','月',-.20),gate('door','月门',.37,.27,'bs','月',-.20),
 node('bs','relay','星印',.58,.27,-.20,outputs=['g1'],rune='星'),node('g1','goal','终点 Ⅰ',.80,.28,-.20,units=1,code='月星'),
 node('c','socket','插槽 C',-.08,-.22,.20,choices=[turn('直接送星锁','g2'),turn('替换末印','cx')]),
 swap('cx','日换星',.29,-.22,'g2','日','星',.20),node('g2','goal','终点 Ⅱ',.75,-.22,.20,units=1,code='星')],[1,2,2],
 ['先替换还是先追加，会留下不同的整串印记。','上路必须先日换月，过月门后才加星；下路只需星。','分别检查 B、C 的换印出口。']),
level(11,'牵一处动两处','B、C 成对联动，改变一处会同时改变另一处。',2,1,[
 node('s','source','能源',-.75,0,units=2,outputs=['a']),node('a','socket','插槽 A',-.49,0,choices=[split('分给 B 与 C','b','c'),turn('全部走 B','b')]),
 node('b','socket','插槽 B',-.22,.25,-.12,choices=[turn('B：星印','bs'),turn('B：月印','bm'),turn('B：日印','bd')]),
 node('c','socket','插槽 C',-.22,-.25,.12,choices=[turn('C：星印','cs'),turn('C：日印','cd'),turn('C：月印','cm')]),
 node('bs','relay','星印',.04,.39,-.25,outputs=['db'],rune='星'),node('bm','relay','月印',.04,.23,-.12,outputs=['db'],rune='月'),node('bd','relay','日印',.04,.06,.03,outputs=['db'],rune='日'),
 node('cs','relay','星印',.06,-.08,-.03,outputs=['dc'],rune='星'),node('cd','relay','日印',.06,-.25,.12,outputs=['dc'],rune='日'),node('cm','relay','月印',.06,-.41,.25,outputs=['dc'],rune='月'),
 gate('db','月门',.39,.25,'g1','月',-.12),gate('dc','星门',.39,-.25,'g2','星',.12),
 node('g1','goal','终点 Ⅰ',.75,.25,-.12,units=1,code='月'),node('g2','goal','终点 Ⅱ',.75,-.25,.12,units=1,code='星')],[1,2,1],
 ['不能逐路独立调好，B、C 的出口是成对变化的。','从月门倒推 B，再检查它同步带动的 C 是否走星印。','把 B 调到月印；同步后的 C 应走星印。']),
level(12,'三钥星匣','完整二格要换印过门；另两格由联动双路分别解锁。',3,2,[
 node('s','source','能源',-.78,.02,units=4,outputs=['a']),node('a','socket','插槽 A',-.52,.02,choices=[split('保留上路二格','b','c'),split('提前送一格路','b','d'),turn('全部送下路','c')]),
 node('b','socket','插槽 B',-.26,.29,-.22,choices=[turn('星印近路','bs'),turn('月印换星','bm')]),
 node('bm','relay','月印',-.01,.32,-.22,outputs=['cv'],rune='月'),node('bs','relay','星印',-.03,.13,-.40,outputs=['cv'],rune='星'),
 swap('cv','月换星',.25,.31,'door','月','星',-.22),gate('door','星门',.49,.31,'g2','星',-.22),node('g2','goal','终点 Ⅰ',.78,.32,-.22,units=2,code='星'),
 node('c','socket','插槽 C',-.27,-.25,.18,choices=[split('分给 D 与 E','d','e'),turn('占用上路','b')]),
 node('d','socket','插槽 D',-.03,-.05,.12,choices=[turn('D：月印','dm'),turn('D：日印','dd')]),
 node('e','socket','插槽 E',-.03,-.33,.30,choices=[turn('E：月印','em'),turn('E：日印','ed')]),
 node('dm','relay','月印',.22,.04,.04,outputs=['dg'],rune='月'),node('dd','relay','日印',.23,-.14,.12,outputs=['dg'],rune='日'),
 gate('dg','日门',.49,-.03,'g1','日',.12),node('g1','goal','终点 Ⅱ',.79,-.02,.12,units=1,code='日'),
 node('em','relay','月印',.29,-.29,.30,outputs=['g3'],rune='月'),node('ed','relay','日印',.24,-.45,.43,outputs=['g3'],rune='日'),node('g3','goal','终点 Ⅲ',.77,-.34,.30,units=1,code='月')],[1,2,1,2,1],
 ['二格目标不能经过两次等分；换印器只接收末印月。','B 的星印近路会被换印器拒绝；D、E 的配对要同时满足日锁与月锁。','保留 B 二格走月换星；C 等分，再把 D 调日印、E 调月印。'])]
levels[6].update(tutorial='gate',tutorialNode='door',tutorialSocket='a')
levels[8].update(tutorial='swap',tutorialNode='cv',tutorialSocket='a')
levels[10].update(tutorial='link',tutorialNode='b',tutorialSocket='b')
levels[10]['links']=[dict(a='b',b='c',map=[0,3,1,2])]
levels[11]['links']=[dict(a='d',b='e',map=[0,2,1])]
find(levels[10],'bd')['y']=.09;find(levels[10],'cs')['y']=-.10;find(levels[10],'cm')['y']=-.37
find(levels[11],'em')['y']=-.24;find(levels[11],'ed')['y']=-.37


# New rules are introduced separately, then combined. No geometry randomizer.
def ratio(name,a,b,left,right):
 c=split(name,a,b);c['parts']=[left,right];return c
def process(id,title,x,y,target,op,z=0):
 n=node(id,'relay',title,x,y,z,outputs=[target]);n['operation']=op;return n
def wait(name,target,beats):
 c=turn(name,target);c['delay']=beats;return c
levels += [
level(13,'偏光天平','棱镜按标记分配，不一定均分；从终点所需格数倒推。',0,1,[
 node('s','source','能源',-.66,0,units=4,outputs=['a']),
 node('a','socket','棱镜 A',-.18,0,choices=[ratio('上2 / 下2','g1','g2',2,2),ratio('上3 / 下1','g1','g2',3,1),ratio('上1 / 下3','g1','g2',1,3)]),
 node('g1','goal','终点 Ⅰ',.48,.25,-.18,units=3),node('g2','goal','终点 Ⅱ',.48,-.25,.18,units=1)],[2],
 ['两个终点要的格数不同，均分不会成功。','留意棱镜每个出口的格数，而不是只看方向。','使用上3、下1。']),
level(14,'重光的通行证','比例与符文必须一起满足，三格和一格不能互换。',2,1,[
 node('s','source','能源',-.78,0,units=4,outputs=['a']),
 node('a','socket','棱镜 A',-.49,0,choices=[ratio('上1 / 下3','b','c',1,3),ratio('上3 / 下1','b','c',3,1),ratio('均分','b','c',2,2)]),
 node('b','socket','插槽 B',-.17,.25,-.18,choices=[turn('月印路','rm'),turn('星印路','rs')]),
 node('c','socket','插槽 C',-.17,-.24,.18,choices=[turn('星印路','cs'),turn('月印路','cm')]),
 node('rm','relay','月印',.12,.36,-.26,outputs=['db'],rune='月'),node('rs','relay','星印',.12,.15,-.10,outputs=['db'],rune='星'),
 node('cs','relay','星印',.12,-.14,.10,outputs=['dc'],rune='星'),node('cm','relay','月印',.12,-.36,.26,outputs=['dc'],rune='月'),
 gate('db','月门',.42,.25,'g1','月',-.18),gate('dc','星门',.42,-.24,'g2','星',.18),
 node('g1','goal','终点 Ⅰ',.77,.25,-.18,units=3,code='月'),node('g2','goal','终点 Ⅱ',.77,-.24,.18,units=1,code='星')],[2,1,1],
 ['先分配格数，再满足两个门。','三格给上路月锁，一格给下路星锁。','A上3下1；B月印，C星印。']),
level(15,'三格再分','三格不能均分；两级棱镜守恒，避免能量错配。',0,2,[
 node('s','source','能源',-.73,0,units=4,outputs=['a']),
 node('a','socket','棱镜 A',-.37,0,choices=[ratio('三格给B','b','g3',3,1),ratio('一格给B','b','g3',1,3),ratio('均分给B','b','g3',2,2)]),
 node('b','socket','棱镜 B',.04,.16,-.18,choices=[split('均分','g1','g2'),ratio('上2 / 下1','g1','g2',2,1),ratio('上1 / 下2','g1','g2',1,2)]),
 node('g1','goal','终点 Ⅰ',.58,.35,-.27,units=2),node('g2','goal','终点 Ⅱ',.66,.02,-.10,units=1),node('g3','goal','终点 Ⅲ',.56,-.30,.22,units=1)],[1,2],
 ['终点的总数是4，先为下路保留1格。','B收到3格，只能使用2＋1比例，不能均分。','A给B三格；B上2下1。']),
level(16,'倒映的密语','倒序镜反转整串符文；它不替换单个印记。',1,0,[
 node('s','source','能源',-.77,0,units=1,outputs=['rm']),node('rm','relay','月印',-.51,.12,-.14,outputs=['rs'],rune='月'),node('rs','relay','星印',-.25,.16,-.18,outputs=['a'],rune='星'),
 node('a','socket','插槽 A',.02,.02,choices=[turn('直接送锁','g'),turn('经过倒序镜','rv')]),
 process('rv','倒序镜',.32,-.24,'g','reverse',.20),node('g','goal','终点 Ⅰ',.72,.10,units=1,code='星月')],[2],
 ['比较能源带来的印记与终点顺序。','当前月→星，目标星→月。需要反转整串。','选择倒序镜。']),
level(17,'镜前与镜后','反转时机不同，追加后的整串也不同。',1,0,[
 node('s','source','能源',-.77,0,units=1,outputs=['rm']),node('rm','relay','月印',-.54,0,outputs=['rs'],rune='月'),node('rs','relay','星印',-.31,0,outputs=['a'],rune='星'),
 node('a','socket','插槽 A',-.08,0,choices=[turn('先加日再倒序','d1'),turn('先倒序再加日','r2')]),
 node('d1','relay','日印',.13,.26,-.22,outputs=['r1'],rune='日'),process('r1','倒序镜',.40,.26,'g','reverse',-.22),
 process('r2','倒序镜',.13,-.25,'d2','reverse',.22),node('d2','relay','日印',.40,-.25,.22,outputs=['g'],rune='日'),node('g','goal','终点 Ⅰ',.76,0,units=1,code='日星月')],[1],
 ['目标的第一个日，在原路线中应当最后加入。','整串月星日反转才是日星月。','先加日，再倒序。']),
level(18,'双面证词','分流继承相同印记，两路分别加工成不同的完整口令。',2,1,[
 node('s','source','能源',-.78,0,units=2,outputs=['m']),node('m','relay','月印',-.55,0,outputs=['s1'],rune='月'),node('s1','relay','星印',-.32,0,outputs=['a'],rune='星'),
 node('a','socket','插槽 A',-.08,0,choices=[split('分向B与C','b','c')]),
 node('b','socket','插槽 B',.16,.26,-.18,choices=[turn('上路倒序','rv'),turn('上路直送','g1')]),
 node('c','socket','插槽 C',.16,-.25,.18,choices=[turn('替换末印','cv'),turn('直送','g2')]),
 process('rv','倒序镜',.44,.26,'g1','reverse',-.18),swap('cv','星换日',.44,-.25,'g2','星','日',.18),
 node('g1','goal','终点 Ⅰ',.77,.26,-.18,units=1,code='星月'),node('g2','goal','终点 Ⅱ',.77,-.25,.18,units=1,code='月日')],[1,1,1],
 ['两路都继承月星；上路要星月，下路要月日。','上路倒序，下路只替换星。','A等分，B倒序，C换印。']),
level(19,'退去的尾音','退印台移除最后一个符文；空白印记不能继续退印。',1,0,[
 node('s','source','能源',-.75,0,units=1,outputs=['m']),node('m','relay','月印',-.48,.10,-.10,outputs=['s1'],rune='月'),node('s1','relay','星印',-.21,.10,-.10,outputs=['a'],rune='星'),
 node('a','socket','插槽 A',.05,0,choices=[turn('直接送锁','g'),turn('经过退印台','er')]),
 process('er','退印台',.34,-.23,'g','erase',.20),node('g','goal','终点 Ⅰ',.71,.10,units=1,code='月')],[2],
 ['能量没有错，锁印多了末尾的星。','退印只移除最后一枚。','选择退印路线。']),
level(20,'删掉开头','退印只删末尾；要去掉开头，先改变整串的方向。',1,0,[
 node('s','source','能源',-.78,0,units=1,outputs=['m']),node('m','relay','月印',-.55,0,outputs=['s1'],rune='月'),node('s1','relay','星印',-.32,0,outputs=['a'],rune='星'),
 node('a','socket','插槽 A',-.08,0,choices=[turn('先退印','er1'),turn('先倒序','rv')]),
 process('er1','退印台',.18,.25,'g','erase',-.20),process('rv','倒序镜',.18,-.25,'er2','reverse',.20),process('er2','退印台',.44,-.25,'g','erase',.20),node('g','goal','终点 Ⅰ',.77,.06,units=1,code='星')],[2],
 ['要保留星，直接退印却会删掉星。','先把月移到末尾，再退印。','倒序得到星月，再删月。']),
level(21,'重写遗言','棱镜、倒序、退印共同决定两路能量和口令。',2,1,[
 node('s','source','能源',-.79,0,units=4,outputs=['m']),node('m','relay','月印',-.57,.03,outputs=['st'],rune='月'),node('st','relay','星印',-.35,.03,outputs=['a'],rune='星'),
 node('a','socket','棱镜 A',-.12,.03,choices=[ratio('上1下3','b','c',1,3),ratio('上3下1','b','c',3,1),split('均分','b','c')]),
 node('b','socket','插槽 B',.12,.28,-.22,choices=[turn('退印后换月','er'),turn('直送','g1')]),
 process('er','退印台',.35,.34,'cv','erase',-.24),swap('cv','月换日',.56,.30,'g1','月','日',-.24),node('g1','goal','终点 Ⅰ',.80,.29,-.24,units=3,code='日'),
 node('c','socket','插槽 C',.12,-.25,.20,choices=[turn('先倒序','rv'),turn('直接退印','bad')]),
 process('rv','倒序镜',.35,-.31,'er2','reverse',.22),process('er2','退印台',.57,-.27,'g2','erase',.22),process('bad','退印台',.40,-.08,'g2','erase',.08),node('g2','goal','终点 Ⅱ',.80,-.25,.20,units=1,code='星')],[2,1,1],
 ['先分三格与一格，两路都继承月星。','上路删星再月换日；下路倒序后删月。','A上3下1，B退印再换日，C倒序再退印。']),
level(22,'同拍之约','本章每段导轨1拍；短路等待，让两个终点同时收到光。',1,1,[
 node('s','source','能源',-.72,0,units=2,outputs=['a']),node('a','socket','插槽 A',-.42,0,choices=[split('分两路','b','l')]),
 node('b','socket','节拍阀 B',-.10,.23,-.16,choices=[wait('等0拍','g1',0),wait('等1拍','g1',1),wait('等2拍','g1',2)]),
 node('l','relay','长路Ⅰ',-.10,-.23,.16,outputs=['l2']),node('l2','relay','长路Ⅱ',.25,-.23,.16,outputs=['g2']),
 node('g1','goal','终点 Ⅰ',.61,.23,-.16,units=1),node('g2','goal','终点 Ⅱ',.61,-.23,.16,units=1)],[1,2],
 ['短路比长路少一段。','每段1拍，短路需要等待1拍。','B等1拍。']),
level(23,'有条件的等候','能量、锁印与到达拍数都必须符合。',1,1,[
 node('s','source','能源',-.77,0,units=4,outputs=['a']),node('a','socket','棱镜 A',-.47,0,choices=[ratio('上3下1','b','m',3,1),ratio('上1下3','b','m',1,3)]),
 node('b','socket','节拍阀 B',-.15,.24,-.18,choices=[wait('等0拍','rs',0),wait('等1拍','rs',1),wait('等2拍','rs',2)]),
 node('rs','relay','星印',.20,.24,-.18,outputs=['g1'],rune='星'),
 node('m','relay','月印',-.15,-.24,.18,outputs=['st'],rune='月'),node('st','relay','星印',.13,-.24,.18,outputs=['rv'],rune='星'),process('rv','倒序镜',.41,-.24,'g2','reverse',.18),
 node('g1','goal','终点 Ⅰ',.74,.24,-.18,units=3,code='星'),node('g2','goal','终点 Ⅱ',.74,-.24,.18,units=1,code='星月')],[1,2],
 ['先确定三格去上路。','下路有三段，上路只有两段；多等1拍。','A上3下1，B等1拍。']),
level(24,'终章 · 四重誓约','守恒、顺序、联动与同拍同时成立；逐段观察定位偏差。',2,1,[
 node('s','source','能源',-.79,0,units=4,outputs=['a']),node('a','socket','棱镜 A',-.51,0,choices=[ratio('上3下1','b','c',3,1),ratio('上1下3','b','c',1,3)]),
 node('b','socket','节拍阀 B',-.24,.27,-.20,choices=[wait('上路等0拍','bm',0),wait('上路等1拍','bm',1),wait('上路等2拍','bm',2)]),
 node('c','socket','节拍阀 C',-.24,-.26,.20,choices=[wait('下路等2拍','cm',2),wait('下路等0拍','cm',0),wait('下路等1拍','cm',1)]),
 node('bm','relay','月印',.04,.27,-.20,outputs=['bs'],rune='月'),node('bs','relay','星印',.27,.27,-.20,outputs=['rv'],rune='星'),process('rv','倒序镜',.50,.27,'g1','reverse',-.20),node('g1','goal','终点 Ⅰ',.80,.27,-.20,units=3,code='星月'),
 node('cm','relay','月印',.04,-.26,.20,outputs=['cx'],rune='月'),swap('cx','月换日',.35,-.26,'g2','月','日',.20),node('g2','goal','终点 Ⅱ',.80,-.26,.20,units=1,code='日')],[1,2,1],
 ['上下相差一段，两个节拍阀又成对联动。','B等1拍会令C等2拍；较短的下路应多等1拍。','棱镜上3下1，B等1拍，C随动等2拍。'])]
# Challenge revisions: a single certified state is a correctness check, not
# a difficulty metric. These layouts add coupled decisions and counterexamples
# which pass energy/rune checks but fail the remaining global constraint.
levels[14]=level(15,'借位的棱镜','先给无替代出口的支路留通道，再决定三格在哪儿分。',3,2,[
 node('s','source','能源',-.82,0,units=4,outputs=['a']),
 node('a','socket','棱镜 A',-.55,0,choices=[ratio('B2 / C2','b','c',2,2),ratio('B3 / C1','b','c',3,1),ratio('B1 / C3','b','c',1,3)]),
 node('b','socket','棱镜 B',-.26,.25,-.18,choices=[split('均分 D / E','d','e'),ratio('D1 / E2','d','e',1,2),ratio('D2 / E1','d','e',2,1)]),
 node('c','socket','插槽 C',-.23,-.26,.22,choices=[turn('后层共道','j'),turn('前层月印','m')]),
 node('d','socket','插槽 D',.04,.35,-.24,choices=[turn('后层共道','j'),turn('前层月印','m')]),
 node('e','socket','插槽 E',.05,.05,-.04,choices=[turn('中层日印','u'),turn('后层共道','j')]),
 node('m','relay','月印',.39,.35,-.26,outputs=['g1'],rune='月'),
 node('u','relay','日印',.39,.04,-.02,outputs=['g2'],rune='日'),
 node('j','relay','共道星印',.36,-.28,.30,outputs=['g3'],rune='星'),
 node('g1','goal','终点 Ⅰ',.77,.35,-.26,units=2,code='月'),node('g2','goal','终点 Ⅱ',.77,.04,-.02,units=1,code='日'),node('g3','goal','终点 Ⅲ',.77,-.28,.30,units=1,code='星')], [2,3,1,2,1],
 ['后层共道只通往星锁，而且只允许一路进入；先从 C 的去向倒推。','C 必须保留星锁通道。D/E 联动后，要分别留下二格月印与一格日印；B收到的三格不能均分。','A给B3格C1格；B给D2格E1格；C走共道，D走月印，E同步走日印。'])
levels[14]['links']=[dict(a='d',b='e',map=[0,2,1])]

levels[17]=level(18,'借镜还印','镜子放在分流之前或之后，决定两路继承什么；同一接点不能再经过。',4,1,[
 node('s','source','能源',-.83,-.05,units=2,outputs=['m']),node('m','relay','月印',-.65,.14,-.18,outputs=['st'],rune='月'),node('st','relay','星印',-.45,.24,-.18,outputs=['p'],rune='星'),
 node('p','socket','插槽 P',-.25,.12,choices=[turn('先分流','a'),turn('先借镜','rv')]),
 process('rv','共用倒序镜',-.07,.38,'d','reverse',-.32),
 node('d','socket','插槽 D',.21,.35,-.30,choices=[turn('送往分流台','a'),turn('直接送上锁','g1')]),
 node('a','socket','等分台 A',-.03,-.04,choices=[split('分给 B / C','b','c')]),
 node('b','socket','插槽 B',.27,.11,-.10,choices=[turn('返回共用镜','rv'),turn('直送上锁','g1')]),
 node('c','socket','插槽 C',.23,-.29,.23,choices=[turn('月换日','cv'),turn('星换日','bad')]),
 swap('cv','月换日',.50,-.32,'g2','月','日',.27),swap('bad','星换日',.48,-.09,'g2','星','日',.13),
 node('g1','goal','终点 Ⅰ',.80,.25,-.16,units=1,code='星月'),node('g2','goal','终点 Ⅱ',.80,-.25,.23,units=1,code='星日')], [2,1,1,2,1],
 ['先读下方星→日锁：星必须留在开头，单纯替换月→星的末印做不到。','分流后借镜只会改变一路；两路都需要以星开头，应先反转再分流。共用镜不能再绕第二次。','P先借镜，D送分流台，A等分；B直送星月，C将末月换日。'])

levels[22]=level(23,'三声回廊','三条路长度不同；联动阀一加一减，先解两路关系，再对齐第三路。',3,2,[
 node('s','source','能源',-.83,0,units=4,outputs=['a']),
 node('a','socket','分流台 A',-.57,0,choices=[split('B / C 各2格','b','c'),ratio('B3 / C1','b','c',3,1),ratio('B1 / C3','b','c',1,3)]),
 node('b','socket','节拍阀 B',-.28,.34,-.25,choices=[wait('月路等'+str(i)+'拍','m',i) for i in range(5)]),
 node('c','socket','分流台 C',-.28,-.12,.12,choices=[turn('全部送 D','d'),split('D / E 各1格','d','e'),turn('全部送 E','e')]),
 node('d','socket','节拍阀 D',.01,.02,-.03,choices=[wait('星路等'+str(i)+'拍','st',i) for i in range(5)]),
 node('e','socket','节拍阀 E',.01,-.33,.26,choices=[wait('日路等'+str(i)+'拍','u',i) for i in range(5)]),
 node('m','relay','月印',.30,.34,-.25,outputs=['g1'],rune='月'),
 node('st','relay','星印',.34,.02,-.03,outputs=['g2'],rune='星'),
 node('u','relay','日印',.24,-.33,.26,outputs=['rv'],rune='日'),process('rv','倒序镜',.49,-.33,'g3','reverse',.26),
 node('g1','goal','终点 Ⅰ',.79,.34,-.25,units=2,code='月'),node('g2','goal','终点 Ⅱ',.79,.02,-.03,units=1,code='星'),node('g3','goal','终点 Ⅲ',.79,-.33,.26,units=1,code='日')], [1,4,2,3,2],
 ['先看导轨段数：月路4拍，星路5拍，日路6拍；B与E反向联动，等待总和为4。','月路需要比日路多等2拍，而两者等待相加为4；解出它们后，再把星路补齐。','A均分，C再均分；B等3拍，E随动等1拍，D等2拍，三路第7拍合奏。'])
levels[22]['links']=[dict(a='b',b='e',map=[0,5,4,3,2,1])]

levels[23]=level(24,'终章 · 三钥共鸣','先守恒，再决定三把钥匙的来路；联动既改变路由，也改变等待。',3,2,[
 node('s','source','能源',-.84,-.09,units=4,outputs=['m']),node('m','relay','月印',-.66,.10,-.15,outputs=['st'],rune='月'),node('st','relay','星印',-.47,.23,-.18,outputs=['a'],rune='星'),
 node('a','socket','棱镜 A',-.29,-.02,choices=[ratio('B3 / C1','b','c',3,1),ratio('B1 / C3','b','c',1,3),split('B2 / C2','b','c')]),
 node('b','socket','分流台 B',-.07,.17,-.12,choices=[turn('全部送 D','d'),split('D / E 各1格','d','e'),turn('全部送 E','e')]),
 node('c','socket','节拍阀 C',.11,-.32,.27,choices=[wait('完整锁等'+str(i)+'拍','door',i) for i in range(5)]),
 node('d','socket','节拍阀 D',.15,.36,-.28,choices=[wait('退印路等'+str(i)+'拍','er',i) for i in range(3)]+[wait('倒序路等'+str(i)+'拍','rv',i) for i in range(3)]),
 node('e','socket','节拍阀 E',.15,.02,-.01,choices=[wait('倒序路等'+str(i)+'拍','rv',i) for i in range(3)]+[wait('退印路等'+str(i)+'拍','er',i) for i in range(3)]),
 process('er','退印台',.37,.36,'cv','erase',-.28),swap('cv','月换日',.60,.36,'g1','月','日',-.28),
 process('rv','倒序镜',.37,.02,'er2','reverse',-.01),process('er2','退印台',.60,.02,'g3','erase',-.01),
 gate('door','月星门',.52,-.32,'g2','月星',.27),
 node('g1','goal','终点 Ⅰ',.84,.36,-.28,units=1,code='日'),node('g3','goal','终点 Ⅱ',.84,.02,-.01,units=1,code='星'),node('g2','goal','终点 Ⅲ',.84,-.32,.27,units=2,code='月星')], [3,2,4,2,2],
 ['完整月星锁必须留2格，另外两格才能分别变成日和星。D/E联动，不可逐路独立调整。','两条加工路各8拍，完整锁6拍；加工路必须选择不同来路且等得一样，再让完整锁多等2拍。','A均分，B再均分；D退印路等1拍、E倒序路随动等1拍；C等3拍，三锁第9拍共鸣。'])
levels[23]['links']=[dict(a='d',b='e',map=[0,3,2,1,5,6,4])]

for i,kind,target,socket in [(12,'prism','a','a'),(15,'reverse','rv','a'),(18,'erase','er','a'),(21,'sync','b','b')]:
 levels[i].update(tutorial=kind,tutorialNode=target,tutorialSocket=socket)
for l in levels[21:]:l['syncWindow']=.1
# Spatial evidence supplements the exact visible rules; it never gates the
# energy solver. Text access gives the same evidence for seated/access use.
levels[14]['relic']=dict(kind='parallax',title='视角拼符 · 星印的缺口',angle=45,
 instruction='前后两层各留一半残纹。转动遗迹，试着让断开的刻线接成完整符号。',
 discovery='残纹拼成一枚星印：共道只认星，而且只容一路。先为只能去星锁的支路留路，再分配其余能量。')
levels[17]['relic']=dict(kind='mirror',title='镜面寻印 · 水晶背刻',angle=180,
 instruction='正面是月在前、星在后。把水晶碑翻到背面，寻找两条支路共同需要的开头。',
 discovery='背刻由星起首、月在后。两把锁都以星开头，共用镜应在分流前处理整串；分流后再借镜只改变一路。')
levels[22]['relic']=dict(kind='resonance',title='三路共鸣 · 回廊刻度',angle=-45,
 instruction='三列刻度朝向侧面。转到刻纹清楚的一面，数月、星、日各有几格，再结合两阀联动表。',
 discovery='月、星、日刻度为4、5、6。月与日的等待合计4拍；短路多等，长路少等，再把星路补到同拍。')
# Geometry-bearing upgrades retain all original twelve levels and progress IDs.
# All orientation alternatives keep the same graph destination: geometry alone
# distinguishes their answers, so this is not a renamed route-choice puzzle.
def pose(name,target,angle):
 c=turn(name,target);c.update(angle=angle);return c
l=levels[14]
l['turns']+=2
find(l,'m')['outputs']=['h']
l['nodes'] += [node('h','socket','桥臂 H',.43,.34,-.27,choices=[pose('H -135°','i',-135),pose('H -45°','i',-45),pose('H 45°','i',45),pose('H 135°','i',135)]),
 node('i','socket','桥臂 I',.60,.34,-.27,choices=[pose('I -45°','g1',-45),pose('I 45°','g1',45),pose('I 135°','g1',135),pose('I -135°','g1',-135)])]
find(l,'m').update(x=.32,y=.34)
find(l,'g1').update(x=.82,y=.34)
l['reference'] += [3,1]
l['spatial']=dict(kind='bridge',controls=['h','i'],speed=.30)
l['lesson']='先为星锁保留共道，再对接三维桥臂；投影相邻不代表空间接通。'
l['relic'].update(title='双桥接合 · 空间中的缺口',instruction='两轴相距48cm，桥臂各长约34cm。前后两侧都可能合拢，但前层暗石会阻光；转到侧面找可行接合。',discovery='桥臂 H 与 I 的真实末端必须对接并避开前层暗石；接通后，仍须满足三格分光与共道争用。')
l['hints']=['先将双桥臂向中央合拢，再为只能去星锁的 C 保留共道。','正面看起来相接仍可能撞上前层暗石，需向后层合拢；棱镜B需把3格分为2与1。','桥臂H选45°、I选-45°；A给B3格C1格，B给D2格E1格，C共道，D月印。']
l=levels[17]
l['turns']+=1
find(l,'rv')['outputs']=['h']
l['nodes'] += [node('h','socket','反射镜 H',.08,.43,-.31,choices=[pose('法线 0°','d',0),pose('法线 -45°','d',-45),pose('法线 45°','d',45),pose('法线 90°','d',90)])]
l['reference'] += [2]
l['spatial']=dict(kind='mirror',controls=['h'],speed=.30)
l['relic'].update(title='镜面寻路 · 真实反射',instruction='光从左向右入射。转动镜面，让反射束穿过后方水晶；然后推理借镜与分流的先后顺序。',discovery='镜面方向决定反射命中，倒序镜则改变整串符文；两路都需要以星开头，因此先处理再分流。')
l['hints']=['先让反射光命中水晶；两把锁又都需要星开头，应在分流前借镜。','镜面法线平分入射与反射关系；分流后再反转只能修正一路。','反射镜H法线选-45°；P先借镜，D送分流台，A等分，B直送上锁，C末月换日。']
l=levels[22]
l['spatial']=dict(kind='path',controls=['b','d','e'],speed=.30)
for id,prefix in [('b','月'),('d','星'),('e','日')]:
 n=find(l,id);n['title']=prefix+'路折返 '+id.upper()
 for i,c in enumerate(n['choices']):c.update(name=prefix+'路折返'+str(i)+'段',loops=i,delay=0)
l['relic'].update(title='折返共鸣 · 长度就是时间',instruction='基础路程月4拍、星5拍、日6拍；每展开30cm折返就多走1拍。月、日两路的折返段数反向联动，合计4。',discovery='月、星、日基础为4、5、6。月路要比日路多展开两段，且两者段数合计4，再补齐星路。')
l['lesson']='折返改变实际光路长度，不是停住等待；空间长度与联动决定三声能否同拍。'
l['hints']=['基础路程月4拍、星5拍、日6拍；月日展开段数合计4。','月路比日路短2拍，故需多展开2段；再让星路与它们同拍。','A均分、C再均分；月路展开3段、日路随动1段、星路2段，三路第7拍共鸣。']

# 0.9.5: transfer challenge. Extract the middle rune with three committed
# operations. The visible gate constrains the first intermediate result, so
# inverse symmetric recipes cannot accidentally become another accepted answer.
levels[19]=level(20,'留下中间的星','从锁印倒推每一步留下什么；不能直接删除开头或中间。',3,0,[
 node('s','source','能源',-.84,0,units=1,outputs=['m']),
 node('m','relay','月印',-.68,.21,-.17,outputs=['s1'],rune='月'),
 node('s1','relay','星印',-.49,.29,-.17,outputs=['u'],rune='星'),
 node('u','relay','日印',-.30,.29,-.17,outputs=['a'],rune='日'),
 node('a','socket','步骤 A',-.51,-.02,choices=[turn('先退印','er0'),turn('先倒序','rv0')]),
 process('er0','前段退印',-.28,.04,'door','erase',-.20),
 process('rv0','前段倒序',-.28,-.30,'door','reverse',.22),
 gate('door','月星刻门',-.02,.25,'b','月星',-.22),
 node('b','socket','步骤 B',.07,.01,choices=[turn('中段退印','er1'),turn('中段倒序','rv')]),
 process('er1','中段退印',.28,.26,'c','erase',-.20),
 process('rv','中段倒序',.27,-.28,'c','reverse',.23),
 node('c','socket','步骤 C',.49,-.02,choices=[turn('末段退印','er2'),turn('末段倒序','rv2')]),
 process('er2','末段退印',.68,.26,'g','erase',-.18),
 process('rv2','末段倒序',.68,-.28,'g','reverse',.23),
 node('g','goal','终点',.85,.02,units=1,code='星')],[1,2,1],
 ['目标只留中间的星；月星门还要求第一步之后留下月→星。',
  '先从刻门推断步骤A，再从单独星印倒推最后一次退印前必须是什么顺序。',
  'A先退印，留下月星；B倒序得到星月；C再退印，留下星。'])

# Each selection card explains its learning role, not its solution. Geometry
# and progress IDs remain stable except for the explicitly redesigned level20.
curriculum=[
 ('入门实验','把一次出口变化与光的实际去向对应起来。','这条出口真的通向目标，还是封闭断口？'),
 ('规则学习','用一次等分满足两个目标，确认能量守恒。','两个目标各要多少格，分流前一共有多少？'),
 ('动手教学','先读锁印，再亲手选择带正确印记的路线。','能量够了，为什么有的终点仍打不开？'),
 ('迁移挑战','把顺序和能量同时作为条件，从锁印倒推。','同样两个符文，经过顺序不同会怎样？'),
 ('空间辨认','区分前后层与共用接点，给必要支路留路。','投影交叉处真的是同一个接点吗？'),
 ('综合检验','同时规划库存、分流位置和符文顺序。','哪一个目标最缺少替代路线？'),
 ('规则学习','理解条件门检查完整印记，亲手送光验证。','门检查的是最后一枚，还是整串？'),
 ('迁移挑战','分别给两条支路准备通行印记。','分流以后，哪一段会改变各自的印记？'),
 ('规则学习','观察末印替换前后的实际结果。','换印以后，前面的印记会保留吗？'),
 ('加工推理','比较加工发生在路线不同位置时的结果。','先追加再替换，与先替换再追加相同吗？'),
 ('规则学习','操作一组联动插槽，观察两处同时变化。','一边调整时，另一条路线会付出什么代价？'),
 ('综合检验','让同一次联动同时满足两条路径。','怎样先固定最没有余地的一条支路？'),
 ('规则学习','区分比例棱镜与均分，按目标需求分光。','三格目标能靠二加二的均分满足吗？'),
 ('迁移挑战','同时满足上下支路的能量与通行印记。','比例正确以后，还要检查什么？'),
 ('空间挑战','用真实桥臂接合绕过暗石，再完成共道分光。','桥口合拢就一定通光吗？'),
 ('规则学习','亲手验证倒序镜会反转整串印记。','倒序改变能量，还是改变先后顺序？'),
 ('加工推理','比较追加与倒序的先后关系。','把同一个日印放在镜前或镜后，得到的开头相同吗？'),
 ('空间挑战','让真实反射命中，再决定共用镜放在哪里。','两路都需要改变开头，应该在分流前还是之后处理？'),
 ('规则学习','验证退印只删末尾，空白不能继续退印。','想保留哪个符文，就必须先让谁处在末尾？'),
 ('迁移挑战','通过三步加工保留中间的星，并满足中途刻门。','不能直接删开头，怎样让它变成末尾？'),
 ('综合检验','组合加工次序，为不同锁印保留不同结果。','每条支路最后一次加工之前，应该留下什么？'),
 ('规则学习','暂停逐拍观察，比较路程和等待。','早到的一路应补几拍，才能和另一边相遇？'),
 ('空间挑战','把真实折返长度与月日反向联动一起推算。','月日总段数固定，短路应比长路多展开几段？'),
 ('终章检验','综合能量、符文加工、联动与三路同拍。','先锁定能量需求，再推加工顺序，最后怎样补齐拍数？')]
for l,(role,objective,question) in zip(levels,curriculum):
 l.update(curriculum=role,objective=objective,question=question)

# The finale has its own route-and-delay permutation above.
(ROOT/'Assets/Resources/EnergyBoxes.json').write_text(json.dumps(dict(version=3,levels=levels),ensure_ascii=False,indent=2),encoding='utf-8')
print('Wrote 24 authored puzzles, four new mechanic families with practical tutorials')
