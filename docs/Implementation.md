# HUD Customization 0.1.0 实施记录

日期：2026-11-17。首版工程已实现并安装；**多行菜谱遮挡、主菜卡多余纸面及分阶段入场已完成局部真实 Unity 截图与几何验证，其余 UI 路径仍按下文保留待验范围**。研究报告保持原始研究语义，当前实现与限制以本记录和根目录 GOAL.md 为准。

2026-11-18 当前开发增量：**可拖动的原生风格编辑面板、统一类别选择与菜谱/容器参数草稿**已实现，见 [编辑器说明](HudEditor.md)。公开 Release 尚未更新；测试后恢复的本机 DLL 是上一轮编辑器构建。本页各历史章节的构建和安装状态仅适用于当时，不代表当前部署。

## 原生编辑面板与统一类别（2026-11-18）

- `HudNativeAssets` 从实际 GameOptions 捕获纸面、标题条、按钮素材，复制为自有 sprite/texture；原生字体经 bundle22 身份校验和成对资源租约保留，依赖 fallback 随 bundle 管理。没有克隆整个游戏菜单、复制动态字体或修改共享材质。前端 Show 窄补丁捕获资源，未就绪时每秒从实际 root 重试，就绪后停止发现。
- `HudEditorPanel/HudNativeUi/HudNumberRow` 提供独立 uGUI Canvas、私有 EventSystem、GraphicRaycaster、按钮、数值框和滑块。标题栏拖动限制在屏内，同进程重开保留位置。原生 T17/玩家输入及暂停仍复用已有占用；IMGUI 只保留 HUD 边框、手柄和已有拖动算法。
- 一次类别点击统一选中、高亮、临时预览和允许拖动；再次点击结束，换类恢复上一类显隐/显示进度。前八类保留旧隐藏配置，菜谱与容器两类复用原有八项显示参数，没有新增业务容量或配置键。
- 隐藏几何与原显示参数共享编辑草稿，滑块和合法输入只实时预览；保存一次提交真实 ConfigEntry，取消恢复。无效文本标红并阻止保存；反引号/波浪号不会写入数值框；写盘失败回滚内存设置，保留编辑会话。原9可见项、56隐藏项、12语言、GUID及版本均延续。

最终构建与本轮证据见 [hud-native-panel-verification.json](evidence/hud-native-panel-verification.json)。桌面 **1262** 项生产规则/配置、**622** 项实际提供端/编译消费者 metadata、**45+1** 项生产菜谱边界测试通过；静态 **27** 个补丁签名、**22** 个反射字段、**77** 处 public 游戏成员直接引用、非公开直访 **0**。

本轮正式 DLL **150,016 字节**、版本仍为 **0.1.0**，SHA256 `B66EABB8A93B8741BBAF51AAE76878B61FED71D806A3B8B615811684642B2048`。桌面、最终编辑器和最终菜谱实机加载的 hash 相同；构建不会自动安装。

真实 Unity 原生面板 `.validation/unity-recipes-20261118-014634-573` **116 项**通过，实际画面及 PNG **3840×2160**。真实 GraphicRaycaster 命中类别/保存按钮及标题栏，标题拖动 **(-120,+40) UI 单位**（向左240、向下80 px）；面板 **470×836 UI 单位**、缩放2、**940×1672 px**。计分板、倒计时的位移/边缘/角缩放及切菜同类偏移回归通过，具体数字见编辑器说明。已查看 [实际面板截图](../.validation/unity-recipes-20261118-014634-573/editor-native-panel.png)。这是实际运行时的脚本事件验收，尚未覆盖物理鼠标/手柄。

失败历史保留：`012854-942` 暴露首次原生资产捕获不就绪，已补受限重试；`013059-501` 的计时器测试点击被移动面板正确遮挡，调整测试前面板位置；`013357-216` 首帧 Canvas 尚未渲染，测试等待渲染后再射线。`013620-341` 106项、`014012-957` 115项为通过的中间阶段，最终116项另列，不覆盖旧结果。此前缺 TextRenderingModule 的测试工程编译失败也保留，没有部署该失败产物。

同hash菜谱回归 `.validation/unity-recipes-20261118-015240-024` **65项**通过，实际 **2560×1440**；双列/单列落位采样约 **0.195/0.202秒**。此前 `014909-245` 在47项后失败：唯一违规采样是 normalizedTime=1.000153 的原生clip完成帧，shader offset回卷至1920，而卡片已正确落位（误差约0.000031）。测试只对尚未完成的入场阶段检查横移前80%，保留全部原始采样及完成帧目的地断言；未修改生产菜谱动画来规避测试。

最终两轮自有进程均退出，每轮 **22个清单文件** 原样恢复，存档hash不变，临时测试DLL移除，恢复冲突0。恢复原件是上一轮 **126,464字节** 编辑器 DLL，SHA256 `2BAECB386F24B94423B2A6845A5DCC05FB9599033170237C78EF65ED2FD4E5FD`，不是历史75,776字节首版。最终生产HUD/测试插件错误0；外部街机随机选关仍以Error记录，其他已安装组件警告保留，不把整份日志称为无告警。

