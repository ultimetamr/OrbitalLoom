# Orbital Loom 星轨织机 · Vibe Lab 资料包

整理日期：2026-11-05。基于当前0.9.8/code25、24关版本。这里是发布文案、宣传美术和技术交付资料，**不是已通过平台验证的Gradle构建工程**。

沿用既有GitHub组织ultimetamr，专用私有仓库：https://github.com/ultimetamr/OrbitalLoom 。其他应用仓库未改动。

## 直接使用

- docs/01-发布表单填写稿.md：名称、简介、版本说明、标签。
- docs/02-玩家操作说明.md：给试玩者的完整操作说明。
- docs/03-审核试玩说明.md：推荐审核路径与已知验证范围。
- docs/04-隐私信息草稿-待发行者确认.md：基于现有实现的事实说明，含待补字段。
- docs/05-提交状态与下一步.md：工程提交方式和当前缺口。
- docs/06-素材来源说明.md：区分海报、图标、Editor参考图和待补真机截图。
- docs/07-创作说明.md：可用于介绍创作过程的文案。
- technical/submission-fields.json：可复制的结构化表单文案。
- technical/project-facts.json：工程、测试及APK信息。
- technical/24关目录.txt：从当前关卡配置提取的真实关卡目录。
- media：符合尺寸和体积要求的宣传海报、现有图标。
- reference-editor：仅供选景的Editor参考图，不能冒充真机截图。

## 重要状态

现有工程使用Unity2022.3.62f3c1，现有APK为开发测试包。按2026-11-05核对的Vibe Lab要求，Unity作品需来自Unity6及以上的独立Android Gradle导出工程，并在本机预编译IL2CPP，再通过GitHub仓库与完整40位commit提交。资料包commit不是这个构建commit。

必填的1—7张正式详情截图仍需在提交版本确定后采集。现有Editor参考图含历史界面文字，不能标为0.9.8实机画面。宣传海报为AI宣传插画，不是游戏内截图。

local-testing中的0.9.8 APK仅在本地资料包中提供，Git忽略；平台不接受直接上传APK。不要把技术验证文档或隐私草稿直接当成已通过审核的声明。

资料包不包含账号、token、签名私钥、设备日志或用户存档。第三方SDK及美术授权不因本资料包自动授予。
