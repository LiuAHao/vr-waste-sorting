# 开发 A 先行任务：Gameplay 接口骨架

## 1. 为什么开发 A 要先做

V0.3 由三名开发并行完成，但开发 B 和开发 C 都依赖开发 A 的公共接口：

- B 需要知道哪些物体是垃圾、能不能抓取、抓取时如何标记状态。
- C 需要接收分类结果，用来更新 UI、进度、得分和结算。

因此开发 A 需要先提交一版“接口骨架 PR”。这版 PR 的目标不是把所有分类功能写完，而是先把公共类、字段、方法和事件定下来，让 B/C 可以基于同一套接口并行开发。

## 2. 第一版 PR 的目标

第一版 PR 只做 Gameplay 基础框架：

```text
Assets/Scripts/Gameplay/
├── WasteCategory.cs
├── GarbageItem.cs
├── TrashBin.cs
├── DropZone.cs
├── ClassificationResult.cs
└── ClassificationEvents.cs
```

完成后应满足：

- Unity 能正常编译。
- B 可以引用 `GarbageItem` 做选中、抓取、释放。
- C 可以订阅 `ClassificationEvents.OnClassified` 接收分类结果。
- 公共命名基本稳定，不需要后续频繁改名。

## 3. 第一版 PR 需要做什么

### 3.1 新建 `WasteCategory.cs`

定义四类垃圾枚举：

```csharp
public enum WasteCategory
{
    Recyclable,
    Hazardous,
    Kitchen,
    Other
}
```

要求：

- 枚举名使用英文。
- 不要新增其他类别。
- UI 中文显示后续由 UI 或转换方法处理。

### 3.2 新建 `GarbageItem.cs`

`GarbageItem` 挂在每个垃圾物体上。

第一版至少提供以下公开属性：

```csharp
public string ItemId { get; }
public string ItemName { get; }
public WasteCategory Category { get; }
public string WrongReason { get; }
public bool IsCompleted { get; }
public bool IsHeld { get; }
```

第一版至少提供以下公开方法：

```csharp
public bool CanInteract();
public void SetHeld(bool held);
public void MarkCompleted();
public void ResetToStartPosition();
```

建议内部字段：

```csharp
[SerializeField] private string itemId;
[SerializeField] private string itemName;
[SerializeField] private WasteCategory category;
[SerializeField] private string wrongReason;
```

状态要求：

- `CanInteract()`：未完成且未被抓取时返回 true。
- `SetHeld(true)`：标记垃圾正在被玩家抓取。
- `SetHeld(false)`：标记垃圾被释放。
- `MarkCompleted()`：标记垃圾已正确分类。
- `ResetToStartPosition()`：恢复初始位置和旋转。

第一版可以先实现简单逻辑，不需要复杂物理处理。

### 3.3 新建 `TrashBin.cs`

`TrashBin` 挂在垃圾桶上。

第一版至少提供：

```csharp
public WasteCategory Category { get; }
public string DisplayName { get; }
public bool Accepts(GarbageItem item);
```

建议内部字段：

```csharp
[SerializeField] private WasteCategory category;
[SerializeField] private string displayName;
```

`Accepts()` 只做分类比较：

```csharp
return item != null && item.Category == category;
```

不要在 `TrashBin` 中处理 UI、得分或结算。

### 3.4 新建 `ClassificationResult.cs`

用于把一次投放结果传给开发 C。

第一版至少包含：

```csharp
public GarbageItem Item { get; }
public TrashBin Bin { get; }
public bool IsCorrect { get; }
public WasteCategory CorrectCategory { get; }
public WasteCategory SelectedCategory { get; }
public string Reason { get; }
```

建议使用构造函数一次性传入这些值，避免外部随意修改。

### 3.5 新建 `ClassificationEvents.cs`

用于发布分类结果。

第一版至少提供：

```csharp
public static event Action<ClassificationResult> OnClassified;
public static void RaiseClassified(ClassificationResult result);
```

要求：

- `RaiseClassified` 内部触发 `OnClassified`。
- 如果 result 为空，可以直接忽略或输出 warning。
- C 后续会订阅 `OnClassified`。

### 3.6 新建 `DropZone.cs`

`DropZone` 挂在垃圾桶桶口或桶前方的触发区。