未验：全部12语言实际长文本/字形、物理输入、所有进度类完整真实业务、双peer/双队、生存独立计时、Horde。没有本轮 push、Release、MOD源修改或永久安装；需安装本轮 DLL 并重启，ModSettings ≥1.1.1。

## 历史：局内 HUD 编辑与隐藏元素预览（2026-11-18）

本节记录上一轮 IMGUI 面板；分离白名单/预览控件已被上节统一类别按钮替代。

- `src/Editing/` 按几何规则、隐藏配置、显示目标登记、暂停/输入占用、会话、浮窗和临时预览拆分。`Plugin` 复用原生配置、主线程 Update/LateUpdate 及 SDK；原 9 项可见设置和 GUID/版本保持。
- ModSettings ≥1.1.1 中总开关下一个 `ClosePage` 操作。限无远端玩家的普通厨房，关闭设置页后借用原生 Main/UI/Camera 暂停及输入 token，反引号保存退出。消费者窄补丁协调 ModSettings `NativeShortcut.Tick(Boolean)`，不修改提供端源码；异常、离场、总开关关闭、远端加入取消草稿并释放占用。
- 计分板/倒计时及切菜、烹饪、搅拌、炉温、洗碗、灭火按类别共用相对屏幕 XY、大小/宽高倍率和不透明度；主体拖动、四边单轴、四角等比。56 个隐藏 ConfigEntry 的 `Browsable=false` 经实际提供端 metadata 验证；参数草稿在保存退出时集中写盘。
- 浮窗右侧逐类“预览”勾选只显示已创建的原生隐藏显示对象，使用半满原生进度条；取消或退出恢复原显隐和显示值。未创建实例置灰。未改订单、食材处理进度、控制器完成状态，不为预览创建业务实体。
- 编辑器启动空引用已修正为只在本地用户处于 `InLevel` 时读取关卡；固定 HUD 在 Awake 后整个祖先树重挂时刷新 Canvas 缓存，按实际 `ScalingHUDCanvas` 层级筛选，避免把结算/前端同类型 UI 当局内对象。反射缓存、窄 owner 生命周期登记和既有属性所有权规则复用。

最终 DLL **126,464 字节**，`0.1.0`，SHA256 `2BAECB386F24B94423B2A6845A5DCC05FB9599033170237C78EF65ED2FD4E5FD`；编译、桌面和两轮最终实机均使用同一 hash。`scripts/Test.ps1` **1175** 项、`Test-ModSettings.ps1` **622** 项、`Test-RecipeBounds.ps1` **45+1** 项通过；静态核验 **26 个补丁签名、22 个反射字段、70 处 public 直接游戏引用、0 非公开直访**。退出输入 IL 仅一处 `Input.GetKeyDown(BackQuote)`。

真实 Unity 编辑器 run `.validation/unity-recipes-20261118-004024-605`：**82 项**通过，实际屏幕及截图 **3840×2160**。实际 ModSettings 页面按钮命中后关闭，原生计时跨真实时间保持；脚本事件经生产 OnGUI 验证计分板主体移动 (+307.2,-259.2) px、右边宽度 709.75→851.70 px、角缩放至 936.87×304.37 px；倒计时移动 (-268.8,-324) px，高度 222→266.4 px 且底边固定。两条原生切菜显示同时上移 216 px、后创建显示继承、opacity .45；预览从隐藏恢复为半满显示，取消/保存退出恢复隐藏和原进度。预览截图已查看：[隐藏元素预览](../.validation/unity-recipes-20261118-004024-605/editor-preview.png)。测量使用生产 TryBounds，配合截图确认，未将其描述为独立几何算法验证。

同一 DLL 的原菜谱实机回归 `.validation/unity-recipes-20261118-004531-162`：**65 项**通过，实际 **1920×1080**，包含分阶段入场、单/双列、0.5/1/2 倍、重建与恢复。新 cfg 已存在，未触发旧迁移分支，不能把本轮 65 项重述为早期包含迁移的 73 项。两轮最终测试均退出自有进程；原存档 hash 保持，22 个清单文件按各自存在性/hash 核验恢复，测试插件移除，冲突 **0**。生产 HUD 和测试插件错误 **0**；街机 MOD 仍以 Error 级记录随机选关，原生资源/外部 loader 警告归为安装环境，未声称整份日志无告警。

保留的失败历史：首次随机进 Horde 后未完成、一次 native anchor=.1 的错误测试假设、三次固定 HUD canvas 缓存/层级筛选失败、四次 IMGUI 脚本输入命中失败。后者修复测试在 `GUI.matrix` 改变后被原生 GUIClip 重算的指针坐标，不用修改生产控件或绕过原断言；首次 Horde run 有 HostUtilities cfg 恢复冲突，保留现场与备份，未擅自覆盖。各 run 与后续 PASS 分开记录，见 [核验摘要](evidence/hud-editor-verification.json)。

