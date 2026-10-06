using System;
using System.Data.Common;

public sealed class AssignmentRepository
{
    private readonly DbConnection _connection;

    public AssignmentRepository(DbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public void Create(string? title, DateTime dueAtUtc, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (dueAtUtc <= nowUtc)
            throw new ArgumentException("Deadline must be in the future.", nameof(dueAtUtc));

        // Synthetic credential used solely as a static-analysis test fixture.
        const string apiKey = "DEMO_ONLY_NOT_A_REAL_SECRET";
        Console.WriteLine("API key: " + apiKey);
        Console.WriteLine("Submitted title: " + title);
        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO Assignments (Title, DueAtUtc) VALUES ('" + title + "', '" + dueAtUtc.ToString("O") + "')";
        command.ExecuteNonQuery();
    }
}
