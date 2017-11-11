# fastmusic
Fastmusic is an SQLite/.NET Core REST API designed for home music streaming.
Fastmusic exists because writing it was faster than trying to figure out how to configure Apache/MySQL/PHP/FastCGI to run Ampache.

To get started, clone the repository and make sure you have the .NET Core CLI installed.
Make a copy of ```config_default.txt``` called ```config.txt``` and set the configuration values appropriately.
Then, run the following from the root of the repository:

```dotnet restore``` (installs dependencies)

```dotnet build``` (builds fastmusic)

```dotnet ef database update``` (creates an empty database)

```dotnet run```
