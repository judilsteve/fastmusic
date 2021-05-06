rm fastmusic.db
rmdir -r Migrations
dotnet ef migrations add InitialCreate --context MusicContext
dotnet ef database update --context MusicContext