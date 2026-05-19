# UI 模块说明

本模块负责运行时 UI 的创建与展示：

- `WasteHudView` 展示倒计时、分数、进度和即时反馈
- `WasteResultView` 展示结算统计、错误记录和环保提示
- `WasteUiFactory` 统一创建 Canvas、文本、按钮和面板

当前实现使用运行时动态创建的 UGUI，不依赖手工搭场景 UI。
