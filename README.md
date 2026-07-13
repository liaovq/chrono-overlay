# ChronoOverlay

ChronoOverlay 是一个轻量、可锁定、始终置顶的 Windows 桌面悬浮时钟。

[项目官网](https://liaovq.github.io/chrono-overlay/) · [下载最新版](https://github.com/liaovq/chrono-overlay/releases/latest)

## 功能

- 24 小时制时间与中文日期，始终显示秒
- 时间和日期严格右对齐，数字使用等宽字体
- 独立调整时间字号、日期字号、文字颜色与背景浓度
- 在系统等宽、JetBrains Mono、IBM Plex Mono 与 Space Mono 之间即时切换并自动保存
- 控制面板接近屏幕底边时自动翻到时钟上方，锁定按钮始终可操作
- 解锁时自由拖动，锁定后固定坐标
- 锁定后背景、日期和空白区域鼠标穿透
- 时间数字区域保留双击解锁热点
- 始终置顶、系统托盘、开机自启与单实例运行
- 自动恢复位置、样式、锁定和自启状态
- 多显示器、负坐标与 Per-Monitor V2 DPI 支持
- 不联网、不收集数据、不包含遥测

## 使用

1. 安装微软官方的 [.NET 8 Desktop Runtime（Windows x64）](https://dotnet.microsoft.com/download/dotnet/8.0)。已经安装 .NET 8 SDK 的电脑无需重复安装。
2. 从 [GitHub Releases](https://github.com/liaovq/chrono-overlay/releases) 下载最新的版本化 EXE。
3. 双击 EXE 直接运行；ChronoOverlay 是便携式程序，没有单独的安装向导。
4. 调整样式和位置后点击“锁定”。
5. 双击时间数字，或从系统托盘选择“显示面板 / 解锁”。
6. 从托盘菜单选择“第三方许可”，可在完全离线状态下查看并复制内置字体的完整 SIL OFL 1.1 文本。
7. 从托盘菜单选择“退出程序”以真正结束进程。

锁定状态采用选择性鼠标穿透。背景、日期和空白区域不会阻挡下方软件；时间数字区域会拦截点击，以便接收双击解锁操作。

## 系统支持

- Windows 10/11 x64
- .NET 8 Desktop Runtime x64
- 普通桌面窗口和常见无边框全屏窗口始终置顶
- 不保证覆盖 UAC、安全桌面、Windows 锁屏或独占全屏游戏

## 开发

需要 .NET 8 SDK、Node.js 20+ 和 pnpm。

```powershell
dotnet restore ChronoOverlay.sln
dotnet build ChronoOverlay.sln -c Release --no-restore
dotnet test ChronoOverlay.sln -c Release --no-build

cd site
pnpm install --frozen-lockfile
pnpm run build
```

发布 Windows 单文件版本：

```powershell
dotnet publish src/ChronoOverlay/ChronoOverlay.csproj `
  -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:PublishTrimmed=false
```

## 配置

配置保存在：

```text
%LOCALAPPDATA%\ChronoOverlay\config.json
```

配置采用防抖和原子写入。损坏的配置会备份为 `config.corrupt-时间戳.json`，应用随后使用安全默认值启动。详见 [配置说明](docs/configuration.md)。

## 发布

所有改动通过 PR。PR CI 验证 Windows 构建、测试、单文件发布预检、网站构建和版本规则；合并到 `main` 后自动创建版本标签、GitHub Release、版本化 EXE 并部署 GitHub Pages。详见 [发布说明](docs/releasing.md)。

## 隐私与许可证

ChronoOverlay 不访问网络、不收集或上传任何数据。项目代码采用 [MIT License](LICENSE)；随应用分发的字体及其 OFL 1.1 许可见 [第三方声明](THIRD-PARTY-NOTICES.md)。完整许可文本同时嵌入发布 EXE，可从托盘菜单“第三方许可”离线查看。
