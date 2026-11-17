# 独立局内 HUD 自定义 MOD 可行性研究

研究日期：2026-11-17。状态：静态研究完成，尚未实现；研究阶段未创建 C# 工程、修改业务、编译、部署或启动游戏。

本报告与核验资料现已按用户要求迁入 `E:/code/OC2/OtherMOD/Overcooked-2-HudCustomization`，作为独立 MOD 的开发资料；当前进度见项目根 GOAL.md。

## 1. 目标与证据边界

目标是后续新建一个独立 BepInEx MOD，供本地玩家调整：①顶部订单菜谱卡片大小及每行个数；②容器上方内容物/空位加号图标大小及每行个数；③菜谱、加号、已加入食材三类 UI 各自的不透明度。每行个数只控制展示排布，不改变订单上限、容器容量、菜谱配方、计时或联机消息。

本报告以本地原版游戏反编译源码为第一手行为证据，路径根记作 `GAME = E:/code/OC2/Assembly-CSharp/Windows/Assembly-CSharp`。`RecipeViewer = E:/code/OC2/DLL/Decompiled/Overcooked2RecipeViewer` 仅作既有展示实现参考，不把第三方 MOD 的做法视为原版契约。

主调查已核对实际安装游戏 `E:/SteamLibrary/steamapps/common/Overcooked! 2/Overcooked2_Data/Managed/Assembly-CSharp.dll` 与 `E:/code/OC2/Assembly-CSharp/Windows/Assembly-CSharp.dll` 的 SHA256 相同：`9BB6A3791331201D32CA89C3509F019A9780309DA7110002F04020E8491E1908`。Cecil 元数据为 AssemblyVersion `0.0.0.0`、MVID `114d606a-29d5-472c-8ebe-c2c20fbe3be3`。因此以下源码可与当前安装 Windows DLL 对应；这不代表跨平台、其他版本或实际运行结果已经验收。

结论标签：**事实**来自所列原版源码/原始 DLL 元数据；**建议**为后续实现方案；**待验**为源码不足以证明的界面、资源或联机表现。默认值和数值区间如标为建议，尚未成为产品决定。

## 2. 结论摘要

- **可行（静态结论）**：三类目标已有明确本地 UI 控制器和布局入口，无需修改原配方、订单生成或容器容量。菜谱多行布局比容器网格调整复杂，需要与原版逐帧布局配合。〔`GAME/RecipeFlowGUI.cs:9-18,186-230`；`GAME/IngredientContentsUIContainer.cs:67-79,99-174`〕
- **建议作为新独立 MOD 实现**：复用原版 UI 和真实状态，仅在最后展示环节应用本地参数；不把功能塞进 HostUtilities，不复制原版业务控制器，也不依赖 RecipeViewer。此为本任务明确的交付方向，本轮只形成研究文档。
- **不能直接写一次 `transform.localScale`**：原版 `RectTransformExtension.LateUpdate → UpdateAspectRatio` 每帧把 `localScale` 写回 1。应优先评估公开 `scale` 属性并验证像素偏移与锚点缩放的实际效果。〔`GAME/RectTransformExtension.cs:99-109,128-177`〕
- **“原来每行两个”有源码依据，但不是统一容量限制**：运行时创建的容器网格单元为父区域的一半；默认 Flexible 排布可因此形成两列。预制体已存在的 `GeneratedChildren` 会被复用，不能断言所有容器预制体都由这段默认值生成。〔`GAME/IngredientContentsUIContainer.cs:11-53,67-79`；`GAME/UnityEngine/UI/AnchorGridLayoutGroup.cs:175-208`〕
- **三类不透明度要分别归属**：菜谱整体一个倍率；容器内空位加号与真实内容各自一个倍率，并随每次内容变化重新分类。共同父节点单一 alpha 无法表达后两者独立配置。〔`GAME/IngredientContentsUIContainer.cs:99-174`〕

## 3. 已确认的入口

