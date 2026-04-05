using TShockAPI;

namespace ItemSearch
{
    public static class RemoveCommands
    {
        public static void RemoveOnline(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            
            // 新用法：/is del player <玩家名/*all> <物品名称/ID> [数量，不填=全部]
            if (args.Parameters.Count < 4)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is del player <玩家名/*all> <物品名称/ID> [数量]");
                return;
            }

            string target = args.Parameters[2];
            string itemInput = args.Parameters[3];
            int amount = 0; // 0表示全部移除
            
            if (args.Parameters.Count > 4 && !int.TryParse(args.Parameters[4], out amount))
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 数量必须是数字");
                return;
            }

            // 解析物品
            if (!ItemUtils.TryParseItem(itemInput, out int itemId, out string itemName))
            {
                if (!string.IsNullOrEmpty(itemName))
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配: {itemName}");
                else
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到物品: {itemInput}");
                return;
            }

            bool all = target == "*all*";

            if (!all)
            {
                var plr = TShock.Players.FirstOrDefault(p => p?.Active == true && 
                    p.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
                
                if (plr == null)
                {
                    args.Player.SendErrorMessage(string.Format(cfg.Msg.PlayerOffline, target));
                    return;
                }

                int removed = PlayerInventoryUtils.RemoveFromOnline(plr, itemId, amount);
                string msg = amount == 0 ? "全部" : $"{removed}个";
                args.Player.SendSuccessMessage($"[c/00FF00:物品搜索] 已从 {plr.Name} 移除 {ItemUtils.FormatItem(itemId, itemName)} x{msg}");
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    int totalPlayers = 0;
                    int totalRemoved = 0;
                    
                    var players = TShock.Players.Where(p => p?.Active == true).ToList();
                    foreach (var plr in players)
                    {
                        int removed = PlayerInventoryUtils.RemoveFromOnline(plr, itemId, amount);
                        if (removed > 0)
                        {
                            totalPlayers++;
                            totalRemoved += removed;
                            plr.SendInfoMessage($"管理员移除了你的 {ItemUtils.FormatItem(itemId, itemName)} x{removed}");
                        }
                    }

                    string msg = amount == 0 ? "全部" : $"{totalRemoved}个";
                    args.Player.SendSuccessMessage($"[c/00FF00:物品搜索] 已从 {totalPlayers}位在线玩家 移除 {msg} {itemName}");
                });
            }
        }

        public static void RemoveChests(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            
            // 新用法：/is delchest <箱子ID/*all> <物品名称/ID> [数量，不填=全部]
            if (args.Parameters.Count < 3)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is delchest <箱子ID/*all> <物品名称/ID> [数量]");
                return;
            }

            string idStr = args.Parameters[1];
            string itemInput = args.Parameters[2];
            int amount = 0;

            if (args.Parameters.Count > 3 && !int.TryParse(args.Parameters[3], out amount))
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 数量必须是数字");
                return;
            }

            // 解析物品
            if (!ItemUtils.TryParseItem(itemInput, out int itemId, out string itemName))
            {
                if (!string.IsNullOrEmpty(itemName))
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配: {itemName}");
                else
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到物品: {itemInput}");
                return;
            }

            if (idStr == "*all*")
            {
                var progress = new Progress<(int current, int total)>(p =>
                {
                    if (p.current % 100 == 0)
                        args.Player.SendInfoMessage($"[c/00FFFF:物品搜索] 处理中... {p.current}/{p.total}");
                });

                int perTick = cfg.Perf.ChestsPerTick;
                if (amount == 0) perTick = 100; // 全部移除时加快速度

                _ = ChestUtils.RemoveFromAllAsync(itemId, amount, perTick, progress)
                    .ContinueWith(t =>
                    {
                        var (chests, total) = t.Result;
                        string msg = amount == 0 ? "全部" : $"{total}个";
                        args.Player.SendSuccessMessage($"[c/00FF00:物品搜索] 已从 {chests}个箱子 移除 {msg} {itemName}");
                    });
            }
            else if (int.TryParse(idStr, out int chestId))
            {
                if (chestId < 0 || chestId >= Main.maxChests || Main.chest[chestId] == null)
                {
                    args.Player.SendErrorMessage(string.Format(cfg.Msg.InvalidChest, chestId));
                    return;
                }

                int removed = 0;
                int remaining = amount;
                for (int i = 0; i < ChestUtils.ChestMaxItems; i++)
                {
                    if (Main.chest[chestId].item[i]?.type == itemId)
                    {
                        int toRemove = amount == 0 ? Main.chest[chestId].item[i].stack : Math.Min(Main.chest[chestId].item[i].stack, remaining);
                        ChestUtils.RemoveItem(chestId, i, toRemove, out int rem);
                        if (amount > 0) remaining -= rem;
                        removed += rem;
                        if (amount > 0 && remaining <= 0) break;
                    }
                }

                string msg = amount == 0 ? "全部" : $"{removed}个";
                args.Player.SendSuccessMessage($"[c/00FF00:物品搜索] 已从箱子#{chestId} 移除 {msg} {itemName}");
            }
            else
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is delchest <箱子ID/*all> <物品名称/ID> [数量]");
            }
        }

        public static void RemoveSSC(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            
            // 新用法：/is delssc <玩家名/*all> <物品名称/ID> [数量，不填=全部]
            if (args.Parameters.Count < 3)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is delssc <玩家名/*all> <物品名称/ID> [数量]");
                return;
            }

            string target = args.Parameters[1];
            string itemInput = args.Parameters[2];
            int amount = 0;

            if (args.Parameters.Count > 3 && !int.TryParse(args.Parameters[3], out amount))
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 数量必须是数字");
                return;
            }

            if (!PlayerInventoryUtils.IsSSCEnabled())
            {
                args.Player.SendErrorMessage(cfg.Msg.SSCDisabled);
                return;
            }

            // 解析物品
            if (!ItemUtils.TryParseItem(itemInput, out int itemId, out string itemName))
            {
                if (!string.IsNullOrEmpty(itemName))
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配: {itemName}");
                else
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到物品: {itemInput}");
                return;
            }

            bool all = target == "*all*";

            if (!all)
            {
                if (!PlayerInventoryUtils.RemoveFromSSC(target, itemId, amount, out int removed))
                {
                    args.Player.SendErrorMessage(string.Format(cfg.Msg.SSCNotFound, target));
                    return;
                }
                string msg = amount == 0 ? "全部" : $"{removed}个";
                args.Player.SendSuccessMessage($"[c/00FF00:物品搜索] 已从离线玩家 {target} 移除 {msg} {itemName}");
            }
            else
            {
                var progress = new Progress<string>(msg =>
                    args.Player.SendInfoMessage($"[c/00FFFF:物品搜索] 处理中: {msg}"));

                _ = PlayerInventoryUtils.RemoveFromAllSSCAsync(itemId, amount, progress)
                    .ContinueWith(t =>
                    {
                        var (players, total) = t.Result;
                        string msg = amount == 0 ? "全部" : $"{total}个";
                        args.Player.SendSuccessMessage($"[c/00FF00:物品搜索] 已从 {players}位离线玩家 移除 {msg} {itemName}");
                    });
            }
        }
    }
}
