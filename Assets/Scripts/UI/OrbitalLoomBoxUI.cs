using System;
using System.Linq;
using OrbitalLoom.Rendering;

namespace OrbitalLoom.UI
{
    public sealed partial class OrbitalLoomUI
    {
        private int boxPage,boxRulePage,previewIndex;
        private void BoxPlaying()
        {
            var box=game.BoxSession;bool edit=!box.Running&&!box.Solved;int selected=box.Selected;
            var piece=selected<0?null:box.Installed(selected);var guide=box.Tutorial;
            bool teaching=guide.Active;
            Panel(0,.575f,1.30f,.26f);
            var metadata=Text("ORBITAL LOOM     /     "+(game.BoxLevelIndex+1).ToString("00")+" / "+game.BoxCampaign.levels.Length.ToString("00")+"     ·     余转 "+(box.Level.turns-box.Used("turn"))+" / 分 "+(box.Level.splitters-box.Used("split")),0,.68f,1.3f,.04f,13,SpatialUITheme.Secondary);
            if(box.Level.syncWindow>0&&box.Running)beatLabel=metadata;
            Text(box.Solved?"机关解开 · "+box.Level.title:box.Level.title,0,.585f,1.2f,.065f,27,box.Solved?SpatialUITheme.Accent:SpatialUITheme.Ink);
            Text((box.Level.syncWindow>0?"同拍目标   ":"目标   ")+box.GoalSummary,0,.49f,1.35f,.055f,15,SpatialUITheme.Secondary);
            // Keep the whole manipulation dock outside the three-dimensional graph.
            string selection=selected<0?"先点机关上的插槽":box.Sockets[selected].title+"  /  "+(piece==null?"空插槽":piece.name);
            Panel(0,-.67f,1.30f,.49f);
            Text(BoxWrap(teaching?guide.Instruction:box.Running?(box.ReviewingFailure?"首错复盘 · 已定格，可逐段或继续播放":box.Paused?"已暂停 · 可逐段检查":"跟随光点观察能量与符文"):box.Solved?"能量与锁印全部吻合":selection,40),0,-.475f,1.3f,.075f,16,SpatialUITheme.Ink);
            if(teaching&&!guide.Started)
            {
                Button("开始教学",-.18f,-.62f,.30f,box.BeginTutorial,true);
                Button("跳过教学",.18f,-.62f,.30f,box.SkipTutorial);
            }
            else if(box.Running)
            {
                Button(box.Paused?"继续":"暂停",-.23f,-.59f,.25f,box.TogglePause);
                Button("下一段",0,-.59f,.25f,box.Step);
                Button("停止",.23f,-.59f,.25f,box.Stop);
            }
            else if(box.Solved)
            {
                Button(game.BoxLevelIndex==game.BoxCampaign.levels.Length-1?"完成挑战":"下一关",0,-.59f,.40f,game.NextLevel,true);
            }
            else
            {
                bool hasTurn=selected>=0&&box.Sockets[selected].choices.Any(c=>c.kind=="turn");
                bool hasSplit=selected>=0&&box.Sockets[selected].choices.Any(c=>c.kind=="split");
                Button("装转向件",-.38f,-.59f,.22f,()=>box.Install("turn"),enabled:guide.Allows("turn")&&edit&&hasTurn&&(piece?.kind=="turn"||box.Used("turn")<box.Level.turns));buttons[buttonIndex-1].SetSelected(piece?.kind=="turn");
                Button(selected>=0&&box.Sockets[selected].choices.Any(c=>c.parts.Length>0)?"装棱镜":"装等分件",-.19f,-.59f,.22f,()=>box.Install("split"),enabled:guide.Allows("split")&&edit&&hasSplit&&(piece?.kind=="split"||box.Used("split")<box.Level.splitters));buttons[buttonIndex-1].SetSelected(piece?.kind=="split");
                Button("出口 ‹",.035f,-.59f,.15f,()=>box.Rotate(-1),enabled:guide.Allows("rotate")&&(!teaching||guide.Stage!=3&&guide.Stage!=14)&&edit&&piece!=null&&box.Sockets[selected].choices.Count(c=>c.kind==piece.kind)>1);
                Button("出口 ›",.18f,-.59f,.15f,()=>box.Rotate(1),enabled:guide.Allows("rotate")&&edit&&piece!=null&&box.Sockets[selected].choices.Count(c=>c.kind==piece.kind)>1);
                Button("取回",.35f,-.59f,.17f,box.Remove,enabled:guide.Allows("remove")&&edit&&piece!=null);
            }
            string message=teaching?guide.Stage==0?"教学可随时跳过，也可在“更多”中重学":guide.Stage==12&&box.Inspected!=null?box.Message:guide.Heading+" · 完成当前一步后继续":box.Message;
            Text(BoxWrap(message,42),0,-.70f,1.35f,.105f,14,box.Result!=null&&!box.Running&&!box.Result.Complete?SpatialUITheme.Error:SpatialUITheme.Secondary);
            Button("撤销",teaching?-.30f:-.38f,-.83f,.20f,game.Undo,enabled:guide.Allows("undo")&&!box.Running&&box.UndoAvailable>0,height:.09f);
            // Test is a momentary command. Persistent primary/selected styling
            // made it look pressed even while disabled or after a completed test.
            Button("开始",teaching?-.10f:-.19f,-.83f,.23f,()=>{box.Test();},false,edit&&guide.Allows("test"),.09f);
            if(!teaching)Button("提示 "+(3-box.Hints),0,-.83f,.19f,()=>{game.Hint();OpenOverlay("box-hint");},enabled:edit&&box.Hints<3,height:.09f);
            Button("目标",teaching?.13f:.19f,-.83f,.19f,()=>OpenOverlay("box-goals"),height:.09f);
            Button("更多",teaching?.33f:.38f,-.83f,.19f,()=>OpenOverlay("box-more"),height:.09f);
            Button("转视角 ‹",-.51f,.02f,.16f,()=>input.boxView.RotateView(-1),height:.085f);
            Button("转视角 ›",.51f,.02f,.16f,()=>input.boxView.RotateView(1),height:.085f);
            if(box.Observation!=null)Button("空间机关",.51f,-.15f,.16f,()=>OpenOverlay("box-relic"),height:.10f);
            for(int i=0;i<buttonIndex;i++)buttons[i].SetTutorialFocus(teaching,buttons[i].Caption==guide.Button);
        }
        // Native labels are meter-sized and do not share Unity TextMesh wrapping.
        // Explicit line breaks keep long clue/hint messages within the dock.
        private static string BoxWrap(string value,int columns)
        {
            if(string.IsNullOrEmpty(value)||value.Length<=columns)return value;
            var text=new System.Text.StringBuilder();int count=0;
            foreach(char c in value){if(c=='\n'){count=0;text.Append(c);continue;}if(count>=columns){text.Append('\n');count=0;}text.Append(c);count++;}
            return text.ToString();
        }
        private bool BoxOverlay()
        {
            if(RelicOverlay())return true;
            if(overlay=="box-hint")
            {
                var box=game.BoxSession;Back(()=>{overlay=null;Build();});
                Text("推理提示 · "+box.Hints+" / 3",0,.40f,1.3f,.08f,28);
                Text(BoxWrap(box.Message,28),0,.09f,1.35f,.40f,21);
                Text("先试着解释原因，再主动展开下一层",0,-.26f,1.35f,.08f,16,SpatialUITheme.Secondary);
                if(box.Observation!=null)Button("查看遗迹",-.26f,-.48f,.40f,()=>OpenOverlay("box-relic"));
                Button("继续推理",box.Observation!=null?.26f:0,-.43f,.40f,()=>{overlay=null;Build();},true);
                if(box.Hints<3)Button("再给一层提示",0,-.59f,.46f,game.Hint);return true;
            }
            if(overlay=="box-links")
            {
                var box=game.BoxSession;Back(()=>OpenOverlay("box-goals"));Text("联动表 · 规则线索",0,.48f,1.2f,.08f,28);
                var link=box.Level.links.First();var a=box.Level.nodes.First(n=>n.id==link.a);var b=box.Level.nodes.First(n=>n.id==link.b);
                Text(a.title+"    ↔    "+b.title,0,.34f,1.3f,.06f,17,SpatialUITheme.Secondary);
                for(int i=0;i<a.choices.Length;i++)
                {
                    float y=.20f-i*.125f;
                    Text(a.choices[i].name,-.30f,y,.65f,.07f,17);
                    Text(b.choices[link.map[i+1]-1].name,.30f,y,.65f,.07f,17);
                }
                Text("同一行一起变化 · 表中列出所有规则，不标答案",0,-.58f,1.4f,.07f,15,SpatialUITheme.Secondary);
                return true;
            }
            if(overlay=="box-select")
            {
                Back(()=>{overlay=null;Build();});Text("星域档案",0,.48f,1.2f,.08f,28);
                Text(ChapterNames[boxPage],0,.34f,1.2f,.06f,17,SpatialUITheme.Secondary);
                for(int cell=0;cell<6&&boxPage*6+cell<game.BoxCampaign.levels.Length;cell++)
                {
                    int i=boxPage*6+cell;
                    int index=i;var level=game.BoxCampaign.levels[i];
                    Button((i+1).ToString("00")+"  "+level.title+(game.BoxProgress.cleared.Contains(level.id)?" ✓":""),cell%2==0?-.27f:.27f,.15f-cell/2*.22f,.49f,()=>{previewIndex=index;OpenOverlay("box-preview");},height:.13f);
                }
                Button("‹ 上一星域",-.28f,-.51f,.40f,()=>{boxPage--;Build();},enabled:boxPage>0);
                Button("下一星域 ›",.28f,-.51f,.40f,()=>{boxPage++;Build();},enabled:(boxPage+1)*6<game.BoxCampaign.levels.Length);
                return true;
            }
            if(overlay=="box-preview")
            {
                var level=game.BoxCampaign.levels[previewIndex];Back(()=>OpenOverlay("box-select"));
                Text((previewIndex+1).ToString("00")+" / "+level.title,0,.43f,1.3f,.08f,27);
                Text(level.curriculum,0,.28f,1.3f,.07f,17,SpatialUITheme.Accent);
                Text(BoxWrap(level.objective,29),0,.10f,1.35f,.18f,20);
                Text("想一想",0,-.09f,1.3f,.06f,17,SpatialUITheme.Secondary);
                Text(BoxWrap(level.question,31),0,-.24f,1.35f,.18f,18);
                Button("进入谜题",0,-.49f,.48f,()=>{overlay=null;game.StartBox(previewIndex);},true);
                return true;
            }
            if(overlay=="box-goals")
            {
                var box=game.BoxSession;Back(()=>{overlay=null;Build();});Text("目标与诊断",0,.48f,1.2f,.08f,28);
                int row=0;
                foreach(var goal in box.Level.nodes.Where(n=>n.kind=="goal"))
                {
                    float y=.29f-row++*.20f;
                    Text(goal.title, -.40f,y,.29f,.055f,18,SpatialUITheme.Accent);
                    Text(box.GoalReport(goal),.12f,y,.98f,.13f,17);
                }
                bool failed=box.TestFinished&&!box.Result.Complete;
                if(failed&&box.Diagnosis!=null)
                {
                    Text(box.Diagnosis.Category+" · "+box.Diagnosis.Stamp(box.Level.syncWindow>0),0,-.22f,1.4f,.06f,17,SpatialUITheme.Error);
                    Text(BoxWrap(box.FailureDetail,38),0,-.34f,1.4f,.14f,16);
                    Button("首错复盘",0,-.47f,.39f,()=>{overlay=null;box.ReviewFailure();Build();},height:.075f);
                }
                else Text(BoxWrap(box.Running?"星光传递中 · 完整播放后再看诊断":box.Solved?"所有目标均满足 · 已通关":box.LearningQuestion,36),0,-.34f,1.4f,.18f,17,SpatialUITheme.Secondary);
                bool linked=box.Level.links.Length>0;
                if(linked)Button("联动表",-.26f,-.57f,.40f,()=>OpenOverlay("box-links"));
                Button(failed?"定位错误":"回到机关",linked?.26f:0,-.57f,linked?.40f:.48f,()=>{overlay=null;if(failed)box.FocusFailure();Build();},true);
                return true;
            }
            if(overlay=="box-more")
            {
                Back(()=>{overlay=null;Build();});Text("继续推理",0,.43f,1.1f,.08f,28);
                Button("回到机关",0,.22f,.48f,()=>{overlay=null;Build();},true);
                Button("重新开始",-.24f,.05f,.38f,()=>OpenOverlay("box-reset"));
                Button("星域选关",.24f,.05f,.38f,()=>OpenOverlay("box-select"));
                Button("正面视角",-.24f,-.13f,.38f,()=>{input.boxView.Front();overlay=null;Build();});
                Button("重新居中",.24f,-.13f,.38f,()=>{windows.ResetContent();overlay=null;Build();});
                Button("设置",-.24f,-.31f,.38f,game.OpenSettings);
                Button("主菜单",.24f,-.31f,.38f,game.MainMenu);
                Button("机关规则",-.24f,-.49f,.38f,()=>OpenOverlay("box-help"));
                Button(game.BoxSession.Tutorial.Active?"跳过本段教学":"重学教学",.24f,-.49f,.38f,()=>{overlay=null;if(game.BoxSession.Tutorial.Active)game.BoxSession.SkipTutorial();else game.StartBox(game.BoxSession.Level.spatial!=null?game.BoxLevelIndex:game.BoxLevelIndex>=21?21:game.BoxLevelIndex>=18?18:game.BoxLevelIndex>=15?15:game.BoxLevelIndex>=12?12:game.BoxLevelIndex>=10?10:game.BoxLevelIndex>=8?8:game.BoxLevelIndex>=6?6:game.BoxLevelIndex<3?game.BoxLevelIndex:2,true);Build();});
                return true;
            }
            if(overlay=="box-help")
            {
                Back(()=>OpenOverlay("box-more"));Text("先规划，再通电",0,.46f,1.2f,.08f,29);
                if(boxRulePage>=1)
                {
                    if(boxRulePage==2)
                    {
                        Text("07   比例棱镜与倒序镜",0,.29f,1.2f,.05f,18,SpatialUITheme.Accent);
                        Text("棱镜按出口标记分配，3＋1不同于2＋2。\n倒序镜反转整串：月→星→日变为日→星→月。",0,.16f,1.4f,.14f,18);
                        Text("08   退印台与同拍抵达",0,-.05f,1.2f,.05f,18,SpatialUITheme.Accent);
                        Text("退印只删末印，空白会阻断。\n每段1拍；听水晶合奏，也可静音看拍数。联动表在目标页。",0,-.20f,1.4f,.17f,18);
                        Button("‹ 前一页",0,-.56f,.42f,()=>{boxRulePage=1;Build();});return true;
                    }
                    Text("04   条件门",0,.29f,1.2f,.05f,18,SpatialUITheme.Accent);
                    Text("检查到达时的完整印记；必须与门标记完全一致。\n多印、少印或顺序不同都会阻断，能量不变。",0,.18f,1.4f,.12f,18);
                    Text("05   换印器",0,.01f,1.2f,.05f,18,SpatialUITheme.Accent);
                    Text("只替换最后一个符文，前面的印记全部保留。\n例如 月→日 经过 日换星 后成为 月→星。",0,-.10f,1.4f,.12f,18);
                    Text("06   联动组",0,-.27f,1.2f,.05f,18,SpatialUITheme.Accent);
                    Text("相同编号的两处成对变化，装件、转向、取回同步。\n一次撤销恢复两处；需同时满足两条路线。",0,-.38f,1.4f,.12f,18);
                    Button("‹ 基础规则",-.25f,-.56f,.40f,()=>{boxRulePage=0;Build();});Button("新机关 ›",.25f,-.56f,.40f,()=>{boxRulePage=2;Build();});return true;
                }
                Text("01   分配能量",0,.29f,1.2f,.05f,18,SpatialUITheme.Accent);
                Text("每个蓝色终点都要恰好收满。转向不改变能量。\n等分只能 4 → 2 + 2 或 2 → 1 + 1。",0,.18f,1.4f,.12f,18);
                Text("02   读懂导轨",0,.01f,1.2f,.05f,18,SpatialUITheme.Accent);
                Text("实线是当前出口，虚线是候选；只有接点相通。\n交叉不相连，不可合流、循环或丢弃能量。",0,-.10f,1.4f,.12f,18);
                Text("03   解开符文锁",0,-.27f,1.2f,.05f,18,SpatialUITheme.Accent);
                Text("点击终点读锁印。锁“月 → 星”要求依次过月、星。\n不能多印；等分复制已有印记。能量对，顺序错也失败。",0,-.38f,1.4f,.12f,18);
                Button("进阶机关 ›",0,-.56f,.42f,()=>{boxRulePage=1;Build();});
                return true;
            }
            if(overlay=="box-reset")
            {
                Text("清空当前布局？",0,.24f,1.1f,.09f,28);
                Text("元件会退回库存，已通关记录保留。",0,.07f,1.1f,.08f,18,SpatialUITheme.Secondary);
                Button("继续思考",-.22f,-.13f,.38f,()=>{overlay=null;Build();});
                Button("清空布局",.22f,-.13f,.38f,()=>{overlay=null;game.ResetLevel();Build();},true);
                return true;
            }
            return false;
        }
    }
}
