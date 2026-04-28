# 2802147499

这个目录现在已经整理成一个可直接用于 RimWorld 测试的模组根目录。

## 直接使用

你可以把整个 `2802147499/` 目录直接放到：

- RimWorld 本地 `Mods/` 目录
- 或 Steam 测试用的模组目录

游戏实际会使用的模组内容包括：

- `About/`
- `1.6/`
- `Languages/`
- `Sounds/`
- `Textures/`

## 辅助内容

- `tests/`
  人工测试清单，不影响游戏加载，保留用于验收。
- `LICENSE`
- `LICENSE.zh-CN.md`

## 说明

- 当前构建基于 `1.6/Defs` 和新编译的 `1.6/Assemblies/AntiAirWeapon.dll`。
- 如果只是进游戏测试，直接使用这个目录即可。
- 如果后续需要重新生成交付物，仍以仓库根目录的 `build-local.sh` 与 `package-release.sh` 为准。
- 仓库根目录的 `2802147499.zip` 是原始 Workshop 包，不代表当前修改版；当前修改版 zip 位于 `artifacts/release/AntiAirWeapon.zip`。
