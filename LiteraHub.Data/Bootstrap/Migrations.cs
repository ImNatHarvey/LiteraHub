namespace LiteraHub.Data.Bootstrap;

internal sealed record Migration(int Version, string Description, string[] Statements);

internal static class Migrations
{
    public static IReadOnlyList<Migration> All { get; } = new[]
    {
        new Migration(1, "Create core tables and indexes", V1()),
        new Migration(2, "Seed roles, statuses, categories, settings", V2()),
    };

    private static string Index(string name, string table, string columns) =>
        $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'dbo.{table}')) " +
        $"CREATE INDEX {name} ON dbo.{table} ({columns});";

    private static string[] V1() => new[]
    {
        """
        IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
        CREATE TABLE dbo.Roles (
            RoleId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
            Name     NVARCHAR(50) NOT NULL CONSTRAINT UQ_Roles_Name UNIQUE,
            IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT 1
        );
        """,
        """
        IF OBJECT_ID(N'dbo.Students', N'U') IS NULL
        CREATE TABLE dbo.Students (
            StudentId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Students PRIMARY KEY,
            StudentNo    NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Students_StudentNo UNIQUE,
            FirstName    NVARCHAR(100) NOT NULL,
            MiddleName   NVARCHAR(100) NULL,
            LastName     NVARCHAR(100) NOT NULL,
            HouseNo      NVARCHAR(50)  NOT NULL,
            Street       NVARCHAR(150) NOT NULL,
            Municipality NVARCHAR(100) NOT NULL,
            CityProvince NVARCHAR(100) NOT NULL,
            ZipCode      NVARCHAR(10)  NOT NULL,
            BirthDate    DATE NOT NULL,
            Gender       NVARCHAR(20) NOT NULL,
            MobileNo     NVARCHAR(30) NOT NULL,
            LandlineNo   NVARCHAR(30) NULL,
            Email        NVARCHAR(150) NOT NULL,
            IsActive     BIT NOT NULL CONSTRAINT DF_Students_IsActive DEFAULT 1,
            CreatedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_Students_CreatedAt DEFAULT SYSUTCDATETIME()
        );
        """,
        """
        IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
        CREATE TABLE dbo.Users (
            UserId             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
            Username           NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
            PasswordHash       NVARCHAR(100) NOT NULL,
            RoleId             INT NOT NULL CONSTRAINT FK_Users_Roles REFERENCES dbo.Roles(RoleId),
            StudentId          INT NULL CONSTRAINT FK_Users_Students REFERENCES dbo.Students(StudentId),
            MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChange DEFAULT 0,
            IsActive           BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
            CreatedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME()
        );
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_StudentId' AND object_id = OBJECT_ID(N'dbo.Users'))
        CREATE UNIQUE INDEX UX_Users_StudentId ON dbo.Users (StudentId) WHERE StudentId IS NOT NULL;
        """,
        """
        IF OBJECT_ID(N'dbo.Authors', N'U') IS NULL
        CREATE TABLE dbo.Authors (
            AuthorId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Authors PRIMARY KEY,
            FirstName  NVARCHAR(100) NOT NULL,
            MiddleName NVARCHAR(100) NULL,
            LastName   NVARCHAR(100) NOT NULL,
            BirthDate  DATE NULL,
            Gender     NVARCHAR(20) NOT NULL,
            IsActive   BIT NOT NULL CONSTRAINT DF_Authors_IsActive DEFAULT 1,
            CreatedAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Authors_CreatedAt DEFAULT SYSUTCDATETIME()
        );
        """,
        """
        IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
        CREATE TABLE dbo.Categories (
            CategoryId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
            Name        NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
            Description NVARCHAR(500) NULL,
            IsActive    BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1
        );
        """,
        """
        IF OBJECT_ID(N'dbo.BookStatuses', N'U') IS NULL
        CREATE TABLE dbo.BookStatuses (
            StatusId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookStatuses PRIMARY KEY,
            Name         NVARCHAR(50) NOT NULL CONSTRAINT UQ_BookStatuses_Name UNIQUE,
            IsBorrowable BIT NOT NULL CONSTRAINT DF_BookStatuses_Borrowable DEFAULT 1,
            IsActive     BIT NOT NULL CONSTRAINT DF_BookStatuses_IsActive DEFAULT 1
        );
        """,
        """
        IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
        CREATE TABLE dbo.Books (
            BookId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
            Isbn            VARCHAR(20) NOT NULL CONSTRAINT UQ_Books_Isbn UNIQUE,
            Title           NVARCHAR(300) NOT NULL,
            Description     NVARCHAR(MAX) NULL,
            CategoryId      INT NOT NULL CONSTRAINT FK_Books_Categories REFERENCES dbo.Categories(CategoryId),
            TotalCopies     INT NOT NULL CONSTRAINT DF_Books_Total DEFAULT 1,
            AvailableCopies INT NOT NULL CONSTRAINT DF_Books_Available DEFAULT 1,
            StatusId        INT NOT NULL CONSTRAINT FK_Books_Statuses REFERENCES dbo.BookStatuses(StatusId),
            CoverPath       NVARCHAR(500) NULL,
            IsActive        BIT NOT NULL CONSTRAINT DF_Books_IsActive DEFAULT 1,
            CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Books_CreatedAt DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CK_Books_Copies CHECK (TotalCopies >= 0 AND AvailableCopies >= 0 AND AvailableCopies <= TotalCopies)
        );
        """,
        """
        IF OBJECT_ID(N'dbo.BookAuthors', N'U') IS NULL
        CREATE TABLE dbo.BookAuthors (
            BookId   INT NOT NULL CONSTRAINT FK_BookAuthors_Books REFERENCES dbo.Books(BookId),
            AuthorId INT NOT NULL CONSTRAINT FK_BookAuthors_Authors REFERENCES dbo.Authors(AuthorId),
            CONSTRAINT PK_BookAuthors PRIMARY KEY (BookId, AuthorId)
        );
        """,
        """
        IF OBJECT_ID(N'dbo.BorrowReturns', N'U') IS NULL
        CREATE TABLE dbo.BorrowReturns (
            BorrowId                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BorrowReturns PRIMARY KEY,
            StudentId               INT NOT NULL CONSTRAINT FK_BorrowReturns_Students REFERENCES dbo.Students(StudentId),
            BookId                  INT NOT NULL CONSTRAINT FK_BorrowReturns_Books REFERENCES dbo.Books(BookId),
            Isbn                    VARCHAR(20) NOT NULL,
            BorrowDate              DATE NOT NULL,
            DueDate                 DATE NOT NULL,
            ReturnDate              DATE NULL,
            Remarks                 NVARCHAR(500) NULL,
            Status                  NVARCHAR(20) NOT NULL CONSTRAINT DF_BorrowReturns_Status DEFAULT N'Borrowed'
                                    CONSTRAINT CK_BorrowReturns_Status CHECK (Status IN (N'Borrowed', N'Returned')),
            ReturnConditionStatusId INT NULL CONSTRAINT FK_BorrowReturns_Condition REFERENCES dbo.BookStatuses(StatusId),
            CreatedByUserId         INT NULL CONSTRAINT FK_BorrowReturns_Users REFERENCES dbo.Users(UserId),
            CreatedAt               DATETIME2(0) NOT NULL CONSTRAINT DF_BorrowReturns_CreatedAt DEFAULT SYSUTCDATETIME()
        );
        """,
        """
        IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
        CREATE TABLE dbo.Settings (
            SettingKey   NVARCHAR(50)  NOT NULL CONSTRAINT PK_Settings PRIMARY KEY,
            SettingValue NVARCHAR(200) NOT NULL
        );
        """,
        Index("IX_Books_Title", "Books", "Title"),
        Index("IX_Books_CategoryId", "Books", "CategoryId"),
        Index("IX_Books_StatusId", "Books", "StatusId"),
        Index("IX_BookAuthors_AuthorId", "BookAuthors", "AuthorId"),
        Index("IX_Authors_Name", "Authors", "LastName, FirstName"),
        Index("IX_Students_Name", "Students", "LastName, FirstName"),
        Index("IX_BorrowReturns_BorrowDate", "BorrowReturns", "BorrowDate"),
        Index("IX_BorrowReturns_DueDate", "BorrowReturns", "DueDate"),
        Index("IX_BorrowReturns_ReturnDate", "BorrowReturns", "ReturnDate"),
        Index("IX_BorrowReturns_StudentId", "BorrowReturns", "StudentId"),
        Index("IX_BorrowReturns_BookId", "BorrowReturns", "BookId"),
        Index("IX_BorrowReturns_Isbn", "BorrowReturns", "Isbn"),
    };

    private static string[] V2() => new[]
    {
        """
        INSERT INTO dbo.Roles (Name)
        SELECT v.Name FROM (VALUES (N'Admin'), (N'Librarian'), (N'Borrower')) AS v(Name)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Name = v.Name);
        """,
        """
        INSERT INTO dbo.BookStatuses (Name, IsBorrowable)
        SELECT v.Name, v.IsBorrowable FROM (VALUES (N'Good Condition', 1), (N'Damaged', 0)) AS v(Name, IsBorrowable)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.BookStatuses s WHERE s.Name = v.Name);
        """,
        """
        INSERT INTO dbo.Categories (Name, Description)
        SELECT v.Name, v.Description FROM (VALUES
            (N'Fiction', N'Novels and short stories'),
            (N'Non-Fiction', N'Factual and informational books'),
            (N'Science', N'Natural and applied sciences'),
            (N'Technology', N'Computing, engineering and IT'),
            (N'Mathematics', N'Mathematics and statistics'),
            (N'History', N'History and social studies'),
            (N'Literature', N'Poetry, drama and literary works'),
            (N'Reference', N'Dictionaries, encyclopedias and handbooks')
        ) AS v(Name, Description)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = v.Name);
        """,
        """
        INSERT INTO dbo.Settings (SettingKey, SettingValue)
        SELECT v.SettingKey, v.SettingValue FROM (VALUES (N'LoanDays', N'7'), (N'MaxActiveLoans', N'3')) AS v(SettingKey, SettingValue)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings s WHERE s.SettingKey = v.SettingKey);
        """,
    };
}