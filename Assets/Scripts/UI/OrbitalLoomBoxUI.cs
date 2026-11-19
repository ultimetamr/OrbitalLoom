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
            var box=game.BoxSession;var guide=box.Tutorial;bool edit=!box.Running&&!box.Solved;
            Panel(0,.59f,1.22f,.22f);
            Text("O R B I T A L   L O O M     /     "+(game.BoxLevelIndex+1).ToString("00")+" — 24",0,.665f,1.2f,.04f,12,SpatialUITheme.Accent);
            Text(box.Level.title,0,.585f,1.15f,.06f,25);
            Text(box.GoalSummary,0,.505f,1.25f,.05f,14,SpatialUITheme.Secondary);
            string instruction=guide.Active?guide.Instruction:box.Running?"观察星光 · 可暂停并逐段检查":box.Solved?"遗迹已苏醒 · 所有目标均满足":"捏住晶体拖入插槽 · 抓住机关转动 · 松手吸附档位";
            Panel(0,-.73f,1.50f,.32f);
            Text(BoxWrap(instruction,39),0,-.655f,1.27f,.105f,15);
            if(guide.Active&&!guide.Started)
            {
                Button("开始教学",-.19f,-.79f,.31f,box.BeginTutorial,true,height:.095f);
                Button("跳过教学",.19f,-.79f,.31f,box.SkipTutorial,height:.095f);
            }
            else if(box.Running)
            {
                Button(box.Paused?"继续":"暂停",-.25f,-.79f,.23f,box.TogglePause,height:.095f);
                Button("下一段",0,-.79f,.23f,box.Step,height:.095f);
                Button("停止",.25f,-.79f,.23f,box.Stop,height:.095f);
            }
            else if(box.Solved)Button("下一关",0,-.79f,.38f,game.NextLevel,true,height:.095f);
            else
            {
                Button("撤销",-.39f,-.79f,.19f,game.Undo,enabled:guide.Allows("undo")&&box.UndoAvailable>0,height:.095f);
                Button("开始",-.13f,-.79f,.24f,box.Test,enabled:edit&&guide.Allows("test"),height:.095f);
                Button("目标",.13f,-.79f,.19f,()=>OpenOverlay("box-goals"),height:.095f);
                Button("更多",.39f,-.79f,.19f,()=>OpenOverlay("box-more"),height:.095f);
            }
            if(!guide.Active&&!box.Running&&!box.Solved)
                Text(box.Selected<0?"青晶 / 转向      紫晶 / 分流":box.Sockets[box.Selected].title+" · "+(box.Installed(box.Selected)?.name??"等待放入晶体"),0,-.46f,1.05f,.05f,13,SpatialUITheme.Secondary);
            if(!box.Running&&!box.Solved)
            {
                if(box.Level.turns>0)Text("转向晶体",-.48f,.17f,.22f,.04f,12,SpatialUITheme.Secondary);
                if(box.Level.splitters>0)Text("分流晶体",-.48f,-.19f,.22f,.04f,12,SpatialUITheme.Secondary);
            }
            if(box.Observation!=null)Button("遗迹线索",.55f,.28f,.19f,()=>OpenOverlay("box-relic"),height:.085f);
            if(box.Level.syncWindow>0&&box.Running)beatLabel=Text("",0,-.48f,1.2f,.05f,14,SpatialUITheme.Accent);
            for(int i=0;i<buttonIndex;i++)buttons[i].SetTutorialFocus(guide.Active,buttons[i].Caption==guide.Button);
        }
        private bool AssistedControls()
        {
            if(overlay!="box-assist")return false;
            var box=game.BoxSession;int selected=box.Selected;
            Back(()=>OpenOverlay("box-more"));Text("辅助操作",0,.44f,1.2f,.08f,27);
            Text("手部疲劳时可使用这些等效操作",0,.31f,1.25f,.06f,16,SpatialUITheme.Secondary);
            Text(selected<0?"先回到机关选中一个插槽":box.Sockets[selected].title,0,.18f,1.2f,.06f,19);
            bool edit=selected>=0&&!box.Running&&!box.Solved;
            Button("装转向件",-.25f,.02f,.39f,()=>box.Install("turn"),enabled:edit&&box.Tutorial.Allows("turn"));
            Button("装等分件",.25f,.02f,.39f,()=>box.Install("split"),enabled:edit&&box.Tutorial.Allows("split"));
            Button("出口 ‹",-.25f,-.15f,.39f,()=>box.Rotate(-1),enabled:edit&&box.Tutorial.Allows("rotate"));
            Button("出口 ›",.25f,-.15f,.39f,()=>box.Rotate(1),enabled:edit&&box.Tutorial.Allows("rotate"));
            Button("取回",0,-.32f,.39f,box.Remove,enabled:edit&&box.Tutorial.Allows("remove"));
            Button("回到机关",0,-.51f,.48f,()=>{overlay=null;Build();},true);return true;
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
            if(AssistedControls()||RelicOverlay())return true;
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
                Button("辅助操作",-.24f,-.31f,.38f,()=>OpenOverlay("box-assist"));
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
