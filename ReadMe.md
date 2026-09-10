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

## 本地验证

在仓库根目录运行下列命令，可依次完成还原、Release 构建、Core/集成测试和 x64 portable 发布：

```powershell
.\eng\verify.ps1
```

## 编译与运行

### 前置条件

- Windows 10 1809（17763）或更新版本，x64。
- 开发时需要 .NET 10 SDK，以及可用于 WinUI 3 的 Windows App SDK 开发环境。
- 运行发布版时需要预先安装 x64 .NET 10 Desktop Runtime 与 Windows App Runtime 1.8 或兼容更新版本；发布目录不再携带这些运行时。
- 若要实际转换媒体，请自行准备同一套 FFmpeg 中的 `ffmpeg.exe` 与 `ffprobe.exe`。开发仓库中的 `ffmpeg/` 仅供本机测试，未纳入 Git 或发布包。

### Debug 编译与运行

在仓库根目录执行：

```powershell
dotnet restore .\src\MediaForge.App\MediaForge.App.csproj -p:Platform=x64
dotnet build .\src\MediaForge.App\MediaForge.App.csproj -c Debug -p:Platform=x64
& .\src\MediaForge.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\MediaForge.exe
```

这是 unpackaged WinUI 3 应用；请直接启动生成的 `MediaForge.exe`，而不是使用安装包或 MSIX 部署流程。

### Release framework-dependent 发布与运行

```powershell
dotnet publish .\src\MediaForge.App\MediaForge.App.csproj -c Release -p:Platform=x64
& .\artifacts\publish\framework-dependent-win-x64\MediaForge.exe
```

发布结果位于 `artifacts\publish\framework-dependent-win-x64\`，可整体复制到任意可写目录运行。首次运行会在该目录创建 `config\`，用于保存设置、队列和日志。此发布方式依赖目标电脑已安装的 x64 .NET 10 Desktop Runtime 与 Windows App Runtime，未安装时应用无法启动。

将 `ffmpeg.exe` 和 `ffprobe.exe` 一同复制到 `MediaForge.exe` 所在目录；或者启动应用后在“设置”页指定二者所在的目录。应用不会从 PATH 查找、下载或更新 FFmpeg。

### 测试

完整的还原、Release 构建、单元测试、集成测试和 portable 发布：

```powershell
.\eng\verify.ps1
```

仅运行测试时：

```powershell
dotnet test .\tests\MediaForge.Core.Tests\MediaForge.Core.Tests.csproj -c Debug -p:Platform=x64
dotnet test .\tests\MediaForge.IntegrationTests\MediaForge.IntegrationTests.csproj -c Debug -p:Platform=x64
```

## 许可证

MediaForge 使用 [MIT License](LICENSE) 开源。FFmpeg 是独立软件并使用自己的许可证，详见[第三方软件说明](THIRD_PARTY_NOTICES.md)。