未验边界：物理输入、多语言全字形/长文本、六类全部真实业务生命周期、远端加入双端、生存独立计时和对战双队；Horde 不支持。单轴宽高会拉伸整组图像/文字，世界 Z 不作为本次屏幕编辑参数。详见编辑器说明。没有本轮 push、发布或 MOD 源修改；原本已安装的 **75,776 字节**旧 DLL 在隔离测试后恢复，使用本轮功能需要显式安装新构建并重启。

## 身份、构建与设置

- 独立插件 `com.ch3ngyz.plugin.overcooked2.hudcustomization`，程序集 `OC2HudCustomization.dll`，版本 `0.1.0`，目标 .NET Framework 3.5 / CLR 2，使用实际安装的原始游戏 DLL、BepInEx 5、HarmonyX。早期 GUID `com.oc2.hudcustomization` 仅用于配置迁移，历史证据保留原身份。
- `scripts/Build.ps1` 仅构建，可传 `-GameRoot` 和 `-MSBuild`；不自动复制 DLL 到游戏、不启动游戏。
- 设置全部通过插件原生 `Config.Bind` 创建，共 9 个 `ConfigEntry`，正常配置路径 `BepInEx/config/com.ch3ngyz.plugin.overcooked2.hudcustomization.cfg`。首次升级在新文件不存在时导入旧 GUID 的 cfg，已有新文件不覆盖。不依赖 HostUtilities、RecipeViewer 或 ModSettings DLL。
- 用户确认公共 ModSettings 可直接读取这些配置。本机 ModSettings `src/Core/ConfigCatalog.cs` 的 `Chainloader.PluginInfos → plugin.Instance.Config → ConfigDefinition/ConfigEntry` 路径也与此一致；这里只核对源码契约，未执行实际面板读取。
- `ConfigFile.SettingChanged` 只标记 dirty；主线程 Update 拍摄参数快照，LateUpdate 改 UI。ModSettings 的合法提交可触发此路径，不另建一份设置或持久化格式。不自动轮询磁盘 cfg，外部手工改文件需重启或由配置工具 Reload。

| Section.Key | 默认 | 范围与含义 |
| --- | --- | --- |
| General.Enabled | true | 关闭时恢复本插件仍拥有的属性 |
| Recipes.RecipeScale | 1 | 0.5–2；基于实例原始 RectTransformExtension.scale |
| Recipes.RecipesPerRow | 0 | 0–20；0 原版单行，正数为上限，宽度不足提前换行 |
| Recipes.RecipeRowSpacing | 8 | 0–100 Canvas 单位；只用于自定义多行 |
| Contents.ContentsScale | 1 | 0.5–2；空位和真实内容共用大小 |
| Contents.ContentsPerRow | 0 | 0–20；0 保留实例原网格，正数固定水平列数 |
| Opacity.RecipeOpacity | 100 | 0–100%，整张常规菜谱 |
| Opacity.EmptySlotOpacity | 100 | 0–100%，当前实例的空位 sprite |
| Opacity.IngredientOpacity | 100 | 0–100%，真实内容、烧焦及过搅拌图标 |

默认几何/alpha 参数保留原生效果，不把研究中观察到的 5 个订单或 2 列写成业务限制。无效非有限浮点值先经过 BepInEx 范围校验，快照再以该项默认值防御；不会直接送进 Unity 几何/alpha，也不另建代码写回 cfg。

## 运行职责

- `Plugin/ConfigMigration/HudSettings`：独立身份、旧 GUID 配置的首次导入、ConfigEntry、主线程应用及启停。
- `HudRuntime/HudPatches/GameAccess`：明确的 10 个窄挂点、12 个缓存字段、启动契约验证、按订单/内容物两组独立降级；不替换原生业务方法，不全局 Patch Graphic 或 RectTransformExtension。
- `Recipes/`：只登记实际 RecipeFlowGUI 列表拥有的卡片；读取原版排序后的活跃及退出卡片；使用当前生成节点的实际 Graphic 联合边界，按实际宽度排布，下一行依据上一行最大高度下移。主菜 Graphic 顶边作为运动锚点，完整卡片高度负责行间净空；两者不混作同一基准。没有 flow 归属的 RecipeViewer 克隆不进入适配。
- `Contents/`：只登记 ClientIngredientContentGUI 实际持有的 m_uiInstance；RefreshIcons 完成后从 m_iconsInUse 与 m_emptySprite 分类。本轮未使用图标维持原生隐藏，不改 sprite、容量、minimum size、跟随目标、UI_Move 材质或网络。
- `ContentsGeometry`：从原父区域、cellSize、spacing 计算单图标尺寸；固定列数时横向扩宽、纵向按实际行数收缩，再反算归一化 cellSize。保持图标大小与列数独立。列数为 0 时保留实例原 constraint/startAxis 等，只按需缩放区域。
- `Presentation/`：组件专属 CanvasGroup alpha 倍率保留原生 Image.color/动画；不修改已有外来 CanvasGroup。属性恢复先比较最后写值，发现其他持有者则保留并去重警告；只删除自己创建且仍拥有的 alpha 组件。
- 原生 RefreshSubElements 会重置 GeneratedChildren 几何，因此在它前面先撤回自己的几何，RefreshIcons 后重新应用；这是附加的窄前置清理，原方法仍完整运行。对象复用、刷新、停用、销毁、总开关关闭均有清理路径。
- 每帧只处理已登记的少量实例和已存在 tile；首次启动只受限发现一次。反射成员不重复查找，无逐帧全场扫描或日志。

