-- =============================================================
--  School database
--  Creates the database, the tables and fills them with data.
--  Running this script again resets everything.
--  (The C# app runs this file automatically if the database is missing.)
-- =============================================================

IF DB_ID('School') IS NULL
    CREATE DATABASE School;
GO

USE School;
GO

-- Drop child tables first, since they reference Programs
DROP TABLE IF EXISTS Students;
DROP TABLE IF EXISTS Courses;
DROP TABLE IF EXISTS Programs;
GO

-- A program (e.g. ".NET Developer") contains several courses
CREATE TABLE Programs(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL
);

-- A course belongs to one program
CREATE TABLE Courses(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(100) NOT NULL,
    Points    INT NOT NULL,
    ProgramId INT NULL,
    FOREIGN KEY (ProgramId) REFERENCES Programs(Id) ON DELETE SET NULL
);

-- A student is enrolled in one program and attends all courses in that program
CREATE TABLE Students(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50)  NOT NULL,
    LastName  NVARCHAR(50)  NOT NULL,
    Email     NVARCHAR(100) NOT NULL,
    BirthDate DATE NULL,
    ProgramId INT NULL,
    FOREIGN KEY (ProgramId) REFERENCES Programs(Id) ON DELETE SET NULL
);
GO

-- ---------- 2 Programs ----------
INSERT INTO Programs (Name, Description)
VALUES (N'.NET Developer', N'Full stack development with C#, SQL and ASP.NET'),
       (N'Java Developer', N'Backend development with Java and Spring');

-- ---------- 5 Courses ----------
INSERT INTO Courses (Name, Points, ProgramId)
VALUES (N'C# Fundamentals',      40, 1),
       (N'SQL & Databases',      30, 1),
       (N'ASP.NET Core',         50, 1),
       (N'Java Fundamentals',    40, 2),
       (N'Spring Boot',          50, 2);

-- ---------- 20 Students (random) ----------
-- Random first name + last name combinations, random birth date and random program
DECLARE @FirstNames TABLE (Name NVARCHAR(50));
DECLARE @LastNames  TABLE (Name NVARCHAR(50));

INSERT INTO @FirstNames VALUES (N'Erik'), (N'Anna'), (N'Lars'), (N'Sara'), (N'Johan'), (N'Emma'),
                               (N'Oskar'), (N'Maja'), (N'Viktor'), (N'Elin'), (N'Lukas'), (N'Ida');
INSERT INTO @LastNames  VALUES (N'Andersson'), (N'Johansson'), (N'Karlsson'), (N'Nilsson'), (N'Eriksson'), (N'Larsson'),
                               (N'Olsson'), (N'Persson'), (N'Svensson'), (N'Lindqvist'), (N'Berg'), (N'Holm');

INSERT INTO Students (FirstName, LastName, Email, BirthDate, ProgramId)
SELECT TOP (20)
       f.Name,
       l.Name,
       LOWER(f.Name + '.' + l.Name) + '@school.se',
       DATEADD(DAY, ABS(CHECKSUM(NEWID())) % 9500, '1980-01-01'),   -- born 1980-2005
       1 + ABS(CHECKSUM(NEWID())) % 2                               -- program 1 or 2
FROM @FirstNames f
CROSS JOIN @LastNames l
ORDER BY NEWID();
GO

-- ---------- Quick check ----------
SELECT * FROM Programs;
SELECT * FROM Courses;
SELECT * FROM Students;
