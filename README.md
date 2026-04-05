# ItemSearch 全服物品搜索

- 作者: 星梦
- 出处: `https://github.com/ICU-Club`
- 这是一个TShock服务器插件，主要用于全服物品搜索与批量管理，支持在线/离线玩家背包查询、箱子查询、模糊搜索、数量筛选和物品移除功能
- 支持物品ID或中文名称模糊匹配（如"泰拉"匹配泰拉刃、泰拉马桶等）
- 支持数量条件筛选（如>500查询持有超过500个物品的玩家）
- 支持一键移除指定物品，数量参数可选（不填默认移除全部）

> [!NOTE]
> 使用有关离线玩家的命令前，请确保您的服务器的SSC是开启状态

## 指令

| 语法 | 别名 | 权限 | 说明 |
|------|------|------|------|
| `/is search <物品名/ID> [数量条件]` | `/is all` | `itemsearch.player.search` | 查询所有玩家 |
| `/is player <物品名/ID> [数量条件]` | `/is online` | `itemsearch.player.search` | 仅查询在线玩家 |
| `/is ssc <物品名/ID> [数量条件]` | `/is offline` | `itemsearch.ssc.search` | 仅查询离线玩家（SSC）|
| `/is box <物品名/ID>` | `/is chest` | `itemsearch.box.search` | 查询世界中的箱子 |
| `/is del player <玩家/*all> <物品名/ID> [数量]` | 无 | `itemsearch.player.remove` | 移除在线玩家物品（不填数量=全部）|
| `/is delssc <玩家/*all> <物品名/ID> [数量]` | 无 | `itemsearch.ssc.remove` | 移除离线玩家物品（SSC）|
| `/is delchest <箱子ID/*all> <物品名/ID> [数量]` | 无 | `itemsearch.box.remove` | 移除箱子中的物品 |

> 权限给予方法：`/group addperm [组名] [权限名]`
> 例如：`/group addperm default itemsearch.player.search` 给予玩家查询物品的权限

### 数量条件语法

- `>500` - 大于500个
- `<100` - 小于100个
- `=50` 或 `50` - 等于50个
- `>=1000` - 大于等于1000个

## 配置
> 配置文件位置：tshock/ItemSearch.json

```json5

{
  "插件指令名": "is",
  "指令权限": {
    "查询在线背包": "itemsearch.player.search",
    "删除在线背包": "itemsearch.player.remove",
    "查询箱子": "itemsearch.box.search",
    "删除箱子物品": "itemsearch.box.remove",
    "查询离线玩家": "itemsearch.ssc.search",
    "删除离线玩家": "itemsearch.ssc.remove"
  },
  "提示消息": {
    "查询结果头": "[c/00FFFF:物品搜索] 查询结果:",
    "发现物品格式": "  {0}: [i/s{1}:{2}] ×{3}",
    "箱子坐标格式": "  箱子 #{0} 位于 X:{1} Y:{2} - [i/s{3}:{4}] ×{5}",
    "删除成功": "[c/00FF00:物品搜索] 已成功从 {0} 移除 {1} 个 [i:{2}]",
    "玩家不在线": "[c/FF0000:物品搜索] 玩家 {0} 不在线",
    "离线玩家不存在": "[c/FF0000:物品搜索] 数据库中未找到玩家 {0}",
    "箱子索引无效": "[c/FF0000:物品搜索] 无效的箱子索引: {0}",
    "SSC未启用": "[c/FF0000:物品搜索] 服务器未启用SSC，无法操作离线玩家",
    "参数错误": "[c/FF0000:物品搜索] 参数错误，用法: {0}",
    "操作进度": "[c/00FFFF:物品搜索] 正在处理... 已完成 {0}/{1}",
    "批量完成": "[c/00FF00:物品搜索] 批量操作完成，共影响 {0} 个目标"
  },
  "性能设置": {
    "每帧处理箱子数": 50,
    "每帧处理玩家数": 5,
    "启用异步扫描": true
  }
}

```

## 更新日志

### v1.0.1 (2026-4-5)
- 新增模糊搜索功能，支持中文物品名称匹配
- 新增数量条件筛选（>500等）
- 新增仅数量查询模式:`/is search >500` 查询所有超量物品）
- 优化信息显示，按在线/离线分栏显示
- 移除操作数量参数改为可选，不填默认全部移除


### v1.0.0 (2026-4-4)
- 初始版本发布

## 反馈
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：`816771079` -> 星梦：`1011819146`