## 当前证据

运行命令：

```powershell
.\scripts\Build.ps1
.\scripts\Test.ps1
.\scripts\Verify-Contracts.ps1
```

- 构建使用真实安装引用，WarningsAsErrors；版本、依赖、输出 SHA256、成员访问与目标签名见 [implementation-verification.json](evidence/implementation-verification.json)。输出目录只包含本插件 DLL 和调试 PDB，不复制任何原版/第三方 DLL。
- 桌面测试直接编译生产 `HudLayoutMath`、`OwnedValue`、`HudSettings` 源码，使用真实 BepInEx ConfigFile；覆盖默认值、盒装外部写入事件、独立设置、范围、cfg 保存/重读、非有限数值、宽度/列数换行、混合行高、0/1/多图标、重复应用不累乘、重新捕获与恢复所有权。**不运行 Unity ECall，不构成真实 UI 或 Harmony 执行证据。**
- 验证脚本用实际安装 DLL 的 Cecil 元数据核验 10 个目标签名、12 个字段；扫描编译 IL 中直接游戏成员访问均为 public，且无 HostUtilities/RecipeViewer/ModSettings/测试依赖。
- 初版本地结果（下节多语言改动前）：生产构建成功、DLL 34,816 字节；`scripts/Test.ps1` **35 项断言通过**，原始结果 `.validation/logic-20261117-135007-716/result.txt`。元数据/IL：**10 个补丁签名、11 个字段、21 处直接 public 游戏成员引用、0 处非公开直访**。3 个 PowerShell 脚本语法检查和本地文档链接检查通过；当时最大生产源码文件 149 行。当前产物摘要以实施核验 JSON 为准。
- 测试脚本初次运行出现两项桌面 harness 问题：csc 默认 rsp 与 Mono 引用重复；ConfigFile 初始化缺少链加载器通常注入的路径。分别以 `/noconfig` 和仅指向测试目录的 BepInEx Paths 初始化修正，未修改游戏或生产逻辑绕过失败。失败目录保留在 `.validation/`，不提交临时文件。
- 后续非有限数值测试的最初断言预期“必然回退默认值”，实际 BepInEx 的范围校验先把输入转换为合法边界，故原断言失败；保留该轮失败证据，改为验证最终进入 Unity 的值有限且在范围内，再运行原测试。此项不构成游戏中 NaN 行为证据。

## 配置多语言与 ModSettings 刷新（2026-11-17）

- `src/Localization/SettingsText.cs` 集中维护 22 个消息（4 个分区、9 个名称、9 个说明），同一消息的 12 种游戏语言放在一起：英语、法语、意大利语、德语、西班牙语、俄语、巴西葡萄牙语、波兰语、简体中文、日语、韩语、繁体中文；未知枚举值回退英语。无额外语言选择配置。
- `HudSettings.Display.cs` 为原有 9 个条目绑定可变 `ModSettingsAttributes`，启动时填入英文；切换时原地更新同一个 tag 的 `DispName/Description/Category`。cfg 仍使用原 Section/Key、类型、默认值、范围和路径；`ConfigDescription` 中的磁盘说明保留英文，刷新不重新 Bind、不 Reload、不 Save、不触发值变更事件。分区优先级 400/300/200/100 固定，分区内 Order 固定，翻译不改变原功能顺序。
- `GameLanguage.Read` 在 SteamPlayerManager 未就绪时返回未知，保留英文，避免提前采用游戏的 OS fallback；就绪后读取实际游戏 `Localization.GetLanguage()`。`Plugin.Update` 在 Unity 主线程每秒轮询一次，支持语言晚就绪与后续变化；临时读取失败保留上次有效显示。
- `SettingsLanguageSync` 在更新整批 tag 后只发一次刷新，语言不变不重建设置页；ModSettings 缺失/未就绪时保留待通知状态，稍后重试。SDK 抛错由现有去重日志记录完整异常，不中断 HUD；插件重新启用会再通知一次。
- `src/ModSettingsSdk/` 复制用户指定的两个 SDK 源文件，与当时提供端原文件 SHA256 一致；编进本 DLL，通过 `ModSettingsClient.Connect/Refresh` 调用选中的服务。只声明服务 GUID 的软依赖，不引用服务 DLL 或 Configuration Manager，不修改提供端源码。显示元数据要求 ModSettings **1.1.0+**；旧服务不理解新 tag 时可能仍显示原始英文 key。按 SDK 当前契约，显式刷新会取消打开页中未确认的编辑/拖动；只在初次连接、语言变化和重新启用时请求。

验证命令：`scripts/Test.ps1`、`scripts/Test-ModSettings.ps1`、`scripts/Verify-Contracts.ps1`；本机可显式传 `-MSBuild 'D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'`。

