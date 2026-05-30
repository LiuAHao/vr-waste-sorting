# ParkClean VR — 项目结构设计大纲（VR 分支）

> 本文档描述 `VRadjustment` 分支的工程结构现状。
> 桌面端 `main` 分支的早期规划请参考 Git 历史版本。

---

## 1. 当前项目结构

```
vr-waste-sorting/
├── Assets/
│   ├── Art/
│   │   ├── GarbageItems/          # 12 个垃圾物品 GLB 模型
│   │   └── TrashBins/             # 4 个分类垃圾桶 GLB 模型
│   ├── Resources/                 # PICO XR SDK 调试资源
│   ├── Samples/
│   │   └── XR Interaction Toolkit/2.6.5/Starter Assets/
│   │       ├── Prefabs/
│   │       │   ├── XR Interaction Setup.prefab   # XR Rig 主 Prefab（场景中使用）
│   │       │   ├── XR Origin (XR Rig).prefab     # XR Origin（已调整移动/转向/重力）
│   │       │   └── Controllers/                   # 左右手手柄 Prefab
│   │       └── Scripts/
│   │           └── ActionBasedControllerManager.cs # 平滑转向控制
│   ├── Scenes/
│   │   └── 2.unity                # 唯一游戏场景
│   ├── Scripts/
│   │   ├── Analytics/             # 行为数据追踪
│   │   ├── Core/
│   │   │   ├── WasteGameBootstrap.cs      # 游戏主引导（UI 初始化、流程、VR 暂停键）
│   │   │   ├── WasteGameFlowController.cs # 游戏流程状态机
│   │   │   └── ...（各游戏模式控制器）
│   │   ├── Gameplay/
│   │   │   ├── GarbageItem.cs     # 垃圾物品组件（分类属性、持有/完成状态）
│   │   │   ├── DropZone.cs        # 垃圾桶投放区（触发判定，支持 VR 物理投掷）
│   │   │   └── ...（得分、关卡配置等）
│   │   ├── Interaction/
│   │   │   ├── VRGarbageInteractor.cs      # VR 抓取桥接（新增）
│   │   │   ├── VRInteractionBootstrap.cs   # VR 自动初始化（新增）
│   │   │   └── DesktopInteractionBootstrap.cs # 桌面端（VR 下禁用）
│   │   └── UI/
│   │       ├── WasteUiFactory.cs       # UI 工厂（VR 模式自动 World Space）
│   │       ├── VRCanvasFollower.cs     # Canvas 跟随摄像机组件（新增）
│   │       ├── VRCanvasHelper.cs       # Canvas 转换辅助（已被 Factory 替代）
│   │       ├── WasteHudView.cs         # 游戏局内 HUD
│   │       ├── WastePauseView.cs       # 暂停菜单
│   │       ├── WasteStartView.cs       # 标题页
│   │       ├── WasteResultView.cs      # 结算页
│   │       └── ...（其余 UI 视图）
│   └── XR/
│       └── Settings/                  # OpenXR 配置（Controller Profile 等）
├── Packages/
│   └── manifest.json                  # 包含 XRI 2.6.5、OpenXR、PICO XR SDK
├── ProjectSettings/
│   └── ProjectSettings.asset          # 包含 XR Plug-in Management 配置
└── docs/
    ├── README.md（即本文件）
    ├── PRD.md
    ├── PROJECT_STRUCTURE.md
    ├── design/
    ├── plans/
    └── v1/
```

---

## 2. 核心脚本职责

### 2.1 VR 新增脚本（`VRadjustment` 分支独有）

| 脚本 | 路径 | 职责 |
|---|---|---|
| `VRGarbageInteractor` | `Scripts/Interaction/` | 桥接 XRI Select 事件到游戏抓取逻辑；管理抓取位置（摄像机前方 0.6m）；追踪手柄速度用于投掷 |
| `VRInteractionBootstrap` | `Scripts/Interaction/` | RuntimeInitialize 自动初始化；为场景中的 Interactor 挂载 VRGarbageInteractor；为 GarbageItem 添加 XRGrabInteractable；禁用 TunnelingVignette |
| `VRCanvasFollower` | `Scripts/UI/` | MonoBehaviour，挂在所有 World Space Canvas 上；LateUpdate 检测摄像机朝向变化，超过阈值时平滑跟随；激活时立即 Snap 到摄像机正前方 |

### 2.2 VR 适配修改的脚本

