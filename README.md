# AntiAirWeapon

`AntiAirWeapon` 是一个面向 RimWorld 的防空炮模组源码仓库，核心目标是让地图上的防空设施能够识别并拦截空中目标，例如高空炮弹、飞行舱与部分 `Skyfaller` 类目标。

本仓库当前主要包含核心 C# 逻辑代码，适合阅读、二次开发、移植与兼容性调整。

## 功能概览

- 通过 Harmony 监听飞行物生成，将高空炮弹与飞行舱类目标纳入全局缓存。
- 防空炮按射程、供电、冷却、燃料与屋顶状态决定是否开火。
- 默认仅拦截敌对飞行舱与敌对炮弹，避免误击中立贸易货仓。
- 支持通过设置页目标选择器管理 `defName` 规则，兼容其他模组的特殊飞行物。
- 记录最近检测到的空中目标，方便在大型 mod 列表中快速定位需要配置的对象。

## 当前拦截策略

代码中的默认策略如下：

- `Projectile`：尝试根据 `Launcher.Faction` 识别派系，仅拦截敌对目标。
- `IActiveTransporter`：优先读取目标自身派系；若为空，则尝试从舱内物品或单位推断派系，仅拦截敌对目标。
- 普通 `Skyfaller`：默认不拦截，除非在设置中显式启用，或被自定义名单覆盖。

这套规则的目的，是在保持防空炮有效性的同时，尽量避免误伤友方、中立事件和其他模组的特殊空投。

## 模组设置

当前源码已加入以下可配置项：

- `拦截敌对飞行舱`
- `拦截敌对炮弹`
- `拦截未知 Skyfaller`
- `永不拦截 defName`
- `总是拦截 defName`
- `按敌对关系拦截 defName`

三个 `defName` 列表仍然作为真实存储格式保留，旧配置可以继续读取。设置页现在默认显示摘要行，并通过 `管理...` 按钮打开目标选择器，不需要玩家手动记忆或输入大段 `defName`。

目标选择器支持：

- 搜索目标名称、`defName`、来源 mod、目标类型。
- 按 `全部`、`炮弹`、`飞行舱/Skyfaller`、`最近检测到`、`已选择`、`缺失项` 筛选。
- 每页最多显示前 200 条候选，候选过多时提示继续搜索缩小范围，避免大型 mod 列表下明显卡顿。
- 在三组规则之间移动目标时自动互斥：加入当前列表会从另外两个列表移除。
- 未安装 mod 产生的旧 `defName` 会作为 `缺失项` 保留并显示，不会被自动删除。
- 设置页的 `高级` 区域可展开原始文本编辑，支持逗号、分号、换行、中文逗号、中文分号分隔。

推荐用法：

- 将贸易货仓、友方运输目标加入 `永不拦截 defName`
- 将某些危险事件飞行物加入 `总是拦截 defName`
- 将具备明确派系概念的第三方模组飞行物加入 `按敌对关系拦截 defName`

## 仓库结构

- [About](About): 模组元数据、预览图与 Workshop 发布编号
- [1.6](1.6): 当前 RimWorld 1.6 使用的 `Defs` 与编译产物目录
- [Languages](Languages): 多语言文本资源
- [Sounds](Sounds): 模组音效资源
- [Textures](Textures): 贴图资源
- [Buildings](Buildings): 防空炮主体与炮塔顶部相关源码
- [Settings](Settings): 目标选择器、候选扫描、规则管理与最近检测到目标的设置 UI
- [AntiAirWeaponModBase.cs](AntiAirWeaponModBase.cs): HugsLib 入口、设置注册与全局配置缓存
- [HarmonyHere.cs](HarmonyHere.cs): Harmony 补丁与飞行物收集入口
- [AllMapProjectileStorage.cs](AllMapProjectileStorage.cs): 全地图飞行物缓存与最近检测到目标持久化

## 开发说明

