# fastmusic
Fastmusic is an SQLite/.NET Core REST API designed for home music streaming.
Fastmusic exists because writing it was faster than trying to figure out how to configure Apache/MySQL/PHP/FastCGI to run Ampache.

## Getting Started
Clone the repository and make sure you have the .NET Core CLI installed.
Make a copy of ```config_default.txt``` called ```config.txt``` and set the configuration values appropriately.
Then, run the following from the root of the repository:

1. ```dotnet restore``` (installs dependencies)

2. ```dotnet build``` (builds fastmusic)

3. ```dotnet ef database update``` (creates an empty database)

4. ```dotnet run```

5. You can now start querying the API (try ```/api/Music/AlbumsByArtist``` to see all your discographies). The database will populate in the background as fastmusic searches over your collection.

## Performance
My music collection, which is comprised of just under 10,000 tracks, takes about 20 seconds to load from a WD Green mechanical HDD into the SQLite database from a clean install. Returning the full set of discographies (```/api/Music/AlbumsByArtist```) with a cold cache takes about 150ms.