| 脚本 | 修改内容 |
|---|---|
| `WasteUiFactory` | `CreateCanvasRoot()` 检测 XR 活跃状态，VR 下创建 World Space Canvas（1920×1080 参考尺寸，scale=0.001），自动挂 `TrackedDeviceGraphicRaycaster` 和 `VRCanvasFollower` |
| `WasteHudView` | `SetVisible(true)` 时调用 `SnapToCamera()`，确保 HUD 在游戏开始时出现在玩家视野内 |
| `WastePauseView` | `Show()` 时调用 `SnapToCamera()`；按钮和面板尺寸放大以便射线点击 |
| `WasteGameBootstrap` | `Update()` 增加 VR 暂停按键检测（左手柄 X 键上升沿触发）；暂停提示文字更新为 VR 操作说明 |
| `DropZone` | 新增 `OnTriggerEnter` 支持 VR 投掷后物理飞入触发判定 |
| `DesktopInteractionBootstrap` | 检测 XR 活跃状态，VR 下跳过桌面端初始化 |
| `Player` | 检测 XR 活跃状态，VR 下禁用键鼠输入逻辑 |

### 2.3 XR Rig 配置（Prefab 修改）

**`XR Origin (XR Rig).prefab`**：
- `ContinuousMoveProvider.m_MoveSpeed = 5`（移动速度，原为 1）
- `ActionBasedControllerManager.m_SmoothTurnEnabled = 1`（平滑连续转向，替代 Snap Turn）
- `ContinuousTurnProvider.m_TurnSpeed = 90`（转向速度）
- 所有 `DynamicMoveProvider.m_UseGravity = 0`（禁用重力，PICO4 使用 Floor 追踪）

**`XR Interaction Setup.prefab`（场景实例覆盖）**：
- `Camera Offset.m_LocalPosition.y = 1.1`（摄像机高度降至 1.1m，原为 1.41m）
- `XRRayInteractor.m_InteractionManager`：绑定到场景中的 `XR Interaction Manager`

---

## 3. UI 架构

所有 UI 均通过 `WasteUiFactory.CreateCanvasRoot()` 代码生成，不依赖场景内预置 Canvas。

```
CreateCanvasRoot(name)
  ├── 桌面模式 → Screen Space Overlay Canvas
  │     └── CanvasScaler (ScaleWithScreenSize, 1920×1080)
  │     └── GraphicRaycaster
  │
  └── VR 模式 → World Space Canvas
        ├── RectTransform.sizeDelta = (1920, 1080)
        ├── localScale = (0.001, 0.001, 0.001)
        ├── TrackedDeviceGraphicRaycaster (XRI, maxDistance=10)
        └── VRCanvasFollower (distance=2.5m, snap on activate)
```

**创建时机**：`WasteGameBootstrap.Awake()` 统一创建所有视图，`DontDestroyOnLoad` 保持跨场景。

---

## 4. 垃圾投放判定流程

```
VR 抓取流程：
XRRayInteractor 射线检测 GarbageItem
  → SelectEntered 事件
    → VRGarbageInteractor.OnSelectEntered()
      → 设置 Rigidbody Kinematic，禁用重力
      → item.SetHeld(true)
  → Update 每帧（LateUpdate）
      → 物品位置 Lerp 到 Camera.main.forward * 0.6m（摄像机前方固定位置）
  → SelectExited 事件
    → VRGarbageInteractor.OnSelectExited()
      → Rigidbody 恢复动态，施加手柄速度向量
      → item.SetHeld(false)
      → DropZone.TryClassifyReleasedItem()（即时判定）

物理飞入判定：
GarbageItem.Rigidbody（动态）飞行
  → 进入 DropZone.Collider（isTrigger）
    → DropZone.OnTriggerEnter()
      → 验证 item 未被持有、未被判定
      → Classify(item)（正误判定 + 反馈 + 计分）
```

---

## 5. 分支对比

| 功能 | `main`（桌面端）| `VRadjustment`（VR）|
|---|---|---|
| 输入 | 键鼠（WASD + 鼠标） | PICO4 手柄（XRI OpenXR）|
| 抓取 | 鼠标点击 + 射线跟随 | 手柄 Ray/Direct Interactor + Grip |
| UI 模式 | Screen Space Overlay | World Space（跟随摄像机）|
| 移动 | Player.cs WASD | ActionBasedControllerManager 左摇杆 |
| 转向 | 鼠标 | 右摇杆连续转向 |
| 暂停 | ESC 键 | 左手柄 X 键 |
| 物理 | 标准 | 禁用重力（Floor 追踪），投掷速度计算 |
