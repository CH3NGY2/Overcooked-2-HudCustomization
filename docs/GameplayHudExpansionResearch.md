# 局内 HUD 扩展：拖动缩放、隐藏配置与进度 UI

研究日期：2026-11-17。**证据级别仅为本地原版源码与安装 DLL 静态核验**；本文不是实现或实机验收记录。本次只新增研究文档，未修改生产代码、部署 DLL、启动/结束游戏、修改配置/存档或进行多人验证。

## 1. 来源与结论范围

- `GAME = E:/code/OC2/Assembly-CSharp/Windows/Assembly-CSharp`，以下 `GAME/文件.cs:行号` 均指该目录的第一手反编译源码。
- 实际安装 DLL：`E:/SteamLibrary/steamapps/common/Overcooked! 2/Overcooked2_Data/Managed/Assembly-CSharp.dll`。
- 本轮重新计算安装 DLL 与 `E:/code/OC2/Assembly-CSharp/Windows/Assembly-CSharp.dll` 的 SHA256，二者均为 `9BB6A3791331201D32CA89C3509F019A9780309DA7110002F04020E8491E1908`。因此本文所查 Windows 源码与当前安装程序集对应；不推及其他平台或版本。
- 原有订单/容器研究见 [GameplayHudCustomizationResearch.md](GameplayHudCustomizationResearch.md)。以下 **事实** 是源码行为，**建议** 是后续窄适配方案，**待验** 是资源、真实布局或运行期结论。
- 本轮原始 DLL 访问性核验见 [hud-expansion-contracts.json](evidence/hud-expansion-contracts.json)：40 个方法、24 个字段；包含固定分数/倒计时、布局跟随和核心进度 UI。新增候选未全部做成员级核验，仍按各项的源码证据理解，不能把整份列表视为已测试支持。

**用户要求与方案摘要**：计分板/倒计时可以作为拖动、边缘改宽高、角落等比缩放的适配目标；位置尺寸以隐藏 ConfigEntry 持久化，不进入 ModSettings，细节见 §8。切菜/烹饪/炉温的偏移、大小和不透明度有清晰展示入口，但 XYZ 要明确坐标语义（§3），特殊模式不能假定与普通 HUD 完全相同。本文只完成可行性调查，没有实现这些新增功能。

**静态结论**：切菜工作进度、烹饪完成度、搅拌完成度、炉台热量表盘均存在明确的本地展示入口，适合增加独立大小、不透明度与跟随偏移设置。必须按创建它们的 owner 分类；不能全局缩放所有 `ProgressBarUI`，否则会同时影响订单剩余时间、Horde 等不同语义的条。`ProgressUI`、`WarningGUIBar`、`OverheadIconGUI` 是另有 IMGUI 绘制路径的类型，不能仅凭名字把它们当作当前切菜/烹饪 UI 的实际入口。〔`GAME/ClientWorkableItem.cs:16-26`；`GAME/ClientCookingHandler.cs:83-89`；`GAME/ClientMixingHandler.cs:19-27`；`GAME/ClientHeatedStationGUI.cs:60-82`；`GAME/ProgressUI.cs:16-28`；`GAME/OverheadIconGUI.cs:73-97`〕

## 2. 先区分玩家看到的几种“进度/火候”

| 玩家可见内容 | 实际 owner → 展示链 | 读到的状态；不能混用的意义 |
| --- | --- | --- |
| 切菜/工作进度条 | `ClientWorkableItem → ProgressUIController → ProgressBarUI` | 条跟随可加工食材对象，进度由阶段和本阶段刀数共同产生；不是每块砧板拥有一个常驻条。〔`GAME/ClientWorkableItem.cs:16-26,164-195`〕 |
| 普通烹饪完成度 | `ClientCookingHandler → CookingUIController → ProgressBarUI` | `cookingProgress / cookingtime`；完成后隐藏条，播放勾号；过火后改显示警告图标。〔`GAME/ClientCookingHandler.cs:126-146`；`GAME/CookingUIController.cs:20-59,98-107`〕 |
| 过火/烧焦警告 | 同一个 `CookingUIController` 的 `m_warningIcon: ImagePulser` | `OverDoing` 显示闪烁警告，`Ruined` 隐藏条与警告。不是第二条显示“烧焦百分比”的 `ProgressBarUI`。闪烁速度读取过火程度，并且闪烁回调发出声音。〔`GAME/CookingUIController.cs:46-52,104-122`〕 |
| 搅拌完成度与过搅拌 | `ClientMixingHandler → CookingUIController` | 与烹饪复用同一 UI 类，owner 不同；进度是 `mixingProgress / mixingTime`，也有 `Completed/OverDoing/Ruined`。〔`GAME/ClientMixingHandler.cs:19-27,37-103`〕 |
| 炉台热量/炉温表盘 | `ClientHeatedStationGUI → HeatValueUIController → ProgressGaugeUI` | 输入 `ClientHeatedStation.HeatValue`，表针在配置的空/满角间转动；不是食物完成度。热量会自然耗散，炉台按 0.33/0.66 分为低/中/高档。〔`GAME/ClientHeatedStationGUI.cs:49-82`；`GAME/ClientHeatedStation.cs:23-28,71-85`；`GAME/HeatedStation.cs:8-18`；`GAME/ProgressGaugeUI.cs:10-24`〕 |

**状态边界证据**：服务器烹饪逻辑在达到 `cookingtime` 后完成，超过 `1.3 × cookingtime` 进入 `OverDoing`，超过 `2 × cookingtime` 为烧焦；搅拌也有对应的 1.3 倍过搅拌切换。这里只用它解释 UI 意义，不建议 Patch 这些业务阈值。`HeatValueUIController` 这条链没有表示“最佳火候区间”的独立 C# 字段；若玩家指某个具体小游戏的最佳区间，需要再对应它实际的 UI 类型，不能把炉温表盘或 `OverDoing` 命名为最佳火候条。〔`GAME/ServerCookingHandler.cs:94-135`；`GAME/ServerMixingHandler.cs:49-75`〕