第一版至少提供：

```csharp
[SerializeField] private TrashBin targetBin;
```

第一版可以先实现基础触发逻辑：

1. `OnTriggerEnter(Collider other)`。
2. 获取 `GarbageItem`。
3. 如果垃圾为空、已完成、正在被抓取，则忽略。
4. 使用 `targetBin.Accepts(item)` 判断正误。
5. 生成 `ClassificationResult`。
6. 正确时调用 `item.MarkCompleted()`。
7. 触发 `ClassificationEvents.RaiseClassified(result)`。

注意：

- 如果 B 的交互设计要求“松手后才判定”，则 `DropZone` 必须忽略 `IsHeld == true` 的垃圾。
- 错误投放第一版可以先不自动复位，但必须保留重试可能。

## 4. 第一版 PR 不需要做什么

这版不要扩大范围。

不要做：

- 键盘移动。
- 鼠标视角。
- 鼠标抓取。
- UI 面板。
- 倒计时。
- 得分。
- 结算页。
- VR 手柄输入。
- 复杂投掷物理。
- 高级动画或音效。
- 12 个模型的完整场景摆放。

这些分别由开发 B、开发 C 或后续联调阶段完成。

## 5. 给开发 B 的接口

B 会依赖：

```csharp
GarbageItem.CanInteract()
GarbageItem.SetHeld(bool held)
GarbageItem.ResetToStartPosition()
GarbageItem.IsCompleted
GarbageItem.IsHeld
```

B 负责：

- 判断鼠标射线是否选中 `GarbageItem`。
- 抓取前调用 `CanInteract()`。
- 抓取时调用 `SetHeld(true)`。
- 释放时调用 `SetHeld(false)`。

B 不负责：

- 修改垃圾分类。
- 判断是否投对。
- 触发 UI 结算。

## 6. 给开发 C 的接口

C 会依赖：

```csharp
ClassificationEvents.OnClassified
ClassificationResult
```

C 负责：

- 订阅 `OnClassified`。
- 根据 `ClassificationResult.IsCorrect` 更新 UI。
- 统计正确数、错误数、得分和错误列表。
- 判断是否完成任务。

C 不负责：

- 重新比较垃圾类别和垃圾桶类别。
- 直接操作 `DropZone`。
- 直接改变垃圾状态。

## 7. 第一版 PR 验收标准

提交 PR 前自查：

- [ ] 新建了 `Assets/Scripts/Gameplay/`。
- [ ] 6 个基础文件都存在。
- [ ] Unity 编译无报错。
- [ ] `WasteCategory` 枚举可被其他脚本引用。
- [ ] `GarbageItem` 暴露 B 需要的状态和方法。
- [ ] `TrashBin.Accepts(GarbageItem item)` 可判断分类。
- [ ] `ClassificationResult` 包含 C 需要的结果信息。
- [ ] `ClassificationEvents.OnClassified` 可订阅。
- [ ] `DropZone` 能触发基础分类事件。
- [ ] 没有写 UI、得分、键鼠输入或 VR 输入。

## 8. 推荐 PR 描述

```text
本 PR 完成 V0.3 Gameplay 接口骨架：

- 新增 WasteCategory、GarbageItem、TrashBin、DropZone。
- 新增 ClassificationResult 和 ClassificationEvents。
- 提供 B 需要的垃圾交互状态接口。
- 提供 C 需要的分类结果事件。

验证方式：
- Unity 编译通过。
- 场景中给垃圾挂 GarbageItem，给垃圾桶挂 TrashBin 和 DropZone。
- 垃圾进入 DropZone 后能触发 ClassificationEvents.OnClassified。

注意：
- 本 PR 不包含键鼠抓取、UI、倒计时、结算和 VR 适配。
```

## 9. 合入后的并行开发方式

这版 PR 合入后：

- 开发 B 开始做 `PlayerController`、`DesktopGarbageInteractor`、`SelectableHighlighter`。
- 开发 C 开始做 `GameManager`、`TaskController`、`HUDController`、`FeedbackPanel`、`ResultPanel`。
- 开发 A 继续补完整分类判定细节、错误复位、DropZone 稳定性和垃圾桶反馈接口。

也就是说：A 先交付接口，不需要等 A 完整做完，B/C 就可以并行推进。

