CREATE DATABASE AilogDB;
GO

USE AilogDB;
GO

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
GO

CREATE TABLE Authors (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(200) NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(50) NULL,
    Avatar NVARCHAR(500) NULL,
    CoverPhoto NVARCHAR(MAX) NULL,
    Bio NVARCHAR(1000) NULL,
    FollowersCount INT DEFAULT 0,
    FollowingCount INT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT DEFAULT 0,
    ProfileVisibility NVARCHAR(20) DEFAULT 'Public',
    SearchEngineVisibility BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
GO

CREATE TABLE BlogPosts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Summary NVARCHAR(MAX) NULL,
    AuthorId INT NOT NULL,
    CategoryId INT NOT NULL,
    ReadCount INT DEFAULT 0,
    Slug NVARCHAR(500) NOT NULL,
    IsPublished BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (AuthorId) REFERENCES Authors(Id),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);
GO

CREATE TABLE ReadHistories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL DEFAULT 1,
    BlogPostId INT NOT NULL,
    ReadAt DATETIME2 DEFAULT GETDATE(),
    ReadProgress INT DEFAULT 100,
    FOREIGN KEY (BlogPostId) REFERENCES BlogPosts(Id)
);
GO

CREATE TABLE AuthorInterests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AuthorId INT NOT NULL,
    CategoryId INT NOT NULL,
    FOREIGN KEY (AuthorId) REFERENCES Authors(Id),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);
GO

CREATE TABLE AuthorSessions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AuthorId INT NOT NULL,
    DeviceName NVARCHAR(255) NOT NULL,
    Location NVARCHAR(255) NOT NULL,
    IpAddress NVARCHAR(50) NOT NULL,
    LastActive DATETIME2 NOT NULL,
    IsCurrentDevice BIT NOT NULL DEFAULT 0,
    IsRevoked BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_AuthorSessions_Authors FOREIGN KEY (AuthorId) REFERENCES Authors(Id) ON DELETE CASCADE
);
GO

CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    ReferenceLink NVARCHAR(255) NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(7) NOT NULL,
    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Notifications_Authors_UserId FOREIGN KEY(UserId) REFERENCES Authors (Id) ON DELETE CASCADE
);
GO


INSERT INTO Categories (Name, Slug) VALUES 
('Technology', 'technology'),
('History', 'history'),
('Lifestyle', 'lifestyle'),
('Travel', 'travel'),
('Science', 'science'),
('Finance', 'finance');
GO

INSERT INTO Authors (Name, Bio, FollowersCount, Email, PasswordHash, PhoneNumber, Avatar) VALUES 
('Diane Merlotte', 'Web development enthusiast and software architect', 15200, 'diane.merlotte@gmail.com', 'hashed_password', '+13256172292', '/images/avatars/DianeMerlotte.png'),
('Prof. Marcus Time', 'History professor and researcher', 8500, NULL, NULL, NULL, NULL),
('Daisy Paws', 'Pet lover and lifestyle blogger', 12300, NULL, NULL, NULL, NULL),
('Clara Voyager', 'Solo traveler and adventure writer', 9800, NULL, NULL, NULL, NULL),
('Luna Armstrong', 'Space journalist and science communicator', 18500, NULL, NULL, NULL, NULL),
('Coinader', 'Cryptocurrency and fintech analyst', 14200, NULL, NULL, NULL, NULL),
('Dr. Sarah Gene', 'Geneticist and medical researcher', 11000, NULL, NULL, NULL, NULL),
('Esther Duflo', 'Nobel Prize economist', 95000, NULL, NULL, NULL, '/images/avatars/EstherDuflo.png'),
('Fei-Fei Li', 'AI researcher and professor', 120000, NULL, NULL, NULL, '/images/avatars/FeiFeiLi.png'),
('Christiane Amanpour', 'International journalist', 85000, NULL, NULL, NULL, '/images/avatars/ChristianeAmanpour.png'),
('Jimmy Donaldson (MrBeast)', 'YouTube creator and philanthropist', 250000, NULL, NULL, NULL, '/images/avatars/JimmyDonaldson.png'),
('Yuval Noah Harari', 'Historian and author', 180000, NULL, NULL, NULL, '/images/avatars/YuvalNoahHarari.png');
GO

INSERT INTO BlogPosts (Title, Content, AuthorId, CategoryId, ReadCount, Slug, CreatedAt) VALUES 
('Modern Approaches in Web Development: Transitioning from Monolithic to Microservices', 
'The saying the only constant is change applies most fittingly to the world of web technologies. Monolithic architectures, which we have used safely for years, can sometimes remain clumsy in the face of rapidly changing requirements. Microservices architecture offers a solution to these challenges by breaking down applications into smaller, independent services that can be developed, deployed, and scaled independently. This approach brings numerous benefits including improved fault isolation, technology flexibility, and easier maintenance. However, it also introduces complexity in terms of service discovery, data consistency, and distributed system management. In this article, we will explore the key considerations when transitioning from a monolithic architecture to microservices, including best practices and common pitfalls to avoid.',
1, 1, 1250, 'modern-web-development', DATEADD(day, -1, GETDATE())),