## 3. XYZ 偏移的准确坐标语义

### 3.1 原生跟随不是直接移动 RectTransform

**事实**：`HoverIconUIController` 的 `SetFollowOffset(Vector3)`、两种 `SetFollowTransform` 都会立即调用 `UpdateFollow`，随后每次 `LateUpdate` 再跟随。存在跟随目标时，坐标经过：

`followTransform.TransformPoint(followOffset) → Camera.main.WorldToViewportPoint → (viewportXY + anchorOffset) × Canvas.rect.size → UI_Move.Offset`。

因此 `followOffset` 是**跟随目标的局部 XYZ**，受目标的平移、旋转、缩放影响；它不是简单世界空间 XYZ，更不是屏幕像素 XYZ。厨具被拿起、旋转，或跟随可视根节点缩放时，同一偏移可能在屏幕产生不同方向/距离。世界 Y 常对应高度，但最终投影还取决于相机；Z 是游戏世界深度，不能解释为 UI 叠放顺序。无跟随目标时源码直接返回 `m_followOffset` 再转为 Vector2，这又是 viewport 坐标语义，不应沿用有目标时的世界解释。〔`GAME/HoverIconUIController.cs:23-47,76-117`〕

**事实**：`SetAnchorOffset(Vector2)` 写入的是 viewport 比例偏移；其 X/Y 最终分别乘 Canvas 宽/高，且 setter 本身不立即 `UpdateFollow`，正常下一次 `LateUpdate` 生效。若需要玩家直观调“屏幕向右/向上”，适合暴露独立屏幕 XY 设置，再按实际 Canvas 尺寸换算。若同时提供 XYZ，应明确叫“跟随目标局部 XYZ”，或在实现层将世界偏移正确转换成目标局部点；不要把两者混在一个不注明单位的分组。〔`GAME/HoverIconUIController.cs:11-31,99-117`〕

**事实**：`UI_Move.Offset` 写入实例材质的 `_Offset`；`Start/UpdateGraphics` 收集子 `Graphic` 并赋该材质，`LateUpdate` 继续写 uniform。因此普通 `RectTransform.position`、`GetWorldCorners` 或标准 UI raycast 得到的位置，不自动等于 shader 最终显示位置。本轮没有 shader 源码/资源或真实画面，不能宣称坐标、点击区域或遮罩已正确。〔`GAME/UI_Move.cs:11-23,28-67`〕

### 3.2 偏移挂点与恢复

**建议**：在明确 owner 创建实例后登记 owner→UI，并捕获该实例原生 followOffset/anchorOffset；自定义值以**原生值 + 用户增量**重算，不能从上次已加增量的当前值再加。现有 followOffset 无公开 getter，需要一次缓存读取 `HoverIconUIController.m_followOffset`；anchorOffset 可用 `GetOffset()`。尽量调用原生 public setters，不改 `ShouldFollow`、原跟随目标、相机或材质。对 `SetFollowOffset/SetFollowTransform` 的后续原生调用要防重入、重新建立基准，不能静态写一次后假定生命周期内不变。〔`GAME/HoverIconUIController.cs:11-47,124-131`〕

**不建议**：全局 Patch 所有 `HoverIconUIController` 强制统一参数。该基类也服务世界地图、对话、重生、Horde 和内容物，必须由 owner/具体用途决定是否纳入；也不要每帧全场扫描寻找进度条。〔`GAME/RespawnCounterUIController.cs:6,46-62`；`GAME/GameModes/Horde/HordeOrderUIController.cs:11-21`；`GAME/ClientDialogueController.cs:95`〕

## 4. 大小、不透明度与原生覆盖

### 4.1 大小

**事实**：横条 `ProgressBarUI.UpdateFill` 把 filled Image 的 `localScale.x` 或 mask 的 `anchorMax.x` 写成业务进度；同时调整 cap 的 X anchor。刷新子对象时还会重置一批子 RectTransform 的 anchors、offsets、scale 和 pivot。直接把 filled Image 的 scale 当“大小倍率”会改变进度语义，且下一次进度更新立即覆盖。表盘则每次填充更新重写表针 localRotation。〔`GAME/ProgressBarUI.cs:11-67,94-101`；`GAME/ProgressGaugeUI.cs:10-24`；`GAME/UIUtils.cs:8-15`〕

**建议**：缩放属于整个显示框/已确认外层的几何，不改填充条、cap、表针和刻度的位置规则。先检查实例是否带 `RectTransformExtension`；若存在，优先使用它公开的 `scale` 属性，并核对父锚点/像素 inset 的真实效果，因为其 `LateUpdate → UpdateAspectRatio` 每帧会把 Transform.localScale 写回 1。没有该组件且外层无动画驱动时，才考虑基于原始 localScale 的倍率。不能从 C# 声明推断每个 prefab 的实际组件、尺寸和 pivot。〔`GAME/RectTransformExtension.cs:96-109,128-177`〕

### 4.2 不透明度

**事实**：`BaseProgressBarUI.Update`、`SetValue`、`OnEnable` 和重建刷新都写三个核心 Image 的颜色；颜色取决于原版 ColorSequence。`CookingUIController.RunTickAnimation` 每帧写勾号 alpha，`ImagePulser.Update` 每帧写警告 alpha。因此一次给 Image.color.a 设值不稳定，持续强写常量又会破坏颜色渐变、勾号淡入淡出与警告闪烁。刻度在独立 `m_notches`，不包含在那三个核心图片内。〔`GAME/BaseProgressBarUI.cs:70-108,161-196,244-251`；`GAME/CookingUIController.cs:83-94`；`GAME/ImagePulser.cs:28-47`〕

