# 开发 C 任务安排：任务流程、UI 反馈与结算统计

## 1. 任务定位

开发 C 负责把整个垃圾分类过程组织成一个完整回合，包括：

- 开局初始化
- 倒计时
- 得分与进度更新
- 正确/错误即时反馈
- 错误记录
- 结算展示
- 重开流程

这一层不负责垃圾桶判定，也不负责玩家抓取实现。

## 2. 当前主干对应实现

当前 `main` 中已经对应到以下脚本：

- `Assets/Scripts/Core/WasteGameBootstrap.cs`
- `Assets/Scripts/Core/WasteGameFlowController.cs`
- `Assets/Scripts/Core/WasteGameSceneConfig.cs`
- `Assets/Scripts/UI/WasteHudView.cs`
- `Assets/Scripts/UI/WasteResultView.cs`
- `Assets/Scripts/UI/WasteUiFactory.cs`
- `Assets/Scripts/UI/WasteCategoryText.cs`
- `Assets/Scripts/Analytics/WasteAnalyticsTracker.cs`

## 3. 当前已经完成的流程

主流程现在已经具备：

1. 运行时自动创建主流程入口。
2. 进入场景后自动绑定 HUD、结算页和统计对象。
3. 根据场景中的垃圾数量和配置初始化回合。
4. 回合中更新剩余时间、得分和完成进度。
5. 收到分类事件后区分正确和错误。
6. 正确投放加分并推进目标。
7. 错误投放扣分、提示原因并复位垃圾。
8. 达成目标或超时后进入结算。
9. 支持重开当前场景。

## 4. 当前需要优先修好的点

这一层当前最需要收口的是：

1. 用户可见文案必须保持正常编码。
2. 旧 HUD 不能与新 HUD 同时叠层显示。
3. 结算页信息要清晰，包括正确数、错误数、正确率、用时和错误记录。
4. 场景内不应保留测试监听对象干扰正式演示。

## 5. 正确的行为边界

这一层应该坚持：

- 只消费 `ClassificationEvents.OnClassified`
- 不在 UI 层重写分类判断
- 不在玩家控制脚本里写任务状态
- 统计数据统一由流程层维护

## 6. 后续可继续优化的方向

不阻塞 MVP，但可以排进下一阶段：

1. 更稳定的旧 UI 清理方式，不再靠文本匹配。
2. 更明确的成功/失败视觉效果。
3. 更丰富但仍克制的结算文案。
4. 一份独立的 MVP 验收清单，作为每次回归标准。

## 7. 验收清单

- [ ] 开场后 HUD 正常显示时间、得分、进度
- [ ] 正确投放后显示正向反馈
- [ ] 错误投放后显示正确类别和原因
- [ ] 错误记录能进入结算页
- [ ] 达成目标后显示成功结算
- [ ] 超时后显示失败结算
- [ ] 重开后数据归零
- [ ] 不出现旧版 HUD 叠层
