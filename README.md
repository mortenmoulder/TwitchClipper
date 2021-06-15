# TwitchClipper
Download Twitch.tv clips using youtube-dl on any platform that can compile and run .NET 5.0 applications.

Currently only supported on Windows and Linux, as I need to test MacOS (it should compile, but currently I cannot confirm if youtube-dl works or not.

**There is also no error handling what so ever, so if a request fails.. it might just fail. Will have to look into that later. Made this in a couple of hours, so bear with me.**

## Configuration
`appsettings.json` contains all the things you need to change, in order to start using TwitchClipper. 

There's a section called `TwitchConfiguration` you need to modify:

```
"TwitchConfiguration": {
  "ClientID": "CLIENT_ID_GOES_HERE",
  "DownloadThreads": 5
}
```

1. Go to https://dev.twitch.tv/console and create your app
2. Go to your app and find the Client ID
3. Replace `CLIENT_ID_GOES_HERE` with your newly copied Client ID

`DownloadThreads` spawns x amount of youtube-dl instances. The higher the number, the faster your download will be. I do not recommend going over 10, as you might get throttled by Twitch.

## Installation
### Debian 9
Found this tutorial on Microsoft's website: https://docs.microsoft.com/en-us/dotnet/core/install/linux-debian#debian-9-
```
wget -O - https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
wget https://packages.microsoft.com/config/debian/9/prod.list
sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list

sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-5.0
  
git clone https://github.com/mortenmoulder/TwitchClipper.git

cd TwitchClipper

dotnet build

dotnet publish -c Release -o publish -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true --runtime linux-x64 TwitchClipper.sln

cd publish

./TwitchClipper -u TWITCH_USERNAME
```

### Debian 10
Found this tutorial on Microsoft's website: https://docs.microsoft.com/en-us/dotnet/core/install/linux-debian#debian-10-
```
wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-5.0
  
git clone https://github.com/mortenmoulder/TwitchClipper.git

cd TwitchClipper

dotnet build

dotnet publish -c Release -o publish -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true --runtime linux-x64 TwitchClipper.sln

cd publish

./TwitchClipper -u TWITCH_USERNAME
```

### Windows
1. Download this file and place it somewhere: https://dot.net/v1/dotnet-install.ps1
2. Open PowerShell and run these commands:

```
cd to\folder\that\contains\dotnet-install.ps1
dotnet-install.ps1 -Channel 5.0 -Runtime aspnetcore

git clone https://github.com/mortenmoulder/TwitchClipper.git

cd TwitchClipper

dotnet build

dotnet publish -c Release -o publish -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true --runtime win-x64 TwitchClipper.sln

cd publish

TwitchClipper.exe -u TWITCH_USERNAME
```

## Few minor things
This is my first published .NET app. I'm a web developer, so please bear with me.

1. If you ever need to get the new version of this, you simply need to do `git pull` followed by `dotnet build` and then the long `dotnet publish .....` command.
2. Edit appsettings.json BEFORE you run the `dotnet build/publish` commands. The file will automatically get copied to the `publish` folder
3. I added MacOS support, even though it downloads the youtube-dl executable for Linux. Someone please let me know if it works or not.