**建议**：以完整显示组的不透明度倍率组合原生 alpha；如需“进度条/完成勾/警告”分别控制，再给三个已确认子组独立倍率。首选本 MOD 自有的 CanvasGroup 倍率层，但不能随意改父子关系、现有 CanvasGroup 或 shader 材质。若 prefab 结构或材质不支持，应明确降级并在对应原生颜色写入后乘倍率，保留原值/上次写值防止累乘，覆盖动态新建的背景、filled、cap、刻度。CanvasGroup 与本游戏 UI_Move/mask 的实效仍待实机验收。〔组合约束见原研究 §6；本链动态重建见 `GAME/BaseProgressBarUI.cs:139-196`〕

**特别风险**：把 `ImagePulser` 组件禁用来“隐藏警告”会同时停止其 pulse callback，进而影响 `CookingWarning` 提示音。只调显示不透明度时应保留状态机、active/enable、计时、回调与声音。〔`GAME/ImagePulser.cs:28-39`；`GAME/CookingUIController.cs:116-122`〕

## 5. 每类进度 UI 的窄挂点与生命周期

| 类别 | 建议登记/应用挂点 | 已确认生命周期与风险 |
| --- | --- | --- |
| 切菜/工作 | `ClientWorkableItem.StartSynchronising(Component)` Postfix 取得 `m_guibar`；`UpdateProgressVisuals` Postfix 只处理该 owner 所属实例；配置变更时重应用已登记实例。 | 创建于开始同步，`AutoHide=false`，跟随食材 `transform`；原版在没放工作台时逐帧隐藏，禁用隐藏，启用按已有进度再开，销毁时销毁整 UI GameObject。不得设置 active 来强行露条，不改 stages、chopsPerSlice 或 DoWork。〔`GAME/ClientWorkableItem.cs:16-26,61-87,110-125,190-195`〕 |
| 普通烹饪 | `ClientCookingHandler.StartSynchronising(Component)` Postfix 取得 `m_gui`，此时已应用 prefab 的原始 `m_cookingUIPrefabOffset`；`CookingUIController.SetState` 后仅刷新已登记子组/材质引用。 | 跟随 `NetworkUtils.FindVisualRoot`；服务端更新才更新进度/状态，原生根据进度、是否在烹饪区和 owner active 隐藏/重开，owner OnDisable 回 Idle 并隐藏。本类未找到销毁 `m_gui` 的 OnDestroy，不应声称其 teardown 完整或擅自全局补 Destroy；MOD 缓存要能丢弃失效 owner/UI。〔`GAME/ClientCookingHandler.cs:83-89,123-180,215-228,246-252`；`GAME/CookingUIController.cs:20-59`〕 |
| 搅拌 | `ClientMixingHandler.StartSynchronising(Component)` Postfix 取得 `m_progressUI`，与烹饪共用展示适配，通过 owner 类别选不同配置。 | 跟随内容对象的 visual root，初始化 offset=0；服务端更新设置进度/过搅拌程度和状态，移出搅拌机后按原版延迟回 Idle；本类源码没有独立 OnDestroy/OnDisable 清理 UI，不能假定与烹饪或切菜完全相同。〔`GAME/ClientMixingHandler.cs:19-27,37-110,149-165`〕 |
| 炉温表盘 | `ClientHeatedStationGUI.OnHeatValueChanged(float)` Postfix 取得懒创建/复用的 `m_uiInstance`；只改其展示，不能改 HeatValue。 | 热量>0 或 `m_displayWhenCold` 时才生成/重启；使用 `HeatedStationGUI.m_Offset`、visual root。禁用隐藏，销毁时 Destroy 整 UI 并置空；HeatValueUIController 有自己的变化插值、停变后隐藏计时。〔`GAME/ClientHeatedStationGUI.cs:18-44,60-82`；`GAME/HeatedStationGUI.cs:9-17`；`GAME/HeatValueUIController.cs:8-49`〕 |
| 水槽洗碗 | `ClientWashingStation.Awake()` Postfix 取得 `m_progressUI`。 | 水槽 Awake 建一份，跟随水槽 transform，显示已洗时间/单盘时间；交互及最后一盘完成控制可见性。OnDestroy 原码只 Destroy controller 组件，未显式销毁 UI GameObject；MOD 清理只能移除自有状态/组件，不能据此擅自修游戏销毁。〔`GAME/ClientWashingStation.cs:61-108,151-159`〕 |
| 可清洗物体 | `ClientWashable.StartSynchronising(Component)` Postfix 取得可选 `m_progressBar`。 | prefab 可为 null；条跟随物体，客户端按 rate 推进并归一化 duration×multiplier，清理时销毁 UI GameObject；勿改时长/倍率。〔`GAME/ClientWashable.cs:16-27,67-75,89-110`〕 |

以上 Postfix 是未来适配建议，本次没有注册 Harmony Patch。所有非公开字段读取必须基于真实 DLL 缓存反射，不以 publicizer 编译成功代替权限核验；Unity 操作放主线程。全局 `BaseProgressBarUI.UpdateColors` 如作为颜色降级挂点，必须先查该实例是否已由目标 owner 登记，不碰其他条。

## 6. 其他局内 UI 可缩小候选

本节按 owner 列出扩展候选；固定分数/倒计时及鼠标交互见 §8。以下仅列可见实例的局部展示参数，不建议统一缩放整个 HoverIconCanvas 或 HUD Canvas。

