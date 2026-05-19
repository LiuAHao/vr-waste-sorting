# 开发 A 任务安排：核心分类逻辑与垃圾桶判定

## 1. 任务定位

开发 A 负责 V0.3 MVP 的“分类规则核心”。这个模块决定每个垃圾属于什么类别、每个垃圾桶接受什么类别、垃圾进入桶口后如何判定正误，以及如何把判定结果交给 UI 和任务流程模块。

本任务不负责玩家移动、鼠标抓取、UI 面板和结算页。你的代码应尽量独立，后续无论输入来自鼠标还是 VR 手柄，分类逻辑都可以复用。

## 2. 前置条件

开发正式开始前，需要统筹侧先交付 V0.3 资产包：

- 12 个垃圾模型。
- 4 个垃圾桶模型。
- 垃圾数据表：ID、中文名、分类、错误解释、模型状态。
- 优先接入的 8 个基础垃圾名单。

如果资产未完全就绪，可以先用简单几何体或现有垃圾模型验证逻辑，但脚本字段和命名要按最终数据表设计，避免后续大改。

## 3. 需要达成的目标

### 必须完成

1. 定义四类垃圾枚举：可回收物、有害垃圾、厨余垃圾、其他垃圾。
2. 每个垃圾物体可配置名称、分类、错误解释、初始位置和完成状态。
3. 每个垃圾桶可配置桶类型和中文显示名称。
4. 每个垃圾桶有一个投放触发区，可以检测垃圾进入。
5. 垃圾进入触发区后能判断分类是否正确。
6. 正确投放后，垃圾进入已完成状态，不能重复计分。
7. 错误投放后，垃圾不进入完成状态，可继续重试。
8. 判定结果能以事件或公开方法形式传给开发 C 的任务/UI 模块。

### 尽量完成

- 垃圾投错后可以调用 `ResetToStartPosition()` 回到初始位置。
- 垃圾桶可以提供简单高亮接口，例如 `SetHighlight(bool active)`，供后续反馈使用。
- 在 Inspector 里配置字段时有清晰中文或英文提示。

### 不做

- 不做玩家移动。
- 不做鼠标抓取。
- 不做 UI 排版。
- 不做结算统计。
- 不做 VR 输入。
- 不做复杂物理投掷和动画。

## 4. 建议代码修改区域

只新增或修改以下区域：

```text
Assets/Scripts/Gameplay/
```

建议新增文件：

```text
Assets/Scripts/Gameplay/WasteCategory.cs
Assets/Scripts/Gameplay/GarbageItem.cs
Assets/Scripts/Gameplay/TrashBin.cs
Assets/Scripts/Gameplay/DropZone.cs
Assets/Scripts/Gameplay/ClassificationResult.cs
Assets/Scripts/Gameplay/ClassificationEvents.cs
```

如果目录不存在，先创建目录。不要继续把新逻辑写进旧的 `Assets/Scripts/Garbage.cs`。旧脚本可以保留，等新逻辑稳定后再决定是否弃用。

## 5. 建议实施步骤

### 第 1 步：建立分类枚举

创建 `WasteCategory.cs`：

```csharp
public enum WasteCategory
{
    Recyclable,
    Hazardous,
    Kitchen,
    Other
}
```

开发规范：

- 枚举名用英文，避免中文枚举在代码中造成输入和编码问题。
- UI 显示中文由 `TrashBin.displayName` 或工具方法转换，不直接依赖枚举名。

### 第 2 步：实现垃圾物品组件

创建 `GarbageItem.cs`，挂在每个垃圾物体上。

建议内部字段与公开属性：

```text
string itemId -> public string ItemId { get; }
string itemName -> public string ItemName { get; }
WasteCategory category -> public WasteCategory Category { get; }
string wrongReason -> public string WrongReason { get; }
bool isCompleted -> public bool IsCompleted { get; }
bool isHeld -> public bool IsHeld { get; }
Vector3 startPosition
Quaternion startRotation
Rigidbody rb
```

建议方法：

```text
InitializeStartTransform()
MarkCompleted()
CanInteract()
ResetToStartPosition()
SetHeld(bool held)
```

实现要点：

- `Start()` 或 `Awake()` 记录初始位置和旋转。
- `MarkCompleted()` 后设置内部完成状态，并让 `IsCompleted` 返回 true，可禁用 Collider 或隐藏物体。
- `ResetToStartPosition()` 用于错误重试，恢复位置、旋转和速度。
- 不要在 `GarbageItem` 中直接加分或打开 UI。

### 第 3 步：实现垃圾桶组件

创建 `TrashBin.cs`，挂在垃圾桶主体上。

建议内部字段与公开属性：

```text
WasteCategory category -> public WasteCategory Category { get; }
string displayName -> public string DisplayName { get; }
Renderer highlightRenderer
Color normalColor
Color correctColor
Color wrongColor
```

建议方法：

```text
bool Accepts(GarbageItem item)
string GetCategoryDisplayName()
void ShowCorrectFeedback()
void ShowWrongFeedback()
void ClearFeedback()
```

实现要点：

- `Accepts()` 只通过公开属性比较 `item.Category == Category`。
- 如果暂时不做高亮，反馈方法可以先空实现，但保留接口。
- 不要在 `TrashBin` 里查找 UI 或 GameManager。

