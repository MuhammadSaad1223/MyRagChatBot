--use MyRagChatBotDB
--GO

-- ============================================
-- RAG CHATBOT DATABASE SETUP SCRIPT
-- Run this in SQL Server Management Studio
-- ============================================


-- Step 3: Drop tables if they exist (in correct order due to FK constraints)
/*IF OBJECT_ID('ChatMessages', 'U') IS NOT NULL
    DROP TABLE ChatMessages;
    
IF OBJECT_ID('ChatSessions', 'U') IS NOT NULL
    DROP TABLE ChatSessions;
    
IF OBJECT_ID('DocumentChunks', 'U') IS NOT NULL
    DROP TABLE DocumentChunks;
    
IF OBJECT_ID('DocumentInfo', 'U') IS NOT NULL
    DROP TABLE DocumentInfo;
GO

PRINT '✓ Old tables cleaned up.';
GO

-- Step 4: Create DocumentChunks table
CREATE TABLE DocumentChunks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DocumentName NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    EmbeddingJson NVARCHAR(MAX) NULL,
    UploadedDate DATETIME DEFAULT GETDATE(),
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastModified DATETIME DEFAULT GETDATE()
);
GO

CREATE INDEX IX_DocumentChunks_DocumentName ON DocumentChunks(DocumentName);
CREATE INDEX IX_DocumentChunks_UploadedDate ON DocumentChunks(UploadedDate);
PRINT '✓ DocumentChunks table created.';
GO

-- Step 5: Create ChatSessions table
CREATE TABLE ChatSessions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SessionId NVARCHAR(450) NOT NULL UNIQUE,
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastActivity DATETIME DEFAULT GETDATE(),
    UserName NVARCHAR(100) NULL,
    IPAddress NVARCHAR(50) NULL,
    IsActive BIT DEFAULT 1
);
GO

CREATE INDEX IX_ChatSessions_SessionId ON ChatSessions(SessionId);
CREATE INDEX IX_ChatSessions_CreatedDate ON ChatSessions(CreatedDate);
PRINT '✓ ChatSessions table created.';
GO

-- Step 6: Create ChatMessages table
CREATE TABLE ChatMessages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SessionId NVARCHAR(450) NOT NULL,
    Sender NVARCHAR(50) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Timestamp DATETIME DEFAULT GETDATE(),
    SessionIntId INT NULL,
    SourceType NVARCHAR(50) NULL,
    ConfidenceScore FLOAT NULL,
    TokensUsed INT NULL,
    ResponseTimeMs INT NULL
);
GO

ALTER TABLE ChatMessages
ADD CONSTRAINT FK_ChatMessages_ChatSessions
FOREIGN KEY (SessionIntId) REFERENCES ChatSessions(Id)
ON DELETE CASCADE;
GO

CREATE INDEX IX_ChatMessages_SessionId ON ChatMessages(SessionId);
CREATE INDEX IX_ChatMessages_Timestamp ON ChatMessages(Timestamp);
CREATE INDEX IX_ChatMessages_Sender ON ChatMessages(Sender);
PRINT '✓ ChatMessages table created.';
GO

-- Step 7: Create DocumentInfo table (Optional)
CREATE TABLE DocumentInfo (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FileName NVARCHAR(500) NOT NULL,
    FileSize BIGINT NULL,
    FileType NVARCHAR(50) NULL,
    UploadedBy NVARCHAR(100) NULL,
    UploadDate DATETIME DEFAULT GETDATE(),
    TotalChunks INT DEFAULT 0,
    ProcessingStatus NVARCHAR(50) DEFAULT 'pending',
    ErrorMessage NVARCHAR(MAX) NULL,
    Title NVARCHAR(500) NULL,
    Author NVARCHAR(200) NULL,
    Summary NVARCHAR(MAX) NULL
);
GO

CREATE INDEX IX_DocumentInfo_FileName ON DocumentInfo(FileName);
CREATE INDEX IX_DocumentInfo_UploadDate ON DocumentInfo(UploadDate);
PRINT '✓ DocumentInfo table created.';
GO

-- Step 8: Insert Sample Data
INSERT INTO DocumentChunks (DocumentName, Content, UploadedDate)
VALUES 
('AI_Introduction.txt', 'Artificial Intelligence is the simulation of human intelligence in machines. These machines are programmed to think like humans and mimic their actions.', GETDATE()),
('AI_Introduction.txt', 'The term AI was first coined in 1956 at a conference at Dartmouth College. Since then, AI has evolved significantly.', GETDATE()),
('Blazor_Guide.txt', 'Blazor is a free and open-source web framework that enables developers to create web apps using C# and HTML.', GETDATE()),
('Blazor_Guide.txt', 'Blazor apps can run on the client-side in the browser using WebAssembly or on the server-side with SignalR.', GETDATE()),
('SQL_Basics.txt', 'SQL stands for Structured Query Language. It is used to communicate with databases and perform various operations.', GETDATE()),
('SQL_Basics.txt', 'Common SQL commands include SELECT, INSERT, UPDATE, DELETE, and CREATE. Each serves a specific purpose in database management.', GETDATE());
GO

PRINT '✓ Sample data inserted.';
GO

-- Step 9: Test Queries
PRINT 'Testing database setup...';
GO

-- Test 1: Count documents
SELECT 'Document Count' AS Test, COUNT(*) AS Value FROM DocumentChunks
UNION ALL
-- Test 2: Check tables
SELECT 'Tables Created' AS Test, COUNT(*) AS Value 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG = 'MyRagChatBotDB';
GO

-- Step 10: Display Final Status
PRINT '============================================';
PRINT 'DATABASE SETUP COMPLETED SUCCESSFULLY!';
PRINT '============================================';
PRINT '';
PRINT 'Tables Created:';
PRINT '1. DocumentChunks - Stores text chunks and embeddings';
PRINT '2. ChatSessions - Stores chat session information';
PRINT '3. ChatMessages - Stores individual chat messages';
PRINT '4. DocumentInfo - Stores document metadata (optional)';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Update connection string in appsettings.json';
PRINT '2. Run your Blazor application';
PRINT '3. Test with sample queries';
PRINT '============================================';
GO*/