- 真实安装引用的 Release 构建、C# 3、WarningsAsErrors 通过；产物 **69,120 字节**，仍为 `0.1.0`，未新增配置项。
- 桌面生产源码/真实 ConfigFile 验证 **694 项断言通过**，包括 22×12 非空文案、实际游戏枚举映射、9×12 tag 更新、同语言无操作、值/键/磁盘保持、服务晚就绪与失败重试等；保留原 35 项布局/所有权/配置回归。结果 `.validation/logic-20261117-155121-089/result.txt`。模拟就绪/错误使用注入回调，不代表真正 Steam 或 Chainloader 运行。
- 独立加载**编译后的本 MOD** 与相邻项目的 **ModSettings 提供端 DLL**，经真实 `EntryMetadata.Read`/`CategorySort.Sort` 验证 **553 项断言通过**：9 项×12 语言的显示名称/说明/分区、字段跨程序集识别、优先级和顺序、可见与可编辑、中文说明搜索、历史快照与 cfg 保持。结果 `.validation/modsettings-20261117-155410-780/result.txt`；摘要及双方 DLL hash 见 [localization-verification.json](evidence/localization-verification.json)。测试通过反射调用实际内部方法，不执行 Unity 内部调用，也不伪造面板或服务实例。
- 实际游戏元数据/本插件 IL 核验：10 个补丁签名、11 个反射字段、**23 处 public 游戏成员直接引用，0 处非公开直访**；另外确认语言就绪/读取公开方法、游戏语言 Steam 来源、ModSettings 软依赖和编译后的 SDK Connect→Refresh 路径。见 [implementation-verification.json](evidence/implementation-verification.json)。
- 当前目录仍未初始化 Git；没有执行建仓、提交、推送、发布、安装或游戏进程操作。桌面读取与编译证据不代替真实 ModSettings 刷新和 Unity 字形/布局验收。
- 实机准备止于只读检查：发现未带本次测试标记的游戏进程 PID 73192（2026-11-17 16:01:57 启动），遵守现有玩家进程保护约定，没有部署测试文件、启动新实例或接管/结束该进程。故本轮没有真实面板截图、文字边界或刷新延迟测量，不能宣称视觉验收完成。

实机仍需验证：游戏启动前英文→语言就绪后刷新、打开面板切换语言、名称/悬停说明/搜索/分区的实际字体与遮挡、长语言文案溢出、旧 cfg 加载与保存、服务缺失/稍后就绪；记录截图、分辨率及行/文本边界。需要同时安装本次 HUD DLL 和支持新 tag 的 ModSettings 并重启加载，安装和实机操作仍待本项目单独授权。

## 多行菜谱遮住炊具图标修复（2026-11-17）

用户截图呈现两列三行，上方两行炊具被下一行纸面盖住。原版 `RecipeWidgetTile.OnRefreshSubObjectProperties` 将 `BackgroundTop` 固定在 tile 顶部，设置负 Y 缩放并使用像素尺寸 `m_BackgroundStitchSize`（类默认高度 53）。原先 `RecipeCardState.Measure` 只联合 tile 根矩形，漏掉这段向上伸出的纸面；`HudRowLayout` 已使用每行最大高度，错误来自输入边界不完整。已核对真实安装 DLL 的该方法 IL，未用 `GetBounds()` 的单 tile rect 替代完整测量，也未硬编码增加 53 行距。

- `GameAccess/RecipeCardState` 复用缓存反射取得每个 tile 当前 `m_container`，只在创建/重建时缓存该容器内的 RectTransform，排除等待延迟 Destroy 的旧生成容器。布局时按当前激活对象的实际角点实时求 flow 局部联合边界，不逐帧重扫层级或重新创建反射信息。
- `RecipeAdapter.Refresh` 在整张菜谱原生刷新后标记边界缓存失效，包括总开关关闭期间发生的重建。原有每行最大高度、Canvas 行间距、排序、恢复和原生动画链继续使用。
- 桌面回归 `scripts/Test-RecipeBounds.ps1` 直接执行编译后生产 `Measure → HudRowLayout.Place`；仅在临时 DLL 副本替换 Unity 原生对象/坐标调用。旧版在 0.5 倍测试几何中测得 `[-90,0]`，下一行纸面侵入 45 单位；修复后测得 `[-90,53]`，实际净空 8 单位。0.5/1/2 倍、非激活对象过滤、几何变化和刷新失效共 **24 项断言通过**。红色证据 `.validation/recipe-bounds-20261117-163132-957`，绿色 `.validation/recipe-bounds-20261117-163413-186`。最初 harness 打印 Rect 时缺 SharedInternalsModule，改成直接打印浮点字段；保留 `.validation/recipe-bounds-20261117-163053-138`，不将其归为生产缺陷。
- `scripts/Test.ps1` **694 项**、`scripts/Test-ModSettings.ps1` **553 项**通过；`scripts/Verify-Contracts.ps1` 核验 **10 个补丁签名、11 个唯一字段、23 处 public 游戏成员引用、0 处非公开直访**。当前正式 DLL **69,632 字节**，SHA256 `165D3F838A517D7BEB3AF72C855FD3608DAAD418FDFE86FF0E7A97533EA108C6`，版本仍为 `0.1.0`。

