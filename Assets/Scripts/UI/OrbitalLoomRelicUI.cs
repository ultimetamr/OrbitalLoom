namespace OrbitalLoom.UI
{
    public sealed partial class OrbitalLoomUI
    {
        private bool RelicOverlay()
        {
            if(overlay!="box-relic")return false;
            var box=game.BoxSession;var observation=box?.Observation;
            if(box?.Level.spatial!=null)return SpatialOverlay();
            if(observation==null){overlay=null;Build();return true;}
            Panel(0,.58f,1.30f,.26f);
            Text(observation.Clue.title,0,.63f,1.3f,.07f,27);
            string observationStep=observation.Found?"刻印已记录。点发光的“返回机关”。":observation.Aligned?"刻纹已对齐。点发光的“记录刻印”。":"抓住遗迹下方金环并转动，寻找完整刻纹。";
            Text(observationStep,0,.50f,1.35f,.11f,15,SpatialUITheme.Secondary);
            Panel(0,-.65f,1.30f,.48f);
            Text(observation.Status,0,-.455f,1.3f,.05f,17,observation.Aligned||observation.Found?SpatialUITheme.Accent:SpatialUITheme.Ink);
            Text(BoxWrap(observation.Found?observation.Clue.discovery:observation.Clue.instruction,36),0,-.56f,1.35f,.13f,16,SpatialUITheme.Ink);

            Button("记录刻印",0,-.71f,.25f,()=>box.RecordRelic(),enabled:observation.Aligned&&!observation.Found,height:.085f);

            Button("返回机关",-.25f,-.84f,.39f,()=>{overlay=null;Build();},true,height:.085f);
            Button("文字线索",.25f,-.84f,.39f,()=>box.RecordRelic(true),enabled:!observation.Found,height:.085f);
            FocusTutorialButtons(observation.Found?"返回机关":observation.Aligned?"记录刻印":null);
            return true;
        }
        private bool SpatialOverlay()
        {
            var box=game.BoxSession;var rig=box.Level.spatial;
            string title=rig.kind=="bridge"?"空间接合 · 双桥臂":rig.kind=="mirror"?"镜面寻路 · 反射水晶":"三声回廊 · 折返光路";
            string lesson=box.SpatialLessonCompleted?"机关已就绪，返回继续布置光路。":rig.kind=="path"?
                "抓住一条光路起点的金环，向前拖动展开、向后拖动收回；松手确定段数。":
                "抓住机关轴心的金环，转动手腕或沿圆周拖动；松手吸附档位。";
            if(spatialReading)lesson=rig.kind=="bridge"?"模型刻度：轴距48cm，臂长约34cm。两端重合还须避开前层暗石。":rig.kind=="mirror"?"光从左向右入射；镜面法线决定反射方向。让出射束穿过水晶中心。":"模型刻度：速度30cm/拍；折返增加30cm就多走1拍。月日段数合计4。";
            Panel(0,.58f,1.30f,.26f);Text(title,0,.64f,1.3f,.07f,27);
            Text(BoxWrap(lesson,40),0,.51f,1.40f,.12f,15,SpatialUITheme.Secondary);
            Panel(0,-.65f,1.30f,.48f);
            Text(BoxWrap(box.SpatialReport,45),0,-.565f,1.42f,.12f,15,SpatialUITheme.Ink);
            Button("撤销",0,-.70f,.22f,box.Undo,enabled:!box.Running&&box.UndoAvailable>0,height:.085f);
            Button("返回机关",-.25f,-.84f,.39f,()=>{box.DismissSpatialLesson();box.RecordRelic(true);overlay=null;Build();},true,height:.085f);
            Button("规则与读数",.25f,-.84f,.39f,()=>{spatialReading=!spatialReading;Build();},height:.085f);
            if(!box.SpatialLessonDismissed)FocusTutorialButtons(box.SpatialLessonCompleted?"返回机关":null);
            return true;
        }
        private void FocusTutorialButtons(string caption)
        {for(int i=0;i<buttonIndex;i++)buttons[i].SetTutorialFocus(true,buttons[i].Caption==caption);}
        private bool spatialReading;
    }
}
