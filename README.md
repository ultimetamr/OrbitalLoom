# Orbital Loom 星轨织机

面向 PICO 的 3D 混合现实解谜游戏。玩家在神秘遗迹与水晶构成的立体星仪中，推理能量分配、符文顺序、光路方向与同步时序，逐步解开 24 道机关谜题。

当前版本：**0.9.8 / versionCode 25**。采用 Full Space 独立 MR 空间，已验证的操作方式为手柄射线与扳机。包含主菜单、章节选关、分步教学、提示、撤销、进度保存与节拍演出。

## 工程环境

- Unity **2022.3.62f3c1**，C#，Android / ARM64 / IL2CPP。
- PICO XR SDK **6.1.1**、Spatial Adapter **6.1.0**；已嵌入的依赖保存在 `Packages/`。
- 启动场景：`Assets/Scenes/OrbitalLoom.unity`。

## 获取与打开

1. 本仓库为公开仓库，可直接访问或克隆，无需登录 GitHub。
2. 克隆本仓库，使用 Unity Hub 添加克隆目录，以指定的 Unity 版本打开。
3. 安装该 Unity 版本对应的 Android Build Support、SDK/NDK 和 OpenJDK，等待首次导入及 Unity 注册表依赖解析。
4. 打开启动场景，通过 Unity Test Runner 的 EditMode 页运行测试；Android 构建入口为 Unity 的 Build Settings。
5. 真机操作需要兼容的 PICO 设备与已授权的 USB 调试连接。首次导入、构建和真机验收仍取决于本机工具链与设备环境。

## 目录

- `Assets/Scripts/`：核心状态、关卡数据、玩法、机关、输入、渲染、UI、窗口与音频代码。
- `Assets/Tests/EditMode/`：规则、求解、教学、界面与节拍相关测试源码。
- `Assets/Scenes/`、`Assets/Resources/`、`Assets/StreamingAssets/`：场景、资源和关卡配置。
- `Packages/`：包清单、锁文件及已嵌入的第三方 SDK 与依赖。
- `ProjectSettings/`：Unity 工程配置。
- `Tools/`：关卡与音频等内容制作脚本。
- `docs/`、`media/`：发布文案、操作与审核说明、隐私草稿、图标和宣传海报。
- `technical/source-baseline-manifest.json`：源工程 6,038 个文件的大小及 SHA-256 清单。

## 分支与验证范围

`main` 包含完整开发源工程和发布资料；`unity-2022-baseline` 保留最初的 Unity 2022.3 源工程备份。源文件已与作者工程逐一比对。此次上传未改动玩法或升级 SDK，也未重新进行运行测试。

已归档的开发验证记录为 144 项 EditMode 测试通过、24 关界面流程通过；不能将这些记录等同于本次克隆后的构建、真机完整验收或官方评级。

当前是 Unity 开发工程，**不是 Vibe Lab 所需的 Unity 6 独立 Android Gradle 导出工程**，不能将本次源码 commit 直接作为平台可构建应用版本。正式提交的剩余事项见 `docs/05-提交状态与下一步.md`。

## 资源与隐私

未上传 Unity 缓存、临时文件、本地用户设置、设备日志、用户存档、账号凭据或签名私钥。第三方 SDK 与素材遵循各自许可证，本仓库不额外授予第三方授权。

海报为 AI 宣传插画；`reference-editor/` 为历史 Editor 参考图，不能当作最终真机截图。发行者身份与隐私草稿中的待确认字段需在正式提交前核对。
