using System.Text;

namespace SQL_First;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.Title = "School Manager";

        try
        {
            Database.EnsureCreated();   // creates the School database from School.sql if it is missing
        }
        catch (Exception ex)
        {
            Ui.Error("Could not connect to the database: " + ex.Message);
            return;
        }

        while (true)
        {
            int choice = Ui.Menu("Main menu", ["Show", "Edit  (Add / Remove)", "Search student", "Reset database", "Exit"]);
            try
            {
                switch (choice)
                {
                    case 0: ShowMenu(); break;
                    case 1: EditMenu(); break;
                    case 2: SearchStudent(); break;
                    case 3: ResetDatabase(); break;
                    case 4:
                    case -1:
                        if (!Console.IsOutputRedirected) Console.Clear();
                        Ui.Write("\n  Goodbye! 👋\n\n", ConsoleColor.Cyan);
                        return;
                }
            }
            catch (Exception ex)
            {
                Ui.Error("Database error: " + ex.Message);
                Ui.Pause();
            }
        }
    }

    // ================================================================== SHOW

    static void ShowMenu()
    {
        while (true)
        {
            int choice = Ui.Menu("Show", ["Programs", "Courses", "Students", "← Back"]);
            if (choice is -1 or 3) return;

            string[] titles = ["Show · Programs", "Show · Courses", "Show · Students"];
            Ui.Header(titles[choice]);
            if (choice == 0) ShowPrograms();
            if (choice == 1) ShowCourses();
            if (choice == 2) ShowStudents();
            Ui.Pause();
        }
    }

    static void ShowPrograms() => Ui.Table(["Id", "Program", "Description", "Courses", "Students"], Database.GetPrograms());
    static void ShowCourses()  => Ui.Table(["Id", "Course", "Points", "Program"], Database.GetCourses());
    static void ShowStudents() => Ui.Table(["Id", "Name", "Email", "Born", "Program"], Database.GetStudents());

    // ================================================================== EDIT

    static void EditMenu()
    {
        string[] entities = ["Program", "Course", "Student"];
        while (true)
        {
            int entity = Ui.Menu("Edit", ["Programs", "Courses", "Students", "← Back"]);
            if (entity is -1 or 3) return;

            string name = entities[entity];
            int action = Ui.Menu($"Edit · {name}s", [$"Add {name.ToLower()}", $"Remove {name.ToLower()}", "← Back"]);
            if (action is -1 or 2) continue;

            Ui.Header($"{(action == 0 ? "Add" : "Remove")} · {name}");
            switch ((entity, action))
            {
                case (0, 0): AddProgram(); break;
                case (0, 1): RemoveProgram(); break;
                case (1, 0): AddCourse(); break;
                case (1, 1): RemoveCourse(); break;
                case (2, 0): AddStudent(); break;
                case (2, 1): RemoveStudent(); break;
            }
            Ui.Pause();
        }
    }

    // ---------- Programs

    static void AddProgram()
    {
        Ui.Info("Leave a required field empty to cancel.\n");
        string name = Ui.Prompt("Program name");
        if (name == "") { Ui.Warn("Cancelled."); return; }
        string description = Ui.Prompt("Description (optional)");

        int id = Database.AddProgram(name, description);
        Ui.Success($"Program '{name}' added with Id {id}.");
    }

    static void RemoveProgram()
    {
        ShowPrograms();
        Console.WriteLine();
        int? id = Ui.PromptInt("Id of program to remove (empty = cancel)");
        if (id is null) { Ui.Warn("Cancelled."); return; }

        Ui.Info("Courses and students in this program will be left without a program.");
        if (!Ui.Confirm($"Remove program {id}?")) { Ui.Warn("Cancelled."); return; }

        if (Database.DeleteProgram(id.Value) > 0) Ui.Success($"Program {id} removed.");
        else Ui.Error($"No program with Id {id}.");
    }

    // ---------- Courses

    static void AddCourse()
    {
        Ui.Info("Leave a required field empty to cancel.\n");
        string name = Ui.Prompt("Course name");
        if (name == "") { Ui.Warn("Cancelled."); return; }

        int? points = Ui.PromptInt("Points");
        if (points is null) { Ui.Warn("Cancelled."); return; }

        int? programId = AskForProgram();

        int id = Database.AddCourse(name, points.Value, programId);
        Ui.Success($"Course '{name}' added with Id {id}.");
    }

    static void RemoveCourse()
    {
        ShowCourses();
        Console.WriteLine();
        int? id = Ui.PromptInt("Id of course to remove (empty = cancel)");
        if (id is null) { Ui.Warn("Cancelled."); return; }
        if (!Ui.Confirm($"Remove course {id}?")) { Ui.Warn("Cancelled."); return; }

        if (Database.DeleteCourse(id.Value) > 0) Ui.Success($"Course {id} removed.");
        else Ui.Error($"No course with Id {id}.");
    }

    // ---------- Students

    static void AddStudent()
    {
        Ui.Info("Leave a required field empty to cancel.\n");
        string first = Ui.Prompt("First name");
        if (first == "") { Ui.Warn("Cancelled."); return; }
        string last = Ui.Prompt("Last name");
        if (last == "") { Ui.Warn("Cancelled."); return; }

        string suggested = $"{first}.{last}@school.se".ToLower();
        string email = Ui.Prompt($"Email (empty = {suggested})");
        if (email == "") email = suggested;

        DateTime? birthDate = null;
        while (true)
        {
            string input = Ui.Prompt("Birth date yyyy-mm-dd (optional)");
            if (input == "") break;
            if (DateTime.TryParse(input, out var d)) { birthDate = d; break; }
            Ui.Error("Invalid date, try again.");
        }

        int? programId = AskForProgram();

        int id = Database.AddStudent(first, last, email, birthDate, programId);
        Ui.Success($"Student '{first} {last}' added with Id {id}.");
    }

    static void RemoveStudent()
    {
        ShowStudents();
        Console.WriteLine();
        int? id = Ui.PromptInt("Id of student to remove (empty = cancel)");
        if (id is null) { Ui.Warn("Cancelled."); return; }
        if (!Ui.Confirm($"Remove student {id}?")) { Ui.Warn("Cancelled."); return; }

        if (Database.DeleteStudent(id.Value) > 0) Ui.Success($"Student {id} removed.");
        else Ui.Error($"No student with Id {id}.");
    }

    /// <summary>Lets the user pick an existing program. Returns null for "no program".</summary>
    static int? AskForProgram()
    {
        Console.WriteLine();
        ShowPrograms();
        while (true)
        {
            int? id = Ui.PromptInt("Program Id (empty = no program)");
            if (id is null || Database.ProgramExists(id.Value)) return id;
            Ui.Error($"No program with Id {id}.");
        }
    }

    // ================================================================== SEARCH

    static void SearchStudent()
    {
        Ui.Header("Search student");
        string text = Ui.Prompt("Name, email or Id");
        if (text == "") return;

        List<string[]> hits = Database.SearchStudents(text);
        if (hits.Count == 0)
        {
            Ui.Warn($"No student matches '{text}'.");
            Ui.Pause();
            return;
        }

        int id;
        if (hits.Count == 1)
        {
            id = int.Parse(hits[0][0]);
        }
        else
        {
            Console.WriteLine();
            Ui.Table(["Id", "Name", "Email", "Program"], hits);
            Console.WriteLine();
            int? picked = Ui.PromptInt("Id to view (empty = cancel)");
            if (picked is null) return;
            id = picked.Value;
        }

        ShowStudentDetails(id);
        Ui.Pause();
    }

    static void ShowStudentDetails(int id)
    {
        string[]? s = Database.GetStudent(id);
        if (s is null)
        {
            Ui.Error($"No student with Id {id}.");
            return;
        }

        List<string[]> courses = Database.GetStudentCourses(id);
        int totalPoints = courses.Sum(c => int.Parse(c[2]));

        Ui.Header("Student details");
        Ui.Card("STUDENT",
        [
            ("Id", s[0]),
            ("Name", s[1]),
            ("Email", s[2]),
            ("Born", s[3]),
            ("Program", s[4]),
            ("About", s[5]),
        ]);

        Console.WriteLine();
        Ui.Write("  Courses attended\n", ConsoleColor.White);
        Ui.Table(["Id", "Course", "Points"], courses);
        if (courses.Count > 0)
            Ui.Write($"  Total points: {totalPoints}\n", ConsoleColor.Cyan);
    }

    // ================================================================== RESET

    static void ResetDatabase()
    {
        Ui.Header("Reset database");
        Ui.Warn("This deletes ALL data and creates 2 programs, 5 courses and 20 new random students.");
        if (!Ui.Confirm("Are you sure?")) return;

        Database.Reset();
        Ui.Success("Database reset with fresh random data.");
        Ui.Pause();
    }
}
