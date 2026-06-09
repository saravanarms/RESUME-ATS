-- Database Schema for AI Resume Analyzer (Azure SQL Database / SQL Server)

-- 1. Create Subscription Plans Table
CREATE TABLE [dbo].[SubscriptionPlans] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [MaxResumesPerMonth] INT NOT NULL,
    [FeaturesJson] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) DEFAULT GETUTCDATE() NOT NULL,
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 2. Insert Default Subscription Plans
SET IDENTITY_INSERT [dbo].[SubscriptionPlans] ON;
INSERT INTO [dbo].[SubscriptionPlans] ([Id], [Name], [Price], [MaxResumesPerMonth], [FeaturesJson])
VALUES 
(1, 'Free', 0.00, 3, '["3 Resumes/month", "Standard ATS Scoring", "Basic Recommendations"]'),
(2, 'Pro', 19.99, 50, '["50 Resumes/month", "Advanced ATS Matching", "Detailed Skill Gap Analysis", "PDF Report Downloads", "Priority AI Queue"]'),
(3, 'Enterprise', 99.00, 500, '["500 Resumes/month", "Full Dashboard Analytics", "Custom Skill Mapping", "Unlimited PDF Downloads", "Dedicated API Key", "Account Manager"]');
SET IDENTITY_INSERT [dbo].[SubscriptionPlans] OFF;

-- 3. Extend AspNetUsers (Identity User) - representation
-- Note: ASP.NET Core Identity creates AspNetUsers table automatically, 
-- but this SQL schema outlines the extensions and relationships.
ALTER TABLE [dbo].[AspNetUsers] ADD 
    [FullName] NVARCHAR(100) NULL,
    [SubscriptionPlanId] INT DEFAULT 1 NOT NULL,
    [SubscriptionStatus] NVARCHAR(50) DEFAULT 'Active' NOT NULL,
    [SubscriptionStartDate] DATETIME2(7) NULL,
    [SubscriptionEndDate] DATETIME2(7) NULL,
    [UsageCountThisMonth] INT DEFAULT 0 NOT NULL,
    [LastUsageDate] DATETIME2(7) NULL,
    [DateCreated] DATETIME2(7) DEFAULT GETUTCDATE() NOT NULL;

ALTER TABLE [dbo].[AspNetUsers] WITH CHECK ADD CONSTRAINT [FK_AspNetUsers_SubscriptionPlans_SubscriptionPlanId] 
FOREIGN KEY([SubscriptionPlanId]) REFERENCES [dbo].[SubscriptionPlans] ([Id]);

-- 4. Resumes Table
CREATE TABLE [dbo].[Resumes] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(450) NOT NULL,
    [FileName] NVARCHAR(255) NOT NULL,
    [BlobUrl] NVARCHAR(2048) NOT NULL,
    [UploadedAt] DATETIME2(7) DEFAULT GETUTCDATE() NOT NULL,
    [IsDeleted] BIT DEFAULT 0 NOT NULL,
    CONSTRAINT [PK_Resumes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Resumes_AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);

-- Index on UserId for fast history queries
CREATE NONCLUSTERED INDEX [IX_Resumes_UserId] ON [dbo].[Resumes]([UserId] ASC);

-- 5. Analysis Results Table
CREATE TABLE [dbo].[AnalysisResults] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ResumeId] UNIQUEIDENTIFIER NOT NULL,
    [JobTitle] NVARCHAR(100) NULL,
    [JobDescription] NVARCHAR(MAX) NOT NULL,
    [AtsScore] INT NOT NULL,
    [KeywordMatchPercentage] INT NOT NULL,
    [MissingSkillsJson] NVARCHAR(MAX) NOT NULL,
    [RecommendationsJson] NVARCHAR(MAX) NOT NULL,
    [FormattingIssuesJson] NVARCHAR(MAX) NOT NULL,
    [GrammarSuggestionsJson] NVARCHAR(MAX) NOT NULL,
    [ResumeSummary] NVARCHAR(MAX) NULL,
    [AnalyzedAt] DATETIME2(7) DEFAULT GETUTCDATE() NOT NULL,
    CONSTRAINT [PK_AnalysisResults] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AnalysisResults_Resumes_ResumeId] FOREIGN KEY([ResumeId]) REFERENCES [dbo].[Resumes] ([Id]) ON DELETE CASCADE
);

-- Index on ResumeId
CREATE NONCLUSTERED INDEX [IX_AnalysisResults_ResumeId] ON [dbo].[AnalysisResults]([ResumeId] ASC);
