using Newtonsoft.Json;
using TShockAPI;

namespace ItemSearch
{
    public class Configuration
    {
        public static readonly string ConfigPath = Path.Combine(TShock.SavePath, "ItemSearch.json");
        
        [JsonProperty("插件指令名")]
        public string CommandName { get; set; } = "is";
        
        [JsonProperty("指令权限")]
        public PermissionsConfig Perms { get; set; } = new();
        
        [JsonProperty("提示消息")]
        public MessagesConfig Msg { get; set; } = new();
        
        [JsonProperty("性能设置")]
        public PerformanceConfig Perf { get; set; } = new();

        public class PermissionsConfig
        {
            [JsonProperty("查询在线背包")]
            public string SearchPlayer { get; set; } = "itemsearch.player.search";
            [JsonProperty("删除在线背包")]
            public string RemovePlayer { get; set; } = "itemsearch.player.remove";
            [JsonProperty("查询箱子")]
            public string SearchBox { get; set; } = "itemsearch.box.search";
            [JsonProperty("删除箱子物品")]
            public string RemoveBox { get; set; } = "itemsearch.box.remove";
            [JsonProperty("查询离线玩家")]
            public string SearchSSC { get; set; } = "itemsearch.ssc.search";
            [JsonProperty("删除离线玩家")]
            public string RemoveSSC { get; set; } = "itemsearch.ssc.remove";
        }

        public class MessagesConfig
        {
            [JsonProperty("查询结果头")]
            public string SearchHeader { get; set; } = "[c/00FFFF:物品搜索] 查询结果:";
            [JsonProperty("发现物品格式")]
            public string FoundItemFormat { get; set; } = "  {0}: [i/s{1}:{2}] ×{3}";
            [JsonProperty("箱子坐标格式")]
            public string ChestLocationFormat { get; set; } = "  箱子 #{0} 位于 X:{1} Y:{2} - [i/s{3}:{4}] ×{5}";
            [JsonProperty("删除成功")]
            public string RemoveSuccess { get; set; } = "[c/00FF00:物品搜索] 已成功从 {0} 移除 {1} 个 [i:{2}]";
            [JsonProperty("玩家不在线")]
            public string PlayerOffline { get; set; } = "[c/FF0000:物品搜索] 玩家 {0} 不在线";
            [JsonProperty("离线玩家不存在")]
            public string SSCNotFound { get; set; } = "[c/FF0000:物品搜索] 数据库中未找到玩家 {0}";
            [JsonProperty("箱子索引无效")]
            public string InvalidChest { get; set; } = "[c/FF0000:物品搜索] 无效的箱子索引: {0}";
            [JsonProperty("SSC未启用")]
            public string SSCDisabled { get; set; } = "[c/FF0000:物品搜索] 服务器未启用SSC，无法操作离线玩家";
            [JsonProperty("参数错误")]
            public string InvalidArgs { get; set; } = "[c/FF0000:物品搜索] 参数错误，用法: {0}";
            [JsonProperty("操作进度")]
            public string Progress { get; set; } = "[c/00FFFF:物品搜索] 正在处理... 已完成 {0}/{1}";
            [JsonProperty("批量完成")]
            public string BatchComplete { get; set; } = "[c/00FF00:物品搜索] 批量操作完成，共影响 {0} 个目标";
        }

        public class PerformanceConfig
        {
            [JsonProperty("每帧处理箱子数")]
            public int ChestsPerTick { get; set; } = 50;
            [JsonProperty("每帧处理玩家数")]
            public int PlayersPerTick { get; set; } = 5;
            [JsonProperty("启用异步扫描")]
            public bool UseAsync { get; set; } = true;
        }

        public static Configuration Reload()
        {
            if (!File.Exists(ConfigPath))
            {
                var cfg = new Configuration();
                cfg.Save();
                return cfg;
            }
            try
            {
                return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigPath)) ?? new Configuration();
            }
            catch
            {
                return new Configuration();
            }
        }

        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
