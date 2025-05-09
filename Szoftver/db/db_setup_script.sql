CREATE TABLE Calendars (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(255) NOT NULL
);

CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    DefaultCalendarId INT,
    CONSTRAINT UQ_Email UNIQUE (Email),
    FOREIGN KEY (DefaultCalendarId) REFERENCES Calendars(Id)
);

CREATE TABLE CalendarPermissions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CalendarId INT,
    UserId INT,
    PermissionType VARCHAR(50),
    FOREIGN KEY (CalendarId) REFERENCES Calendars(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE Days (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Date DATE NOT NULL,
    CalendarId INT,
    FOREIGN KEY (CalendarId) REFERENCES Calendars(Id)
);

CREATE TABLE CalendarLists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(255) NOT NULL,
    Color VARCHAR(50),
    CalendarId INT,
    FOREIGN KEY (CalendarId) REFERENCES Calendars(Id)
);

CREATE TABLE Tickets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    CurrentPosition INT,
    StartTime TIME,
    EndTime TIME,
    Priority INT,
    CalendarListId INT,
    CurrentParentType VARCHAR(50),
    ParentId INT,
    IsCompleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (CalendarListId) REFERENCES CalendarLists(Id)
);
