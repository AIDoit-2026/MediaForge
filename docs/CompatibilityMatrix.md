# MediaForge MVP 兼容矩阵

> 冻结日期：2026-09-10  
> 适用范围：首版 MVP 的软件编码路径。实际可见选项始终以用户当前 FFmpeg 的能力探测结果为准。

## 验收规则

- 输出容器、编码器和所需 muxer 必须全部由当前 FFmpeg 构建报告支持，否则不得创建任务。
- 默认软件路径必须可用：MP4 + `libx264` + `aac`。
- 硬件编码器不是保证项；只有实际探测成功的 NVENC、QSV、AMF 才显示为可选项。
- 一个容器至少存在一条可用音频或视频输出路径时才展示；无输入流的编码类别在任务创建时由参数验证器阻止。

## 容器与软件编码器

| 输出 | 所需 FFmpeg muxer | 视频编码器 | 音频编码器 | MVP 说明 |
| --- | --- | --- | --- | --- |
| MP4 | `mp4` | `libx264`、`libx265` | `aac` | 默认视频输出；H.264 + AAC 是首选路径。 |
| MKV | `matroska` | `libx264`、`libx265`、`libsvtav1`、`libaom-av1`、`libvpx-vp9` | `aac`、`libmp3lame`、`flac`、`libopus` | 通用归档/高质量输出。 |
| MOV | `mov` | `libx264`、`libx265` | `aac`、`alac`、`pcm_s16le` | 面向常见 MOV 工作流。 |
| WebM | `webm` | `libvpx-vp9`、`libsvtav1`、`libaom-av1` | `libopus` | Web 视频输出。 |
| MP3 | `mp3` | — | `libmp3lame` | 音频提取/音频转码。 |
| M4A | `ipod` 或 `mp4` | — | `aac` | AAC 音频输出。 |
| FLAC | `flac` | — | `flac` | 无损音频输出。 |
| WAV | `wav` | — | `pcm_s16le` | PCM 音频输出。 |
| Opus | `opus` | — | `libopus` | Opus 音频输出。 |

## 可选硬件视频编码器

| 容器 | 可选硬件编码器 | 前提 |
| --- | --- | --- |
| MP4、MKV、MOV | `h264_nvenc`、`hevc_nvenc`、`h264_qsv`、`h264_amf` | FFmpeg 报告编码器，且对应实际硬件探测任务成功。 |

硬件编码不支持两遍模式；不可用时必须隐藏或回退至用户选择的软件编码器，不能静默替换输出参数。

## 不属于首版保证矩阵

AVI、FLV、MPEG-TS 等可在未来按 FFmpeg 能力开放，但不作为内置预设、默认选项或首版逐项验收项。软字幕、ASS/SSA、内嵌字幕选择、外部音轨替换也不属于本矩阵。

## 实现对应

- 格式/编码器目录：`MediaForge.Core/Conversion/ConversionOptionCatalog.cs`
- 组合校验：`ConversionValidator`
- 任务开始前能力拦截：`FfmpegFeatureGuard` 与转换页“开始转换”操作
- 集成验证：`RealFfmpegConversionTests`
