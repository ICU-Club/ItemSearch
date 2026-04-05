using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;

namespace ItemSearch
{
    public static class ItemUtils
    {
        /// <summary>
        /// 检查字符串是否是数量条件（如 >500, <100）
        /// </summary>
        public static bool IsCountCondition(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return Regex.IsMatch(input.Trim(), @"^[><=!]+?\d+$");
        }

        /// <summary>
        /// 解析物品查询参数（支持ID或名称）
        /// </summary>
        public static bool TryParseItem(string input, out int itemId, out string matchedName)
        {
            itemId = 0;
            matchedName = "";
            
            // 先尝试解析为数字ID
            if (int.TryParse(input, out int id) && id > 0 && id < ItemID.Count)
            {
                itemId = id;
                matchedName = Lang.GetItemNameValue(id) ?? $"物品{id}";
                return true;
            }

            // 模糊匹配名称
            var matches = new List<(int Id, string Name)>();
            string search = input.ToLowerInvariant();
            
            for (int i = 1; i < ItemID.Count; i++)
            {
                string itemName = Lang.GetItemNameValue(i);
                if (string.IsNullOrEmpty(itemName)) continue;
                
                if (itemName.ToLowerInvariant().Contains(search))
                {
                    matches.Add((i, itemName));
                }
            }

            if (matches.Count == 1)
            {
                itemId = matches[0].Id;
                matchedName = matches[0].Name;
                return true;
            }
            else if (matches.Count > 1)
            {
                // 返回0表示有多个匹配，需要用户确认
                itemId = 0;
                matchedName = string.Join(", ", matches.Take(10).Select(m => $"{m.Name}({m.Id})"));
                if (matches.Count > 10) matchedName += $", ...等共{matches.Count}个";
                return false;
            }

            return false;
        }

        /// <summary>
        /// 解析数量条件（如 >500, <100, =50, >=1000）
        /// </summary>
        public static Func<int, bool>? ParseCountCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) return null;
            
            condition = condition.Trim();
            var match = Regex.Match(condition, @"^([><=!]+)?(\d+)$");
            
            if (!match.Success) return null;
            
            string op = match.Groups[1].Value.Trim();
            int value = int.Parse(match.Groups[2].Value);

            return op switch
            {
                ">" or ">=" => count => count > value,
                "<" or "<=" => count => count < value,
                "=" or "==" or "" => count => count == value,
                "!=" or "<>" => count => count != value,
                _ => null
            };
        }

        /// <summary>
        /// 格式化物品显示 [i:ID] 或物品名称
        /// </summary>
        public static string FormatItem(int itemId, string? customName = null)
        {
            string name = customName ?? Lang.GetItemNameValue(itemId) ?? $"物品{itemId}";
            return $"[i:{itemId}] {name}";
        }
    }
}