('From Ancient Egypt to the Vikings: Silent Witnesses of History and Their Traces',
'History books usually write about wars, treaties, and kings; however, the real magic of the past is hidden between the lines and in cultural details. From the pyramids rising under the Egyptian sun to the longships of the Vikings crossing stormy seas, ancient civilizations left behind artifacts that tell stories more vivid than any written record. Archaeological discoveries continue to reshape our understanding of these cultures, revealing sophisticated technologies, complex social structures, and artistic achievements that rival our modern accomplishments. In this exploration, we will journey through time to uncover the silent witnesses of history and the traces they left behind.',
2, 2, 980, 'ancient-egypt-to-vikings', DATEADD(day, -2, GETDATE())),

('What My Dog Taught Me About Happiness and Unconditional Love',
'They say a dog is a mans best friend, but they are also our best teachers. Living with a pet teaches us unconditional love, patience, and the joy of living in the present moment. My golden retriever Max has shown me that happiness does not come from material possessions or achievements, but from simple pleasures like a walk in the park, a game of fetch, or simply being together. Dogs live in the moment without worrying about yesterday or tomorrow. They greet us with the same enthusiasm whether we have been gone for five minutes or five hours. This pure, unconditional love has profound lessons for how we can approach our own relationships and find contentment.',
3, 3, 2100, 'what-my-dog-taught-me', DATEADD(day, -3, GETDATE())),

('The Art of Getting Lost: Why Solo Travel is Good for the Soul',
'Traveling alone forces you out of your comfort zone in the best way possible. It is not just about seeing new places, but about discovering who you are when no one is watching. Solo travel strips away the social masks we wear and confronts us with our true selves. Without the safety net of familiar faces, we must navigate not only foreign streets but also our own fears, assumptions, and limitations. The art of getting lost is really about finding yourself. When you travel alone, every decision is yours, every discovery is personal, and every challenge becomes an opportunity for growth.',
4, 4, 1450, 'art-of-getting-lost', DATEADD(day, -4, GETDATE())),

('Artemis Mission Update: The Final Countdown to the Moon Base',
'NASAs Artemis program is making groundbreaking progress toward establishing a permanent human presence on the Moon. The latest updates reveal exciting developments in spacecraft technology, astronaut training, and international partnerships. The Artemis missions represent humanitys return to the Moon after more than 50 years, but this time we are going to stay. The program aims to land the first woman and first person of color on the lunar surface, establishing a sustainable presence that will serve as a stepping stone for future Mars missions. With the Space Launch System and Orion spacecraft ready, the countdown to a new era of space exploration has begun.',
5, 5, 8500, 'artemis-moon-mission', DATEADD(day, -1, GETDATE())),

('The Rise of Central Bank Digital Currencies: End of Cash?',
'Central banks worldwide are exploring digital currencies that could revolutionize how we think about money and financial transactions. From Chinas digital yuan to the European Central Banks digital euro project, CBDCs are moving from concept to reality. These digital currencies promise faster transactions, lower costs, and greater financial inclusion. However, they also raise important questions about privacy, monetary policy, and the future of traditional banking. As cash usage declines globally, are we witnessing the beginning of the end for physical currency? This article examines the promises and pitfalls of the CBDC revolution.',
6, 6, 7200, 'central-bank-digital-currencies', DATEADD(day, -2, GETDATE())),

('Gene Editing Breakthrough: A New Era for Rare Diseases',
'CRISPR technology has reached new milestones in treating genetic disorders that were once considered incurable. Recent clinical trials have shown remarkable success in treating conditions like sickle cell disease and beta-thalassemia, with patients experiencing life-changing improvements. The precision of gene editing tools continues to improve, opening possibilities for treating thousands of rare genetic conditions that affect millions of people worldwide. However, ethical considerations around germline editing and access to these expensive treatments remain important challenges. As we enter this new era of genetic medicine, the promise of eliminating hereditary diseases is closer than ever.',
7, 5, 6800, 'gene-editing-breakthrough', DATEADD(day, -3, GETDATE()));
GO

INSERT INTO ReadHistories (UserId, BlogPostId, ReadAt) VALUES 
(1, 1, DATEADD(day, -1, GETDATE())),
(1, 2, DATEADD(day, -2, GETDATE())),
(1, 3, DATEADD(day, -3, GETDATE())),
(1, 4, DATEADD(day, -4, GETDATE()));
GO

PRINT 'AilogDB Successfully Created and Seeded!';