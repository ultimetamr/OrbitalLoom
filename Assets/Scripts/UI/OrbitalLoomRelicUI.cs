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
            string observationStep=observation.Found?"刻印已记录。点发光的“返回机关”。":observation.Aligned?"刻纹已对齐。点发光的“记录刻印”。":"点发光的“转动 ›”，每次转动15°，寻找完整刻纹。";
            Text(observationStep,0,.50f,1.35f,.11f,15,SpatialUITheme.Secondary);
            Panel(0,-.65f,1.30f,.48f);
            Text(observation.Status,0,-.455f,1.3f,.05f,17,observation.Aligned||observation.Found?SpatialUITheme.Accent:SpatialUITheme.Ink);
            Text(BoxWrap(observation.Found?observation.Clue.discovery:observation.Clue.instruction,36),0,-.56f,1.35f,.13f,16,SpatialUITheme.Ink);
            Button("‹ 转动",-.26f,-.71f,.23f,()=>box.RotateRelic(-1),height:.085f);
            Button("记录刻印",0,-.71f,.25f,()=>box.RecordRelic(),enabled:observation.Aligned&&!observation.Found,height:.085f);
            Button("转动 ›",.26f,-.71f,.23f,()=>box.RotateRelic(1),height:.085f);
            Button("返回机关",-.25f,-.84f,.39f,()=>{overlay=null;Build();},true,height:.085f);
            Button("文字线索",.25f,-.84f,.39f,()=>box.RecordRelic(true),enabled:!observation.Found,height:.085f);
            FocusTutorialButtons(observation.Found?"返回机关":observation.Aligned?"记录刻印":"转动 ›");
            return true;
        }
        private bool SpatialOverlay()
        {
            var box=game.BoxSession;var rig=box.Level.spatial;
            string title=rig.kind=="bridge"?"空间接合 · 双桥臂":rig.kind=="mirror"?"镜面寻路 · 反射水晶":"三声回廊 · 折返光路";
            string lesson=box.SpatialLessonCompleted?"机关已就绪。点发光的“返回机关”。":!box.SpatialControlChosen?"先点发光的机关名称，选中这次要调整的部件。":box.SpatialEdits==0?"点一次发光的右侧调整按钮，观察机关发生什么变化。":
                rig.kind=="bridge"?"继续调整桥臂，让两端相遇；路径须避开暗石。":
                rig.kind=="mirror"?"继续调整镜面，让出射光束穿过水晶。":
                "继续调整折返段数，让三路总拍数相同。";
            if(spatialReading)lesson=rig.kind=="bridge"?"模型刻度：轴距48cm，臂长约34cm。两端重合还须避开前层暗石。":rig.kind=="mirror"?"光从左向右入射；镜面法线决定反射方向。让出射束穿过水晶中心。":"模型刻度：速度30cm/拍；折返增加30cm就多走1拍。月日段数合计4。";
            Panel(0,.58f,1.30f,.26f);Text(title,0,.64f,1.3f,.07f,27);
            Text(BoxWrap(lesson,40),0,.51f,1.40f,.12f,15,SpatialUITheme.Secondary);
            Panel(0,-.65f,1.30f,.48f);
            for(int i=0;i<rig.controls.Length;i++)
            {
                int control=i;float x=(i-(rig.controls.Length-1)*.5f)*.28f;
                string name=rig.kind=="bridge"?"桥臂 "+(i==0?"H":"I"):rig.kind=="mirror"?"反射镜":new[]{"月路 B","星路 D","日路 E"}[i];
                Button(name,x,-.46f,.25f,()=>box.SelectSpatial(control),enabled:!box.Running&&!box.Solved,height:.075f);
                buttons[buttonIndex-1].SetSelected(box.SpatialSelected==i);
            }
            Text(BoxWrap(box.SpatialReport,45),0,-.565f,1.42f,.12f,15,SpatialUITheme.Ink);
            Button(rig.kind=="path"?"收回一段":"机关 ‹",-.29f,-.70f,.24f,()=>box.RotateSpatial(-1),enabled:!box.Running&&!box.Solved,height:.085f);
            Button("撤销",0,-.70f,.22f,box.Undo,enabled:!box.Running&&box.UndoAvailable>0,height:.085f);
            Button(rig.kind=="path"?"展开一段":"机关 ›",.29f,-.70f,.24f,()=>box.RotateSpatial(1),enabled:!box.Running&&!box.Solved,height:.085f);
            Button("返回机关",-.25f,-.84f,.39f,()=>{box.DismissSpatialLesson();box.RecordRelic(true);overlay=null;Build();},true,height:.085f);
            Button("规则与读数",.25f,-.84f,.39f,()=>{spatialReading=!spatialReading;Build();},height:.085f);
            Button("转视角 ‹",-.51f,.02f,.16f,()=>box.RotateRelic(-1),height:.085f);
            Button("转视角 ›",.51f,.02f,.16f,()=>box.RotateRelic(1),height:.085f);
            if(!box.SpatialLessonDismissed)
                FocusTutorialButtons(box.SpatialLessonCompleted?"返回机关":!box.SpatialControlChosen?(rig.kind=="bridge"?"桥臂 H":rig.kind=="mirror"?"反射镜":"月路 B"):rig.kind=="path"?"展开一段":"机关 ›");
            return true;
        }
        private void FocusTutorialButtons(string caption)
        {for(int i=0;i<buttonIndex;i++)buttons[i].SetTutorialFocus(true,buttons[i].Caption==caption);}
        private bool spatialReading;
    }
}
