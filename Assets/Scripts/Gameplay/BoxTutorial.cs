namespace OrbitalLoom.Gameplay
{
    /// <summary>Progress follows committed gameplay state, never a timed text carousel.</summary>
    public sealed class BoxTutorial
    {
        public bool Active { get; private set; }
        public bool Started { get; private set; }
        public bool Completed { get; private set; }
        public bool Dismissed { get; private set; }
        public int Stage { get; private set; }
        private readonly bool splitter, runes;
        private readonly EnergyBoxLevel level;
        private bool readMechanic;
        private bool Advanced=>level!=null&&level.tutorial.Length>0;
        private bool readLock, readRune;
        private string installKind="turn",installCaption="装转向件",routeName;
        private string routeTarget;
        /// <summary>Teaching uses the authored, certified demonstration route.
        /// Check the actual choice, never count rotations: a freshly installed
        /// piece can already have the right exit. Ordinary play is unaffected.</summary>
        private int RouteStage(EnergyBoxSession session)
        {
            int selected=session.Selected;
            if(selected<0){routeTarget=runes?"a":level.tutorialSocket;return 12;}
            var socket=session.Sockets[selected];var piece=session.Installed(selected);
            int expected=level.reference[selected];
            if(expected==0)return NextSocket(session);
            var wanted=socket.choices[expected-1];
            installKind=wanted.kind;routeName=wanted.name;
            installCaption=installKind=="turn"?"装转向件":System.Array.Exists(socket.choices,c=>c.parts.Length>0)?"装棱镜":"装等分件";
            if(piece==null||piece.kind!=installKind)return 13;
            if(session.Choice(selected)!=expected)return 14;
            return NextSocket(session);
        }
        private int NextSocket(EnergyBoxSession session)
        {
            for(int i=0;i<session.Sockets.Length;i++)if(session.Choice(i)!=level.reference[i])
            {routeTarget=session.Sockets[i].id;return 12;}
            return 4;
        }
        public BoxTutorial(bool enabled,bool split,bool runeLesson=false,EnergyBoxLevel definition=null){Active=enabled;splitter=split;runes=runeLesson;level=definition;}
        public void Begin(EnergyBoxSession session){if(!Active)return;Started=true;Observe(session);}
        public void Skip(){Active=false;Dismissed=true;}
        public void Observe(EnergyBoxSession session)
        {
            if(!Active||!Started)return;
            if(Advanced)
            {
                if(session.Solved){Stage=7;Completed=true;Active=false;return;}
                if(session.Inspected==level.tutorialNode||session.Selected>=0&&session.Sockets[session.Selected].id==level.tutorialNode)readMechanic=true;
                if(!readMechanic){Stage=11;return;}
                if(session.Running){Stage=5;return;}
                if(session.TestFinished&&!session.Result.Complete){Stage=6;return;}
                Stage=RouteStage(session);return;
            }
            if(runes)
            {
                var inspected=System.Array.Find(session.Level.nodes,n=>n.id==session.Inspected);
                if(inspected!=null&&inspected.code.Length>0)readLock=true;
                if(readLock&&inspected!=null&&inspected.rune.Length>0)readRune=true;
                if(session.Solved&&readRune){Completed=true;Active=false;Stage=10;return;}
                if(!readRune){Stage=readLock?9:8;return;}
                if(session.Running){Stage=5;return;}
                if(session.TestFinished&&!session.Result.Complete){Stage=6;return;}
                Stage=RouteStage(session);
                return;
            }
            if(session.Solved){Stage=7;Completed=true;Active=false;return;}
            if(session.Running){Stage=5;return;}
            if(session.TestFinished&&!session.Result.Complete){Stage=6;return;}
            if(session.Selected<0){Stage=1;return;}
            if(session.Choice(0)==0){Stage=2;return;}
            Stage=session.Choice(0)==session.Level.reference[0]?4:3;
        }
        // Each step owns one focus: a scene node OR a dock button.
        public string Target => Active&&Started?(Stage==11?level.tutorialNode:Stage==12?routeTarget:Stage==8?"g1":Stage==9?"rs":Stage==1?"a":null):null;
        public string Button => !Active?null:!Started?"开始教学":Stage==13?installCaption:Stage==2?splitter?"装等分件":"装转向件":Stage==3||Stage==14?"出口 ›":Stage==4?"开始":Stage==6?"目标":null;
        private string MechanicIntro => level==null?"":level.tutorial=="prism"?"棱镜按出口标记分配格数，可以是3＋1，不能凭方向猜。":level.tutorial=="reverse"?"倒序镜反转整串印记；月→星变成星→月。":level.tutorial=="erase"?"退印台只移除最后一枚印记；空白会被阻断。":level.tutorial=="sync"?"每段1拍；阀可等待。计拍低音与水晶音帮助听出同拍，静音看拍数也能解。":null;
        private string LessonName => !Advanced?"操作教学":level.tutorial=="prism"?"比例棱镜":level.tutorial=="reverse"?"倒序镜":level.tutorial=="erase"?"退印台":level.tutorial=="sync"?"同步节拍":level.tutorial=="gate"?"条件门":level.tutorial=="swap"?"换印器":"联动插槽";
        public string Heading => Stage==8?"符文教学 · 读终点锁印":Stage==9?"符文教学 · 检查路径印记":LessonName+" · "+(Stage==0?"先看目标":Stage==11?"观察机关":Stage==12?"选择路线":Stage==13||Stage==2?"安装元件":Stage==1?"选中插槽":Stage==3||Stage==14?"调整出口":Stage==4?"验证你的路线":Stage==5?"观察光的去向":"读懂失败，再调整");
        public string Instruction => Advanced&&Stage==0?(MechanicIntro!=null?MechanicIntro+"\n点开始教学，先读发光机关。":level.tutorial=="gate"?"条件门会检查完整符文顺序，不满足就阻断。\n点“开始教学”，先读门的条件。":level.tutorial=="swap"?"换印器只替换末尾符文，前面的印记保留。\n点“开始教学”，先了解替换规则。":"带相同联动标记的两处会一起改变。\n装件、转向和取回均同步；先读联动关系。"):
            Stage==11?(level.tutorial=="link"?"点击发光的联动插槽，查看与谁联动。":"射线点击发光的新机关，读清条件与作用。"):
            Stage==12?"点击发光插槽。结合上方目标，决定从哪条路线送光。":
            Stage==13?"点发光的“"+installCaption+"”，把元件装入当前插槽。":
            Stage==14?"点发光的“出口 ›”，把当前插槽切到“"+routeName+"”。":
            Stage==4?"点发光的“开始”，让星光沿你接好的路线前进。":
            Stage==8?"射线点击发光的终点，查看它要求的符文。":Stage==9?"点击发光的“星印”，查看经过它会留下什么印记。":Stage==0?
            runes?"终点开始带有符文锁。光经过紫色检查点会留下印记。\n能量和符文顺序都要吻合；先学会读线索。":
            splitter?"金色能源有 2 格；两个蓝色终点各要 1 格。\n等分件把 2 格拆成 1＋1。准备好后点“开始教学”。":"金色是能源，蓝色是目标，红色是封闭断口。\n把 1 格光送到蓝色终点。准备好后点“开始教学”。":
            Stage==1?"用手柄射线对准闪亮的“插槽 A”，扣一下扳机。":
            Stage==2?splitter?"插槽已选中。点下方发光的“装等分件”。":"插槽已选中。点下方发光的“装转向件”。":
            Stage==3?"当前出口通向红色断口。点“出口 ›”，让亮线通向蓝色终点。":
            Stage==5?"这一刻只观察光点沿亮线前进，等待本次结果。":
            "点发光的“目标”，查看本次运行最先出错的原因。";
        public bool Allows(string action)
        {
            if(!Active)return true;
            if(action=="more"||action=="view")return true;
            if(runes||Advanced)
            {
                if(!Started||runes&&!readRune||Stage==11)return false;
                if(action=="test")return Stage==4||Stage==6;
                if(Stage==13)return action==installKind;
                if(Stage==14)return action=="rotate";
                return true;
            }
            return Stage==2&&(action==(splitter?"split":"turn")) || Stage==3&&action=="rotate" ||
                Stage==4&&action=="test" || Stage==6&&(action=="undo"||action=="rotate"||action=="remove");
        }
    }
}
