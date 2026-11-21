# Overcooked! 2 HUD Customization

《胡闹厨房 2》的独立局内 HUD 参数调整 MOD，基于 BepInEx 5 / HarmonyX，设置仅影响本机显示。

当前版本为 **0.2.0**，插件标题为 `OC2 HUD 自定义 / OC2 HUD Customization`。本仓当前源码包含菜谱顶边裁剪、最大行数和盘中图标首帧修复。GitHub 的 v0.2.0 附件为此前发行的构建，使用这三项改动需要从当前源码构建 DLL。用法及验收记录见 [编辑器说明](docs/HudEditor.md) 和 [实施记录](docs/Implementation.md)。

**可拖动的原生风格 HUD 编辑面板**需 ModSettings ≥1.1.1。使用纯色底板、游戏蓝色标题栏和原生字体；十类通过一个下拉列表选择，选中即高亮并临时预览，本关未创建的项置灰。容器图标组支持主体拖动、四边改宽高、四角等比缩放，拖一个对全局同类生效；共享 X/Y、大小、宽高倍率和每行上限，原材料与加号不透明度用两条同时显示的滑块调整。保存统一提交，取消恢复，共60项隐藏参数。左右装饰条及顶部说明已移除。编辑期间关卡与订单计时暂停，菜谱仍可展开并平滑重排。

## 基础功能

- 调整常规局内菜谱卡片大小、每行显示数量和最大行数。每行数量设为 `0` 时保持原生单行。开启换行后，最大行数 `0` 表示不限，达到上限则把剩余订单按顺序接在末行右侧，末行可能延伸到屏幕外。
- 调整容器内容物与空位加号图标大小、每行显示数量。盘中食材图标从首次显示起使用已配置的大小和不透明度。
- 分别调整菜谱、空位加号、已加入食材的不透明度。

以上参数只作用于本机显示，保留原订单数量、容器容量、配方、计时及网络同步。Horde 的独立订单显示链需要另外适配。

## 下载与安装

