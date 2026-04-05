using TShockAPI;

namespace ItemSearch
{
    public static class SearchCommands
    {
        /// <summary>
        /// 查询所有玩家（在线+离线）
        /// 支持：/is search <物品名/ID> [数量条件] 或 /is search [数量条件]
        /// </summary>
        public static void SearchAll(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is search <物品名称/ID> [数量条件] 或 /is search [数量条件如>500]");
                return;
            }

            string param1 = args.Parameters[1];
            string param2 = args.Parameters.Count > 2 ? args.Parameters[2] : "";
            
            int itemId = 0;
            string itemName = "";
            Func<int, bool>? countFilter = null;
            bool searchAllItems = false;

            // 判断第一个参数是物品还是数量条件
            if (ItemUtils.IsCountCondition(param1))
            {
                // 仅数量查询：/is search >500
                searchAllItems = true;
                countFilter = ItemUtils.ParseCountCondition(param1);
                if (countFilter == null)
                {
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 无效的数量条件: {param1}");
                    return;
                }
            }
            else
            {
                // 物品+数量查询：/is search 泰拉刃 >500
                if (!ItemUtils.TryParseItem(param1, out itemId, out itemName))
                {
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配结果，请使用ID精确查询: {itemName}");
                    }
                    else
                    {
                        args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到匹配物品: {param1}");
                    }
                    return;
                }

                // 解析数量条件（可能在param2或param1后面）
                if (!string.IsNullOrEmpty(param2))
                {
                    countFilter = ItemUtils.ParseCountCondition(param2);
                    if (countFilter == null)
                    {
                        args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 无效的数量条件: {param2}");
                        return;
                    }
                }
            }

