rm fastmusic.db
rmdir -r Migrations
dotnet ef migrations add InitialCreate --context MusicProvider
dotnet ef database update --context MusicProvider