从本仓库的 [GitHub Releases](https://github.com/CH3NGY2/Overcooked-2-HudCustomization/releases) 下载 `OC2HudCustomization-v0.2.0.zip`，解压后将唯一的 `OC2HudCustomization.dll` 放入游戏的 `BepInEx/plugins`，覆盖旧版并重新启动游戏，只保留一份本插件。ZIP 仅包含运行 DLL；GitHub 自动生成的 Source code 压缩包用于开发，不能直接安装。

需要 Windows 版《胡闹厨房 2》Mono 与已安装的 BepInEx 5 / HarmonyX。游戏内编辑入口需要 **ModSettings ≥1.1.1**；缺少设置面板时，cfg与已保存的参数仍可应用。默认参数保留原生布局与不透明度。编辑器限没有远程玩家的本地普通厨房。

更新说明见 [v0.2.0](docs/releases/v0.2.0.md)，初版记录保留在 [v0.1.0](docs/releases/v0.1.0.md)。当前已验证 Windows 本机环境，物理输入、全部语言和分辨率、完整混合配方/订单生命周期及双端多人仍待验收。

## 开发入口

1. [实施记录](docs/Implementation.md)：已完成工作、验证结果与尚未覆盖的范围。
2. [研究报告](docs/GameplayHudCustomizationResearch.md)：原版调用链、建议挂点、配置、生命周期与验收方案。
3. [原始程序集核验记录](docs/evidence/metadata-verification.json)：DLL 身份与 7 个类型、20 个方法的访问性。
4. [源码引用清单](docs/evidence/source-references.json)：研究引用的源码路径、行号和文件摘要。

研究报告是实现方案来源；实现状态以 [实施记录](docs/Implementation.md) 为准。文档中的 `.validation/`、GOAL.md、截图和存档核验原件是本地开发记录，不随源码发布；仓库保留汇总证据，不含游戏或依赖 DLL、玩家 cfg、存档及测试日志。

## 本机外部依赖位置

- 原版反编译源码：`E:\code\OC2\Assembly-CSharp\Windows\Assembly-CSharp`。
- 原版 DLL：`E:\SteamLibrary\steamapps\common\Overcooked! 2\Overcooked2_Data\Managed\Assembly-CSharp.dll`。
- BepInEx/Harmony：游戏目录下 `BepInEx\core`，后续实施时核对实际安装版本。
- RecipeViewer 兼容研究参考：`E:\code\OC2\DLL\Decompiled\Overcooked2RecipeViewer`。

共享源码、游戏 DLL 和其他 MOD 保留原位。清单中的路径是这台机器的研究环境，换机器需重新定位和核验；它们不属于本项目交付包。

## 构建与设置

```powershell
.\scripts\Build.ps1
.\scripts\Test.ps1
.\scripts\Verify-Contracts.ps1
.\scripts\Package.ps1 -ExpectedSha256 '<已验证构建的64位SHA256>'
```

需要 Visual Studio MSBuild/.NET Framework 3.5 开发引用，以及真实游戏目录内的 BepInEx 5 / HarmonyX。支持 `-GameRoot`，构建和逻辑测试可传 `-MSBuild`。产物位于 `bin/Release/OC2HudCustomization.dll`；构建不会自动安装或启动游戏。

设置使用原生 BepInEx `ConfigEntry`，公共 ModSettings 可直接发现。共 10 项，包含大小、列数、菜谱最大行数、三类不透明度、总开关和菜谱行间距，默认保持原生显示。插件 GUID 为 `com.ch3ngyz.plugin.overcooked2.hudcustomization`，设置文件为 `BepInEx/config/com.ch3ngyz.plugin.overcooked2.hudcustomization.cfg`。面板修改在主线程应用，手工编辑磁盘文件需重启或由工具 Reload。参数范围见 [实施记录](docs/Implementation.md#身份构建与设置)。

另有60项 `HudEditor.*` 隐藏设置，不在设置页逐项列出；ModSettings 1.1.1 通过一个操作按钮进入编辑器。缺少该服务时，原有设置和已保存的隐藏参数仍可应用，但不能打开编辑器。

从早期 `com.oc2.hudcustomization` 升级时，仅在新配置文件不存在时自动复制旧 cfg 并加载，保留全部旧配置原文及旧文件；已有新文件始终优先，后续只使用新文件。不要同时安装新、旧 GUID 的两个插件 DLL。

配置名称、说明和分区默认英文；游戏语言就绪后自动跟随游戏的 12 种语言（含简体/繁体中文、日语、韩语），未知语言回退英文。主线程每秒检查一次；同批文案更新后通过源码 SDK 通知 ModSettings 刷新，语言不变不会重复重建面板。翻译保持 Section/Key 与设置值，不保存或重读 cfg；本次 GUID 调整的文件迁移见上段。显示元数据需要支持 `ModSettingsAttributes` 的 ModSettings 1.1.0 或更新版本；缺少服务时仍可使用配置和 HUD 功能。

多语言桌面验证沿用 `scripts/Test.ps1`。可另运行 `scripts/Test-ModSettings.ps1`，读取相邻 ModSettings 项目已编译的 DLL 验证实际元数据读取/排序；支持 `-ModSettingsDll`，不会构建或修改提供端，也不会安装游戏。两者均不构成真实面板显示验收，详见 [实施记录](docs/Implementation.md#配置多语言与-modsettings-刷新2026-11-17)。

手动安装只需复制本插件 DLL 到游戏 `BepInEx/plugins` 并重启加载。编译和打包不会自动安装；隔离实机测试会恢复启动前的插件、配置和共享日志，并核对原存档。旧身份迁移的历史验证见实施记录；不要分发游戏 DLL、依赖 DLL、测试文件或证据目录。

菜谱换行根据实际显示边界计算行高，每行按最高卡片加行间距下移。自定义大小或列数时，以主菜卡白纸的顶边裁掉上方露出的纸角和展开中的分支。白纸下的食材与炊具继续按原动画展开，每张卡保留一个时间进度条。关闭自定义布局会恢复原生显示。

新菜谱先沿第一行从右向左进入，横向剩余距离降到约 20% 时快速落位，再显示食材与烹饪步骤，接原生展开。落位进度跟随原生 Animator，不另按墙上时钟计时；当前原生 1.5 秒动画下，快速落位约 0.21 秒。已有卡片换行使用平滑回流，不重播入场。

`scripts/Test-RecipeBounds.ps1` 可运行生产测量/排布代码的桌面边界替换回归；`scripts/Test-UnityRecipes.ps1` 会临时部署测试插件、隔离存档并启动真实游戏，只能在玩家游戏未运行时使用。它默认恢复原 DLL，`-KeepProductionBuild` 保留本轮正式构建；两种方式均恢复配置、共享日志及临时测试文件。实机结果见 [菜谱与盘中图标核验](docs/evidence/recipe-top-clip-verification.json)。

`scripts/Test-UnityRecipes.ps1 -Editor` 沿用同一隔离和恢复入口，检查真实设置页编辑按钮、原生 HUD 拖动/缩放、隐藏元素预览及退出恢复。`Package.ps1` 要求显式传入已验证构建的 `-ExpectedSha256`，不重新编译；单DLL白名单、版本一致性和解压后hash均会检查。
