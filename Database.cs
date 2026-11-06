using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SQL_First;

/// <summary>
/// All communication with the School database. Every method uses plain SQL commands.
/// </summary>
static class Database
{
    const string ServerConnection = @"Server=localhost\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True;";
    const string SchoolConnection = ServerConnection + "Database=School;";

    // ------------------------------------------------------------------ setup

    /// <summary>Creates the database (by running School.sql) if it or any table is missing.</summary>
    public static void EnsureCreated()
    {
        using var conn = new SqlConnection(ServerConnection + "Database=master;");
        conn.Open();

        string sql = @"SELECT CASE WHEN OBJECT_ID('School.dbo.Programs') IS NOT NULL
                                    AND OBJECT_ID('School.dbo.Courses')  IS NOT NULL
                                    AND OBJECT_ID('School.dbo.Students') IS NOT NULL
                               THEN 1 ELSE 0 END";
        using var cmd = new SqlCommand(sql, conn);
        if ((int)cmd.ExecuteScalar() == 0)
            RunSetupScript(conn);
    }

    /// <summary>Drops and recreates all tables with new random data.</summary>
    public static void Reset()
    {
        using var conn = new SqlConnection(ServerConnection + "Database=master;");
        conn.Open();
        RunSetupScript(conn);
    }

    static void RunSetupScript(SqlConnection conn)
    {
        string script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "School.sql"));

        // "GO" is not SQL, it's a batch separator used by tools - so split on it and run each batch.
        foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            using var cmd = new SqlCommand(batch, conn);
            cmd.ExecuteNonQuery();
        }
    }

    // ------------------------------------------------------------------ show

    public static List<string[]> GetPrograms() => Query(@"
        SELECT p.Id, p.Name, p.Description,
               (SELECT COUNT(*) FROM Courses  c WHERE c.ProgramId = p.Id) AS Courses,
               (SELECT COUNT(*) FROM Students s WHERE s.ProgramId = p.Id) AS Students
        FROM Programs p
        ORDER BY p.Id");

    public static List<string[]> GetCourses() => Query(@"
        SELECT c.Id, c.Name, c.Points, p.Name AS Program
        FROM Courses c
        LEFT JOIN Programs p ON p.Id = c.ProgramId
        ORDER BY c.Id");

    public static List<string[]> GetStudents() => Query(@"
        SELECT s.Id, s.FirstName + ' ' + s.LastName AS Name, s.Email, s.BirthDate, p.Name AS Program
        FROM Students s
        LEFT JOIN Programs p ON p.Id = s.ProgramId
        ORDER BY s.Id");

    public static bool ProgramExists(int id) =>
        (int)Scalar("SELECT COUNT(*) FROM Programs WHERE Id = @id", new SqlParameter("@id", id)) > 0;

    // ------------------------------------------------------------------ add

    public static int AddProgram(string name, string? description) => (int)Scalar(@"
        INSERT INTO Programs (Name, Description)
        OUTPUT INSERTED.Id
        VALUES (@name, @description)",
        new SqlParameter("@name", name),
        new SqlParameter("@description", OrNull(description)));

    public static int AddCourse(string name, int points, int? programId) => (int)Scalar(@"
        INSERT INTO Courses (Name, Points, ProgramId)
        OUTPUT INSERTED.Id
        VALUES (@name, @points, @programId)",
        new SqlParameter("@name", name),
        new SqlParameter("@points", points),
        new SqlParameter("@programId", OrNull(programId)));

    public static int AddStudent(string firstName, string lastName, string email, DateTime? birthDate, int? programId) => (int)Scalar(@"
        INSERT INTO Students (FirstName, LastName, Email, BirthDate, ProgramId)
        OUTPUT INSERTED.Id
        VALUES (@first, @last, @email, @birth, @programId)",
        new SqlParameter("@first", firstName),
        new SqlParameter("@last", lastName),
        new SqlParameter("@email", email),
        new SqlParameter("@birth", OrNull(birthDate)),
        new SqlParameter("@programId", OrNull(programId)));

    // ------------------------------------------------------------------ remove
    // Each returns the number of deleted rows (0 = the id did not exist).

    public static int DeleteProgram(int id) => NonQuery("DELETE FROM Programs WHERE Id = @id", new SqlParameter("@id", id));
    public static int DeleteCourse(int id)  => NonQuery("DELETE FROM Courses  WHERE Id = @id", new SqlParameter("@id", id));
    public static int DeleteStudent(int id) => NonQuery("DELETE FROM Students WHERE Id = @id", new SqlParameter("@id", id));

    // ------------------------------------------------------------------ search

    public static List<string[]> SearchStudents(string text) => Query(@"
        SELECT s.Id, s.FirstName + ' ' + s.LastName AS Name, s.Email, p.Name AS Program
        FROM Students s
        LEFT JOIN Programs p ON p.Id = s.ProgramId
        WHERE s.FirstName + ' ' + s.LastName LIKE @like
           OR s.Email LIKE @like
           OR CAST(s.Id AS NVARCHAR(10)) = @text
        ORDER BY s.LastName, s.FirstName",
        new SqlParameter("@like", "%" + text + "%"),
        new SqlParameter("@text", text));

    /// <summary>Id, Name, Email, BirthDate, Program, ProgramDescription — or null if not found.</summary>
    public static string[]? GetStudent(int id) => Query(@"
        SELECT s.Id, s.FirstName + ' ' + s.LastName, s.Email, s.BirthDate, p.Name, p.Description
        FROM Students s
        LEFT JOIN Programs p ON p.Id = s.ProgramId
        WHERE s.Id = @id",
        new SqlParameter("@id", id)).FirstOrDefault();

    /// <summary>The courses a student attends = all courses in the student's program.</summary>
    public static List<string[]> GetStudentCourses(int studentId) => Query(@"
        SELECT c.Id, c.Name, c.Points
        FROM Students s
        JOIN Courses c ON c.ProgramId = s.ProgramId
        WHERE s.Id = @id
        ORDER BY c.Name",
        new SqlParameter("@id", studentId));

    // ------------------------------------------------------------------ helpers

    static SqlConnection Open()
    {
        var conn = new SqlConnection(SchoolConnection);
        conn.Open();
        return conn;
    }

    /// <summary>Runs a SELECT and returns every row as an array of display strings.</summary>
    static List<string[]> Query(string sql, params SqlParameter[] parameters)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        using SqlDataReader reader = cmd.ExecuteReader();

        var rows = new List<string[]>();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i) switch
                {
                    DBNull => "-",
                    DateTime d => d.ToString("yyyy-MM-dd"),
                    var v => v.ToString() ?? ""
                };
            rows.Add(row);
        }
        return rows;
    }

    static object Scalar(string sql, params SqlParameter[] parameters)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteScalar();
    }

    static int NonQuery(string sql, params SqlParameter[] parameters)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteNonQuery();
    }

    static object OrNull(object? value) =>
        value is null || (value is string s && string.IsNullOrWhiteSpace(s)) ? DBNull.Value : value;
}
