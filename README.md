# Orbital Loom 星轨织机

面向 PICO 的独立 MR 空间解谜游戏。观察神秘遗迹与水晶，推理能量分配、符文顺序、光路方向和同步时序，解开四个星域的 24 道谜题。

当前版本 **1.0.0 / versionCode 28**，Unity **6000.6.8f1**，Android ARM64 / IL2CPP。SDK 与嵌入依赖保存在 `Packages/`。应用包名为 `com.orbitalloom.puzzle`。

## 本版变化

- 直接抓取水晶并拖入插槽，吸附环出现后松手安装。
- 抓住已安装机关，转动手腕或沿圆周拖动，松手提交档位；支持撤销。
- 空间展品与谜题增加直接操作握环，按钮辅助操作保留在“更多 → 辅助操作”。
- 精简游玩 UI、缩短节点标签、优化蓝石与水晶外观，移除重复装饰。
- 教学按当前一步高亮插槽、水晶或开始按钮。

## 打开与构建

1. 使用 Unity Hub 和 Unity 6000.6.8f1 打开仓库根目录，安装配套 Android Build Support / SDK / NDK / OpenJDK。
2. 启动场景为 `Assets/Scenes/OrbitalLoom.unity`。已保存场景包含原生导出所需对象，不需重新生成旧版场景。
3. EditMode 测试入口为 `OrbitalLoom.Editor.OrbitalLoomTestRunner.Run`；直接操作回归入口为 `OrbitalLoom.Editor.OrbitalLoomDirectSmoke.Run`。
4. 导出入口为 `OrbitalLoom.Editor.OrbitalLoomDirectBuild.Export`，默认导出到 `Builds/AndroidExport-1.0.0`。可用环境变量 `ORBITAL_LOOM_EXPORT_DIR` 指定其他空目录。
5. 执行 `python Tools/package_android.py <导出目录> <Unity AndroidPlayer目录>`，补齐星轨启动图标并使用配套 Gradle 构建 APK。

## 验证与限制

本版 171 项 EditMode 测试通过；24 关使用抓取、移动、旋转、松手入口回归通过；Release APK 构建、签名与 zipalign 检查通过。构建号 28 已安装到 PICO，用户确认菜单可见，设备日志收到晶体抓取和节点点击。

完整的真机手势放置、旋转手感和性能仍待验收，上述结果不代表平台评审通过。部分 PICO 预编译库存在 16KB ELF 对齐警告，未宣称全面兼容。

## 源码与提交工程

- 本仓库：完整 Unity C# 源码、场景、资源、SDK、测试与制作工具。
- Vibe Lab 提交工程：https://github.com/ultimetamr/OrbitalLoom-VibeLab 。GitHub + Commit 表单使用该导出仓库及对应版本 commit。
- `unity-2022-baseline` 分支和 `technical/source-baseline-manifest.json` 为历史归档，不描述当前 Unity 6 文件集合。
- `docs/`、`media/`、`reference-editor/` 中旧版文案、图片及演示脚本为历史材料；当前变化和验证以 `docs/09-1.0.0更新与验证.md` 为准。

未上传 Unity 缓存、用户存档、设备原始日志、账号凭据、签名私钥或 APK。第三方 SDK 与素材保留各自许可；宣传插画不可作为真机截图。
