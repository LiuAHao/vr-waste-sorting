# 开发 C 任务安排：任务流程、UI 反馈与结算统计

## 1. 任务定位

开发 C 负责 V0.3 MVP 的“游戏流程和展示层”。本模块管理倒计时、目标进度、得分、正确/错误提示、错误记录、结算面板和重开流程。

本任务不负责垃圾分类判断，也不负责玩家抓取。你的模块只接收开发 A 发出的分类结果，然后更新任务状态和 UI。

## 2. 前置条件

开发正式开始前，最好已经具备：

- 开发 A 提供的分类结果事件或等价接口。
- 开发 B 提供的基本键鼠可玩场景。
- 统筹侧提供的垃圾名称、分类中文名、错误解释文案。

如果开发 A 的事件暂未完成，可以先做一个临时按钮或测试方法模拟分类结果，但最终必须改为订阅正式分类结果。

## 3. 需要达成的目标

### 必须完成

1. 游戏开始时初始化倒计时、目标数量、得分和统计数据。
2. 游戏中显示倒计时、进度、得分或正确数。
3. 接收一次分类结果后，根据正误更新数据。
4. 正确投放后显示正向提示，并增加完成进度。
5. 错误投放后显示错误原因，并记录错误物品。
6. 完成目标数量后进入成功结算。
7. 倒计时结束后进入失败结算。
8. 结算页显示完成状态、得分、正确率、用时、错误列表和环保影响短文案。
9. 支持重新开始本轮。

### 尽量完成

- 错误列表去重或展示错误次数。
- 正确/错误提示 2-3 秒后自动消失。
- 结算页有“重玩”和“退出/返回”按钮。
- UI 字号和位置适合后续 VR 迁移，不要太小。

### 不做

- 不做联网排行榜。
- 不做长期数据存储。
- 不做 A/B Testing。
- 不做复杂成就系统。
- 不做多场景选择菜单。
- 不做 VR 空间 UI 适配。
- 不做复杂动画和音效系统。

## 4. 建议代码修改区域

只新增或修改以下区域：

```text
Assets/Scripts/Core/
Assets/Scripts/UI/
Assets/Scripts/Analytics/
```

建议新增文件：

```text
Assets/Scripts/Core/GameManager.cs
Assets/Scripts/Core/TaskController.cs
Assets/Scripts/UI/HUDController.cs
Assets/Scripts/UI/FeedbackPanel.cs
Assets/Scripts/UI/ResultPanel.cs
Assets/Scripts/Analytics/SessionStats.cs
Assets/Scripts/Analytics/WrongAttemptRecord.cs
```

旧的 `Assets/Scripts/Player.cs` 里有倒计时、清理计数和胜负面板逻辑。可以参考其思路，但不建议继续把任务/UI 逻辑放在 Player 里。

## 5. 建议实施步骤

### 第 1 步：定义游戏状态

在 `GameManager.cs` 或 `TaskController.cs` 中定义状态：

```text
Ready
Playing
Success
Failed
Paused
```

V0.3 至少需要：

- `Ready`：未开始或刚加载。
- `Playing`：正在分类。
- `Success`：目标完成。
- `Failed`：倒计时结束。

开发规范：

- 状态变化由 Core 模块统一管理。
- UI 只根据状态显示/隐藏，不自己决定游戏是否结束。

### 第 2 步：实现任务控制器

创建 `TaskController.cs`。

建议字段：

```text
int targetCount = 8 或 12
float totalTime = 180f
float remainingTime
int completedCount
int correctCount
int wrongCount
int score
GameState currentState
```

建议方法：

```text
StartTask()
TickTimer(float deltaTime)
HandleClassificationResult(ClassificationResult result)
CompleteTask()
FailTask()
RestartTask()
```

实现要点：

- 只有 `Playing` 状态才减少倒计时。
- 正确分类才增加 `completedCount`。
- 错误分类只增加错误次数和错误记录，不增加完成数。
- 完成数达到目标时立即成功。
- 时间归零时失败。

### 第 3 步：订阅分类结果

开发 A 会提供类似事件：

```text
ClassificationEvents.OnClassified
```

开发 C 应在 `OnEnable()` 订阅，在 `OnDisable()` 取消订阅。

处理逻辑：

```text
收到 result
-> 如果当前不是 Playing，忽略
-> 如果 result.isCorrect，增加进度和分数
-> 如果错误，记录错误物品和原因
-> 更新 HUD
-> 显示正确或错误反馈
-> 检查是否完成任务
```

注意：

- 不要在 UI 脚本中重新判断分类。
- 不要因为错误投放结束游戏。
- 不要让同一个已完成垃圾重复增加进度，这一点也需要和开发 A 的 `isCompleted` 配合。

### 第 4 步：实现游戏中 HUD

创建 `HUDController.cs`。

HUD 最少显示：

```text
倒计时：180
进度：0/8 或 0/12
得分：0
当前提示：分类正确 / 分类错误
```

建议方法：

```text
UpdateTimer(float seconds)
UpdateProgress(int completed, int total)
UpdateScore(int score)
ShowMessage(string text)
ClearMessage()
```

UI 规范：

- 文字要短，字号要大。
- 不遮挡垃圾和垃圾桶。
- 先用 Unity UI Text 或 TextMeshPro 均可，但不要混乱使用多套 UI。
- 如果使用 TextMeshPro，确保项目中包已可用。

### 第 5 步：实现反馈面板

创建 `FeedbackPanel.cs`。

正确提示示例：

```text
分类正确：塑料瓶属于可回收物
```

错误提示示例：

```text
分类错误：污染纸巾应投入其他垃圾。污染纸巾不可回收，应投入其他垃圾。
```