| 目标 | 原版责任入口 | 已确认事实 | 实施含义 |
| --- | --- | --- | --- |
| 订单卡片 | `GAME/RecipeFlowGUI.cs:9-18,115-130,170-183` | 添加卡片、维护当前/退出动画列表、逐帧布局均由本地 UI 控制器处理。 | 应从原生卡片排布挂点调整展示，不替换配方或订单逻辑。 |
| 内容物与加号 | `GAME/IngredientContentsUIContainer.cs:99-141,145-174` | 食材与空位复用 `m_icons`；空位由 `m_emptySprite` 补到 `m_minimumSize`，未使用图标被隐藏。 | 必须在同一图标容器内区分空位与真实内容，不能用两个互不相干的全局图像扫描器。 |
| 容器生命周期 | `GAME/ClientIngredientContentGUI.cs:24-61,65-103` | 内容变化时懒创建/重启显示，OnEnable 重放状态，OnDisable 隐藏，OnDestroy 销毁实例。 | 一次入场修改不足，需在创建/刷新和重新激活后重应用，销毁时释放缓存。 |

## 4. 菜谱卡片大小和每行个数

### 4.1 原版调用链与范围

**事实**：服务器事件在客户端 `ClientKitchenFlowControllerBase.ApplyServerEvent` 分派；`ClientOrderControllerBase.AddNewOrder` 交给 `RecipeFlowGUI.AddElement` 生成显示卡片。卡片初次 Setup 只启动协程，实际子图像在 `WaitForEndOfFrame` 后建立。〔`GAME/ClientKitchenFlowControllerBase.cs:27-64`；`GAME/ClientOrderControllerBase.cs:45-54`；`GAME/RecipeFlowGUI.cs:9-18`；`GAME/RecipeWidgetUIController.cs:87-103`〕

**事实**：`RecipeFlowGUI.Update` 先销毁已播完退出动画的卡片，再调用私有 `LayoutWidgets`。`LayoutWidgets` 先把活跃和退出中的卡片按原订单顺序合并，再逐一读取 `GetBounds().width`，给 `RectTransformExtension.AnchorOffset` 写 0、给 `PixelOffset` 写 `(横向累积值,0)`，原版只有一行。〔`GAME/RecipeFlowGUI.cs:170-230`〕

**建议的首期范围**：常规局内顶部订单栏。Horde 有独立 `GameModes.Horde.HordeOrderUIController`，它自己的 Update 会给 `PixelOffset` 叠加动画位移、给 `scale` 赋动画倍率，不能直接套用常规订单布局并声称覆盖所有模式。若要包含 Horde，应明确新增模式适配和验收。〔`GAME/GameModes/Horde/HordeOrderUIController.cs:14-22,32-78`〕

### 4.2 建议的窄 Harmony 挂点

| 挂点 | 类型与用途 | 边界 |
| --- | --- | --- |
| `RecipeFlowGUI.AddElement` | Postfix，登记当前 flow 实际拥有的卡片，建立配置/恢复状态。 | 不更改 `_data`、`_timeLimit`、返回 token、桌号或 `m_maxOrdersAllowed`；此时尚无完整子图像。〔`GAME/RecipeFlowGUI.cs:9-18`〕 |
| `RecipeWidgetUIController.OnRefreshSubObjectProperties` | Postfix，仅对已登记局内卡片，在原生生成子节点后应用不透明度和刷新边界缓存。 | 必须尊重下一帧生成/重建，不按方法名把所有 RecipeWidget 全局当作局内订单。〔`GAME/RecipeWidgetUIController.cs:87-103,349-352`〕 |
| `RecipeFlowGUI.LayoutWidgets` | **优先 Postfix**：保留原版收集与排序，在其完成后基于同一有序列表改写换行坐标。 | 不跳过整个 `Update`；兼容失败时保留原版一行。布局方法本身每帧调用，热路径只读已缓存访问器与实例状态。〔`GAME/RecipeFlowGUI.cs:170-230`〕 |
| 卡片/flow 销毁与插件停用 | 组件生命周期清理，恢复仍存活对象并释放引用。 | 原版退出动画中的卡片也须保留设置直至真实销毁。〔`GAME/RecipeFlowGUI.cs:115-130,170-181`〕 |

这里只建议改写 `LayoutWidgets` 输出，尚未证明 Postfix 对所有 Animator 资源都足够。除非实际证据表明需要替换该纯布局方法，否则不以 Prefix `return false` 替代整个更新链，也不做全局 `RectTransformExtension`/`Graphic` Patch。

### 4.3 排布模型与大小