| 优先级/候选 | 可调展示参数、窄挂点 | 生命周期、原生覆盖与边界 |
| --- | --- | --- |
| 高：洗碗/可清洗进度 | 可与切菜复用 ProgressUIController 几何/alpha 适配，按 `ClientWashingStation` 与 `ClientWashable` owner 分组；具体挂点见 §5。 | 两条创建链都已找到，不能只适配水槽而宣称覆盖所有可清洗物。原生最后一盘隐藏和自动隐藏保留。〔`GAME/ClientWashingStation.cs:61-108,151-159`；`GAME/ClientWashable.cs:16-27,89-110`〕 |
| 高：灭火/火势条 | `ClientFlammable.Ignite()` 后登记 `m_fireGUIBar`，大小/不透明度/局部 XYZ 或屏幕 XY 独立设置。 | 是 ProgressUIController，跟随起火物，初始隐藏；`SetClampedFireStrength` 在火势<1时显示并重置1秒计时，回到满火后该计时到0才隐藏，Extinguish 销毁整 UI。不要把火势值叫灭火累计百分比，不改燃烧/灭火强度、PFX 或音频。〔`GAME/ClientFlammable.cs:88-108,120-139,151-164,187-197`〕 |
| 高：交互/拾取按键提示 | `ButtonHoverIcon.Awake()` Postfix 通过公开 `HoverIconController` 登记；可调大小、alpha、偏移。具体语义由 `ContextualInteractHoverIcon` / `ContextualPickupHoverIcon` owner 区分。 | Awake 实例化到 HoverIconCanvas，赋控制器图标并订阅设备变更；OnDestroy 退订并 Destroy。原生按 anticipation 计数决定隐藏，不能靠改 active 作为缩小方案；键位更换会刷新 sprite，不能缓存贴图名当分类。〔`GAME/ButtonHoverIcon.cs:10-16,19-69,73-89`；`GAME/ContextualInteractHoverIcon.cs:8-25`；`GAME/ContextualPickupHoverIcon.cs:8-25`〕 |
| 高：烹饪/搅拌完成勾和过火警告 | 使用已登记 CookingUIController 的 `m_tick`、`m_warningIcon` 分组，可独立缩小/移位/乘 alpha；状态切换后重应用组边界。 | 仍与工作条共属一个 owner，整体调整时默认同步移动更少错位；单独缩放警告要保留闪烁、完成勾 alpha 动画和提示音，不取消 Update 或改状态。〔`GAME/CookingUIController.cs:20-59,83-122`；`GAME/ImagePulser.cs:28-47`〕 |
| 高：普通顶部订单条/内容物 | 继续复用已有 `RecipeFlowGUI.LayoutWidgets`、`IngredientContentsUIContainer.RefreshIcons` 适配；候选为整组位置、间距、对应局部大小。 | 已有主报告和当前实现，不能新建平行控制器；订单原生逐帧布局、退出卡、分阶段入场、容器空位/内容复用必须延续本项目现有逻辑。〔`GAME/RecipeFlowGUI.cs:170-230`；`GAME/IngredientContentsUIContainer.cs:87-174`；详见原研究 §4-6及当前 `docs/Implementation.md`〕 |
| 中：出餐口小费浮字 | `ClientPlateStation.OnScoreTipNotification(DataStore.Id,object)` 触发的 `m_tipsFloatingNumberUI` 实例；可调浮字组大小、alpha、偏移。 | 只有小费>0、队伍/出餐口匹配才创建，每次通知可能新建；方法没有把 UI 保存在 owner 字段，单纯 Postfix 没有结果可取。建议在**受该 owner 调用上下文限定**的实例化返回边界登记，而不是为找到浮字扫描场景或全局改 DisplayIntUIController。实际 prefab 的动画/销毁事件未读取，需资源/实机补证。〔`GAME/ClientPlateStation.cs:24-32,151-169`〕 |
| 中：小费倍率/连击显示 | `MultiplierUIController.Awake()` / Value setter 与 `ScoreUIController.ScoreUpdate(TeamID,TeamScoreStats)` 中已明确的倍率对象；调该展示组几何/alpha，不修改 TotalMultiplier。 | 同名 `DisplayMultiplerUIController` 只是 “a x b = c” 文本类，不能当作局内连击入口。ScoreUIController 控制倍率文本 enabled、栏底 sprite 和 x4 火焰，倍率变化还有 Animator trigger；需要先核对具体 prefab 的所有权再决定是否独立于分数组设置。〔`GAME/MultiplierUIController.cs:14-32`；`GAME/ScoreUIController.cs:35-48,91-105`；`GAME/DisplayMultiplerUIController.cs:41-50`〕 |
| 中：缺少盘子提示 | `ClientPlateStation.FailedToDeliver(GameObject)` 创建 `m_needsPlateFloatingUI`；与小费浮字共用受 owner 限定的实例化登记机制。 | 只在传入对象不是盘子且冷却<=0时生成，随后冷却1秒；不改交付检查/冷却。prefab 原生动画/寿命待验，不能以 DisplayIntUIController 类型或名字包含 Floating 全局误伤。〔`GAME/ClientPlateStation.cs:134-140`〕 |
| 中：重生倒计时/玩家识别底板 | `ClientPlayerRespawnBehaviour.StartSynchronising(Component)` 后登记 `m_respawnUiController`；`MoveRespawnPoint` 或重新 RunHoverCounter 后补偏移基准；大小/alpha 可独立。 | UI 起初隐藏，复用并在重生时重设跟随目标/偏移、countdown、队伍/玩家编号，结束后隐藏；owner销毁时 Destroy 整 UI。RespawnCounterUIController 每帧更新文本和底板 sprite。不得改 respawnTime、SetTeam/SetPlayerNum 或重生位置业务。〔`GAME/ClientPlayerRespawnBehaviour.cs:21-42,186-220`；`GAME/RespawnCounterUIController.cs:46-62`〕 |
| 中：玩家脚下/投掷瞄准标记 | `PlayerControls.ThrowIndicator` 获取 `ThrowIndicators`，在 `PlayerControls.SetControlSchemeData` / `ThrowIndicators.UpdateCosmetics` 后登记四个 renderer；可研究只改标记节点原始 scale/offset。 | **这是 MeshRenderer，不是 Canvas UI**，包含 aim/idle 各自阴影。`ClientPlayerControlsImpl_Default.Update_Aim` 按本地控制/瞄准状态调用 Show/Hide，材质贴图按玩家/队伍刷新。不能调玩家根 transform、物理/投掷方向，也不能用 CanvasGroup 声称支持 alpha；shader alpha 与子节点 prefab/Animator 待验。局内其他“玩家标记”尚不能由这个类一概代指。〔`GAME/PlayerControls.cs:185-191,596-619,1030-1087`；`GAME/ClientPlayerControlsImpl_Default.cs:266-300`〕 |
| 中：背包食材图标/拿取提示 | `ClientBackpackCosmeticDecisions.StartSynchronising(Component)` 后登记 `m_contentsHoverIcon` 与已存在 ButtonHoverIcon；可调各自组大小/alpha/offset。 | 背包 attach/detach 切换“拿取提示/食材图标”，其原生 followTarget 与 offset 可不同；不要当作容器网格。禁用隐藏，OnDestroy 此类只见取消 attachment 回调，UI 引用清理需按失效对象处理，不能承诺和 ButtonHoverIcon 完全相同。〔`GAME/ClientBackpackCosmeticDecisions.cs:10-35,40-51,63-82`〕 |
| 中：教程/注意提示图标 | `ClientIconTutorialBase.IconData` 构造完成或其具体 owner 建立 icons 后登记；大小/alpha/offset。 | 图标会由 ActiveCallback 更换跟随对象，显隐切换时另生 attentionDrawer；DisableIcons 销毁已有图标。不要改教程完成状态、回调返回值或 attention 对象寿命；通用参数可复用，owner/类别仍要单独登记。〔`GAME/ClientIconTutorialBase.cs:45-51,155-185`〕 |
| 低：局内表情气泡 | `ClientEmoteWheel.EmoteRoutine(int)` 中 `ForUI=false` 分支创建的 `m_emoteDialog`；可研究大小/alpha/偏移。 | 同方法还有前端角色卡 `ForUI=true` 的普通 RectTransform 分支，必须过滤；原生按表情 duration 或重生中止，CleanUpEmote Destroy。协程在第一次 MoveNext 后才创建实例，给返回 IEnumerator 的普通 Postfix 不等于创建后挂点；优先具体实例化边界登记，避免跳过原协程。〔`GAME/ClientEmoteWheel.cs:161-203,392-398`〕 |

