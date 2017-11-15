# fastmusic
fastmusic is an SQLite/.NET Core REST API designed for home music streaming.
fastmusic exists because writing it was faster than setting up Ampache/HTTPd/MySQL/PHP/FastCGI.

## Getting Started
Clone the repository and make sure you have the .NET Core CLI installed.
Make a copy of ```config_default.txt``` called ```config.txt``` and set the configuration values appropriately.
Then, run the following from the root of the repository:

1. ```dotnet restore``` (installs dependencies)

2. ```dotnet build``` (builds fastmusic)

3. ```dotnet ef database update``` (creates an empty database)

4. ```dotnet run```

5. You can now start querying the API (try ```/api/Music/AlbumsByArtist``` to see all your discographies). The database will populate in the background as fastmusic searches over your collection.

## How fast is fastmusic
My library (just under 10,000 audio files) takes ~7 seconds to load into a clean install of fastmusic, with a nice warm drive cache (this is benchmarking the program, not my hard drive). From a cold start of fastmusic (db already initialised), returning the full set of discographies (```/api/Music/AlbumsByArtist```)  takes about 150ms.