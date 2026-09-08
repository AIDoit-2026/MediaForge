# MediaForge 媒体格式转换工具

> 将 FFmpeg 命令行包装成 Windows 界面工具  
> 完全由 AI 完成  
> 首要工具 Codex CLI，模型 GPT-5.6 Sol

MediaForge 是使用 C#、WinUI 3 和 Windows App SDK 开发的 portable 本地音视频转换工具。

应用不包含 FFmpeg。用户需要把 `ffmpeg.exe` 和 `ffprobe.exe` 放在 `MediaForge.exe` 旁边，或在设置中指定它们所在的目录。

## 项目状态

MVP 需求和设计已经确定，正在按照任务清单开发：

- [需求文档](docs/Requirement.md)
- [设计文档](docs/Design.md)
- [任务清单](docs/Tasks.md)

## 开发流程记录

- 第一轮用免费的 Grok 网页端生成需求文档 `Requirement.md`。原文为英文，Codex 阅读后翻译并重构为中文需求。
- Codex 分析需求，生成设计文档和任务清单。
- 根据产品决策冻结 MVP 范围，开始搭建 WinUI 3 工程。

## 许可证

MediaForge 使用 [MIT License](LICENSE) 开源。FFmpeg 是独立软件并使用自己的许可证，详见[第三方软件说明](THIRD_PARTY_NOTICES.md)。
