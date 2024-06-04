using Microsoft.Xna.Framework;
using MySql.Data.MySqlClient;
using TShockAPI;

namespace TLevelingSystem
{
    public class TDatabase
    {
        private const string connectionString = "Server=localhost; Database=Leveling; Uid=root; Pwd=;";

        public static void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Players (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Name VARCHAR(100) NOT NULL,
                        Exp INT DEFAULT 0,
                        Level INT DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS Leaderboard (
                        PlayerId INT,
                        Name VARCHAR(100),
                        Exp INT,
                        Level INT,
                        PRIMARY KEY (PlayerId)
                    );
                ";
                cmd.ExecuteNonQuery();
            }
        }
        private static void SendFloatingText(TSPlayer player, string text, Vector2 position)
        {
            player.SendData(PacketTypes.CreateCombatTextExtended, text, 255, position.X, position.Y);
        }

        public static async Task AddExpToPlayer(TSPlayer player, int exp)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE Players
                        SET Exp = Exp + @Exp
                        WHERE Name = @Name;
                        SELECT Exp, Level FROM Players WHERE Name = @Name;
                    ";
                    cmd.Parameters.AddWithValue("@Name", player.Name);
                    cmd.Parameters.AddWithValue("@Exp", exp);

                    var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        int totalExp = reader.GetInt32(0);
                        int level = reader.GetInt32(1);
                        SendFloatingText(player, $"+{exp} exp", player.TPlayer.position);

                        // Check for level up
                        int expForNextLevel = GetExpForNextLevel(level);
                        if (totalExp >= expForNextLevel)
                        {
                            level++;
                            await LevelUpPlayer(player, level, totalExp - expForNextLevel);
                            player.SendSuccessMessage($"Congratulations! You have leveled up to level {level}!");
                            GiveLevelUpRewards(player, level);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error adding exp to player {player.Name}: {ex.Message}");
            }
        }
        public static async Task LevelUpPlayer(TSPlayer player, int newLevel, int remainingExp)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE Players
                        SET Level = @Level, Exp = @Exp
                        WHERE Name = @Name;
                    ";
                    cmd.Parameters.AddWithValue("@Level", newLevel);
                    cmd.Parameters.AddWithValue("@Exp", remainingExp);
                    cmd.Parameters.AddWithValue("@Name", player.Name);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error leveling up player {player.Name}: {ex.Message}");
            }
        }

        private static int GetExpForNextLevel(int level)
        {
            return level * 150;
        }

        private static void GiveLevelUpRewards(TSPlayer player, int level)
        {
            // Add rewards for level up
        }

        public static async void DisplayProgressBar(TSPlayer player)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    int currentExp = 0;
                    int expForNextLevel = 0;
                    int level = 0;
                    cmd.CommandText = "SELECT Name, Exp, Level FROM Players ORDER BY Exp DESC LIMIT 10";

                    var reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        currentExp = reader.GetInt32(1);
                        expForNextLevel = GetExpForNextLevel(reader.GetInt32(2));
                        level = reader.GetInt32(2);
                    }

                    int progress = (int)((float)currentExp / expForNextLevel * 100);
                    player.SendInfoMessage($"Your current level is {level} and your exp is {currentExp} / {expForNextLevel}");
                    player.SendInfoMessage($"Level {level}: [{new string('|', progress / 5)}{new string(' ', 20 - progress / 5)}] {progress}%");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error viewing level: {ex.Message}");
            }
        }

        public static async void ViewLeaderboard(CommandArgs args)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT Name, Exp, Level FROM Players ORDER BY Exp DESC LIMIT 10";

                    var reader = await cmd.ExecuteReaderAsync();
                    var leaderboard = new List<string>();
                    while (reader.Read())
                    {
                        leaderboard.Add($"{reader.GetString(0)} - Level {reader.GetInt32(2)} - {reader.GetInt32(1)} exp");
                    }

                    args.Player.SendMessage("Leaderboard:", Microsoft.Xna.Framework.Color.Green);
                    foreach (var entry in leaderboard)
                    {
                        args.Player.SendMessage(entry, Microsoft.Xna.Framework.Color.White);
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error viewing leaderboard: {ex.Message}");
            }
        }
        public static async void InsertUser(TSPlayer player)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO Players (Name, Exp, Level)
                        VALUES (@Name, 0, 1)
                        ON DUPLICATE KEY UPDATE Name = Name;
                    ";
                    cmd.Parameters.AddWithValue("@Name", player.Name);

                    await cmd.ExecuteNonQueryAsync();
                    TShock.Log.ConsoleInfo($"[LevelingSystem] User {player.Name} inserted into database.");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error registering player {player.Name}: {ex.Message}");
                player.Kick("Something Went Wrong, Please Report to Admin.");
            }
        }
        public static async Task<bool> UserExistsAsync(string username)
        {

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM Players WHERE Name = @username LIMIT 1)";
                    cmd.Parameters.AddWithValue("@username", username);

                    var reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        return reader.GetBoolean(0);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error viewing leaderboard: {ex.Message}");
                return false;
            }
        }

        public static async Task<(int Exp, int Level)> GetPlayerData(string playerName)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                SELECT Exp, Level
                FROM Players
                WHERE Name = @Name;
            ";
                    cmd.Parameters.AddWithValue("@Name", playerName);

                    var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        int exp = reader.GetInt32(0);
                        int level = reader.GetInt32(1);
                        return (exp, level);
                    }
                    else
                    {
                        return (0, 0); // Player not found, returning default values
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error fetching data for player {playerName}: {ex.Message}");
                return (0, 0); // In case of an error, returning default values
            }
        }
    }
}