            // 执行查询
            if (searchAllItems)
            {
                // 查询所有物品（仅数量条件）
                SearchAllItemsWithCount(args, countFilter!, cfg);
            }
            else
            {
                // 查询指定物品
                SearchSpecificItem(args, itemId, itemName, countFilter, cfg);
            }
        }

        /// <summary>
        /// 仅按数量条件查询所有物品
        /// </summary>
        private static void SearchAllItemsWithCount(CommandArgs args, Func<int, bool> countFilter, Configuration cfg)
        {
            args.Player.SendInfoMessage(cfg.Msg.SearchHeader);
            args.Player.SendInfoMessage($"  查询条件: 物品数量满足条件");

            var (onlineResults, offlineResults) = PlayerInventoryUtils.FindAllItemsByCount(countFilter);

            // 在线玩家
            if (onlineResults.Count > 0)
            {
                args.Player.SendInfoMessage($"[c/00FF00:在线玩家] ({onlineResults.Select(r => r.Player.Name).Distinct().Count()}人):");
                var grouped = onlineResults.GroupBy(r => r.Player.Name);
                foreach (var group in grouped.Take(20))
                {
                    string items = string.Join(", ", group.Take(5).Select(r => 
                        $"{ItemUtils.FormatItem(r.ItemId, r.ItemName)}x{r.Count}"));
                    if (group.Count() > 5) items += $" ...等{group.Count()}种";
                    args.Player.SendInfoMessage($"  {group.Key}: {items}");
                }
                if (grouped.Count() > 20)
                    args.Player.SendInfoMessage($"  ...还有 {grouped.Count() - 20} 位玩家");
            }

            // 离线玩家
            if (offlineResults.Count > 0)
            {
                args.Player.SendInfoMessage($"[c/808080:离线玩家] ({offlineResults.Select(r => r.Username).Distinct().Count()}人):");
                var grouped = offlineResults.GroupBy(r => r.Username);
                foreach (var group in grouped.Take(20))
                {
                    string items = string.Join(", ", group.Take(5).Select(r => 
                        $"{ItemUtils.FormatItem(r.ItemId, r.ItemName)}x{r.Count}"));
                    if (group.Count() > 5) items += $" ...等{group.Count()}种";
                    args.Player.SendInfoMessage($"  {group.Key}: {items}");
                }
                if (grouped.Count() > 20)
                    args.Player.SendInfoMessage($"  ...还有 {grouped.Count() - 20} 位玩家");
            }

            int totalPlayers = onlineResults.Select(r => r.Player.Name).Distinct().Count() 
                             + offlineResults.Select(r => r.Username).Distinct().Count();
            
            if (totalPlayers == 0)
            {
                args.Player.SendInfoMessage("  未找到符合条件的玩家");
            }
            else
            {
                int totalItems = onlineResults.Count + offlineResults.Count;
                args.Player.SendInfoMessage($"[c/00FFFF:总计] {totalPlayers}位玩家，{totalItems}条记录");
            }
        }

        /// <summary>
        /// 查询指定物品（带数量条件）
        /// </summary>
        private static void SearchSpecificItem(CommandArgs args, int itemId, string itemName, 
            Func<int, bool>? countFilter, Configuration cfg)
        {
            var (online, offline) = PlayerInventoryUtils.FindItemsEverywhere(itemId, countFilter);
            
            args.Player.SendInfoMessage(cfg.Msg.SearchHeader);
            args.Player.SendInfoMessage($"  查询物品: {ItemUtils.FormatItem(itemId, itemName)}");
            if (countFilter != null)
                args.Player.SendInfoMessage($"  数量条件: {args.Parameters[2]}");

            // 在线玩家
            if (online.Count > 0)
            {
                args.Player.SendInfoMessage($"[c/00FF00:在线玩家] ({online.Count}人):");
                foreach (var (plr, slot, count) in online.Take(20))
                {
                    string loc = GetSlotLocation(slot);
                    args.Player.SendInfoMessage($"  {plr.Name}: {count}个 ({loc})");
                }
                if (online.Count > 20)
                    args.Player.SendInfoMessage($"  ...还有 {online.Count - 20} 位玩家");
            }

            // 离线玩家
            if (offline.Count > 0)
            {
                args.Player.SendInfoMessage($"[c/808080:离线玩家] ({offline.Count}人):");
                foreach (var (name, accId, slot, count) in offline.Take(20))
                {
                    args.Player.SendInfoMessage($"  {name}: {count}个 (槽位{slot})");
                }
                if (offline.Count > 20)
                    args.Player.SendInfoMessage($"  ...还有 {offline.Count - 20} 位玩家");
            }

            int total = online.Count + offline.Count;
            if (total == 0)
            {
                args.Player.SendInfoMessage("  未找到符合条件的玩家");
            }
            else
            {
                args.Player.SendInfoMessage($"[c/00FFFF:总计] {total}位玩家持有该物品");
            }
        }

        // ... 其他方法保持不变（SearchOnlinePlayer, SearchSSCPlayer, SearchChests）...
        public static void SearchOnlinePlayer(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is player <物品名称/ID> [数量条件]");
                return;
            }

            string itemInput = args.Parameters[1];
            string countCondition = args.Parameters.Count > 2 ? args.Parameters[2] : "";

            if (!ItemUtils.TryParseItem(itemInput, out int itemId, out string itemName))
            {
                if (!string.IsNullOrEmpty(itemName))
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配: {itemName}");
                else
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到物品: {itemInput}");
                return;
            }

            var countFilter = ItemUtils.ParseCountCondition(countCondition);
            var results = PlayerInventoryUtils.FindOnlineItems(itemId, countFilter).ToList();
            
            if (results.Count == 0)
            {
                args.Player.SendInfoMessage($"{cfg.Msg.SearchHeader}\n  未找到持有 {ItemUtils.FormatItem(itemId, itemName)} 的在线玩家");
                return;
            }

            args.Player.SendInfoMessage(cfg.Msg.SearchHeader);
            args.Player.SendInfoMessage($"  查询物品: {ItemUtils.FormatItem(itemId, itemName)}");
            foreach (var (plr, slot, count) in results)
            {
                string loc = GetSlotLocation(slot);
                args.Player.SendInfoMessage($"  {plr.Name}: {count}个 ({loc})");
            }
            args.Player.SendInfoMessage($"  总计: {results.Count}位玩家");
        }

        public static void SearchSSCPlayer(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is ssc <物品名称/ID> [数量条件]");
                return;
            }

            string itemInput = args.Parameters[1];
            string countCondition = args.Parameters.Count > 2 ? args.Parameters[2] : "";

            if (!PlayerInventoryUtils.IsSSCEnabled())
            {
                args.Player.SendErrorMessage(cfg.Msg.SSCDisabled);
                return;
            }

            if (!ItemUtils.TryParseItem(itemInput, out int itemId, out string itemName))
            {
                if (!string.IsNullOrEmpty(itemName))
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配: {itemName}");
                else
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到物品: {itemInput}");
                return;
            }

            var countFilter = ItemUtils.ParseCountCondition(countCondition);
            var results = PlayerInventoryUtils.FindSSCItems(itemId, countFilter);
            
            if (results.Count == 0)
            {
                args.Player.SendInfoMessage($"{cfg.Msg.SearchHeader}\n  未找到持有 {ItemUtils.FormatItem(itemId, itemName)} 的离线玩家");
                return;
            }

            args.Player.SendInfoMessage(cfg.Msg.SearchHeader);
            args.Player.SendInfoMessage($"  查询物品: {ItemUtils.FormatItem(itemId, itemName)}");
            foreach (var (name, accId, slot, count) in results)
            {
                args.Player.SendInfoMessage($"  {name}: {count}个 (槽位{slot})");
            }
            args.Player.SendInfoMessage($"  总计: {results.Count}位离线玩家");
        }

        public static void SearchChests(CommandArgs args)
        {
            var cfg = Configuration.Reload();
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is box <物品名称/ID>");
                return;
            }

            string itemInput = args.Parameters[1];

            if (!ItemUtils.TryParseItem(itemInput, out int itemId, out string itemName))
            {
                if (!string.IsNullOrEmpty(itemName))
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 找到多个匹配: {itemName}");
                else
                    args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未找到物品: {itemInput}");
                return;
            }

            var results = ChestUtils.FindItems(itemId).ToList();
            if (results.Count == 0)
            {
                args.Player.SendInfoMessage($"{cfg.Msg.SearchHeader}\n  未找到包含 {ItemUtils.FormatItem(itemId, itemName)} 的箱子");
                return;
            }

            args.Player.SendInfoMessage(cfg.Msg.SearchHeader);
            args.Player.SendInfoMessage($"  查询物品: {ItemUtils.FormatItem(itemId, itemName)}");
            foreach (var (chestId, chest, slot, count) in results.Take(20))
            {
                args.Player.SendInfoMessage($"  箱子#{chestId} (X:{chest.x} Y:{chest.y}): {count}个 (槽位{slot})");
            }
            if (results.Count > 20)
                args.Player.SendInfoMessage($"  ...还有 {results.Count - 20} 个箱子");
            args.Player.SendInfoMessage($"  总计: {results.Count}个箱子");
        }

        private static string GetSlotLocation(int slot)
        {
            if (slot < NetItem.InventorySlots) return $"背包[{slot}]";
            if (slot < NetItem.InventorySlots + NetItem.ArmorSlots) return "装备栏";
            if (slot < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots) return "染料栏";
            return "饰品/其他";
        }
    }
}
