# 测试媒体样本

集成测试不提交第三方媒体二进制文件。`RealFfmpegConversionTests` 在临时目录中用 FFmpeg 的
`lavfi` 输入生成一秒钟的 `testsrc2` 视频与 `sine` 音频；两者均为程序合成内容，可重复生成且不含
外部版权素材。

该测试在仓库根目录存在用户提供的 `ffmpeg/ffmpeg.exe` 和 `ffmpeg/ffprobe.exe` 时运行，否则明确标记为跳过。
它验证视频转视频、视频提取音频和音频转音频的输出能被同一工具集的 FFprobe 重新读取。
