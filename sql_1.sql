-- Database.. --comment
-- server     

IF DB_ID('space_port') IS NULL
    CREATE DATABASE space_port;
GO

USE space_port;
GO

DROP TABLE IF EXISTS rocket_crew;   -- child table first, since it references rockets
DROP TABLE IF EXISTS rockets;
GO

CREATE TABLE rockets(
    id INT PRIMARY KEY,
    rocket_name VARCHAR(100),
    manufacturer VARCHAR(100)
);                   -- skapar en tabell vid namn "rockets"

CREATE TABLE rocket_crew(
    id INT PRIMARY KEY,
    crew_name VARCHAR(100),
    rocket_id INT,
    FOREIGN KEY (rocket_id) REFERENCES rockets(id)
);

INSERT INTO rockets (id, rocket_name, manufacturer)
VALUES (1, 'Falcon 9', 'SpaceX'),
       (2, 'Ariane 6', 'ArianeGroup');

SELECT * FROM rockets;

-- CRUD  Create, Read, Update and Delete


INSERT INTO rocket_crew VALUES (1, 'Messi', 1);
INSERT INTO rocket_crew VALUES (2, 'Ronaldo', 2);