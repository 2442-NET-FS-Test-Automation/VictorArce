GO
CREATE TABLE dbo.Authors(
    --Column-name data-type constraints(optional)
    AuthorID INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    BirthDate INT NULL,
    CONSTRAINT PK_Authors PRIMARY KEY (AuthorID),
    CONSTRAINT CK_AUTHORS_BirthDate CHECK (BirthDate IS NULL OR 
    (BirthDate >= 300 AND BirthDate <= 2050))
)

GO
CREATE TABLE dbo.Member(
    MemberID INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(125) NOT NULL UNIQUE,
    JoinedDate DATE NOT NULL DEFAULT (GETDATE()),
)

GO
CREATE TABLE dbo.Books(
    BookID INT IDENTITY(1,1) NOT NULL,
    Title NVARCHAR(100) NOT NULL,
    ISBN NVARCHAR(13) NOT NULL UNIQUE,
    PublishedDate INT NOT NULL,
    Categoryname NVARCHAR(50) NOT NULL CONSTRAINT DF_Book_CategoryName DEFAULT ('General'),
    AuthorID INT NOT NULL,
    TotalCopies INT NOT NULL CONSTRAINT DF_Book_TotalCopies DEFAULT (1),
    AvailableCopies INT NOT NULL CONSTRAINT DF_Book_AvailableCopies DEFAULT (1),

    CONSTRAINT PK_Books PRIMARY KEY (BookID),
    CONSTRAINT UQ_Books_ISBN UNIQUE (ISBN),

    CONSTRAINT FK_Books_Authors FOREIGN KEY (AuthorID) REFERENCES dbo.Authors(AuthorID)
    )

GO

CREATE TABLE dbo.Loans(
    LoanID INT IDENTITY(1,1) NOT NULL,
    BookID INT NOT NULL,
    MemberID INT NOT NULL,
    LoanDate DATE NOT NULL CONSTRAINT DF_Loans_LoanDate DEFAULT (GETDATE()),
    DueDate DATE NOT NULL,
    ReturnDate DATE NULL,

    CONSTRAINT PK_Loans PRIMARY KEY (LoanID),
    CONSTRAINT FK_Loans_Books FOREIGN KEY (BookID) REFERENCES dbo.Books(BookID),
    CONSTRAINT FK_Loans_Member FOREIGN KEY (MemberID) REFERENCES dbo.Member(MemberID),
    CONSTRAINT CK_Loans_Date CHECK (DueDate >= LoanDate)
)