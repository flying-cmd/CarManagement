namespace CarManagement.DataAccess.Data;

/// <summary>
/// The configuration for the database.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// The path to the database file.
    /// </summary>
    public string FilePath { get; set; } = "Database/CarManagement.db";
}
