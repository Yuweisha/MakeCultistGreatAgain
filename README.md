# MCGA 让咔咔再次伟大

《杀戮尖塔 2》模组：把一代的邪教徒头套等元素带回二代。开局在涅奥面前多出三个选择，选完照常进入涅奥的原生遗物三选一。

创意工坊：https://steamcommunity.com/sharedfiles/filedetails/?id=3807672118

## 内容

开局涅奥首屏新增三个选项：

| 选项 | 效果 |
| --- | --- |
| 你感觉你的脸被人被换了。 | 获得遗物 **邪教徒头套** |
| 你发现你身上插了一把小刀。 | 获得遗物 **仪式匕首**，并立即获得 3 层「仪式」、最大生命值 -10 |
| 似乎什么也没有发生。 | 不拿任何东西，直接回到涅奥原生选择页 |

两个遗物都带**战斗开场鸟叫**：每场战斗开始时随机播放三段鸟叫之一，延迟 2 秒，同时角色头顶弹出红色抖动对话气泡「我的力量无人能及！」。

## 构建

需要：

- .NET 9 SDK
- Godot 4.5.1（mono 版）
- 《杀戮尖塔 2》本体（0.111.x）

`MakeCultistGreatAgain.csproj` 里有两处本机路径，按自己的安装位置改：

```xml
<Sts2Dir>D:\OtherFiles\SteamLibrary\steamapps\common\Slay the Spire 2</Sts2Dir>
<GodotPath>C:\Games\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe</GodotPath>
```

构建：

```bash
dotnet build MakeCultistGreatAgain.csproj -c Debug
```

构建会自动导出 `MakeCultistGreatAgain.pck`，并把 `dll / pck / json` 三件套复制到游戏的 `mods\MakeCultistGreatAgain\` 目录，重启游戏即可生效。

手动安装则把这三件套放进同样的目录。

## 目录结构

```
src/                                   C# 源码
  MainFile.cs                          入口，注册遗物与 Harmony 补丁
  CrowAudio.cs                         Godot AudioStreamPlayer 播放鸟叫
  Patches/NeowCrowChoicePatch.cs       涅奥首屏三选项注入
  Relics/                              两个遗物
MakeCultistGreatAgain/
  localization/{zhs,eng}/              简体中文 / 英文本地化
  sounds/                              三段鸟叫 ogg
  images/relics/                       遗物图标
MakeCultistGreatAgain.json             模组清单
```

本地化文件是**扁平点分键**的 `Dictionary<string,string>`，不要写成嵌套对象，否则游戏解析本地化表时会抛异常。

## 说明

模组 id 为 `MakeCultistGreatAgain`，游戏按它拼接 `MakeCultistGreatAgain.dll`、`MakeCultistGreatAgain.pck` 以及 `res://MakeCultistGreatAgain/...` 资源路径，改名时需要一并修改。

作者：BuenaVista

## 许可

[MIT](LICENSE)
