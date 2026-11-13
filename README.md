# SQL First: School Manager

A small **C# / .NET 10 console application** that manages a school's programs, courses and students in **SQL Server Express**. Every database call is hand-written SQL sent with `Microsoft.Data.SqlClient`; there is no ORM. It was written to practise SQL from C# before moving on to Entity Framework (Lexicon Full-Stack course).

The console UI is drawn by the program itself: a block-letter banner, arrow-key menus, bordered tables and a details card.

## What it does

| Menu | What you can do |
|---|---|
| **Show** | List all programs (with their number of courses and students), all courses, or all students |
| **Edit** | Add or remove a program, a course or a student. Removing a program leaves its courses and students without a program |
| **Search student** | Search by name, email or Id. One hit opens the student's details: program, the courses they attend and their total points |
| **Reset database** | Delete all data and create 2 programs, 5 courses and 20 new random students |

Move with the arrow keys and Enter, or press a number as a shortcut. Esc goes back.

## The database

`School.sql` creates the `School` database and its three tables, and fills them with sample data. The program runs the script by itself when the database or a table is missing, so there is nothing to set up by hand.

```
Programs            Courses              Students
--------            -------              --------
Id   (PK)   <----   ProgramId (FK)       Id   (PK)
Name                Id   (PK)            FirstName
Description         Name                 LastName
            <----   Points               Email
                                         BirthDate
            <-------------------------   ProgramId (FK)
```

- A **program** (for example ".NET Developer") has several courses.
- A **course** belongs to one program.
- A **student** is enrolled in one program and attends all courses in it.
- Both foreign keys use `ON DELETE SET NULL`: deleting a program keeps its courses and students.

The 20 students are random: the script combines first and last names with `CROSS JOIN`, picks 20 with `ORDER BY NEWID()` and gives each a random birth date and program.

## SQL used

| What | Where |
|---|---|
| `LEFT JOIN` to show a course's or student's program, also when it has none | `GetCourses`, `GetStudents`, `SearchStudents` |
| Subqueries with `COUNT(*)` for the number of courses and students per program | `GetPrograms` |
| `INSERT ... OUTPUT INSERTED.Id` to get the new row's Id back | `AddProgram`, `AddCourse`, `AddStudent` |
| `LIKE` search on name and email, or an exact match on Id | `SearchStudents` |
| `JOIN` from a student through the program to its courses | `GetStudentCourses` |
| Parameters (`@id`, `@name`, ...) for every value typed by the user, so input is never pasted into the SQL text | all of `Database.cs` |
| `CREATE DATABASE`, `CREATE TABLE`, foreign keys, `DROP TABLE IF EXISTS` | `School.sql` |

## Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and SQL Server Express running as `localhost\SQLEXPRESS` with Windows sign-in.

```
dotnet run
```

To use another server, change `ServerConnection` at the top of `Database.cs`.

## The files

| File | Content |
|---|---|
| `Program.cs` | The menus and what each choice does |
| `Database.cs` | All communication with the database, as plain SQL commands |
| `Ui.cs` | Console drawing: banner, arrow-key menu, tables, cards and prompts |
| `School.sql` | Creates the database, the tables and the sample data. Can also be run by hand in SSMS |
