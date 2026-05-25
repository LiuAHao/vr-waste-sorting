# ParkClean VR V1 玩法扩展总览

## 1. 文档定位

本文档定义 ParkClean VR 在 V1 阶段的三条玩法扩展方向、公共技术边界、协作方式和最终合入要求。

V1 不再停留在“单一 MVP 闭环”，而是在当前主干已经完成的垃圾分类主流程基础上，扩展三种可以并行开发、最终统一合入的玩法模式：

- 限时挑战
- 无尽刷分
- 标准闯关

三条线继续沿用 V0.3 的协作思路：

- 独立功能分支开发
- 公共接口先约定后扩展
- 每条线尽量只改自己的目录和配置
- 最终通过 PR 审核后合入 `main`

## 2. V1 版本目标

V1 的核心目标不是继续扩大资产规模，而是把现有垃圾分类主循环扩展为“可重复游玩、可展示差异化玩法、可支持答辩说明”的玩法集合。

V1 完成后应满足：

1. 主干现有垃圾分类闭环保持可用。
2. 三条玩法模式都能单独运行和验收。
3. 三条玩法复用同一套垃圾分类核心，不重复定义分类规则。
4. 结算层可以展示每种模式对应的成绩和基础统计。
5. 文档、分支和代码边界足够清楚，方便三人并行开发。

## 3. 当前主干可复用基础

截至当前 `main` 分支，以下能力已经具备，可作为三条玩法的共同底座：

- `Assets/Scripts/Gameplay/`
  - `GarbageItem`
  - `TrashBin`
  - `DropZone`
  - `ClassificationResult`
  - `ClassificationEvents`
- `Assets/Scripts/Core/`
  - `WasteGameBootstrap`
  - `WasteGameFlowController`
  - `WasteGameSceneConfig`
- `Assets/Scripts/Interaction/`
  - `DesktopGarbageInteractor`
  - `SelectableHighlighter`
  - `CrosshairController`
- `Assets/Scripts/Analytics/`
  - `WasteAnalyticsTracker`
  - `ClassificationRecord`
  - `WasteSessionSummary`
- `Assets/Scripts/UI/`
  - `WasteStartView`
  - `WasteHudView`
  - `WasteResultView`

V1 玩法扩展必须建立在这套主干结构上，不应重新复制一套“分类事件、垃圾物体、成绩统计、结算 UI”。

## 4. 三条玩法定义

### 4.1 限时挑战

固定时间内尽可能完成更多正确分类。场景中保持一定数量垃圾，玩家每正确或错误处理一件，系统再补充新的随机垃圾，直到时间结束。

玩法重点：

- 压力来自倒计时
- 成绩核心是总分、正确数、正确率
- 强调持续处理能力

### 4.2 无尽刷分

以“持续生成 + 难度递增 + 追求高分”为核心。垃圾会不断补充，随着时间推进或分数提高，垃圾种类池逐步扩大、易混淆物比例上升。

玩法重点：

- 重玩性最强
- 强调持续稳定分类
- 适合做最高分和最长生存表现展示

### 4.3 标准闯关

按关卡配置固定目标数量、可出现垃圾池、时间限制和难度梯度。玩家逐关完成，后续关卡逐步增加垃圾种类和更紧的时间限制。

玩法重点：

- 最适合展示“产品主线”
- 更贴近教学训练产品
- 可作为答辩和演示默认模式

## 5. 推荐文档与分支对应关系

| 开发 | 玩法方向 | 推荐分支名 | 对应文档 |
| --- | --- | --- | --- |
| 开发 A | 限时挑战 | `codex/feature-v1-timed-challenge` | `docs/v1/限时挑战-技术实现文档.md` |
| 开发 B | 无尽刷分 | `codex/feature-v1-endless-score` | `docs/v1/无尽刷分-技术实现文档.md` |
| 开发 C | 标准闯关 | `codex/feature-v1-stage-progression` | `docs/v1/标准闯关-技术实现文档.md` |

要求：

- 不要直接在 `main` 上开发。
- 不要在一个分支中混入另外两条玩法的主要逻辑。
- 如果公共接口必须改动，先更新文档约定，再同步其他开发。

## 6. 共享技术原则

### 6.1 复用主干，不重复造轮子

三条线都必须复用现有：

- 垃圾分类规则
- 投放判定事件
- 桌面抓取交互
- 基础统计结构
- 程序化 UI 体系

禁止：

