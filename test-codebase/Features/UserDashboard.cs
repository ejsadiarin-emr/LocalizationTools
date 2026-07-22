using System;
using System.Collections.Generic;

namespace TestCodebase.Features
{
    /// <summary>
    /// LOC001: String in conditional
    /// LOC002: String in data access
    /// LOC003: String in equality comparison
    /// LOC004: String concatenation in output
    /// </summary>
    public class UserDashboard
    {
        private readonly ILogger _logger;
        private readonly Database _db;

        public UserDashboard(ILogger logger, Database db)
        {
            _logger = logger;
            _db = db;
        }

        // LOC001: String literal in conditional
        public string GetStatus(string input)
        {
            if (input == "active")
            {
                return "User is active";
            }
            return "Unknown status";
        }

        // LOC002: String literal in data access
        public User FindUser(string userId)
        {
            return _db.Find("SELECT * FROM Users WHERE Id = " + userId);
        }

        // LOC003: String literal in equality comparison
        public bool IsAdmin(string role)
        {
            return role == "admin";
        }

        // LOC004: String concatenation in Console output
        public void PrintUserInfo(string name, int age)
        {
            Console.WriteLine("User: " + name + ", Age: " + age);
        }

        // LOC004: String concatenation in logging
        public void LogAction(string action, string userId)
        {
            _logger.Log("User " + userId + " performed " + action);
        }
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public class Database
    {
        public User Find(string query) => new User();
    }

    public interface ILogger
    {
        void Log(string message);
    }
}
