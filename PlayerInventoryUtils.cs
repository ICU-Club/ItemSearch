using System.Text;
using Terraria;
using Terraria.Localization;
using TShockAPI;
using TShockAPI.DB;

namespace ItemSearch
{
    public static class PlayerInventoryUtils
    {
        public static bool IsSSCEnabled()
        {
            return TShock.ServerSideCharacterConfig.Settings.Enabled;
        }

        /// <summary>
        /// 按数量条件查询所有物品（新功能）
        /// 返回所有满足数量条件的物品，不分物品类型
        /// </summary>
        public static (List<(TSPlayer Player, int ItemId, string ItemName, int Slot, int Count)> Online,
                      List<(string Username, int ItemId, string ItemName, int Slot, int Count)> Offline)
            FindAllItemsByCount(Func<int, bool> countFilter)
        {
            var online = new List<(TSPlayer, int, string, int, int)>();
            var offline = new List<(string, int, string, int, int)>();

            // 查询在线玩家 - 遍历所有槽位检查所有物品
            foreach (var plr in TShock.Players.Where(p => p?.Active == true))
            {
                for (int i = 0; i < NetItem.MaxInventory; i++)
                {
                    var item = GetItemByNetSlot(plr.TPlayer, i);
                    if (item?.type > 0 && item.stack > 0 && countFilter(item.stack))
                    {
                        string itemName = Lang.GetItemNameValue(item.type) ?? $"物品{item.type}";
                        online.Add((plr, item.type, itemName, i, item.stack));
                    }
                }
            }

            // 查询离线玩家（SSC）
            if (IsSSCEnabled())
            {
                try
                {
                    var accounts = TShock.UserAccounts.GetUserAccounts();
                    foreach (var account in accounts)
                    {
                        var playerData = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), account.ID);
                        if (playerData?.inventory == null) continue;

                        for (int i = 0; i < playerData.inventory.Length; i++)
                        {
                            var item = playerData.inventory[i];
                            if (item.NetId > 0 && item.Stack > 0 && countFilter(item.Stack))
                            {
                                string itemName = Lang.GetItemNameValue(item.NetId) ?? $"物品{item.NetId}";
                                offline.Add((account.Name, item.NetId, itemName, i, item.Stack));
                            }
                        }
                    }
                }
                catch { }
            }