**建议**：卡片缩放以每个实例最初的 `RectTransformExtension.scale` 为基准计算，不使用“当前值再乘一次”。每行个数为上限，按原始有序列表从左至右累积实际宽度；新行向屏幕内部展开，行高使用该行所有卡片联合视觉边界的最大高度，加独立的行间距。以完整卡片边界（所有 tile，而非单个小图标）测量，避免宽配方横向重叠和多层配方纵向重叠。退出动画的卡片继续参与原版有序位置，待原版移除后再回流。〔设计依据：`GAME/RecipeFlowGUI.cs:186-230`；`GAME/RecipeWidgetUIController.cs:107-173`〕

**事实/待验**：`GetBounds` 只返回排序后最后一个 tile 的 `RectTransform.rect`，适合原版横向排布但不能直接等同整张菜谱的世界边界。`RectTransformExtension.scale` 作用于 fixed-ratio anchors，而 `m_pixelOffset` 被独立写入 offset；只设置该属性不必然让像素 inset、背景拼接和间距严格等比例缩放。应比较所有可见 tile 转入同一个 flow 局部坐标后的联合边界，并在 Canvas 布局完成后测量，验证过场动画时的稳定性。〔`GAME/RecipeWidgetUIController.cs:59-74,107-173`；`GAME/RectTransformExtension.cs:134-177`；`GAME/RecipeWidgetTile.cs:48-107`〕

**建议的溢出策略**：大小和列数各自保持用户意图；当当前行实际宽度超出安全区域时提前换行，并记录一次提示/诊断，不静默缩到另一个大小。这样“每行 N 个”是上限，极宽配方可能少于 N 个；若产品要求严格 N 个，则必须明确选择裁切、缩小或超出安全区，不能同时承诺三者都不发生。安全区应取当前 UI 父区域，不硬编码 1920×1080。此为建议，尚待用户在实施前确认体验。

### 4.4 原生动画风险

1. `RecipeWidgetUIController.Update` 在动画期间逐个重写 `Image.color`；成功动画把 alpha 插值到 0。直接在创建时给所有图片写固定 alpha 会被原版覆盖，逐帧强写固定 alpha 又会破坏成功淡出。〔`GAME/RecipeWidgetUIController.cs:177-208`；`GAME/RecipeSuccessAnimation.cs:25-30`〕
2. 卡片容器带原生 Animator，其控制器来自资源，不只由这些 C# 文件描述。普通卡片 Update 此版本只直接消费颜色修改器；不能因为 `RecipeAppearAnimation.GetScaleModifier` 存在，就断言普通顶部卡片每帧由该 C# 方法缩放。Horde 则明确消费 scale/position 修改器，应分开判断。〔`GAME/RecipeWidgetUIController.cs:107-110,177-208`；`GAME/RecipeAppearAnimation.cs:25-30`；`GAME/GameModes/Horde/HordeOrderUIController.cs:32-74`〕
3. 原版暂停菜单通过关闭卡片下全部 `Image.enabled` 隐藏菜谱。自定义逻辑不得因不透明度应用而重新启用 Image 或重激活 GameObject，避免菜单中漏出订单。〔`GAME/RecipeWidgetUIController.cs:77-83`〕
4. `RecipeViewer` 克隆同一 `RecipeWidgetUIController` 预制体，并禁用 `RectTransformExtension` 后使用自己的内容区/拖动排布。新 MOD 若全局拦截 Setup 或 tile 刷新，会影响其图库和导出；必须按真实 flow 持有关系过滤，不能仅以“位于游戏场景”判断。〔`RecipeViewer/RecipeBoard.cs:304-326`〕

## 5. 容器加号/内容物大小和每行个数

### 5.1 原版内容与排布

**事实**：`ClientIngredientContentGUI.OnOrderChanged` 在需要显示时把内容物 UI 实例化到 `HoverIconCanvas` 并跟随容器；已存在则重新激活。启用空位展示时，原版读取 `IngredientContainer.GetCapacity` 决定最少图标数，空容器时特例为 1；随后调用 `SetOrder`。这是显示容量，不能把“列数”接到 GetCapacity 或 SetMinimumSize 改变真实业务。〔`GAME/ClientIngredientContentGUI.cs:24-61`〕

**事实**：`IngredientContentsUIContainer.SetIconsForOrder` 根据原生组合状态产生已加入食材/物品图标；烧焦和过度搅拌使用专用状态图标。其余空位用 `m_emptySprite` 补齐，列表按需扩容并复用，超出本轮用量的 Image 被隐藏。因此“已加入食材不透明度”建议覆盖全部真实内容与烧焦/过度搅拌状态，空位单独归为加号；若要额外分离故障警告，需新增明确配置。〔`GAME/IngredientContentsUIContainer.cs:99-174`〕