### 6.1 Horde 单独适配候选

| 候选 | 窄挂点与可调参数 | 特殊风险 |
| --- | --- | --- |
| 敌人目标订单卡 | `ClientHordeTargetCosmeticDecisions.OnEntryAdded(RecipeList.Entry)` / `HordeOrderUIController.Setup(Transform,RecipeTileData[],Align)` 完成后登记；局部大小、alpha、相对目标偏移。 | 订单完成后旧卡进入 retired 列表，动画播完才销毁。HordeOrderUIController.Update **每帧在动画中写 `RectTransformExtension.scale`、叠加 PixelOffset、写 Image.color**；因此应组合原动画乘数/增量，不能一次设置或强行覆盖为常量，不能沿用常规顶部 flow 布局。〔`GAME/GameModes/Horde/ClientHordeTargetCosmeticDecisions.cs:58-84,176-184`；`GAME/GameModes/Horde/HordeOrderUIController.cs:14-21,32-74`〕 |
| 目标受袭警告/修理血条 | `ClientHordeTargetCosmeticDecisions.StartSynchronising(Component)` 后登记 `m_healthUIController`，显示控制仍归原生；可调组大小、alpha、offset。 | 数值来自目标 NormalisedHealth；原生根据修理/敌人/目标存活切状态，警告提前量基于下一击伤害。HordeTargetUIController 显示条/闪烁警告并触发声音，不能改变警告阈值、anticipation、repair输入或生命值。该 owner 的 OnDestroy 只见反注册回调，未见 Destroy UI；同样只报告源码边界。〔`GAME/GameModes/Horde/ClientHordeTargetCosmeticDecisions.cs:13-23,43-54,88-98,149-174`；`GAME/GameModes/Horde/HordeTargetUIController.cs:30-89`〕 |
| 锁定区/购买价格提示 | `ClientHordeLockableCosmeticDecisions.StartSynchronising(Component)` 后登记 `m_hoverIconController`；大小、alpha、offset可分组。 | 价格取 UnlockCost，锁/解锁和 anticipation 事件控制 active；不改价格、付费、锁状态或操作判定。此类无独立 UI 销毁方法，后续必须核对实例在切关时由谁清理。〔`GAME/GameModes/Horde/ClientHordeLockableCosmeticDecisions.cs:12-24,29-66`〕 |
| 基地血量/金币/波次栏 | 分别登记 `GameModes.Horde.HealthUIController`、`MoneyUIController`、`WaveUIController` 的实际 HUD 实例后调整外层几何/alpha。 | 血量每帧写 ProgressBarUI.Value，金币变更/血量受损会另生成 ScalingHUDCanvas 浮字并触发 Animator；波次由 BeginWave 事件更新文字。需要单独模式配置，不能把 HealthUIController 当作目标头顶修理条。〔`GAME/GameModes/Horde/HealthUIController.cs:40-65`；`GAME/GameModes/Horde/MoneyUIController.cs:36-53`；`GAME/GameModes/Horde/WaveUIController.cs:12-22,32-51`〕 |

### 6.2 存在类名但不应优先接入的旧路径

