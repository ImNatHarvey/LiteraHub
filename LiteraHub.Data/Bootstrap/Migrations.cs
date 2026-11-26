namespace LiteraHub.Data.Bootstrap;

internal sealed record Migration(int Version, string Description, string[] Statements);

internal static class Migrations
{
    public static IReadOnlyList<Migration> All { get; } = new[]
    {
        new Migration(1, "Create core tables and indexes", V1()),
        new Migration(2, "Seed roles, statuses, categories, settings", V2()),
        new Migration(3, "Rename Good Condition status to Good", V3()),
        new Migration(4, "Link books to multiple categories", V4()),
        new Migration(5, "Add role permission foundation", V5()),
        new Migration(6, "Add account recovery passkey storage", V6()),
        new Migration(7, "Add staff personal names", V7()),
        new Migration(8, "Recovery limits and audit", V8()),
        new Migration(9, "Protect the original system owner", V9()),
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
        SELECT v.Name, v.IsBorrowable FROM (VALUES (N'Good', 1), (N'Damaged', 0)) AS v(Name, IsBorrowable)
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
    private static string[] V3() => new[]
    {
        """
        -- Preserve the existing StatusId so every book keeps its condition.
        -- If a separate Good status already exists, merge references first.
        DECLARE @oldId INT = (
            SELECT TOP (1) StatusId FROM dbo.BookStatuses
            WHERE Name = N'Good Condition' ORDER BY StatusId);
        DECLARE @goodId INT = (
            SELECT TOP (1) StatusId FROM dbo.BookStatuses
            WHERE Name = N'Good' ORDER BY StatusId);

        IF @oldId IS NOT NULL
        BEGIN
            IF @goodId IS NOT NULL
            BEGIN
                UPDATE dbo.Books SET StatusId = @goodId
                WHERE StatusId = @oldId;
                DELETE FROM dbo.BookStatuses WHERE StatusId = @oldId;
            END
            ELSE
            BEGIN
                UPDATE dbo.BookStatuses SET Name = N'Good', IsBorrowable = 1
                WHERE StatusId = @oldId;
            END
        END;
        """,
    };
    // V4 is additive and transactionally applied by DatabaseBootstrapper.
    // Keep Books.CategoryId as the primary category for older queries/clients.
    private static string[] V4() => new[]
    {
        """
        IF OBJECT_ID(N'dbo.BookCategories', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.BookCategories (
                BookId INT NOT NULL
                    CONSTRAINT FK_BookCategories_Books
                    REFERENCES dbo.Books(BookId),
                CategoryId INT NOT NULL
                    CONSTRAINT FK_BookCategories_Categories
                    REFERENCES dbo.Categories(CategoryId),
                CONSTRAINT PK_BookCategories PRIMARY KEY (BookId, CategoryId)
            );
        END;
        """,
        """
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_BookCategories_CategoryId'
              AND object_id = OBJECT_ID(N'dbo.BookCategories')
        )
            CREATE INDEX IX_BookCategories_CategoryId
            ON dbo.BookCategories (CategoryId, BookId);
        """,
        """
        -- Backfill existing books and repair missing primary links on rerun.
        INSERT INTO dbo.BookCategories (BookId, CategoryId)
        SELECT b.BookId, b.CategoryId
        FROM dbo.Books b
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.BookCategories bc
            WHERE bc.BookId = b.BookId AND bc.CategoryId = b.CategoryId
        );
        """
    };

    // V5 is additive. Authorization remains on the existing matrix until the enforcement batch.
    private static string[] V5() => new[]
    {
        """
        IF COL_LENGTH(N'dbo.Roles', N'Description') IS NULL
            ALTER TABLE dbo.Roles ADD Description NVARCHAR(500) NULL;
        IF COL_LENGTH(N'dbo.Roles', N'IsSystem') IS NULL
            ALTER TABLE dbo.Roles ADD IsSystem BIT NOT NULL
                CONSTRAINT DF_Roles_IsSystem DEFAULT 0;
        """,
        """
        UPDATE dbo.Roles SET IsSystem = 1
        WHERE Name IN (N'Admin', N'Librarian', N'Borrower');
        """,
        """
        IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
        CREATE TABLE dbo.RolePermissions (
            RoleId INT NOT NULL CONSTRAINT FK_RolePermissions_Roles
                REFERENCES dbo.Roles(RoleId),
            PermissionKey NVARCHAR(64) NOT NULL,
            CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionKey)
        );
        """,
        """
        INSERT INTO dbo.RolePermissions (RoleId, PermissionKey)
        SELECT r.RoleId, p.PermissionKey
        FROM dbo.Roles r CROSS JOIN (VALUES
            (N'ViewCatalog'),
            (N'ManageBooks'),
            (N'ManageStudents'),
            (N'CreateLookups'),
            (N'ManageLookups'),
            (N'ProcessBorrowReturn'),
            (N'ViewReports'),
            (N'ManageUsers'),
            (N'HardDelete'),
            (N'ViewOwnLoans'),
            (N'BooksView'),
            (N'BooksAdd'),
            (N'BooksEdit'),
            (N'BooksArchiveRestore'),
            (N'BooksDelete'),
            (N'AuthorsView'),
            (N'AuthorsAdd'),
            (N'AuthorsEdit'),
            (N'AuthorsArchiveRestore'),
            (N'AuthorsDelete'),
            (N'CategoriesView'),
            (N'CategoriesAdd'),
            (N'CategoriesEdit'),
            (N'CategoriesArchiveRestore'),
            (N'CategoriesDelete'),
            (N'StudentsView'),
            (N'StudentsAdd'),
            (N'StudentsEdit'),
            (N'StudentsArchiveRestore'),
            (N'StudentsDelete'),
            (N'UsersView'),
            (N'UsersAdd'),
            (N'UsersEdit'),
            (N'UsersArchiveRestore'),
            (N'UsersDelete'),
            (N'RolesView'),
            (N'RolesAdd'),
            (N'RolesEdit'),
            (N'RolesArchiveRestore'),
            (N'RolesDelete'),
            (N'BorrowReturnView'),
            (N'BorrowReturnBorrow'),
            (N'BorrowReturnReturn'),
            (N'ReportsView'),
            (N'ReportsExport'),
            (N'ActivityLogView')
        ) p(PermissionKey)
        WHERE r.Name = N'Admin' AND NOT EXISTS (
            SELECT 1 FROM dbo.RolePermissions rp
            WHERE rp.RoleId = r.RoleId AND rp.PermissionKey = p.PermissionKey);
        """,
        """
        INSERT INTO dbo.RolePermissions (RoleId, PermissionKey)
        SELECT r.RoleId, p.PermissionKey
        FROM dbo.Roles r CROSS JOIN (VALUES
            (N'ViewCatalog'),
            (N'ManageBooks'),
            (N'ManageStudents'),
            (N'CreateLookups'),
            (N'ProcessBorrowReturn'),
            (N'ViewReports'),
            (N'BooksView'),
            (N'BooksAdd'),
            (N'BooksEdit'),
            (N'BooksArchiveRestore'),
            (N'StudentsView'),
            (N'StudentsAdd'),
            (N'StudentsEdit'),
            (N'StudentsArchiveRestore'),
            (N'AuthorsView'),
            (N'AuthorsAdd'),
            (N'CategoriesView'),
            (N'CategoriesAdd'),
            (N'BorrowReturnView'),
            (N'BorrowReturnBorrow'),
            (N'BorrowReturnReturn'),
            (N'ReportsView')
        ) p(PermissionKey)
        WHERE r.Name = N'Librarian' AND NOT EXISTS (
            SELECT 1 FROM dbo.RolePermissions rp
            WHERE rp.RoleId = r.RoleId AND rp.PermissionKey = p.PermissionKey);
        """,
        """
        INSERT INTO dbo.RolePermissions (RoleId, PermissionKey)
        SELECT r.RoleId, p.PermissionKey
        FROM dbo.Roles r CROSS JOIN (VALUES
            (N'ViewCatalog'),
            (N'ViewOwnLoans'),
            (N'BooksView'),
            (N'AuthorsView'),
            (N'CategoriesView')
        ) p(PermissionKey)
        WHERE r.Name = N'Borrower' AND NOT EXISTS (
            SELECT 1 FROM dbo.RolePermissions rp
            WHERE rp.RoleId = r.RoleId AND rp.PermissionKey = p.PermissionKey);
        """,
    };
    private static string[] V6() => new[]
    {
        """
        IF COL_LENGTH(N'dbo.Users', N'RecoveryPasskeyHash') IS NULL
            ALTER TABLE dbo.Users ADD RecoveryPasskeyHash NVARCHAR(100) NULL;
        IF COL_LENGTH(N'dbo.Users', N'RecoveryPasskeyCreatedAt') IS NULL
            ALTER TABLE dbo.Users ADD RecoveryPasskeyCreatedAt DATETIME2(0) NULL;
        """,
    };
    private static string[] V7() => new[]
    {
        """
        IF COL_LENGTH(N'dbo.Users', N'FirstName') IS NULL ALTER TABLE dbo.Users ADD FirstName NVARCHAR(100) NULL;
        IF COL_LENGTH(N'dbo.Users', N'MiddleName') IS NULL ALTER TABLE dbo.Users ADD MiddleName NVARCHAR(100) NULL;
        IF COL_LENGTH(N'dbo.Users', N'LastName') IS NULL ALTER TABLE dbo.Users ADD LastName NVARCHAR(100) NULL;
        """
    };
    private static string[] V8() => new[]
    {
        """
        ALTER TABLE dbo.Users ADD RecoveryFailures INT NOT NULL DEFAULT 0, RecoveryLockedUntil DATETIME2 NULL;
        CREATE TABLE dbo.RecoveryAudit (
            AuditId BIGINT IDENTITY PRIMARY KEY, UserId INT NULL, ActorUserId INT NULL,
            EventType NVARCHAR(40) NOT NULL, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
        """
    };
    private static string[] V9() => new[]
    {
        """
        ALTER TABLE dbo.Users ADD IsSystemOwner BIT NOT NULL
            CONSTRAINT DF_Users_IsSystemOwner DEFAULT 0;
        """,
        """
        IF EXISTS (SELECT 1 FROM dbo.Users)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                WHERE u.UserId=1 AND u.IsActive=1 AND r.Name=N'Admin' AND r.IsActive=1 AND r.IsSystem=1)
                THROW 51001, 'Original bootstrap account is missing or not an active Admin. Restore a verified backup before upgrading.', 1;
            UPDATE dbo.Users SET IsSystemOwner=1 WHERE UserId=1;
        END;
        CREATE UNIQUE INDEX UX_Users_OneSystemOwner ON dbo.Users(IsSystemOwner) WHERE IsSystemOwner=1;
        ALTER TABLE dbo.Users ADD CONSTRAINT UQ_Users_OwnerIdentity UNIQUE (UserId, IsSystemOwner, RoleId, IsActive);
        CREATE TABLE dbo.SystemOwnerAnchor (
            SingletonId INT NOT NULL CONSTRAINT PK_SystemOwnerAnchor PRIMARY KEY CHECK (SingletonId=1),
            UserId INT NOT NULL,
            IsSystemOwner BIT NOT NULL CHECK (IsSystemOwner=1),
            RoleId INT NOT NULL,
            IsActive BIT NOT NULL CHECK (IsActive=1),
            CONSTRAINT FK_SystemOwnerAnchor_Users FOREIGN KEY (UserId,IsSystemOwner,RoleId,IsActive)
                REFERENCES dbo.Users(UserId,IsSystemOwner,RoleId,IsActive)
        );
        INSERT dbo.SystemOwnerAnchor(SingletonId,UserId,IsSystemOwner,RoleId,IsActive)
        SELECT 1,UserId,IsSystemOwner,RoleId,IsActive FROM dbo.Users WHERE IsSystemOwner=1;
        """,
        """
        CREATE TRIGGER dbo.TR_SystemOwnerAnchor_Immutable ON dbo.SystemOwnerAnchor
        AFTER UPDATE, DELETE AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (SELECT 1 FROM deleted)
                THROW 51002, 'The system owner anchor cannot be modified or removed.', 1;
        END;
        """
    };
}
