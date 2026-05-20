## 模块说明

`Interaction` 模块负责桌面端键鼠交互层，不承载垃圾分类规则或任务结算逻辑。

当前内容包括：

- `DesktopInteractionBootstrap`：运行时自动为主摄像机挂接桌面交互组件
- `DesktopGarbageInteractor`：中心射线选中、抓取、持有、释放垃圾
- `SelectableHighlighter`：高亮当前可抓取垃圾
- `CrosshairController`：绘制屏幕中心准星

该模块直接复用主线已有的 `Player`、`GarbageItem`、`DropZone` 和分类事件流程。
