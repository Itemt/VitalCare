-- Print start message
PRINT 'Starting database initialization script...';
GO

-- Check if database exists, if not create it
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CitasEPSDB')
BEGIN
    PRINT 'Creating CitasEPSDB database...';
    CREATE DATABASE CitasEPSDB;
    PRINT 'CitasEPSDB database created successfully.';
END
ELSE
BEGIN
    PRINT 'CitasEPSDB database already exists.';
END
GO

-- Switch to master database for login creation
USE master;
GO

-- Check if login exists, if not create it
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'testuser')
BEGIN
    PRINT 'Creating testuser login...';
    CREATE LOGIN testuser WITH PASSWORD = 'Test1234', DEFAULT_DATABASE = CitasEPSDB;
    PRINT 'testuser login created successfully.';
END
ELSE
BEGIN
    PRINT 'testuser login already exists.';
END
GO

-- Switch to application database for user creation
USE CitasEPSDB;
GO

-- Check if user exists, if not create it
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'testuser')
BEGIN
    PRINT 'Creating testuser database user...';
    CREATE USER testuser FOR LOGIN testuser;
    PRINT 'testuser database user created successfully.';
END
ELSE
BEGIN
    PRINT 'testuser database user already exists.';
END
GO

-- Grant permissions to the user
PRINT 'Granting permissions to testuser...';
ALTER ROLE db_owner ADD MEMBER testuser;
PRINT 'Permissions granted successfully.';
GO

-- Optional: Create a sample table if it doesn't exist for testing
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestTable')
BEGIN
    PRINT 'Creating sample table for testing...';
    CREATE TABLE TestTable (
        ID INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        CreatedDate DATETIME DEFAULT GETDATE()
    );
    PRINT 'Sample table created successfully.';
    
    -- Insert a test record
    INSERT INTO TestTable (Name) VALUES ('Test Record');
    PRINT 'Test record inserted.';
END
ELSE
BEGIN
    PRINT 'Sample table already exists.';
END
GO

PRINT 'Database initialization completed successfully.';
GO
