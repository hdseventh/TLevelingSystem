using TShockAPI;
using Terraria;

namespace TLevelingSystem
{
    public class TCommands
    {
        public static void Leaderboard(CommandArgs args)
        {
            TDatabase.ViewLeaderboard(args);
        }

        public static void LevelCheck(CommandArgs args)
        {
            TDatabase.DisplayProgressBar(args.Player);
        }

        public static async void Give(CommandArgs args)
        {
            var player = args.Player;
            if (args.Parameters.Count < 2 || !int.TryParse(args.Parameters[1], out int amount) || amount <= 0)
            {
                player.SendErrorMessage("Invalid Exp amount.");
                return;
            }

            string targetUser = args.Parameters.Count > 2 ? args.Parameters[2] : player.Name;
            if (!await TDatabase.UserExistsAsync(targetUser))
            {
                player.SendErrorMessage($"User '{targetUser}' does not exist.");
                return;
            }

            for (var i = 0; i < Main.player.Length; i++)
            {
                if (Main.player[i].name.ToLower() == targetUser)
                {
                    await TDatabase.AddExpToPlayer(TShock.Players[i], amount);
                    player.SendSuccessMessage($"Gave {amount} Exp from {targetUser}.");
                }
                else
                {
                    player.SendErrorMessage($"User '{targetUser}' is not online.");
                }
            }
        }

        public static async void Take(CommandArgs args)
        {
            var player = args.Player;
            if (args.Parameters.Count < 2 || !int.TryParse(args.Parameters[1], out int amount) || amount <= 0)
            {
                player.SendErrorMessage("Invalid Exp amount.");
                return;
            }

            string targetUser = args.Parameters.Count > 2 ? args.Parameters[2] : player.Name;
            if (!await TDatabase.UserExistsAsync(targetUser))
            {
                player.SendErrorMessage($"User '{targetUser}' does not exist.");
                return;
            }

            for (var i = 0; i < Main.player.Length; i++)
            {
                if (Main.player[i].name.ToLower() == targetUser)
                {
                    await TDatabase.AddExpToPlayer(TShock.Players[i], -amount);
                    player.SendSuccessMessage($"Took {amount} Exp from {targetUser}.");
                }
                else
                {
                    player.SendErrorMessage($"User '{targetUser}' is not online.");
                }
            }
        }

    }
}
