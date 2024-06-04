using MySql.Data.MySqlClient;
using Terraria;
using Terraria.GameContent;
using TerrariaApi.Server;
using TShockAPI;

namespace TLevelingSystem
{
    [ApiVersion(2, 1)]
    public class TLevelingPlugin : TerrariaPlugin
    {
        public override string Name => "TLevelingSystem";
        public override string Author => "hdseventh";
        public override string Description => "A simple leveling system for TShock";
        public override Version Version => new Version(0, 2, 0);

        public TLevelingPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerGreet);
        }

        private void OnInitialize(EventArgs args)
        {
            TDatabase.InitializeDatabase();
            Commands.ChatCommands.Add(new Command("level.use", LevelingMain, "level"));
        }

        public static void LevelingMain(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                if (args.Player.HasPermission("level.manage"))
                {
                    args.Player.SendErrorMessage("Invalid command. Available commands: check, give, take, leaderboard");
                    return;
                }
                args.Player.SendErrorMessage("Invalid command. Available commands: check, leaderboard");
                return;
            }

            switch (args.Parameters[0])
            {
                case "check":
                    TCommands.LevelCheck(args);
                    break;
                case "add":
                case "give":
                    if (args.Player.HasPermission("level.manage"))
                    {
                        TCommands.Give(args);
                        break;
                    }
                    args.Player.SendErrorMessage("You do not have permission to use this command.");
                    break;
                case "reduce":
                case "take":
                    if (args.Player.HasPermission("level.manage"))
                    {
                        TCommands.Take(args);
                        break;
                    }
                    args.Player.SendErrorMessage("You do not have permission to use this command.");
                    break;
                case "leaderboard":
                case "lb":
                    TCommands.Leaderboard(args);
                    break;
                default:
                    if (args.Player.HasPermission("level.manage"))
                    {
                        args.Player.SendErrorMessage("Invalid command. Available commands: check, give, take, leaderboard");
                        return;
                    }
                    args.Player.SendErrorMessage("Invalid command. Available commands: check, leaderboard");
                    break;

            }
        }


        private async void OnNpcKilled(NpcKilledEventArgs args)
        {
            if (args.npc == null || args.npc.realLife >= 0) return;

            var lastPlayerToHit = GetPlayerWhoDealtFinalBlow(args.npc);
            if (lastPlayerToHit == null || !lastPlayerToHit.Active) return;

            int expGained = CalculateExp(args.npc);
            await TDatabase.AddExpToPlayer(lastPlayerToHit, expGained);
        }

        private TSPlayer GetPlayerWhoDealtFinalBlow(Terraria.NPC npc)
        {
            if (npc.lastInteraction < 0 || npc.lastInteraction >= TShock.Players.Length)
                return null;

            var lastPlayerToHit = TShock.Players[npc.lastInteraction];
            return lastPlayerToHit;
        }


        private int CalculateExp(Terraria.NPC npc)
        {
            if(npc.lifeMax <= 100) return npc.lifeMax / 50;
            else if (npc.lifeMax <= 1000) return npc.lifeMax / 40;
            else return npc.lifeMax / 35;
        }

        private async void OnPlayerGreet(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null || !player.Active) return;
            if (!await TDatabase.UserExistsAsync(player.Name))
            {
                Console.WriteLine("[LevelingSystem] Player " + player.Name + " does not exist in database. Attempting to insert.");
                TDatabase.InsertUser(player);
            }

        }

    }
}