真实 Unity 命令：`scripts/Test-UnityRecipes.ps1 -KeepProductionBuild`。独立测试插件使用当前正式 DLL，经原生登录/加载 profile → 本地街机 → 厨房；在原生 flow 同父区域建立仅显示用的测试 flow，调用实际 `AddElement → RefreshSubElements → LayoutWidgets` 生成 5 张原生 `ChickenBurger` 菜谱，不改变真实订单业务。用独立 Graphic 遍历测量，未复用生产边界缓存作为断言依据。

- 首轮 `.validation/unity-recipes-20261117-164102-600` 的 42 项初步断言通过，但重建截图仍在 Animator 出现动画中，且测试停用原 flow 引起 2 条 inactive coroutine 报错。这是测试自身问题：改为只隐藏原 flow、保持其业务运行；测量前加入最少动画等待和 **连续 20 个渲染帧的 Graphic 边界与 UI_Move.Offset 稳定检查**。保留首轮截图/日志，不作为稳定画面验收结果。
- 最终 `.validation/unity-recipes-20261117-164509-485` **25 项真实 Unity 断言通过**；屏幕和原始 PNG 均为 **3840×2160**，实际厨房为 `s_resort_2_3`（安装的街机随机选关决定）。0.5 倍每行 2 张时完整卡片高度 **154 Canvas 单位**；首行底边 **444**、次行顶边 **423.229**，净空 **20.771**，第三行相同。重建前后完整边界完全一致；1 倍高度 **250**、2 倍 **442**，均满足行间净空，关闭插件恢复原版单行。2 倍三行会超出屏幕底部，当前策略保持用户选定大小，不构成屏内自动适配保证。
- 已查看 0.5 倍和重建后的原始截图，炊具完整显示、无跨行覆盖：[0.5 倍](../.validation/unity-recipes-20261117-164509-485/scale-05.png)、[重建后](../.validation/unity-recipes-20261117-164509-485/rebuilt-scale-05.png)。几何、原始日志、实际加载 hash、断言和恢复记录在同一目录，摘要见 [recipe-layout-verification.json](evidence/recipe-layout-verification.json)。这是厨房内原生 UI fixture 的脚本验证，不代表完成全部真实订单/动画/多人流程。
- 最终日志中 HUD 错误/警告 **0**；其他已安装 MOD 的 `OC2DIYChef.ChefGridToggle.Refresh` 空引用、HostUtilities `LobbyLevelRandomizer` 以 Error 级别记录随机选关、Harmony 缺少可选成员、DIYLevel loader 反射/卡顿提示，以及原生资源缺失脚本/退出时 render texture 提示单独归为安装环境日志，未把整份日志描述为无错误。最终轮没有首轮由 fixture 导致的 inactive coroutine 报错。
- 两轮仅启动/退出带本轮命令行路径的测试进程；原 Meta 存档 hash 均保持 `B986D06F88D0CF1D70D9B00A7B002FBEF8484104F20DA16CF0DD099C27E1AD4A`。当前所有 cfg 和共享日志恢复原 hash，测试 DLL/测试 cfg 已移除；保留修复后的正式 DLL 于 `BepInEx/plugins/OC2HudCustomization.dll`，旧版备份在首轮 `backup/` 目录，恢复冲突 **0**。本次测试开始前用户已在游戏中改过 HUD cfg，故以本轮起始 hash `BA079D892114A4E32342A0DA27DA55DA656D98A837BD8C858630998E5DE376D7` 为恢复基准，不回滚至最早部署时的配置。

## 去除主菜卡空白纸面与分阶段入场（2026-11-17）

- `RecipeCardState` 仅对 `TopRecipeWidgetTile.m_backgroundTop.gameObject` 撤去原本伸出屏幕顶部的空白纸面。保留原进度条、主菜图、普通配料分支纸面和炊具；使用现有 `OwnedValue` 恢复可见性，重建时重新缓存，不反向启用暂停时被原版关闭的 Image。
- 测量改为当前 GeneratedChildren 的实际启用 Graphic，排除没有绘制内容的布局 RectTransform 和待 Destroy 的旧容器；仍实时读取角点，以完整高度决定行高。运动锚点统一为主菜 Graphic 顶边，避免配料展开影响整张卡的定位。没有逐帧层级扫描、反射查找或生产日志。
- 新增 `RecipeMotion` 管理新卡入场和既有卡回流：先在第一行接受原生 `UI_Move` 从右向左滑入，横向剩余距离不高于 20% 时，在原生动画进度的 0.14 区间内快速落位（当前 1.5 秒 clip 对应 0.21 秒），最迟在 0.78 前到位。0.78–0.85 之间才淡入配料与烹饪步骤，接原生主要展开。临时 alpha 使用现有 `AlphaLayer`，不复制或改写 Animator、bundle、业务订单或配方。
- 读取真实 `Override.RecipeTransitionIn` 所在层，不使用始终 Idle 的 Base 层；必须看到非零 shader 偏移后才判断横移接近完成，防止 Animator 尚未求值时初始零偏移提前放行。原生 normalizedTime 驱动落位，暂停不会因墙上时钟继续下落；长帧或原生结束时直接归位，关闭/复用不重播入场。已有卡片回流保留临界阻尼平滑移动。
- 节奏参数源于本机真实 `bundle0` 的 clip：横移约在 normalized 5/9 结束，主要分支展开约从 0.85556 开始；原生资源没有修改。此前“展开同时落位”方案已被用户最新要求替代。