            return (online, offline);
        }

        /// <summary>
        /// 合并查询在线和离线玩家（指定物品ID）
        /// </summary>
        public static (List<(TSPlayer Player, int Slot, int Count)> Online, 
                      List<(string Username, int AccountId, int Slot, int Count)> Offline) 
            FindItemsEverywhere(int itemId, Func<int, bool>? countFilter = null)
        {
            var online = new List<(TSPlayer, int, int)>();
            var offline = new List<(string, int, int, int)>();

            // 查询在线玩家
            foreach (var plr in TShock.Players.Where(p => p?.Active == true))
            {
                for (int i = 0; i < NetItem.MaxInventory; i++)
                {
                    var item = GetItemByNetSlot(plr.TPlayer, i);
                    if (item?.type == itemId && item.stack > 0)
                    {
                        if (countFilter == null || countFilter(item.stack))
                            online.Add((plr, i, item.stack));
                    }
                }
            }

            // 查询离线玩家（SSC）
            if (IsSSCEnabled())
            {
                try
                {
                    var accounts = TShock.UserAccounts.GetUserAccounts();
                    foreach (var account in accounts)
                    {
                        var playerData = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), account.ID);
                        if (playerData?.inventory == null) continue;

                        for (int i = 0; i < playerData.inventory.Length; i++)
                        {
                            var item = playerData.inventory[i];
                            if (item.NetId == itemId && item.Stack > 0)
                            {
                                if (countFilter == null || countFilter(item.Stack))
                                    offline.Add((account.Name, account.ID, i, item.Stack));
                            }
                        }
                    }
                }
                catch { }
            }

            return (online, offline);
        }

        public static List<(string Username, int AccountId, int Slot, int Count)> FindSSCItems(int itemId, Func<int, bool>? countFilter = null)
        {
            var result = new List<(string, int, int, int)>();
            
            if (!IsSSCEnabled()) return result;

            try
            {
                var accounts = TShock.UserAccounts.GetUserAccounts();
                foreach (var account in accounts)
                {
                    var playerData = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), account.ID);
                    if (playerData?.inventory == null) continue;

                    for (int i = 0; i < playerData.inventory.Length; i++)
                    {
                        var item = playerData.inventory[i];
                        if (item.NetId == itemId && item.Stack > 0)
                        {
                            if (countFilter == null || countFilter(item.Stack))
                                result.Add((account.Name, account.ID, i, item.Stack));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[ItemSearch] FindSSCItems 错误: {ex.Message}");
            }
            
            return result;
        }

        public static IEnumerable<(TSPlayer Player, int Slot, int Count)> FindOnlineItems(int itemId, Func<int, bool>? countFilter = null)
        {
            foreach (var plr in TShock.Players.Where(p => p?.Active == true))
            {
                for (int i = 0; i < NetItem.MaxInventory; i++)
                {
                    var item = GetItemByNetSlot(plr.TPlayer, i);
                    if (item?.type == itemId && item.stack > 0)
                    {
                        if (countFilter == null || countFilter(item.stack))
                            yield return (plr, i, item.stack);
                    }
                }
            }
        }

        // ... 保留之前的 RemoveFromSSC, RemoveFromAllSSCAsync, SavePlayerData 等方法 ...
        public static bool RemoveFromSSC(string playerName, int itemId, int amount, out int removed)
        {
            removed = 0;
            if (!IsSSCEnabled()) return false;

            var account = TShock.UserAccounts.GetUserAccountByName(playerName);
            if (account == null) return false;

            try
            {
                var playerData = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), account.ID);
                if (playerData?.inventory == null) return false;

                bool modified = false;
                var newInventory = playerData.inventory.ToArray();
                int remaining = amount;
                
                for (int i = 0; i < newInventory.Length; i++)
                {
                    var item = newInventory[i];
                    if (item.NetId != itemId) continue;
                    
                    int toRemove = amount == 0 ? item.Stack : Math.Min(item.Stack, remaining);
                    int newStack = item.Stack - toRemove;
                    if (amount > 0) remaining -= toRemove;
                    removed += toRemove;
                    modified = true;

                    if (newStack <= 0)
                        newInventory[i] = new NetItem(0, 0, 0);
                    else
                        newInventory[i] = new NetItem(item.NetId, newStack, item.PrefixId);
                }

                if (modified)
                {
                    playerData.inventory = newInventory;
                    SavePlayerData(playerData, account.ID);
                    return true;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[ItemSearch] RemoveFromSSC 错误: {ex.Message}");
            }
            return false;
        }

        public static async Task<(int Players, int TotalRemoved)> RemoveFromAllSSCAsync(int itemId, int amount, IProgress<string>? progress)
        {
            int totalPlayers = 0;
            int totalRemoved = 0;

            if (!IsSSCEnabled()) return (0, 0);

            await Task.Run(() =>
            {
                try
                {
                    var accounts = TShock.UserAccounts.GetUserAccounts();
                    
                    foreach (var account in accounts)
                    {
                        var playerData = TShock.CharacterDB.GetPlayerData(new TSPlayer(-1), account.ID);
                        if (playerData?.inventory == null) continue;

                        int removed = 0;
                        bool modified = false;
                        var newInventory = playerData.inventory.ToArray();
                        int remaining = amount;

                        for (int i = 0; i < newInventory.Length; i++)
                        {
                            var item = newInventory[i];
                            if (item.NetId != itemId) continue;
                            
                            int toRemove = amount == 0 ? item.Stack : Math.Min(item.Stack, remaining);
                            int newStack = item.Stack - toRemove;
                            if (amount > 0) remaining -= toRemove;
                            removed += toRemove;
                            modified = true;

                            if (newStack <= 0)
                                newInventory[i] = new NetItem(0, 0, 0);
                            else
                                newInventory[i] = new NetItem(item.NetId, newStack, item.PrefixId);
                        }

                        if (modified)
                        {
                            playerData.inventory = newInventory;
                            SavePlayerData(playerData, account.ID);
                            totalPlayers++;
                            totalRemoved += removed;
                            progress?.Report(account.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"[ItemSearch] RemoveFromAllSSCAsync 错误: {ex.Message}");
                }
            });

            return (totalPlayers, totalRemoved);
        }

        private static void SavePlayerData(PlayerData data, int accountId)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < data.inventory.Length; i++)
            {
                if (i > 0) sb.Append('~');
                var item = data.inventory[i];
                sb.Append($"{item.NetId},{item.Stack},{item.PrefixId},0");
            }
            
            string inventoryStr = sb.ToString();
            TShock.DB.Query("UPDATE tsCharacter SET Inventory = @0 WHERE Account = @1", inventoryStr, accountId);
        }

        public static int RemoveFromOnline(TSPlayer player, int itemId, int amount)
        {
            int removed = 0;
            int remaining = amount;
            
            for (int i = 0; i < NetItem.MaxInventory; i++)
            {
                var item = GetItemByNetSlot(player.TPlayer, i);
                if (item?.type != itemId) continue;

                int toRemove = amount == 0 ? item.stack : Math.Min(item.stack, remaining);
                item.stack -= toRemove;
                if (amount > 0) remaining -= toRemove;
                removed += toRemove;

                if (item.stack <= 0) item.SetDefaults(0);
                SyncPlayerSlot(player, i, item);
                
                if (amount > 0 && remaining <= 0) break;
            }
            return removed;
        }

        public static Item? GetItemByNetSlot(Player player, int netSlot)
        {
            if (netSlot < NetItem.InventorySlots) return player.inventory[netSlot];
            netSlot -= NetItem.InventorySlots;
            if (netSlot < NetItem.ArmorSlots) return player.armor[netSlot];
            netSlot -= NetItem.ArmorSlots;
            if (netSlot < NetItem.DyeSlots) return player.dye[netSlot];
            netSlot -= NetItem.DyeSlots;
            if (netSlot < NetItem.MiscEquipSlots) return player.miscEquips[netSlot];
            netSlot -= NetItem.MiscEquipSlots;
            if (netSlot < NetItem.MiscDyeSlots) return player.miscDyes[netSlot];
            return null;
        }

        public static void SyncPlayerSlot(TSPlayer player, int slot, Item item)
        {
            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(item.Name), player.Index, slot, item.prefix);
            NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1, NetworkText.FromLiteral(item.Name), player.Index, slot, item.prefix);
        }
    }
}