- 仓库当前已经恢复为标准 RimWorld 模组源码结构。
- 根目录资源包含 `About`、`Languages`、`Sounds`、`Textures`。
- 版本相关内容当前使用 `1.6/Defs` 与构建出的 `1.6/Assemblies`。
- 本地编译依赖与中间产物使用 `.gitignore` 排除，包括 `References/`、`dist/`、`obj/`、`artifacts/`。
- 如需扩展兼容性，优先从 `Building_AirDefense` 中的目标识别与策略判断逻辑入手。
- 目标选择器的候选列表由缓存服务生成，UI 不应在每帧重扫 `DefDatabase`。
- HugsLib 设置 UI 只使用已在 RimWorld 1.6 环境校验过的成员：`SettingHandle.CustomDrawer`、`CustomDrawerHeight`、`SettingHandle<T>.Value`、`StringValue`。
- 为保持 1.6 构建兼容，不使用 `CustomDrawerFullWidth`、`ForceSaveChanges` 等跨引用版本不稳定的 API。
- Harmony 使用 Harmony 2，不再回退到 Harmony 1 的 `HarmonyInstance` API。

## 开发日志

阶段性修复与实现记录见 [DEVELOPMENT_LOG.md](DEVELOPMENT_LOG.md)，当前包含：

- RimWorld 1.6 启动黑屏、mod 列表重置与 XML 编码问题修复。
- Harmony 2 / HugsLib 1.6 构建引用整理。
- 目标选择器 UI、候选缓存、缺失项保留与最近检测到目标记录。

## 本地编译

当前仓库已经补充了本地工程文件与构建脚本：

- `AntiAirWeapon.sln`
- `AntiAirWeapon.csproj`
- `build-local.sh`

编译前需要准备以下引用：

- `References/RimWorld/Managed/`
  - `Assembly-CSharp.dll`
  - `UnityEngine.dll`
  - `UnityEngine.CoreModule.dll`
  - `UnityEngine.IMGUIModule.dll`
  - `UnityEngine.TextRenderingModule.dll`
- `References/HugsLib/`
  - `HugsLib.dll`
- `References/Harmony/`
  - `0Harmony.dll`

RimWorld 1.6 构建会优先使用本地 Steam/Workshop 中的 1.6 依赖路径；如果没有对应路径，再使用 `References/` 下的手动引用。HugsLib 建议使用 Workshop `818773962/v1.6/Assemblies/HugsLib.dll`，避免误引用其他游戏版本的 DLL。

准备完成后，可直接运行：

```bash
./build-local.sh
```

若需要 `Release` 配置，可运行：

```bash
./build-local.sh Release
```

默认输出目录为 `dist/Assemblies/`。

## Release 打包

仓库已提供基础 release 元数据与打包脚本：

- `About/About.xml`
- `1.6/Defs/`
- `package-release.sh`

在引用齐全并能够成功编译后，执行：

```bash
./package-release.sh
```

脚本会自动：

- 以 `Release` 配置编译 DLL
- 生成 `artifacts/release/AntiAirWeapon/`
- 复制 `About`、`Languages`、`Sounds`、`Textures`
- 将 `1.6/Defs` 与新编译的 `1.6/Assemblies/AntiAirWeapon.dll` 一起打包
- 尝试额外生成 `artifacts/release/AntiAirWeapon.zip`
- 同步刷新 `2802147499/`，这个目录可以直接复制到 RimWorld/Steam 的本地模组目录进行游戏内测试

注意：仓库根目录的 `2802147499.zip` 是原始 Workshop 包，用来恢复资源；当前修改版的 zip 产物是 `artifacts/release/AntiAirWeapon.zip`。

## 许可证

本项目采用 `MIT License`。

- 正式许可文本见 [LICENSE](LICENSE)
- 中文说明见 [LICENSE.zh-CN.md](LICENSE.zh-CN.md)

中文说明仅用于阅读与理解，若与正式许可文本存在差异，以英文 `LICENSE` 为准。