- 重新定义另一套 `WasteCategory`
- 复制一份新的 `GarbageItem`
- 在玩法脚本中直接写死垃圾桶判定逻辑
- 为了某个玩法单独复制一整套 HUD 和结算系统

### 6.2 玩法差异主要放在模式层

V1 的主要差异应集中在“模式控制层”，而不是底层分类系统。

推荐新增目录：

```text
Assets/Scripts/Modes/
    TimedChallenge/
    EndlessScore/
    StageProgression/
```

每条线尽量把新增逻辑控制在自己的子目录中。

### 6.3 配置优先于写死

三条玩法都可能涉及以下可配置参数：

- 总时间
- 初始场景垃圾数量
- 垃圾种类池
- 刷新频率
- 关卡目标数量
- 分数规则
- 失败条件

这些配置优先挂在配置组件或 ScriptableObject 上，不要散落在多个脚本常量中。

## 7. 建议的公共扩展点

为减少三线冲突，V1 推荐通过以下共享扩展点承接玩法差异：

### 7.1 模式配置

新增模式配置对象，例如：

- `WasteModeConfig`
- `TimedChallengeConfig`
- `EndlessScoreConfig`
- `StageProgressionConfig`

用途：

- 统一管理不同玩法的参数
- 支持同一场景切换不同模式
- 避免把 V1 参数挤进 `WasteGameSceneConfig` 一个类中

### 7.2 模式控制器

新增模式控制器接口，例如：

```csharp
public interface IWasteGameMode
{
    void Initialize();
    void BeginSession();
    void Tick(float deltaTime);
    void HandleClassification(ClassificationResult result);
    bool ShouldFinish(out string finishReason);
}
```

不要求完全按这个接口实现，但三条线都应沿着“模式层控制开始、过程、结束”的方向扩展。

### 7.3 垃圾生成与回收

限时挑战和无尽刷分都会涉及“动态补垃圾”，建议抽出公共生成器能力，例如：

- `WasteSpawnManager`
- `WasteSpawnPoint`
- `WastePoolRuntime`

标准闯关也可以复用同一套生成器，只是改为“按关卡规则生成固定数量”。

### 7.4 结果扩展

当前 `WasteSessionSummary` 适合 MVP，但 V1 建议支持扩展字段，例如：

- `ModeId`
- `HighestCombo`
- `MostMistakenItemId`
- `MostMistakenItemName`
- `MistakeCountsByItem`
- `StageIndex`
- `ClearedStageCount`

新增字段应以“向后兼容”方式扩展，避免破坏当前结果页直接读取逻辑。

## 8. 展示与数据扩展要求

V1 需要在结算和答辩展示层补充基础数据能力。三条玩法都建议至少支持：

- 正确数
- 错误数
- 正确率
- 用时或剩余时间
- 最终得分
- 错误最多的垃圾
- 错误类别分布

扩展建议：

- 限时挑战：展示“单位时间处理效率”
- 无尽刷分：展示“最高连续正确次数”或“坚持时长”
- 标准闯关：展示“通关关数 / 当前关完成情况”

## 9. 开发边界建议

| 公共区域 | 允许修改原则 |
| --- | --- |
| `Assets/Scripts/Gameplay/` | 仅做兼容式扩展，不重写现有分类主逻辑 |
| `Assets/Scripts/Core/` | 允许为模式切换扩展入口，但不要把三条玩法全塞进同一个超大类 |
| `Assets/Scripts/Analytics/` | 允许补统计字段，但需要保持旧主流程可运行 |
| `Assets/Scripts/UI/` | 允许扩展模式差异化展示，但要复用现有 UI 工厂和基础视图 |
| `Assets/Scripts/Interaction/` | 原则上不改核心抓取逻辑，除非玩法确实需要 |

推荐每条线主改目录：

```text
Assets/Scripts/Modes/TimedChallenge/
Assets/Scripts/Modes/EndlessScore/
Assets/Scripts/Modes/StageProgression/
```

## 10. 联调规则

三条玩法虽然分线开发，但最终必须能在同一项目里共存。

联调要求：

1. 同一主干场景不能因为接入某一玩法而破坏另外两条玩法。
2. 公共 UI 和公共分类逻辑修改要保持回归可测。
3. 每条线都需要提供“如何进入该模式”的说明。
4. 所有模式都要能完成一轮从开始到结算的演示。

## 11. PR 与验收要求

每条玩法线提交 PR 时，至少需要说明：