- `ProgressUI` 在 Awake 直接 `AddComponent<GUIBar>()`，创建 `GUIBarWidgetConfig`，宽40/高10、offset(0,25)，更新时有1秒隐藏计时；它不继承 HoverIconUIController。对 GAME 全部 C# 搜索未找到其他类型 `GetComponent/AddComponent/RequireComponent<ProgressUI>` 或字段调用，因此不能确认当前发布 prefab 是否仍挂它，也不能凭它的默认尺寸解释实际切菜条。〔`GAME/ProgressUI.cs:8-38`〕
- `GUIBar/GUIBarWidget` 用 OnGUI 绘制：从目标位置投影，每相机以像素宽高与 offset 得到 Rect，再用 GUI.color 画 border/empty/fill。它与 UI_Move/CanvasGroup 的适配方式不同；缩放普通 Transform 不改变其 config 的宽高。`WarningGUIBar.Update` 还持续改三种颜色；若未来实机确认某实例使用此路，应独立处理 config，不全局接管 GUI.color。〔`GAME/GUIBar.cs:15-29,45-48`；`GAME/GUIBarWidget.cs:38-85`；`GAME/WarningGUIBar.cs:25-33,47-62`〕
- `OverheadIconGUI` 确有 public `SetIconScale`、`SetScreenOffset`、`SetWorldOffset`，但 OnGUI 才投影/绘制，自己的淡入淡出 alpha 每帧更新，CanvasGroup 对它没有作用。其世界 offset 是 `position + rotation × offset`，**不含 TransformPoint 的缩放语义**，与 HoverIconUIController 不同；screen offset 在乘相机像素尺寸之前相加。对 GAME 全部 C# 搜索没有找到外部使用该类型的调用方，prefab 是否使用、具体对应哪种图标仍待资源/实机确认，不能先拿它当通用玩家头标控制器。〔`GAME/OverheadIconGUI.cs:21-35,47-69,73-105`〕
- `TimedInputUIController` 的值是 `button.GetHeldTimeLength()/targetTime`，写入 `T17FilledImage.SetFilledAmount`，它表示按住按钮的时长。实际 C# 调用点在过场跳过、制作人员跳过与开场教程按住关闭，均给1秒目标；不能因名字有 TimedInput 把它宣称成最佳火候/切菜 QTE，不列为首批局内工作条。〔`GAME/TimedInputUIController.cs:8-12,28-42`；`GAME/ClientCutsceneController.cs:227-234`；`GAME/CreditsWorldFlow.cs:123-129`；`GAME/LevelIntroFlowroutine.cs:86-99`〕

## 7. 实施顺序与验证缺口

**建议首批**：工作进度、烹饪/搅拌组、炉温表盘、水槽/可清洗物、灭火条。共用一套“登记实例 → 保存原生几何/alpha基准 → 配置增量组合 → 原生重建后补应用 → 关闭/销毁恢复”的底层机制，以 owner 分类选择配置。下一批再扩展交互提示、警告子组与小费浮字；MeshRenderer 玩家标记和 Horde 为独立适配域。不要为每个 owner 复制跟随/缩放/alpha 算法，也不要为了复用而把不同计时、状态机、可见性搬进同一自建控制器。

**重建注意**：`UISubElementContainer.RefreshSubElements` 可能销毁并重新创建 GeneratedChildren，随后 `SetupFillParentAreaRect` 再重置生成容器几何；BaseProgressBarUI 的 Image/刻度引用随之换代。owner 本身未变不代表原子节点仍有效。只在生成/刷新/配置变更等边界重新收集，不能把扫描放在每个进度更新或每帧。〔`GAME/UISubElementContainer.cs:11-15,29-48,52-67`；`GAME/BaseProgressBarUI.cs:161-196`〕

后续真正实施后，最低验收矩阵：

| 范围 | 需要实际测量/观察 |
| --- | --- |
| 几何/偏移 | 每类实际 prefab、分辨率/宽高比、Canvas尺寸、原始/目标宽高与屏幕包围盒；0.5/1/2倍只是候选档位；目标局部 XYZ 分轴、屏幕 XY 分轴，拿起/旋转/传送/移动平台，禁止只用 Transform 世界角点代替 UI_Move 实际视觉边界。 |
| 状态/动画 | 切菜每刀/完成、离开砧板；烹饪 raw→progressing→completed勾→overdoing闪烁→ruined及离灶；搅拌同类状态；炉温升降/静止自动隐藏；最后一盘洗净；起火/灭火/重燃；都要保留原生 active、动画与声音。 |
| alpha | 0/50/100% 与原生 ColorSequence、ImagePulser、勾号淡出、刻度/表针/背景/mask/UI_Move组合；反复改值、禁用/重启、对象复用/重建，确认不累乘、不漏子节点。 |
| 生命周期/恢复 | 原生隐藏不会被重新激活；失效owner/UI引用会清理；场景/关卡重开、热配置、关闭插件、其他MOD写同属性时保护所有权。源码没显式teardown的owner需实际观察，不能先宣称泄漏或完整清理。 |
| 多人 | 主机单装、客机单装、双方不同参数、未安装对端；所有数值/状态/操作/网络消息保持原生，只各自画面变化。当前没有第二Steam peer证据。 |

**本报告已经验证**：目标创建/更新/显隐/部分销毁调用链、原生每帧几何/颜色覆盖、局部XYZ与屏幕比例偏移语义、安装与反编译DLL SHA256一致，以及具体源文件/行号。**未验证**：实际预制体组件树与序列化值、Animator/Shader资源、真实像素/截图/点击范围、编译、新补丁运行、联机效果、其他平台；不以静态结果替代任何一项。

**当前运行时前提**：阅读本报告无需安装、重启或改游戏。若后续授权实现，加载变更DLL需要在适当时机重启游戏；真实UI验收需单独确认游戏/存档/插件隔离与测试进程所有权，不能继承过去的部署或退出玩家游戏授权。

## 8. 计分板、倒计时：拖动与八向缩放

### 8.1 实际 owner 与模式差异

