# 余宠

Windows 桌面宠物，用来盯 DeepSeek API 余额。角色坐在气泡上，总额一眼能看见；悬停再看赠送 / 充值。余额偏低或用完时，表情会跟着变。

只支持 Windows（WPF）。密钥存在本机 `%AppData%\DeepSeekPet\settings.json`，用 DPAPI 加密，不会进仓库。

## 功能

- 定时拉取 [DeepSeek 余额接口](https://api.deepseek.com/user/balance)，展示总额、赠送、充值
- 低于阈值、余额为 0、密钥无效时切换表情，并用托盘气泡提醒
- 贴边吸附；收起后只露出角色。收起时可沿边滑动，往屏幕里拉出约 40px 后自由拖动
- 记住上次是自由、停靠还是收起，下次启动按这个状态还原
- 悬停点「赠送 · 充值」，或右键 / 托盘选「打开用量页」，跳转到 [用量页](https://platform.deepseek.com/usage)
- 开机启动、置顶、透明度、缩放、刷新间隔、低余额阈值可在设置里改

## 环境

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（`8.0.400` 或同功能带，见 `global.json`）

## 运行

仓库根目录：

```powershell
dotnet run --project src\DeepSeekPet.App\DeepSeekPet.App.csproj
```

Release：

```powershell
dotnet run --project src\DeepSeekPet.App\DeepSeekPet.App.csproj -c Release
```

编译后的程序：

```powershell
.\src\DeepSeekPet.App\bin\Release\net8.0-windows\DeepSeekPet.exe
```

首次启动如果还没填 API Key，会弹出设置窗口。密钥在 [DeepSeek 开放平台](https://platform.deepseek.com) 创建。

## 操作

| 动作 | 效果 |
| --- | --- |
| 拖角色 | 移动窗口；靠近边缘会吸附 |
| 点气泡空白处 | 立即刷新（有短暂冷却） |
| 悬停气泡 | 展开赠送 / 充值；点这一行打开用量页 |
| 双击角色 | 收起 / 展开边缘 |
| 右键角色或托盘 | 刷新、打开用量页、设置、退出 |

## 项目结构

```
DeepSeekPet.sln
src/DeepSeekPet.App/     WPF 窗口、角色、托盘
src/DeepSeekPet.Core/    余额、吸附、设置
tools/                   托盘图标等小工具
```

余额刷新在 `BalanceMonitor`，吸附在 `SnapService`，设置读写在 `SettingsStore`。

## 许可证

个人学习与自用。DeepSeek 是对应服务的商标，与本项目无官方关系。