**事实**：`EnsureImagesExist` 创建 `AnchorGridLayoutGroup`，`cellSize=(0.5,0.5)`、对齐 MiddleCenter。布局组将归一化 cellSize 乘父 Rect 的实际 x/y 尺寸后写每个 child 的位置/尺寸；FixedColumnCount 可确定每行数量，但单元尺寸不会因列数自动缩小。〔`GAME/IngredientContentsUIContainer.cs:67-79`；`GAME/UnityEngine/UI/AnchorGridLayoutGroup.cs:160-252`〕

### 5.2 建议的窄挂点和几何模型

**建议**：以私有 `IngredientContentsUIContainer.RefreshIcons(GameObject)` Postfix 为主要入口；原方法已完成 sprite、图标可见性和 `UI_Move.UpdateGraphics`，此时才能给真实在用图标分类和应用本轮布局。只匹配容器内容物实例；不 Patch 所有 AnchorGrid、不替换 `ClientIngredientContainer`、服务器容器或配方解析。用 `ClientIngredientContentGUI.OnOrderChanged` Postfix 登记 owner→`m_uiInstance` 并补应用，以覆盖实例复用和 `SetOrder` 相同配方提前返回。Awake 只能登记早期对象，不能保证动态容器已创建；单独 Patch SetOrder 又会漏过 SetMinimumSize 独立触发的刷新。配置修改时只重应用登记的存活实例。〔`GAME/IngredientContentsUIContainer.cs:11-17,57-79,87-95,177-182`；`GAME/ClientIngredientContentGUI.cs:24-103`〕

**建议的列数设置**：使用布局组公开 `constraint = FixedColumnCount`、`constraintCount = N`，并保留/显式设置水平填充方向；N 至少为 1。不能只改 N，否则 `cellSize.x=0.5` 时 4 列占两倍原父区域宽度。实际计算可按下述原则做样机后再定稿：

- 先从原布局的父 Rect 和 cellSize 捕获单个图标基础宽高，应用用户大小倍率得到目标图标宽高。
- 令本次有效列数为 `min(N,可见图标数)`，行数为 `ceil(可见图标数/N)`；图标顺序、空位数和可见性全部保持原版。
- 为满足“每行更多且不把单个图标缩小”的目标，让受控 UI 区域宽度随列数扩展、总高度随实际行数缩短，再把目标图标尺寸除以新父 Rect 的实际尺寸，得到归一化 cellSize/spacing。
- 另一种“固定总宽自动缩小图标”属于独立策略，只在产品明确选择时启用；不能以 `cellSize=1/N` 偷换用户设置的独立图标大小。

**重要约束**：基础类每次 Refresh 都会 `SetupFillParentAreaRect(m_container)`，其中明确重设 anchor、offset 和 localScale。若修改的是 `GeneratedChildren` 尺寸，下一次原生刷新会覆盖；需要在刷新结束后恢复受控几何，或调整真正控制可用区域的已核实父节点。后者还要检查父节点是否有 `RectTransformExtension`；若有，不能只写其 localScale/sizeDelta。〔`GAME/UISubElementContainer.cs:29-37`；`GAME/UIUtils.cs:8-15`；`GAME/RectTransformExtension.cs:134-150`〕

**待验**：所列算法是几何设计，尚未读出所有关卡预制体的实际父区域尺寸、pivot 和可用遮挡空间。首次实现必须用真实锅/碗/盘等实例验证跟随中心、上下生长方向及多个容器彼此遮挡，再确定是否按图标数动态扩宽；仅凭 `0.5` 和默认 constraintCount 不能给出统一的屏幕像素值。

### 5.3 保留原生跟随和局内行为

`HoverIconUIController.LateUpdate` 根据跟随目标世界坐标计算屏幕坐标，再写 `UI_Move.Offset`；`UI_Move.UpdateGraphics` 为递归图形配置自己的材质。应改内容排布与尺度，不覆盖跟随目标、世界偏移或该材质；这样端锅、传送、移动平台等仍沿原生路径定位。实际能否跟随稳定仍须实机。〔`GAME/HoverIconUIController.cs:76-117`；`GAME/UI_Move.cs:41-67`；`GAME/GameUtils.cs:354-359`〕

## 6. 菜谱、加号、已加入食材各自不透明度