- 本玩法完成了哪些规则
- 修改了哪些主要目录
- 新增了哪些配置对象
- 如何在 Unity 中进入该模式
- 如何完成一轮验证
- 当前已知限制

统一验收标准：

1. 模式可以正常开始。
2. 玩家可以抓取和投放垃圾。
3. 分类结果能正确计入该模式的规则。
4. 模式结束条件正确触发。
5. 结算页能显示该模式的关键成绩。
6. 不会破坏现有主干的基础垃圾分类流程。

## 12. V1 执行规范补充

本节为 V1 阶段新增的强制执行规范，用于避免再次出现“分支来源错误、PR base 错误、提交 Unity/IDE 生成文件、视频已录但代码未正确提交”的问题。

### 12.1 分支创建规范

所有 V1 玩法分支必须从最新 `main` 创建。

标准流程：

```bash
git checkout main
git pull --ff-only origin main
git checkout -b codex/feature-v1-xxx
```

禁止：

- 从其他功能分支再切新分支
- 从旧的 `feature-desktop-interaction`、`feature-task-ui-flow` 之类历史分支继续分叉
- 在未同步最新 `main` 的情况下直接开始开发

### 12.2 PR 目标分支规范

V1 三条玩法线的 PR 必须统一提向 `main`。

禁止：

- 提向另一个玩法分支
- 提向历史开发分支
- 不说明依赖关系地使用 stacked PR

如果确实要做 stacked PR，必须在 PR 描述里写清楚：

- 当前 PR 依赖哪个上游分支
- 最终如何进入 `main`
- review 时应该只关注哪些增量改动

没有上述说明时，一律按“base 选错”处理。

### 12.3 禁止提交的文件

以下内容属于本地生成文件或 IDE 辅助文件，默认禁止提交：

- `.vsconfig`
- `.vs/`
- `.vscode/`
- `*.csproj`
- `*.sln`
- `UpgradeLog.htm`
- 其他 Unity / IDE 自动生成且 `.gitignore` 已忽略的文件

如果 PR 中出现这些文件，默认视为提交污染，必须先清理后再 review。

### 12.4 提交前强制自检

每次提交前至少执行以下命令：

```bash
git status --short
git diff --name-only
git diff --cached --name-only
git check-ignore -v .vsconfig *.csproj *.sln 2>/dev/null
```

自检要求：

1. 改动文件必须能和当前玩法目标对应。
2. 必须能看到实际代码、配置、场景或文档变更，而不是只有工程生成文件。
3. 暂存区中的文件必须是准备提交的真实实现内容。
4. 如果出现被 `.gitignore` 命中的文件，不能强行提交，除非团队明确批准。

### 12.5 提审前核对清单

开发者在提 PR 前，必须自己确认：

1. 当前分支是否从最新 `main` 创建。
2. 当前 PR 的 base 是否是 `main`。
3. 本次提交是否真正包含目标玩法的代码。
4. 是否误把本地工程文件、IDE 文件、升级日志带入提交。
5. PR 描述里是否写清楚进入模式的方法和验证方式。
6. 如果录了视频，视频内容是否和 PR 中实际提交的代码一致。

### 12.6 视频不能替代代码

录屏、截图、演示视频可以作为辅助验证材料，但不能替代代码审查本身。

如果出现以下情况，PR 仍然视为不合格：

- 有视频，但分支里没有对应实现代码
- 有视频，但 PR 只有 README 或工程文件改动
- 视频展示的是本地未提交版本

结论原则：

- review 只以 PR 中实际可审查的代码、配置、场景和文档为准
- 视频只能证明“本地可能跑过”，不能证明“代码已经正确提交”

### 12.7 错误提交后的修复流程

如果已经误提交了 `.csproj`、`.sln`、`.vsconfig` 等文件，或 PR base 选错，应按以下顺序修复：

1. 在本地分支上撤销错误提交，但保留本地修改。
2. 清理误加入暂存区的工程生成文件。
3. 确认真正功能代码仍在工作区。
4. 重新只提交正确文件。
5. 必要时强制更新远端分支，并重新发起指向 `main` 的 PR。

禁止直接把错误 PR 硬合入，再指望后续修复。

## 13. V1 文档索引

- [限时挑战-技术实现文档](./限时挑战-技术实现文档.md)
- [无尽刷分-技术实现文档](./无尽刷分-技术实现文档.md)
- [标准闯关-技术实现文档](./标准闯关-技术实现文档.md)
