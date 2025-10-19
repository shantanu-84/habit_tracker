using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace habit_tracker
{
    class Program
    {
        static string connectionString = @"Data Source=habit-Tracker.db";
        static void Main(string[] args)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();

                tableCmd.CommandText = @"CREATE TABLE IF NOT EXISTS drinking_water (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            Date TEXT,
                                            Quantity INTEGER
                                        );";

                tableCmd.ExecuteNonQuery();

                connection.Close();

            }
            GetUserInput();
        }

        static void GetUserInput()
        {
            Console.Clear();
            bool closeApp = false;
            while (closeApp == false)
            {
                Console.WriteLine("\n\nMAIN MENU");
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine("\nType 0 to close application");
                Console.WriteLine("Type 1 to View all records");
                Console.WriteLine("Type 2 to Insert record");
                Console.WriteLine("Type 3 to Delete record");
                Console.WriteLine("Type 4 to Update record");
                Console.WriteLine("------------------------------------------\n");

                string commandInput = Console.ReadLine() ?? "";
                switch (commandInput)
                {
                    case "0":
                        Console.WriteLine("\nGoodbye!\n");
                        closeApp = true;
                        Environment.Exit(0);
                        break;
                    case "1":
                        GetAllRecords();
                        break;
                    case "2":
                        Insert();
                        break;
                    case "3":
                        Delete();
                        break;
                    case "4":
                        Update();
                        break;
                    default:
                        Console.WriteLine("\nInvalid command. Please type a number from 0 to 4.\n");
                        break;
                }
            }
        }
        private static void GetAllRecords()
        {
            Console.Clear();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText =
                $"SELECT * FROM drinking_water";

                List<DrinkingWater> tableData = new();

                SqliteDataReader reader = tableCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tableData.Add(
                        new DrinkingWater
                        {
                            Id = reader.GetInt32(0),
                            Date = ParseDateSafe(reader.GetString(1)),
                            Quantity = reader.GetInt32(2)
                        });
                    }
                }
                else
                {
                    Console.WriteLine("No rows found");
                }

                connection.Close();

                Console.WriteLine("-------------------------------------\n");
                foreach (var dw in tableData)
                {
                    Console.WriteLine($"{dw.Id} - {dw.Date.ToString("dd-MM-yyyy")} - Quantity: {dw.Quantity}");
                }
                Console.WriteLine("-------------------------------------\n");
            }

        }
        private static void Insert()
        {
            string date = GetDateInput();
            if (date == "0") return; // user cancelled and returned to menu

            int quantity = GetNumberInput("\n\nPlease insert the number of glasses or other measure of your choice (no decmials allowed)\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText =
                $"INSERT INTO drinking_water (Date, Quantity) VALUES ('{date}', {quantity});";

                tableCmd.ExecuteNonQuery();

                connection.Close();

            }
        }

        private static void Delete()
        {
            Console.Clear();
            GetAllRecords();

            var recordId = GetNumberInput("\n\nPlease insert the Id of the record you want to delete or Type 0 to return to main menu.\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText = $"DELETE from drinking_water WHERE Id = '{recordId}'";

                int rowCount = tableCmd.ExecuteNonQuery();

                if (rowCount == 0)
                {
                    Console.WriteLine($"\n\nNo record with {recordId} found with that Id. Please try again.\n\n");
                    Delete();
                }

            }

            Console.WriteLine($"\n\nRecord with {recordId} deleted successfully!\n\n");

            GetUserInput();
        }

        internal static void Update()
        {
            GetAllRecords();

            var recordId = GetNumberInput("\n\nPlease insert the Id of the record you want to update. Type 0 to return to main menu.\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM drinking_water WHERE Id = '{recordId}')";
                int checkQuery = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (checkQuery == 0)
                {
                    Console.WriteLine($"\n\nRecord with Id {recordId} doesn't exist.\n\n");
                    connection.Close();
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    return;
                }
                string date = GetDateInput();
                if (date == "0") return; // user cancelled and returned to menu

                int quantity = GetNumberInput("\n\nPlease insert the new number of glasses or other measure of your choice (no decmials allowed)\n\n");

                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText = $"UPDATE drinking_water SET Date = '{date}', Quantity = {quantity} WHERE Id = '{recordId}'";

                tableCmd.ExecuteNonQuery();

                connection.Close();
            }
        }

        internal static string GetDateInput()
        {
            Console.WriteLine("\n\nPlease insert the date: (Format: dd-mm-yyyy). Type 0 to return to main menu.");
            string dateInput = Console.ReadLine() ?? "";
            if (dateInput == "0") return "0"; // return sentinel immediately

            while (!DateTime.TryParseExact(dateInput, "dd-MM-yyyy", new CultureInfo("en-US"), DateTimeStyles.None, out _))
            {
                Console.WriteLine("\n\nInvalid date format. Please use the (format: dd-mm-yyyy). Type 0 to return to main menu.");
                dateInput = Console.ReadLine() ?? "";
                if (dateInput == "0") return "0"; // allow cancel during retry
            }
            return dateInput;
        }

        internal static int GetNumberInput(string message)
        {
            Console.WriteLine(message);
            string numberInput = Console.ReadLine() ?? "";
            if (numberInput == "0") GetUserInput();

            while (!int.TryParse(numberInput, out _))
            {
                Console.WriteLine("\n\nInvalid input. Please insert a valid number. Type 0 to return to main menu.");
                numberInput = Console.ReadLine() ?? "";
            }
            int finalInput = Convert.ToInt32(numberInput);
            return finalInput;
        }

        // Safely parse dates from DB supporting multiple common formats
        private static DateTime ParseDateSafe(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return DateTime.MinValue;

            var formats = new[] { "dd-MM-yyyy", "dd-MM-yy", "yyyy-MM-dd", "MM-dd-yy", "MM-dd-yyyy" };
            if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            if (DateTime.TryParse(dateStr, out dt)) return dt;

            Console.WriteLine($"Warning: could not parse date '{dateStr}'. Using DateTime.MinValue.");
            return DateTime.MinValue;
        }
    }

    public class DrinkingWater
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }
}
