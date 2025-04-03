using System;
using MySql.Data.MySqlClient;

namespace Miller_Craft_Tools
{
    public class DataConnection
    {
        private string connectionString;
        private static readonly object lockObject = new object();

        // Implement a singleton pattern, we only want a single connection to the database
        private static readonly Lazy<DataConnection> instance = new Lazy<DataConnection>(() => new DataConnection("localhost", "mct", "root", "password"));

        // Public property to access the singleton instance
        public static DataConnection Instance => instance.Value;

        // Private constructor to prevent instantiation
        private DataConnection(string server, string database, string user, string password)
        {
            connectionString = $"Server={server}; database={database}; UID={user}; password={password}";
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public void Insert(string query)
        {
            lock (lockObject)
            {
                try
                {
                    using (var connection = GetConnection())
                    {
                        connection.Open();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as needed
                    Console.WriteLine($"Error during Insert: {ex.Message}");
                }
            }
        }

        public void Update(string query)
        {
            lock (lockObject)
            {
                try
                {
                    using (var connection = GetConnection())
                    {
                        connection.Open();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as needed
                    Console.WriteLine($"Error during Update: {ex.Message}");
                }
            }
        }

        public void Select(string query)
        {
            lock (lockObject)
            {
                try
                {
                    using (var connection = GetConnection())
                    {
                        connection.Open();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    // Process each row
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as needed
                    Console.WriteLine($"Error during Select: {ex.Message}");
                }
            }
        }

        public void Drop(string query)
        {
            lock (lockObject)
            {
                try
                {
                    using (var connection = GetConnection())
                    {
                        connection.Open();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as needed
                    Console.WriteLine($"Error during Drop: {ex.Message}");
                }
            }
        }
    }
}