### 6.1 参数语义和原生动画组合

配置名称建议使用“不透明度（%）”：100 表示保持原生显示，0 表示完全透明，避免“透明度 0”歧义。值只作为原始显示的不透明度乘数，不把食材原生透明度或动画颜色覆盖为固定值。

**外部事实**：Unity 2017.4 官方手册明确 `CanvasGroup.alpha` 取 0～1、与组内各元素自身 alpha 相乘；`Ignore Parent Groups` 控制该组是否受上级组影响。建议最终菜谱 alpha = 原生颜色/动画 alpha × 用户菜谱不透明度。现有子 CanvasGroup 若忽略上级组，会形成边界，不能擅自清空其设置来强行覆盖。〔[Unity 2017.4 Canvas Group](https://docs.unity.cn/2017.4/Documentation/Manual/class-CanvasGroup.html)，Properties/Alpha、Ignore Parent Groups；本次已读取核验〕

**建议**：优先给每张局内卡片建立本 MOD 拥有的 alpha 层，避免逐帧改原生 Image.color。若目标已有 CanvasGroup，先调查其所有者和动画，禁止把其当前 alpha 当作可自由覆盖的常量。图标本身也可用专属 CanvasGroup 倍率，但不能为了添加分组而随意重新挂父节点、破坏原布局组的直接 child 关系；已有组或不兼容 shader 应进入明确降级路径。

### 6.2 容器内加号与内容分类

**建议的权威分类**：在原版 RefreshIcons 完成后，从该实例的 `m_icons[0..m_iconsInUse)` 取实际在用 Image，并比较其当前 sprite 是否为该实例 `m_emptySprite`。加号倍率用于空位 sprite，其他食材/物品/烧焦/过搅拌图标用内容倍率；不根据全局贴图名字、子节点名字“Icon”或索引小于容器容量来猜。若空位资源缺失或契约异常，记录一次警告并保留原流程，不能误把未知图标隐藏。源码只能证明这里是“空位 sprite”；它在所有具体预制体是否都是可见“+”号，仍需资源/实机核对。〔`GAME/IngredientContentsUIContainer.cs:99-174,196-210`〕

**复用约束**：同一个 Image 可以由空位变食材，再变回空位，必须每次原生刷新后重新分类；本轮未使用的图标不重新 SetActive，也不修改其 sprite。即使用户把加号 alpha 设为 0，该格仍是原生容量中的空位，是否折叠空白位置是另一个产品选项，默认不折叠。〔`GAME/IngredientContentsUIContainer.cs:136-174`〕

**事实**：容器这条链的 `SetNextIcon` 只换 sprite，不写 Image.color；没有 C# 常量规定“原生加号比食材更透明”。专项调查另用真实 UnityEngine.UI.dll 元数据核对 `Graphic::.ctor` 初始化 m_Color 为 Color.white、`Image.set_sprite` 只标记 dirty 而不重置颜色。因此可在容器刷新后按各图标原色 alpha × 当前分类倍率处理，但必须记录原色和上次应用值，复用时不可累乘。预制图片原色、纹理自身 alpha、上级 CanvasGroup、CanvasRenderer 和 shader 仍可能影响最后可见效果；本轮没有读取出统一原生透明度数值。〔`GAME/IngredientContentsUIContainer.cs:145-162`；实际 UnityEngine.UI.dll 的 `UnityEngine.UI.Graphic::.ctor` 与 `UnityEngine.UI.Image::set_sprite` IL〕

**待验**：`UI_Move` 为 Graphic 替换自定义材质。CanvasGroup 在 Unity UI 的通用语义已确认，但该游戏材质、可能的 mask 和现有组边界是否完全符合预期仍需真实游戏检查。若不得不改 Image.color，仅在对应原生刷新/动画之后以“原生 alpha × 用户倍率”写入；需保存可靠原值与本 MOD 上次写值，防止每轮累乘，不能把菜谱成功淡出恢复成 100%。〔`GAME/UI_Move.cs:41-67`；`GAME/RecipeWidgetUIController.cs:177-208`〕

## 7. 独立 MOD 结构与配置建议

### 7.1 结构

建议新独立工程按职责分为插件入口/配置、订单展示适配、内容物展示适配、实例快照与恢复、文案；同一配置值和边界算法只有一个权威实现，避免几个 Patch 各算一遍。Harmony ID 和配置命名空间独立于 HostUtilities，真实游戏程序集仅作编译/元数据引用，不打包原版 DLL。此处为架构建议，本轮尚未创建独立工程或业务源文件。

配置可直接通过 BepInEx ConfigEntry 暴露；若后续接公共设置页，沿用其可选接入契约，设置服务缺失时仍能通过 cfg 工作，不因此引入对 HostUtilities、RecipeViewer 或配置面板的硬依赖。配置变更事件只标记 dirty/快照参数，Unity 对象的读取、重排、增删组件和恢复统一在 Unity 主线程执行。

### 7.2 配置表（提议）

| 建议键 | 语义 | 建议默认/边界 |
| --- | --- | --- |
| `Enabled` | 总开关 | 建议启用，其他参数默认保留原版显示；关闭时恢复本 MOD 拥有的改动，最终默认待实施决定。 |
| `RecipeScale` | 菜谱大小倍率 | 1.0，建议初期限定 0.5～2.0，真实 UI 验证后定范围。 |
| `RecipesPerRow` | 每行菜谱上限 | 0 = 原版一行；正整数 = 自定义上限，不增加真实订单总量。 |
| `ContentsScale` | 容器内容图标大小倍率 | 1.0，建议初期限定 0.5～2.0。加号与已加入图标共享几何尺度。 |
| `ContentsPerRow` | 容器图标每行上限 | 0 = 保留原布局；正整数 = 固定列数，不改容量或空位数。 |
| `RecipeOpacity` | 整张菜谱不透明度 | 100%，0～100%。 |
| `EmptySlotOpacity` | 空位加号不透明度 | 100%，0～100%。 |
| `IngredientOpacity` | 已加入内容/原生状态图标不透明度 | 100%，0～100%。 |

不要把 RecipeFlowGUI 的 `m_maxOrdersAllowed=5` 或 AnchorGridLayoutGroup 的 `m_ConstraintCount=2` 当作所有预制体真实值；它们有序列化字段，保存值可不同。上述 0 = 原版应保留每个实例的实际原值而非写回猜测默认。〔`GAME/RecipeFlowGUI.cs:232-246`；`GAME/UnityEngine/UI/AnchorGridLayoutGroup.cs:256-282`〕

## 8. 生命周期、恢复和兼容

### 8.1 实例状态

**建议**：以 flow/container 实例建立短生命周期状态，记录原始 scale/布局属性、拥有的组件、本轮图标引用、已应用参数版本；Unity 对象失效立即丢弃。快照应在该对象第一次被本 MOD 改动前捕获；GeneratedChildren 被原版重建后按新一代对象重新捕获，不能把上一代 Rect/图片句柄套回去。〔重建依据：`GAME/UISubElementContainer.cs:29-69`；`GAME/RecipeWidgetUIController.cs:99-173`〕

| 事件 | 建议行为 | 源码依据 |
| --- | --- | --- |
| 中途启用/配置改变 | 对登记实例标 dirty；配置快照在主线程一次重应用。初次已有实例可在明确边界做一次受限发现，之后禁止逐帧场景全量扫描。 | 原版卡片/容器会晚于入场逐个生成，见 `GAME/RecipeFlowGUI.cs:9-18`、`ClientIngredientContentGUI.cs:24-61`。 |
| 卡片下一帧生成、刷新重建 | 在实际子节点就绪后补齐大小/alpha和测量；旧引用失效，重新捕获当前原生状态。 | `GAME/RecipeWidgetUIController.cs:87-103`；`UISubElementContainer.cs:41-69`。 |
| 加料、取出、清空、烧焦/过搅拌 | 让原版先设置 sprite/数量/显示，再按当前类别和配置重应用。 | `GAME/IngredientContentsUIContainer.cs:87-174`。 |
| 容器重新激活/禁用 | 遵守原版 active 状态；重新启用后应用，禁用只恢复自身状态/释放不再需要的句柄，不主动把原生 UI 显示出来。 | `GAME/ClientIngredientContentGUI.cs:65-103`。 |
| 订单提交/失败/过期 | 保留原动画推进、退出列表和计时；退出卡片继续展示到原版销毁。 | `GAME/ClientOrderControllerBase.cs:57-86`；`RecipeFlowGUI.cs:115-130,154-183`。 |
| 关卡重开/场景切换/销毁 | 恢复仍存活且仍由本 MOD 拥有的改动，清理对象缓存；新场景重新登记。 | `GAME/ClientIngredientContentGUI.cs:95-103`；`RecipeWidgetUIController.cs:354-360`。 |
| 总开关关闭/插件卸载 | 卡片由原版下一次 Layout 恢复单行；手动还原本 MOD 改过的 scale/布局/alpha，只删除自己新增的组件，按本插件 ID 解除 Patch。 | 原版每帧布局 `GAME/RecipeFlowGUI.cs:170-201`；恢复为设计建议。 |

### 8.2 恢复和其他 MOD 所有权

建议只记录/恢复自己写过的属性；恢复前比较当前值是否仍等于本 MOD 上次写值，若其他 MOD 已接管则停止覆盖并记录一次诊断。若两者持续写同一布局，不能宣称仅设置 Harmony priority 就完成兼容，应选择明确的软兼容/用户关闭其中一方。不要恢复整个 GameObject 快照、材质或 active 状态，也不要销毁不属于自己的 CanvasGroup。RecipeViewer 的实例过滤是已发现的具体兼容边界；其窗口、导出、拖动排序必须保持原样。〔具体冲突依据：`RecipeViewer/RecipeBoard.cs:304-326`〕

### 8.3 主客机

**静态结论**：订单/食材已经由游戏同步，研究挂点处位于客户端 UI 展示；原版把 IngredientContainer 等业务 synchroniser 和 IngredientContentGUI 的 synchroniser 分开注册。可设计为每位玩家独立配置，主机不强制客机布局，也无需新增 169/171 消息。主机本机同样有自己的客户端展示，不能以 `IsHost` 为由只给主机或只给客机应用。该结论由显示调用链支持，尚未用第二 Steam peer 实测。〔`GAME/ClientKitchenFlowControllerBase.cs:27-64`；`GAME/ClientOrderControllerBase.cs:45-54`；`GAME/ClientIngredientContentGUI.cs:9-61`；`GAME/MultiplayerController.cs:392-404`〕

后续至少分别验收：仅主机装、仅客机装、双方安装且参数不同、对方旧版/无 MOD。预期只有各自画面变化，菜谱数、容器容量、加料、提交和时间行为一致；不能把单端截图当作全部组合通过。

## 9. 实际 Mono 访问边界与失败策略

以下访问性来自本次原始游戏 DLL Cecil 元数据核验，并由相应反编译声明交叉确认：

| 对象/成员 | 原版访问性 | 实施要求 |
| --- | --- | --- |
| `RecipeFlowGUI.LayoutWidgets`、`FindAllWidgetsOrdered`、`Awake` | private | Harmony 按准确签名定位；需要调用时缓存 AccessTools MethodInfo，不直接用 nstrip 暴露成员。〔`GAME/RecipeFlowGUI.cs:143-147,186-205`〕 |
| `RecipeFlowGUI.m_widgets/m_dyingWidgets/m_ordererWidgets`、间距/边距字段 | private | 初始化一次缓存 FieldInfo/受支持的字段访问器；热路径不构造 Traverse、不反复查找。〔`GAME/RecipeFlowGUI.cs:232-264`〕 |
| `RecipeWidgetUIController.GetBounds/SetupFromOrderDefinition/Update` | public | 可优先使用公开 API，但 GetBounds 的语义限制见 §4.3。〔`GAME/RecipeWidgetUIController.cs:59-74,87-96,177-208`〕 |
| `RecipeWidgetUIController.OnRefreshSubObjectProperties` | protected | Harmony 精确挂点；不是可直接从外部调用的 public API。〔`GAME/RecipeWidgetUIController.cs:349-352`〕 |
| `IngredientContentsUIContainer.RefreshIcons/InstantiateIcon/Awake/SetNextIcon` | private | 只缓存必要成员，缺失即该适配停用，不让异常逃到原版。〔`GAME/IngredientContentsUIContainer.cs:11-17,87-95,145-162`〕 |
| `IngredientContentsUIContainer.EnsureImagesExist/OnRefreshSubObjectProperties/SetIconsForOrder` | protected | 不直接访问；可选择最窄刷新方法 Patch。〔`GAME/IngredientContentsUIContainer.cs:67-99`〕 |
| `m_emptySprite/m_icons/m_iconsInUse`；`UISubElementContainer.m_container` | 前三者 private；container protected | 类型/列表元素/空位资源检查失败时只警告并保留原生显示。〔`GAME/IngredientContentsUIContainer.cs:196-210`；`UISubElementContainer.cs:100-104`〕 |
| `AnchorGridLayoutGroup.cellSize/spacing/constraint/constraintCount` setter | public | 通过属性设置以触发布局 dirty，不直接写其 protected backing field。〔`GAME/UnityEngine/UI/AnchorGridLayoutGroup.cs:44-102`〕 |
| `RectTransformExtension.scale/PixelOffset` setter | public | 优先用这些属性；不要强行每帧覆盖原版 private `UpdateAspectRatio`。〔`GAME/RectTransformExtension.cs:25-39,96-109,128-177`〕 |

**建议的失败策略**：首次启动只做一次类型/方法签名契约检查，菜谱和容器适配分别可降级；Patch 主逻辑 try/catch，输出目标与完整异常并继续原版。配置变化/对象生成等低频路径记录 Debug；异常或不可识别结构 Warning 去重；不逐帧打印布局和不透明度值。不要因展示适配失败阻断订单计时、加料或原生动画。

## 10. 后续验证与实施前提

### 10.1 本轮已完成的研究层验证

- 原版行为源码定位，保留精确文件/行号；实际 Windows 游戏 DLL 与反编译目标 SHA256 一致，并核对关键成员原始访问性。
- 持久静态证据：[metadata-verification.json](evidence/metadata-verification.json)，记录两份 DLL 的 SHA256/MVID/AssemblyVersion、7 个类型及 20 个目标方法的访问级别（按 JSON 实际数组计数）。资料由原 HostUtilities 的 BuildOutput 搬入本项目 docs/evidence，仅作调查证据，不属于可发布 MOD 产物；源码路径和文件摘要见同目录 source-references.json。
- 检查局内 UI 的增删、延迟生成、内容复用、原生逐帧覆盖、暂停隐藏、材质和跟随逻辑；只读比较 RecipeViewer 的原版卡片克隆路径。
- Unity 2017.4 官方 CanvasGroup 语义核验；只说明通用 alpha 组合，不扩大为游戏自定义 shader 已验证。

### 10.2 后续实现的有效验收

| 层级 | 具体检查与需保留证据 |
| --- | --- |
| 静态/编译 | 独立工程 .NET 3.5/游戏 Mono 引用构建；原版私有成员直接访问扫描；Harmony 签名存在；不引用/打包 HostUtilities 或 RecipeViewer DLL。编译通过单独报告。 |
| 本地逻辑 | 变宽卡片每行上限/安全区换行、不同高度行高、0图标/1图标/多图标、列数与scale独立、空位→食材→空位分类、反复应用不累乘、失效对象/恢复所有权；优先复用真实源/已有可用入口，不为研究临时建立新框架。 |
| 真实 Unity UI | 记录窗口/分辨率/宽高比、Canvas区域、每卡片/图标宽高、每行数量、行高/间距、总包围盒、是否越界/遮挡；每个档位截图，不能只报“看起来正常”。菜谱和内容物单独测量。 |
| 动画/生命周期 | 卡片出现/成功淡出/失败警告/过期/暂停；容器空→加料→满→取出/清空→烧焦/过搅拌；拿起移动/传送/重开/切关；中途改配置、关闭恢复、再次开启无累乘。 |
| alpha | 三项分别0/50/100%，检查只影响目标类别；菜谱原生淡出继续；加号透明不改变容量，材质/mask/子CanvasGroup边界；截图与组件alpha数据分开报告。 |
| 兼容/多人 | 主客机组合及不同本地参数；RecipeViewer窗口/导出不受影响；与实际使用的HUD MOD同装时检查谁写同一属性，记录版本和冲突。 |

**本轮没验证**：没有新 MOD DLL、没有构建/测试执行、没有新增框架、没有实际 UI 数值或截图、没有第二客户端、没有 Horde 适配、没有原生 Animator 资源逐条分析，也没有确认每种容器预制体的具体 sprite/pivot/尺寸。任何“视觉效果已正确”“性能无影响”“所有模式兼容”的承诺均超出本报告证据。

**后续运行前提**：独立资料目录现已建立，业务实现尚未开始；收到实施要求后再创建 C# 工程，按真实游戏原版 DLL 编译/验证。加载新 DLL 需要重启游戏，实际 UI 验收必须在游戏中执行，双端结论需要真实第二 Steam peer。当前资料不要求用户修改配置、重启或安装任何内容。
