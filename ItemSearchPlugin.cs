using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace ItemSearch
{
    [ApiVersion(2, 1)]
    public class ItemSearchPlugin : TerrariaPlugin
    {
        public override string Name => "ItemSearch";
        public override string Author => "星梦";
        public override string Description => "全服物品搜索与批量管理工具";
        public override Version Version => new Version(2, 0, 0);

        internal static Configuration Config = null!;

        public ItemSearchPlugin(Main game) : base(game)
        {
            Order = 10;
        }

        public override void Initialize()
        {
            Config = Configuration.Reload();
            
            TShockAPI.Commands.ChatCommands.Add(new Command(Config.Perms.SearchPlayer, MainCommand, "is")
            {
                HelpText = "物品搜索主指令。用法: /is search/player/ssc/box/del/delchest/delssc ..."
            });
        }

        private static void MainCommand(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                SendHelp(args.Player);
                return;
            }

            string subCommand = args.Parameters[0].ToLowerInvariant();
            
            // 检查是否是数量条件（如 >500）
            if (ItemUtils.IsCountCondition(subCommand))
            {
                args.Parameters.Insert(0, "search");
                SearchCommands.SearchAll(args);
                return;
            }

            bool looksLikeItem = subCommand.Length > 0 && 
                (char.IsDigit(subCommand[0]) || ItemUtils.TryParseItem(subCommand, out _, out _));

            switch (subCommand)
            {
                case "search":
                case "all":
                    SearchCommands.SearchAll(args);
                    break;
                case "player":
                case "online":
                    SearchCommands.SearchOnlinePlayer(args);
                    break;
                case "ssc":
                case "offline":
                    SearchCommands.SearchSSCPlayer(args);
                    break;
                case "box":
                case "chest":
                    SearchCommands.SearchChests(args);
                    break;
                case "del":
                case "remove":
                    if (args.Parameters.Count > 1 && args.Parameters[1].ToLowerInvariant() == "player")
                        RemoveCommands.RemoveOnline(args);
                    else
                        args.Player.SendErrorMessage("[c/FF0000:物品搜索] 用法: /is del player <玩家名/*all> <物品名称/ID> [数量]");
                    break;
                case "delchest":
                    RemoveCommands.RemoveChests(args);
                    break;
                case "delssc":
                    RemoveCommands.RemoveSSC(args);
                    break;
                default:
                    if (looksLikeItem)
                    {
                        args.Parameters.Insert(0, "search");
                        SearchCommands.SearchAll(args);
                    }
                    else
                    {
                        args.Player.SendErrorMessage($"[c/FF0000:物品搜索] 未知子命令: {subCommand}");
                        SendHelp(args.Player);
                    }
                    break;
            }
        }

        private static void SendHelp(TSPlayer player)
        {
            player.SendInfoMessage("[c/00FFFF:===== ItemSearch 帮助 =====]");
            player.SendInfoMessage("[c/FFB6C1:插件开发][c/40E0D0:by][c/66CCFF:星梦]");
            player.SendInfoMessage("[c/FFFF00:查询命令:]");
            player.SendInfoMessage("  /is search <物品名/ID> [数量条件] - 查询所有玩家（在线+离线）");
            player.SendInfoMessage("  /is search [数量条件如>500] - 查询所有物品数量满足条件的玩家");
            player.SendInfoMessage("  /is player <物品名/ID> [数量条件] - 仅查询在线玩家");
            player.SendInfoMessage("  /is ssc <物品名/ID> [数量条件] - 仅查询离线玩家");
            player.SendInfoMessage("  /is box <物品名/ID> - 查询箱子");
            player.SendInfoMessage("  /is >500 - 快捷查询");
            player.SendInfoMessage("[c/FFFF00:移除命令:]");
            player.SendInfoMessage("  /is del player <玩家/*all> <物品名/ID> [数量] - 移除玩家物品（不填=全部）");
            player.SendInfoMessage("  /is delssc <玩家/*all> <物品名/ID> [数量] - 移除离线玩家物品");
            player.SendInfoMessage("  /is delchest <箱子ID/*all> <物品名/ID> [数量] - 移除箱子物品");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var asm = Assembly.GetExecutingAssembly();
                TShockAPI.Commands.ChatCommands.RemoveAll(c => 
                    c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
            }
            base.Dispose(disposing);
        }
    }
}
