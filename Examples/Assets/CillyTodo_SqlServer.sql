-- Drop old data
DROP PROCEDURE IF EXISTS [Cilly].[CreateTodoItem];
DROP PROCEDURE IF EXISTS [Cilly].[DeleteTodoItem];
DROP PROCEDURE IF EXISTS [Cilly].[GetTodoItemById];
DROP PROCEDURE IF EXISTS [Cilly].[GetAllTodoItems];
DROP PROCEDURE IF EXISTS [Cilly].[UpdateTodoItem];
DROP PROCEDURE IF EXISTS [Cilly].[MarkItemAsComplete];
DROP TABLE IF EXISTS [Cilly].[Todo];
DROP SCHEMA IF EXISTS [Cilly];
GO
CREATE SCHEMA [Cilly];
GO
CREATE TABLE [Cilly].[Todo] (
  Id INT IDENTITY(1,1),
  Name VARCHAR(200) NOT NULL,
  Description VARCHAR(400) NULL,
  Completed BIT NOT NULL DEFAULT (0)
);
INSERT INTO [Cilly].Todo (Name, Description, Completed) VALUES
('Wake up', 'zzzzzz', 1),
('Write Code', 'Gotta grind', 0),
('Contemplate life', 'Why am I coding?', 0),
('Sleep', NULL, 0);
GO

CREATE PROCEDURE [Cilly].[CreateTodoItem] @Name VARCHAR(200), @Description VARCHAR(400), @Id INT OUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [Cilly].Todo (Name, Description) VALUES 
    (@Name, @Description);
    SET @Id = SCOPE_IDENTITY();
END
GO
CREATE PROCEDURE [Cilly].[DeleteTodoItem] @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [Cilly].[Todo]
    WHERE [Id] = @Id;
END
GO
CREATE PROCEDURE [Cilly].[GetTodoItemById] @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, Completed FROM [Cilly].[Todo]
    WHERE [Id] = @Id;
END
GO
CREATE PROCEDURE [Cilly].[GetAllTodoItems]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, Completed FROM [Cilly].[Todo];
END
GO
CREATE PROCEDURE [Cilly].[UpdateTodoItem] @Id INT, @Name VARCHAR(200), @Description VARCHAR(400), @Completed BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [Cilly].[Todo]
    SET [Name] = @Name,
        [Description] = @Description,
        [Completed] = @Completed
    WHERE [Id] = @Id;
END
GO
CREATE PROCEDURE [Cilly].[MarkItemAsComplete] @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [Cilly].[Todo]
    SET [Completed] = 1
    WHERE [Id] = @Id;
END
GO