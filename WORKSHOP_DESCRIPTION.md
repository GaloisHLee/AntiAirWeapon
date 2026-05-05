# AntiAirWeapon[forked]

【中文说明】

AntiAirWeapon[forked] 是基于 AKreedz 原版 Anti-Air Weapon 的 RimWorld 1.6 fork。

这个 fork 主要用于为原防空炮模组补充更友好的目标配置方式，修复原逻辑中可能错误击落中立贸易货仓的问题，并移除 HugsLib 前置依赖。

它不是原 Workshop 项目的官方更新，也不应与原版 Anti-Air Weapon 同时启用。

功能说明：

- 修复原版拦截逻辑中可能误击中立贸易货仓的问题。
- 默认仅拦截可识别为敌对派系的飞行舱、空投舱与高空炮弹。
- 修复 RimWorld 1.6 下 Harmony 补丁目标签名变化导致的启动失败问题。
- 使用 RimWorld 原生 ModSettings 设置系统，不再需要 HugsLib。
- 保留针对特殊目标的 defName 规则配置，方便兼容其他 mod 的飞行目标。
- 在 Mod 选项中提供更友好的目标管理界面，不需要手动记忆或输入 defName。
- 支持搜索目标名称、defName、来源 mod 与目标类型。
- 支持三类规则：永不拦截、总是拦截、按敌对关系拦截。
- 最近检测到的空中目标会出现在配置窗口中，方便处理大型 mod 列表中的特殊目标。
- 已卸载 mod 留下的旧 defName 会作为缺失项保留，不会自动删除玩家配置。

前置需求：

- Harmony
- RimWorld 1.6

HugsLib 不再是前置需求。

本 fork 与原版不兼容。请不要同时启用原版和本 fork。

来源与授权：

- 原作者：AKreedz
- Fork 维护者：Cred Mao
- 原项目：https://github.com/AKreedz/AntiAirWeapon
- Package ID：credmao.antiairweapon.forked
- Workshop ID：3715925883
- 许可协议：MIT License

本 fork 保留原作者版权声明，并在 MIT License 允许范围内进行维护、修复和再发布。

【English】

AntiAirWeapon[forked] is a RimWorld 1.6 fork of the original Anti-Air Weapon mod by AKreedz.

This fork focuses on improving the configuration experience for the original anti-air turret mod, fixing an issue in the original interception logic that could cause neutral trade pods or cargo pods to be shot down incorrectly, and removing the HugsLib prerequisite.

This is not an official update of the original Workshop item, and it should not be enabled together with the original Anti-Air Weapon mod.

Features:

- Fixes an issue where the original interception logic could incorrectly shoot down neutral trade pods or cargo pods.
- By default, only intercepts drop pods, transporters, and overhead projectiles that can be identified as hostile.
- Fixes a RimWorld 1.6 Harmony startup failure caused by changed target method signatures.
- Uses native RimWorld ModSettings; HugsLib is no longer required.
- Keeps defName-based rule configuration for special targets from other mods.
- Adds a friendlier target management UI in Mod Options, so players do not need to manually remember or type defNames.
- Supports searching by target label, defName, source mod, and target type.
- Supports three rule groups: Never Intercept, Always Intercept, and Hostile Only.
- Recently detected airborne targets are shown in the configuration window, making it easier to handle special targets in large mod lists.
- Missing defNames from removed mods are preserved as missing entries instead of being deleted automatically.

Requirements:

- Harmony
- RimWorld 1.6

HugsLib is no longer required.

This fork is incompatible with the original version. Please do not enable both the original mod and this fork at the same time.

Credits and license:

- Original author: AKreedz
- Fork maintainer: Cred Mao
- Original project: https://github.com/AKreedz/AntiAirWeapon
- Package ID: credmao.antiairweapon.forked
- Workshop ID: 3715925883
- License: MIT License

This fork keeps the original copyright notice and is maintained, fixed, and redistributed under the terms of the MIT License.
