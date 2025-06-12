using System.Configuration;
using MySql.Data.MySqlClient;

public static class Database
{
    public static MySqlConnection GetConnection()
    {
        string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
        var conn = new MySqlConnection(connStr);
        conn.Open();
        return conn;
    }
}