### 第 4 步：实现投放触发区

创建 `DropZone.cs`，挂在垃圾桶桶口或桶前方的 Trigger Collider 上。

建议字段：

```text
TrashBin targetBin
bool classifyOnTriggerEnter
```

建议逻辑：

1. `OnTriggerEnter(Collider other)`。
2. 从 `other` 或 `other.GetComponentInParent<GarbageItem>()` 获取垃圾。
3. 如果垃圾为空、已完成、正在被玩家抓取，则忽略。
4. 调用分类判断。
5. 触发全局分类结果事件。

注意：

- Trigger Collider 要勾选 `Is Trigger`。
- 垃圾物体需要 Collider，通常还需要 Rigidbody 才能稳定触发 Unity 物理事件。
- 为了避免重复触发，可在 `GarbageItem` 中记录完成状态，正确后立即标记，让 `IsCompleted` 返回 true。

### 第 5 步：定义分类结果数据

创建 `ClassificationResult.cs`。

建议公开属性：

```text
GarbageItem Item
TrashBin Bin
bool IsCorrect
WasteCategory CorrectCategory
WasteCategory SelectedCategory
string Reason
```

用途：

- 开发 C 用它更新 UI、统计错误、判断任务进度。
- 开发 B 不需要理解里面的统计逻辑，只要保证垃圾能进入触发区。

### 第 6 步：定义事件出口

可以创建 `ClassificationEvents.cs`，使用 C# event 或 UnityEvent。

建议简单接口：

```text
public static event Action<ClassificationResult> OnClassified;
```

开发规范：

- 事件只表示“分类判定完成”，不要在事件类里做 UI 或统计。
- 触发事件前要确保 `ClassificationResult` 信息完整。
- 如果使用静态事件，注意在订阅方 `OnEnable` 订阅、`OnDisable` 取消订阅。

## 6. 与其他开发的接口

### 提供给开发 B

开发 B 的键鼠交互需要调用或读取：

- `GarbageItem.CanInteract()`：判断是否还能抓取。
- `GarbageItem.SetHeld(bool held)`：抓取时标记状态。
- `GarbageItem.ResetToStartPosition()`：必要时复位。

开发 B 不应直接修改：

- `Category`
- `WrongReason`
- `IsCompleted`，除非通过公开方法。

### 提供给开发 C

开发 C 需要订阅分类结果：

```text
ClassificationEvents.OnClassified(ClassificationResult result)
```

开发 C 依赖的数据：

- `result.Item.ItemName`
- `result.Item.ItemId`
- `result.IsCorrect`
- `result.CorrectCategory`
- `result.SelectedCategory`
- `result.Reason`

开发 C 不应自己重新判断分类正误。

## 7. 开发规范

### 代码规范

- 类名、方法名、字段名使用英文。
- Inspector 可配置字段使用 `[SerializeField] private`，必要时提供只读属性。
- 不把 UI、输入、任务统计写进 Gameplay 模块。
- 不在 `Update()` 中做无意义查找。
- 不频繁使用 `FindObjectOfType`，跨模块通信优先用事件或明确引用。
- 复杂逻辑前可以写短注释，但不要写重复解释型注释。

### Unity 组件规范

- 每个垃圾 Prefab 至少有：模型、Collider、Rigidbody、`GarbageItem`。
- 每个垃圾桶至少有：模型、`TrashBin`、子物体 `DropZone`。
- `DropZone` 的 Collider 必须是 Trigger。
- 垃圾桶和垃圾物体的 Layer 不要随意改，除非和开发 B 协商 Raycast 层。

### 数据配置规范

- 分类以统筹数据表为准。
- 错误解释文案以统筹数据表为准。
- 8 个基础垃圾先完整配置，再补 4 个扩展垃圾。
- 不自行增加新垃圾类别。

## 8. 验收清单

开发 A 完成后，需要能通过以下检查：

- [ ] 四类垃圾枚举存在且所有脚本共用同一个枚举。
- [ ] 至少 8 个垃圾物体能在 Inspector 中配置名称、分类、解释。
- [ ] 四个垃圾桶能在 Inspector 中配置类别。
- [ ] 每个垃圾桶的 DropZone 能触发分类判断。
- [ ] 正确投放触发 `ClassificationResult.IsCorrect = true`。
- [ ] 错误投放触发 `ClassificationResult.IsCorrect = false`，并返回原因。
- [ ] 正确投放后同一垃圾不会重复触发计分。
- [ ] 错误投放后同一垃圾仍可再次尝试。
- [ ] 分类结果事件能被开发 C 订阅。
- [ ] 代码没有依赖鼠标、键盘、VR 手柄或具体 UI 面板。

## 9. 常见风险

| 风险 | 处理方式 |
| --- | --- |
| Trigger 不触发 | 检查 Collider、Is Trigger、Rigidbody、Layer 碰撞矩阵 |
| 正确垃圾重复计分 | 正确后立即 `MarkCompleted()`，DropZone 忽略 completed 垃圾 |
| 投错后垃圾卡在桶里反复触发 | 错误后调用复位，或给 DropZone 加短暂冷却 |
| UI 收不到结果 | 检查事件订阅时机，使用 `OnEnable`/`OnDisable` |
| 开发 B 无法抓取 | 确认 `CanInteract()` 没有误判，Collider 没被提前禁用 |
