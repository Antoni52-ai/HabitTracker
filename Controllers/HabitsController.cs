using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

[ApiController]
[Route("api/[controller]")]
public class HabitsController : ControllerBase
{
    private readonly string _dbPath = "habits.db";

    public HabitsController()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();

        // Создаём таблицу с IsCompleted
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Habits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL DEFAULT 0
            )";
        command.ExecuteNonQuery();

        // Если таблица пустая — добавим примеры
        command.CommandText = "SELECT COUNT(*) FROM Habits";
        var count = (long)command.ExecuteScalar();
        if (count == 0)
        {
            command.CommandText = @"
                INSERT INTO Habits (Name, IsCompleted) VALUES 
                ('Пить воду', 0),
                ('Прогулка', 0),
                ('Читать книгу', 0)";
            command.ExecuteNonQuery();
        }
    }

    [HttpGet]
    public List<HabitItem> Get()
    {
        var habits = new List<HabitItem>();
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, IsCompleted FROM Habits";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            habits.Add(new HabitItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                IsCompleted = reader.GetBoolean(2)
            });
        return habits;
    }

    [HttpPost]
    public IActionResult Add([FromBody] HabitDto habit)
    {
        if (string.IsNullOrWhiteSpace(habit?.Name))
            return BadRequest("Название привычки не может быть пустым.");

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Habits (Name) VALUES (@name)";
        command.Parameters.AddWithValue("@name", habit.Name.Trim());
        command.ExecuteNonQuery();

        return Ok();
    }

    [HttpPut("{id}")]
    public IActionResult Toggle(int id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Habits SET IsCompleted = NOT IsCompleted WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        var rows = command.ExecuteNonQuery();
        return rows > 0 ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Habits WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        var rows = command.ExecuteNonQuery();
        return rows > 0 ? Ok() : NotFound();
    }
}

// DTO и модель
public class HabitDto
{
    public string? Name { get; set; }
}

public class HabitItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}