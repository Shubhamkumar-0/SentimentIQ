CONSULTATION SENTIMENT ANALYSIS - START HERE

This ZIP is a starter implementation for a C# course project.
Stack: ASP.NET Core MVC, C#, Entity Framework Core, SQL Server.
The sentiment logic is keyword-based (not a trained AI/ML model).

IMPORTANT: This is a prepared starter project to help you learn and finish your
work. Do not claim you independently wrote code you have not understood. Run it,
test it, modify it, and be ready to explain each part to your instructor.

REQUIREMENTS
- .NET 8 SDK
- Visual Studio with ASP.NET and web development workload
- SQL Server LocalDB or another SQL Server instance
- Internet for NuGet restore

OPEN
1. Extract this ZIP.
2. Open ConsultationSentimentProject.csproj in Visual Studio.
3. Check appsettings.json. Default SQL Server connection uses:
   (localdb)\MSSQLLocalDB
4. If using SQL Express, change Server=... to your instance, e.g. .\SQLEXPRESS.

CREATE DATABASE TABLES
Open Visual Studio > Tools > NuGet Package Manager > Package Manager Console.
Run:
  Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.12
  Add-Migration InitialCreate
  Update-Database

Or in a terminal in the project directory:
  dotnet restore
  dotnet tool install --global dotnet-ef --version 8.0.12
  dotnet ef migrations add InitialCreate
  dotnet ef database update
Then run:
  dotnet run

SSMS is a management tool; it is not the SQL Server database engine. LocalDB
must be installed separately if it is not already available.

TEST EXAMPLES
"The consultation was excellent and helpful." -> Positive
"The service was poor and slow." -> Negative
"The consultation happened yesterday." -> Neutral
"The service was not helpful." -> Negative

LIMITATIONS
Keyword matching cannot reliably understand sarcasm, context, mixed-language
comments, or complex grammar. A trained ML.NET model would be a future upgrade.

FILES
Models/Comment.cs - data model
Data/ApplicationDbContext.cs - EF Core database context
Services/SentimentAnalyzer.cs - sentiment classification
Controllers/CommentsController.cs - list, create and delete actions
Views/Comments - dashboard and form
Program.cs - application setup
appsettings.json - SQL Server connection string