建议方法：

```text
ShowCorrect(string itemName, string categoryName)
ShowWrong(string itemName, string categoryName, string reason)
Hide()
```

规范：

- 错误提示必须包含“正确类别”和“原因”。
- 提示自动消失时间建议 2-3 秒。
- 不要用强惩罚式文案，保持教学口吻。

### 第 6 步：实现统计数据

创建 `SessionStats.cs` 和 `WrongAttemptRecord.cs`。

`SessionStats` 建议字段：

```text
int totalTarget
int completedCount
int correctCount
int wrongCount
int score
float elapsedTime
List<WrongAttemptRecord> wrongAttempts
```

`WrongAttemptRecord` 建议字段：

```text
string itemId
string itemName
WasteCategory correctCategory
WasteCategory selectedCategory
string reason
```

正确率计算建议：

```text
correctRate = correctCount / max(1, correctCount + wrongCount)
```

注意：

- V0.3 只做内存统计，不需要写文件。
- 如果一个物品多次投错，可以先全部记录；结算页空间不够时只显示前 3-5 条。

### 第 7 步：实现结算面板

创建 `ResultPanel.cs`。

结算页显示：

- 成功或失败。
- 得分。
- 正确率。
- 用时。
- 完成数量。
- 错误次数。
- 错误物品列表。
- 环保影响短文案。
- 重玩按钮。

环保影响文案示例：

```text
本轮你完成了 8 件垃圾分类。继续练习易混淆物品，可以减少现实生活中的错误投放。
```

如果正确率较高：

```text
本轮分类表现很好。稳定识别常见垃圾，是减少资源浪费的第一步。
```

如果错误较多：

```text
本轮有一些易混淆物品。复盘错误原因后，再试一次会更接近真实分类习惯。
```

### 第 8 步：实现重开流程

重开需要恢复：

- 倒计时。
- 分数。
- 进度。
- 错误记录。
- UI 显示。
- 所有垃圾的状态和位置。

如果开发 A 提供 `GarbageItem.ResetToStartPosition()`，重开时可以遍历场景内垃圾并调用。更稳的方式是由 `GameManager` 统一重载当前场景，但 V0.3 可以先用简单场景重载。

推荐优先级：

1. 时间紧：使用 `SceneManager.LoadScene(currentScene)` 重载。
2. 时间够：写 `ResetTask()` 并复位所有垃圾。

## 6. 与其他开发的接口

### 依赖开发 A

需要订阅：

```text
ClassificationEvents.OnClassified(ClassificationResult result)
```

需要读取：

```text
result.item.itemId
result.item.itemName
result.isCorrect
result.correctCategory
result.selectedCategory
result.reason
```

不应该做：

- 不重新比较 `item.category == bin.category`。
- 不直接修改垃圾分类。
- 不直接控制 DropZone。

### 依赖开发 B

开发 C 不依赖开发 B 的抓取实现，只要分类结果能触发即可。

可选接口：

```text
ShowHoverItemName(itemName)
ClearHoverItemName()
```

如果开发 B 不需要显示物品名，可以不做。

### 提供给其他模块

可以提供：

```text
bool IsPlaying
void ShowCorrect(...)
void ShowWrong(...)
void RestartTask()
```

但建议其他模块不要直接改任务数据，只通过分类结果事件驱动。

## 7. 开发规范

### 代码规范

- 任务状态只在 Core 模块中改变。
- UI 脚本只负责显示，不负责分类判定。
- 统计数据放到 Analytics 模块，不散落在多个 UI 脚本里。
- 订阅事件必须取消订阅，避免重开后重复触发。
- 不在 `Update()` 中频繁查找 UI 对象。
- 不把中文文案硬编码到很多脚本里，集中在 Feedback 或数据配置处。

### UI 规范

- UI 先清楚，再美观。
- 字体不宜过小。
- 游戏中 UI 不遮挡中心抓取区域。
- 结算页信息不要太多，错误列表最多先显示 3-5 条。
- 按钮文案简短，例如“重新开始”“返回”。

### 数据规范

- 正确率使用正确次数和总尝试次数计算。
- 错误记录要包含物品名、正确分类、错误投放分类和原因。
- 不要因为重试成功而删除之前的错误记录，复盘需要保留学习过程。

## 8. 验收清单

开发 C 完成后，需要能通过以下检查：

- [ ] 游戏开始后倒计时正常减少。
- [ ] HUD 显示倒计时、进度、得分。
- [ ] 正确分类后进度增加。
- [ ] 错误分类后进度不增加。
- [ ] 错误分类后显示正确类别和原因。
- [ ] 错误记录能进入结算页。
- [ ] 达到目标数量后显示成功结算。
- [ ] 倒计时结束后显示失败结算。
- [ ] 结算页显示正确率、用时、得分、错误列表。
- [ ] 重开后数据不会沿用上一轮。
- [ ] UI/任务代码没有重新实现分类规则。
- [ ] 事件订阅不会在重开或切场景后重复触发。

## 9. 常见风险

| 风险 | 处理方式 |
| --- | --- |
| 正确投放后进度加两次 | 和开发 A 确认 `isCompleted`，同时检查事件是否重复订阅 |
| 错误投放也结束任务 | 错误只记录和提示，不增加完成数 |
| 结算页正确率不合理 | 使用 `correctCount / (correctCount + wrongCount)`，注意除零 |
| 重开后数据残留 | 统一通过 `StartTask()` 初始化所有统计字段 |
| UI 收不到分类结果 | 检查事件订阅、对象激活状态、脚本执行顺序 |
| UI 文字太多看不清 | 游戏中只显示短提示，详细错误放结算页 |