| 目标 | 已确认的原生责任链 | 适配边界 |
| --- | --- | --- |
| 普通计分板 | `ScoreUIController.Awake` 订阅 `score.team`；`ScoreUpdate` 写数字、倍率文本、底栏 sprite、x4 火焰，并触发 Animator；`OnDestroy` 退订。 | 调整已确认的完整视觉组，保持分数、倍率与事件。别按类名 `ClockAndCashGUI` 找现代分数面板：该旧类用 Mesh 绘时钟，不含现代分数 UI 的订阅链。〔`GAME/ScoreUIController.cs:8-24,35-60,64-118`；`GAME/ClockAndCashGUI.cs:14-30,102-114`〕 |
| 普通倒计时 | `LevelTimerUIController.Awake` 获取 Animator/子 ProgressBarUI 并订阅 `time.updated`；Value setter 更新时间文字、剩余比例，触发最后数秒脉冲/声音和定时提醒。 | 拖动缩放“数字+沙漏/边框+时间条”的确认外层；不改 Value、timeLimit、pulse/alert阈值、进度值或 Animator 运行。〔`GAME/LevelTimerUIController.cs:9-24,30-60`；`GAME/DisplayTimeUIController.cs:10-20`〕 |
| 练习模式 | `ClientPracticeMode.Setup` 创建 HUD 后搜集 ScoreUIController 与 DisplayTimeUIController；Begin/配置变更按模式开关隐藏整组。 | 保持原生隐藏，不能为了拖动强行 SetActive；重新打开后再应用已存布局。〔`GAME/GameModes/ClientPracticeMode.cs:26-47,56-72`〕 |
| 生存模式 | `ClientSurvivalMode.Setup` 按队伍数选普通/对战 prefab；`SurvivalModeLevelTimerUIController` 分开持有本轮剩余时间和存活时间，并生成加减时间/纪录浮字。 | 两种时间角色应分开识别，不能只 Patch LevelTimerUIController 就声称覆盖所有时间 UI。〔`GAME/GameModes/ClientSurvivalMode.cs:26-39`；`GAME/SurvivalModeLevelTimerUIController.cs:9-31,36-67,71-82`〕 |
| Horde | `ClientHordeFlowController.StartSynchronising` 单独生成模式 HUD；生命、金币和波次各自控制。 | 不是普通“分数+倒计时”的通用副本，先按 §6.1 独立适配，不把普通计分布局盲套给金币/血条。〔`GAME/GameModes/Horde/ClientHordeFlowController.cs:159-166`；§6.1〕 |

普通模式 HUD 的实例化入口将 prefab 放到 `ScalingHUDCanvas/SafeZoneElements`。建议在已确认的模式生成边界或精确控制器 Awake 后登记实例和角色，按实际父 Canvas/安全区计算位置；不让菜单、结算或第三方克隆仅因同基类而被登记。`CompetitiveScoreboardUIController` 管的是赛后计分画面及玩家菜单，不是局内左下分数面板，应排除。〔`GAME/GameUtils.cs:318-332`；`GAME/SafeZoneUI.cs:8-20`；`GAME/GameModes/ClientCampaignMode.cs:26-38`；`GAME/CompetitiveScoreboardUIController.cs:84-121`〕

**容易漏掉的伴随对象**：ScoreUIController 的加减分飘字另行实例化在安全区 HUD 下，并非分数面板的子节点；其位置来自 `m_floatingTextOffset × (anchorMin + anchorMax)`，不保证自动跟随自定义面板大小/位置。生存模式加减时间、破纪录浮字也有相同模式。后续实现需将这些飘字的出生点接到“原生动画 + 用户布局”后的视觉锚点，保留自己的飘动/寿命；不能只拖面板本体就宣称全部跟随完成。〔`GAME/ScoreUIController.cs:49-58`；`GAME/SurvivalModeLevelTimerUIController.cs:36-44,63-67`〕

### 8.2 推荐交互：明确进入 HUD 编辑状态

下列均为**交互方案建议**，不是现有实现：

| 鼠标操作 | 建议行为 |
| --- | --- |
| 主体内按住左键拖动 | 整体平移；记录按下时的鼠标与几何，从该基准计算，不连续累加误差。 |
| 左/右边缘拖动 | 只改宽度，对边位置保持不动。 |
| 上/下边缘拖动 | 只改高度，对边位置保持不动。 |
| 四角拖动 | 以对角为固定点，按开始拖动时的宽高比等比缩放。角落命中优先于边缘。 |
| 悬停边缘/角落 | 画出轮廓与对应方向的缩放鼠标提示；建议命中带按参考分辨率缩放，初始可用1080p下8屏幕像素，实机再验。 |
| 松开鼠标 | 一次提交本次最终布局并保存；拖动期间只预览。 |
| Esc/窗口失焦/切关/打开其他交互面板 | 取消未提交手势并恢复按下前布局，释放自身指针/输入/光标占用；已经完成的布局不回滚。 |
| 编辑状态内“恢复原位” | 清除自定义布局启用标记并保存，回到当前 prefab 的原生位置尺寸，不猜统一默认像素。 |

建议通过一个明确的入口进入/退出编辑状态；进入后显示可拖区域，退出后保持布局。可见入口与**隐藏持久数据**是两回事：用户要求隐藏的是保存的坐标/尺寸，不意味着不得提供编辑入口。入口形式与快捷键尚未产品定稿。

**为什么需要专用交互状态**：原生 `T17StandaloneInputModule.Process → ProcessMouseEvent → ProcessMousePress` 已消费左键/拖动，表情轮也直接读 `Input.GetMouseButtonDown(0)`。设置页还会持有自己的光标与玩家输入占用。因此 HUD 编辑必须处理与暂停/ModSettings/表情轮的优先级、关闭恢复和点击透传；不能给每张图片随意加可点击组件、常驻占用 EventSystem 或只改 Cursor.visible。〔`GAME/T17StandaloneInputModule.cs:241-263,552-607,624-640`；`GAME/ClientEmoteWheel.cs:141-149,209-232`；本地 ModSettings `src/Native/GlobalSettingsHost.cs:43-62`、`src/Native/NativePage.Gameplay.cs:15-30,64-77`〕

实现时可为编辑手柄使用一组明确所有权的 uGUI 命中对象，借助匹配 Canvas 的事件相机转换鼠标坐标；退出编辑就移除命中，不把变成透明当作退出。输入阻断只覆盖本地编辑所需对象，不暂停网络/其他玩家，不复用其他 MOD 的私有状态。Unity 2017.4 的 `ScreenPointToLocalPointInRectangle` 在 ScreenSpaceOverlay 下应传 null 相机，在指针事件中可取 `pressEventCamera`；转换返回 true 只说明射线命中矩形平面，不代表位于边界内部，仍需单独边框/主体命中判断。〔Unity 2017.4 官方 API，见 §9 外部一手参考〕

### 8.3 几何：改宽高和整体缩放要分别处理

