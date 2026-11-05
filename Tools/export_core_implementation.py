"""Build a reviewable snapshot from actual source files, without generated binaries."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
files=sorted([*root.glob('Assets/Scripts/**/*.cs'),*root.glob('Assets/Editor/*.cs')])
parts=['# Orbital Loom 全模块核心实现\n\n版本 0.9.3 / code 20，Full Space 独立 MR，启动主菜单，四星域24关。新增比例棱镜、倒序镜、退印台、同步节拍及对应操作教学。采用神秘遗迹、水晶、青铜刻线与深青高对比UI，清除旧星环、自由连线和旧沙盒运行内容。重做15/18/23/24关的耦合推理，加入84BPM原创节拍、三音同拍、DSP暂停恢复与独立音乐音量。以下按目录汇集当前实际 C# 源码，共 '+str(len(files))+' 个文件。第15/18/23关加入视角拼符、背面刻印、三路刻度与分级提示，祭台修复与水晶升起。验证边界见 Builds/Spatial093-Verification.md。\n']
for file in files:
    parts.append('\n## '+file.relative_to(root).as_posix()+'\n\n```csharp\n'+file.read_text(encoding='utf-8-sig').rstrip()+'\n```\n')
(root/'DELIVERY/CoreImplementation.md').write_text(''.join(parts),encoding='utf-8')
print(f'Exported {len(files)} source files')