最终验证：

- `scripts/Test.ps1` **826** 项生产规则/配置断言；`scripts/Test-RecipeBounds.ps1` **45** 项生产边界替换断言；加 `-Entrance` **1** 项；`scripts/Test-ModSettings.ps1` **554** 项实际提供端/编译 HUD 元数据断言通过。分别见 `.validation/logic-20261117-183412-520`、`recipe-bounds-20261117-183413-530`、`recipe-bounds-20261117-183414-288`、`modsettings-20261117-183415-096`。桌面边界测试替换 Unity ECall，不是渲染证据。
- `scripts/Verify-Contracts.ps1` 通过 **10** 个补丁签名、**12** 个反射字段、**25** 处 public 游戏成员直接引用，非公开直访 **0**。Release/C# 3/.NET 3.5/WarningsAsErrors 构建通过。
- `scripts/Test-UnityRecipes.ps1` 最终轮 `.validation/unity-recipes-20261117-183226-359` **62 项真实 Unity 断言通过**，随后 owned 进程退出且恢复核对通过：5/6 张原生 ChickenBurger 显示 fixture、1/2 列、0.5/1/2 倍、首帧位于第一行、横移前 80% 不下落、快速落位前配料隐藏、落位后再显示、原生展开完成时不回弹、原生重建、切换列数及关闭恢复。静止几何连续 **20 个渲染帧**稳定。
- 实际屏幕 **3840×2160**；0.5 倍最终卡高 **106 Canvas 单位**（去除头部空白纸面前为 154），第一行底边 444、次行顶边 423.229，净空 **20.771**。两列入场采样落位约 **0.194 秒**、单列约 **0.196 秒**；原生展开完成帧主菜顶边误差分别为 **0** 和约 **0.000031** Canvas 单位。时间来自带截图 I/O 的真实帧采样，不作为无开销帧率或性能基准。
- 已查看真实 [落位途中画面](../.validation/unity-recipes-20261117-182903-315/entrance-descending.png) 与 [单列落位画面](../.validation/unity-recipes-20261117-183226-359/single-column.png)：途中主菜/时间条可见、食材步骤未提前露出；稳定画面无额外头部纸边，炊具完整。每卡均只有 **1 个 active 进度条**，关闭后原纸面恢复。
- 用户贴出的 `custom layout hides only the upper paper...` 失败来自测试按全 widget 名称找纸面，误命中普通配料分支。断言已改为先定位 `TopRecipeWidgetTile`，保留原失败日志；未通过删断言或隐藏所有 BackgroundTop 规避。后续 `.validation/unity-recipes-20261117-182552-400` 揭露主菜和整体锚点差 5 单位，统一 Adapter/CardState 的主菜基准后，`182903-315` 的 37 项及上述最终 62 项通过；失败证据全部保留。
- 本轮 HUD 警告/错误 **0**、fixture inactive coroutine 错误 **0**，不把其他 MOD/游戏资源日志描述成整份无错。原生存档、全部配置及共享日志按本轮起始 hash 恢复，测试插件移除，恢复冲突 **0**；本轮 HUD cfg hash 为 `5870E6F59E058CECB43BCF2F139CC516A65974616D01788986DF7DE50B025FB0`，未回滚用户其后自行调整的设置。
- 实机 runner 默认恢复了测试前 DLL；随后在确认游戏未运行、32 个 DLL 中只有一份本 GUID 后，单独覆盖正式插件。构建 = 实机加载 = 安装 SHA256 `8F3ACB0BD77E52FEA605778F821FBC1D91E413E92DDEE69CD47D70CC8A099B48`，**74,240 字节**、版本仍为 **0.1.0**。部署及旧 DLL 备份见 `.validation/deploy-20261117-183705-124/deployment.json`。目录未初始化 Git，未推送或发布。
- 安装完成于 18:37:05；最终整理记录时观察到玩家自行于 18:40:50 启动的非测试进程。未接管或结束该进程，也未再覆盖 DLL、配置、日志或存档。上文恢复核对结论具有测试退出时的时间边界，后续玩家新写入的存档/配置不作为恢复冲突，更不回滚。

## 确认作者命名的 GUID 与配置迁移（2026-11-17）

