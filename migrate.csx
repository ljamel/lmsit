using Microsoft.Data.SqlClient;

var connectionString = "Server=127.0.0.1,1433;Database=NomDeTaBase;User Id=sa;Password=StrongPass123!;Encrypt=False;TrustServerCertificate=True;";

var sql = @"
-- Add Price and IsFree columns to Courses table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Courses') AND name = 'Price')
BEGIN
    ALTER TABLE Courses ADD Price decimal(18,2) NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Courses') AND name = 'IsFree')
BEGIN
    ALTER TABLE Courses ADD IsFree bit NOT NULL DEFAULT 1;
END

-- Create Payments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE Payments (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId nvarchar(max) NOT NULL,
        CourseId int NOT NULL,
        Amount decimal(18,2) NOT NULL,
        Currency nvarchar(max) NOT NULL,
        StripePaymentIntentId nvarchar(max) NOT NULL,
        Status nvarchar(max) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CompletedAt datetime2 NULL,
        FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_Payments_CourseId ON Payments(CourseId);
END

-- Create CourseEnrollments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CourseEnrollments')
BEGIN
    CREATE TABLE CourseEnrollments (
        Id int IDENTITY(1,1) PRIMARY KEY,
        UserId nvarchar(max) NOT NULL,
        CourseId int NOT NULL,
        PaymentId int NULL,
        EnrolledAt datetime2 NOT NULL,
        ExpiresAt datetime2 NULL,
        IsActive bit NOT NULL,
        FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE,
        FOREIGN KEY (PaymentId) REFERENCES Payments(Id)
    );
    
    CREATE INDEX IX_CourseEnrollments_CourseId ON CourseEnrollments(CourseId);
    CREATE INDEX IX_CourseEnrollments_PaymentId ON CourseEnrollments(PaymentId);
END
";

try
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Connected to database");
    
    using var command = new SqlCommand(sql, connection);
    command.CommandTimeout = 60;
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine("✓ Migration applied successfully!");
    Console.WriteLine("  - Added Price and IsFree columns to Courses");
    Console.WriteLine("  - Created Payments table");
    Console.WriteLine("  - Created CourseEnrollments table");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    return 1;
}

return 0;
