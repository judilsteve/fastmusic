# fastmusic
Fastmusic is an SQLite/.NET Core REST API designed for home music streaming.
To get started, clone the repository and make sure you have the .NET Core CLI installed.
Make a copy of ```config_default.txt``` called ```config.txt``` and set the configuration values appropriately.
Then, run the following from the root of the repository:

```dotnet restore``` (installs dependencies)

```dotnet build``` (builds fastmusic)

```dotnet ef database update``` (creates an empty database)

```dotnet run```
