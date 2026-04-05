using Terraria;
using Terraria.Localization;
using TShockAPI;

namespace ItemSearch
{
    public static class ChestUtils
    {
        public const int ChestMaxItems = 40;

        public static IEnumerable<(int ChestId, Chest Chest, int Slot, int Count)> FindItems(int itemId)
        {
            for (int i = 0; i < Main.maxChests; i++)
            {
                var chest = Main.chest[i];
                if (chest == null) continue;

                for (int j = 0; j < ChestMaxItems; j++)
                {
                    var item = chest.item[j];
                    if (item?.type == itemId && item.stack > 0)
                    {
                        yield return (i, chest, j, item.stack);
                    }
                }
            }
        }

        public static bool RemoveItem(int chestId, int slot, int amount, out int removed)
        {
            removed = 0;
            if (chestId < 0 || chestId >= Main.maxChests || Main.chest[chestId] == null)
                return false;

            var chest = Main.chest[chestId];
            var item = chest.item[slot];
            
            if (item == null || item.stack == 0) return false;

            // amount为0表示全部移除
            removed = amount == 0 ? item.stack : Math.Min(item.stack, amount);
            item.stack -= removed;

            if (item.stack <= 0)
                item.SetDefaults(0);

            // 同步箱子物品
            NetMessage.SendData(
                (int)PacketTypes.ChestItem,
                -1, -1,
                null,
                chestId,
                slot,
                item.stack,
                item.prefix,
                item.type
            );

            for (int i = 0; i < TShock.Players.Length; i++)
            {
                var plr = TShock.Players[i];
                if (plr?.Active == true)
                {
                    NetMessage.SendData(
                        (int)PacketTypes.ChestItem,
                        plr.Index, -1,
                        null,
                        chestId,
                        slot,
                        item.stack,
                        item.prefix,
                        item.type
                    );
                }
            }

            return true;
        }

        public static async Task<(int Chests, int TotalRemoved)> RemoveFromAllAsync(
            int itemId, 
            int amount, 
            int perTick,
            IProgress<(int current, int total)>? progress)
        {
            int totalChests = 0;
            int totalRemoved = 0;
            int processed = 0;

            await Task.Run(() =>
            {
                var targets = FindItems(itemId).ToList();
                int total = targets.Count;
                int remaining = amount;

                foreach (var (chestId, chest, slot, count) in targets)
                {
                    // amount为0时移除全部，否则检查是否已达数量上限
                    if (amount > 0 && remaining <= 0) break;

                    int toRemove = amount == 0 ? count : Math.Min(count, remaining);
                    if (RemoveItem(chestId, slot, toRemove, out int removed))
                    {
                        if (amount > 0) remaining -= removed;
                        totalRemoved += removed;
                        totalChests++;
                    }

                    processed++;
                    if (processed % perTick == 0)
                    {
                        progress?.Report((processed, total));
                        Thread.Sleep(16);
                    }
                }
            });

            return (totalChests, totalRemoved);
        }
    }
}