- 用户明确确认完整 GUID `com.ch3ngyz.plugin.overcooked2.hudcustomization`。`PluginInfo.Guid` 改为该值，双语标题仍为 `OC2 HUD 自定义 / OC2 HUD Customization`，版本仍为 0.1.0。Harmony ID 从该常量派生；Unity 测试插件的 GUID、依赖、Chainloader 查找及 ModSettings 消费者测试 Owner 一并更新。静态核验固定检查新身份，旧 GUID 只留在迁移及“旧身份未加载”的断言中。
- 当前安装 BepInEx IL 确认：`BaseUnityPlugin` 构造新的 ConfigFile 时使用 `saveOnInit=false`，未 Bind 时不会创建缺失的文件。因此 `Awake` 在创建 `HudSettings` 前执行 `ConfigMigration.ImportLegacy(Config)`：新 cfg 不存在且旧 cfg 存在时，先复制到同目录唯一临时文件，再用不覆盖的 File.Move 发布，Reload 同一个原生 ConfigFile；旧文件保持不动。已有新 cfg 优先，重复启动不再导入；迁移失败记录完整异常，不中断其余 HUD 初始化。并发目标创建不覆盖，临时文件只清理本次拥有的路径。
- `scripts/Test.ps1` **841 项**通过，较此前增加 **15 项** GUID/迁移断言，直接运行生产迁移实现和真实 BepInEx ConfigFile：无旧配置、新配置优先、原文/未知键保留、9 项值加载、后续修改/重新加载、目标被目录阻挡时保留原文件与清理临时文件。证据 `.validation/logic-20261117-191545-824`。`scripts/Test-ModSettings.ps1` **554 项**通过，证据 `.validation/modsettings-20261117-191547-009`；静态仍为 **10 个补丁、12 个反射字段、25 处 public 直接引用、0 处非公开直访**。更新后的 Unity 测试插件编译通过。
- `scripts/Test-UnityRecipes.ps1` 最终轮 `.validation/unity-recipes-20261117-191735-939` **73 项真实 Unity 断言通过**。在原 62 项菜谱回归基础上，增加新身份已加载且旧身份未加载、原生 ConfigFile 路径对应新 GUID、首次启动导入的 **9 项设置逐项等于旧 cfg**。日志实际记录导入成功，游戏屏幕 **3840×2160**；单/双列落位采样约 **0.202/0.195 秒**。owned 游戏退出后原配置、共享日志和存档核对恢复成功，临时新 cfg、测试插件均已移除，恢复冲突 **0**。
- 19:20:11 游戏未运行时单独部署正式 DLL，同时将用户当前旧 cfg 原样复制到新路径，旧文件保留；两者 SHA256 均为 `C74234589BC5CA4914FC319D9A806B9A2118E24DA577303E1670817E623EB85B`，不采用测试中临时设置。32 个 plugins DLL 中只有一份旧/新 HUD 身份候选，原路径覆盖后实际为新 GUID。构建 = 实机加载 = 安装 SHA256 `ECC1D3CEAC22000C0BEE2F1A544DAE7A07A8957318E598FBE24A14F118AB8FF5`，**75,776 字节**。备份及部署记录 `.validation/deploy-guid-20261117-192011-268/deployment.json`；机器摘要见 [guid-migration-verification.json](evidence/guid-migration-verification.json)。
- 本轮发现目录已被外部初始化为 Git 仓库，但 main 无提交，已有全部源码、脚本和 docs 均为未跟踪文件。无法在不包含此前工作的情况下把本轮文件修改分离成有效基线，因此未混合创建首个提交；未执行远程操作。实际 ModSettings 视觉刷新、旧 GUID 的设置页收藏迁移及多人仍不在本轮验收范围，未修改提供端配置或源码。

## 必须继续做的实机验收

已完成上节范围内的临时测试部署、隔离存档和多行布局验收；下列其余行为仍未全部覆盖，不得把局部验证扩大为完整视觉验收。

1. 使用本项目单独的临时安装/恢复清单，拒绝接管玩家正在运行的游戏；在登录前隔离原生及已安装 MOD 的存档路径，退出后核对原存档 hash。不要直接执行其他仓库会部署其他 MOD 的完整 runner。
2. 真实厨房记录基线与自定义参数的原始截图及屏幕分辨率、Canvas 区域、完整卡片/每个图标边界、列数/行高；重点验证 RTE.scale 对像素 inset 的效果，锅/碗/盘预制体的 pivot、生长方向和遮挡。
3. 菜谱入场已覆盖上述单一配方 fixture；其余混合配方及真实订单的淡出/失败/过期/暂停仍待验。空→加料→满→清空→烧焦/过搅拌；移动、传送、重开和切关；0/50/100% alpha；同帧关闭再开启、原生图标复用和恢复仍需各自实机验收。
4. 已有 CanvasGroup 会保守跳过该对象的不透明度调整；ignoreParentGroups 子组保持原边界，记录一次提示。此边界和游戏 UI_Move shader、mask 是否兼容需要真实画面判断。
5. ModSettings 面板实际发现与改值；RecipeViewer 窗口/拖动/导出；其他 HUD MOD 同写时的实际行为。属性所有权检查是保守恢复机制，不是已验证的全 MOD 冲突仲裁。
6. 真实第二 Steam peer 分别验证只装主机、只装客机、双方不同参数；当前没有多人证据。Horde 使用独立链，首版不适配。

运行需要游戏已有 BepInEx 5 / HarmonyX；本机已安装新 GUID 修复 DLL，下一次正常启动即可加载。目录当前有未建立首个提交的 Git 仓库；本轮未提交、推送或发布。
