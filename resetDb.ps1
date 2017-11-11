rm fastmusic.db
rmdir -r Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update 