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

        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO Assignments (Title, DueAtUtc) VALUES (@title, @dueAtUtc)";
        var titleParameter = command.CreateParameter();
        titleParameter.ParameterName = "@title";
        titleParameter.Value = title;
        command.Parameters.Add(titleParameter);
        var deadlineParameter = command.CreateParameter();
        deadlineParameter.ParameterName = "@dueAtUtc";
        deadlineParameter.Value = dueAtUtc;
        command.Parameters.Add(deadlineParameter);
        command.ExecuteNonQuery();
    }
}