**已确认**：`RectTransformExtension.LateUpdate` 每帧经 `UpdateAspectRatio` 重写 offset、anchor、rotation、pivot，并把 `localScale` 恢复为1；公开 `scale` 和 `AnchorOffset` 是原生参数，但 scale 的结果还取决于原始锚点、缩放轴和 pivot。因此仅给 transform 写 position/sizeDelta/localScale 不构成稳定方案。〔`GAME/RectTransformExtension.cs:12-38,99-109,128-177`〕

**建议**：在当前实例原生几何基础上保存用户布局。优先使用确认外层的原生布局参数；确实需要末端几何合成时，只对已登记实例在原生布局完成后应用，保持动画变换可组合，不能全局禁用 RectTransformExtension 或 Animator。缓存反射和原始几何；原生刷新/重建时更新基准，不能将已修改值当作下一次原生值。

**必须区分两个效果**：角落等比缩放可以整体缩小文字、金币/沙漏、框和条；边缘只改变宽/高时，如果直接做非等比缩放，文字和圆形图标会被拉扁。建议把“面板外框/条的宽高”与“文字/图标等比尺度”分开：拉边改变面板可用区域并重排内部锚点，图标保持比例；拉角才整体缩放。哪些节点可单独布局、背景是否9-slice、是否被 Animator 直接驱动，**仅靠控制器源码无法确定**，需要实际 prefab/运行树与几何截图，尚不承诺资源管理器式无失真拉伸已被证明。

### 8.4 隐藏持久配置

**已确认可复用现成机制**：本项目复制的 `src/ModSettingsSdk/ModSettingsAttributes.cs:23-24` 已有 `Browsable`；提供端 SDK README:50规定 false隐藏，`EntryMetadata.cs:51` 解析为Hidden，`ConfigCatalog.cs:78` 在构建可见行时排除 Hidden。因此位置尺寸仍使用原生 BepInEx ConfigEntry，绑定时传 `ModSettingsAttributes { Browsable = false }`，不用另开并行存储系统。这里是 ModSettings 本地当前源码契约，不能仅凭此断言所有第三方配置编辑器也隐藏。

建议存储模型（键名待实施时沿现有约定定稿）：

- 用稳定英文 Section/Key，绝不使用翻译后的显示名、instanceID、关卡名或可变层级路径作为身份。
- 每个实际角色保存“是否自定义”、安全区内归一化中心 X/Y、相对原始几何的宽/高倍率；必要的格式版本也隐藏。默认不启用自定义，从而不改变尚未编辑的原版布局。
- 普通分数/普通倒计时跨关卡共用；双队分数分Team角色，生存倒计时/累计时间分角色。只对实际出现的角色应用，不因少一个控件写回零，也不把Horde混用到同一坐标。
- 切换分辨率/宽高比时按当前安全区重建位置并做边界/最小尺寸约束，保存的是逻辑布局，不是上次屏幕的绝对像素；只拖尺寸不改变业务值。布局完全越界或非有限值需可恢复原位。
- 所有位置/尺寸/启用标记均 `Browsable=false`，不通过 IsAdvanced伪装隐藏，不在 ModSettings里增加同义可见副本；cfg文本仍按正常BepInEx规则保留可编辑值。
- 拖动时只改内存预览，松开后一次批量更新ConfigEntry并 `Config.Save()`；提交期间临时关闭该ConfigFile的 `SaveOnConfigSet`，finally还原原设置，避免多个键各保存一次。不能整个拖动期间关掉自动保存，影响别的设置。安装BepInEx 5.4.23.5的Cecil已确认公开Save/SaveOnConfigSet和自动保存调用链（§9）。

### 8.5 固定 HUD 的后续验收

实际操作必须补证：主体/四边/四角共9类命中、四角比例误差与对角漂移、编辑边框与实际视觉框重合、字体/图标不变形、脉冲时布局不跳、分数/倒计时及独立飘字跟随、边界/最小尺寸、点击不穿透、失焦/取消/反复进出编辑、切关/重开/重启后恢复、不同分辨率/安全区、双队/练习/生存适配。隐藏配置还需用真实 ModSettings Catalog/界面确认搜索、普通/高级和收藏视图均不显示；仅 cfg 存盘不能证明隐藏完成。当前无上述UI验收数字或截图。

## 9. 本轮证据与实施建议

第一批建议包含计分/倒计时编辑、切菜/烹饪/搅拌/炉温、洗碗和灭火条；第二批是交互/警告提示、加减分/小费浮字、重生计时；玩家脚下Mesh标记与Horde另行处理。共用位置/尺度/alpha的基础能力，按owner和角色独立配置；不复制多套展示算法，也不改业务控制器。

- 原始程序集：安装DLL与反编译侧DLL SHA256一致，MVID `114d606a-29d5-472c-8ebe-c2c20fbe3be3`，CLR `v2.0.50727`。
- 新增访问契约：Cecil实际确认40个方法签名、24个字段及public/protected/private边界，完整列表在 [JSON](evidence/hud-expansion-contracts.json)。非公开调用/读取后续必须缓存反射；该核验没有编译或安装任何新增功能。
- 安装BepInEx检查：5.4.23.5，`ConfigFile.Save()` / `get_SaveOnConfigSet()` / `set_SaveOnConfigSet(bool)`均public；私有 `OnSettingChanged(object,ConfigEntryBase)` 的IL包含读取SaveOnConfigSet并调用Save。此为存储API可用性核验，不是新布局保存测试。
- 外部一手参考（Unity 2017.4）：`https://docs.unity3d.com/2017.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html`（事件相机、平面命中语义）；`https://docs.unity3d.com/2017.4/Documentation/Manual/class-CanvasGroup.html`（组alpha与子元素alpha相乘、Ignore Parent Groups）。这些通用规则支持适配方向，不证明本游戏UI_Move自定义shader及资源的最终表现。
- 当前只形成研究与源码访问证据，未改生产代码、配置、游戏文件、版本、远端或已发布包；无需用户重启。真实实现的编译、输入/几何、跨场景持久化和多人验收均待后续工作。
