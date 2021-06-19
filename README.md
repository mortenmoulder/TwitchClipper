# TwitchClipper
Download Twitch.tv clips using youtube-dl on any platform that can compile and run .NET 5.0 applications.

**There is also no error handling what so ever, so if a request fails.. it might just fail. Will have to look into that later. Made this in a couple of hours, so bear 🐻 with me.**

## Configuration
`appsettings.json` contains all the things you need to change, in order to start using TwitchClipper. 

There's a section called `TwitchConfiguration` you need to modify:

```
"TwitchConfiguration": {
  "ClientID": "CLIENT_ID_GOES_HERE",
  "ClientSecret": "CLIENT_SECRET_GOES_HERE",
  "DownloadThreads": 5
}
```

1. Go to https://dev.twitch.tv/console and create your app
2. Go to your app and find the Client ID and generate a new secret
3. Replace `CLIENT_ID_GOES_HERE` and `CLIENT_SECRET_GOES_HERE` with your newly copied values

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

dotnet publish -c Release -o publish -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true --runtime win-x64 TwitchClipper.sln

cd publish

TwitchClipper.exe -u TWITCH_USERNAME
```

### MacOS
Grab the latest Dotnet SDK for .NET 5.0 and install it: https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.301-macos-x64-installer

```
git clone https://github.com/mortenmoulder/TwitchClipper.git

cd TwitchClipper

dotnet publish -c Release -o publish -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true --runtime osx-x64 TwitchClipper.sln

cd publish

./TwitchClipper -u TWITCH_USERNAME
```

## Custom save path
Since there's nothing I hate more than not being able to pick my own save paths (okay, I am overexaturating a bit), I've decided to make my own custom expressions. You can customize exactly how you want your clips to be saved.

Use your slashes, your A-Z, your 0-9, and so on outside of the curly brackets as much as you want. If you want a folder to be named `awesome`, just write that.

To make your own expression, edit the `appsettings.json` and find the `Download -> SavePathExpression`.

### Example
If you want your clips to be saved as:

```
broadcaster/
├─ year/
│  ├─ month/
│  │  ├─ day/
│  │  │  ├─ clipname.mp4
```

your expression can look like this:

```
/{broadcaster_name}/{yyyy}/{MM}/{dd}/{id}.mp4
```

If you wish to use another language/locale/culture for your months and days, you can specify that in `Download -> Locale` inside `appsettings.json`. Default is en-US. Find a list of languages here: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c (scroll down)

### Requirements
1. Your expressions MUST end with .mp4 or .MP4. This is simply a limitation set by me, and it also makes my life a lot easier. Each clip from youtube-dl is saved as .mp4 by default either way, so might as well do that.
2. A few illegal (operating system dependent) characters have been introduced. As long as you stick to your A-Z, your 0-9, your regular directory and file names.. you should be fine. I'm removing illegal characters anyway, so go ahead and try to break it (and then make a pull request with a fix please)
3. An equal amount of curly brackets. I use curly brackets for my expressions, so you have to deal with that (also who uses curly brackets in their directory or file names??)

### Expressions and reference table
#### Clip details
| Expression       | Value                                | Example value                   |
|------------------|--------------------------------------|---------------------------------|
| id               | ID/Slug of the clip                  | TrappedAmericanBoarAMPTropPunch |
| broadcaster_name | Name of the broadcaster              | Sodapoppin                      |
| broadcaster_id   | ID of the broadcaster                | 26301881                        |
| game_id          | ID of the game being played          | 18122                           |
| title            | Title of the clip                    | OMEGALUL                        |

#### Dates
| Expression       | Value                                | Example value                   |
|------------------|--------------------------------------|---------------------------------|
| yyyy             | Year                                 | 2021                            |
| MMMM             | Full month name                      | June                            |
| MMM              | Abbreviated month name               | Jun                             |
| MM               | Month number with leading zero       | 09                              |
| M                | Month number without leading zero    | 9                               |
| dddd             | Full day name                        | Wednesday                       |
| ddd              | Abbreviated day name                 | Wed                             |
| dd               | Day number with leading zero         | 05                              |
| d                | Day number without leading zero      | 5                               |
| HH               | 24-hour clock hour with leading zero | 08                              |
| H                | 24-hour clock without leading zero   | 8                               |
| mm               | Minutes with leading zero            | 02                              |
| m                | Minutes without leading zero         | 2                               |
| ss               | Seconds with leading zero            | 03                              |
| s                | Seconds without leading zero         | 3                               |
| tt               | AM / PM                              | AM                              | 

## Few minor things
This is my first published .NET app. I'm a web developer, so please bear 🐻 with me.

1. If you ever need to get the new version of this, you simply need to do `git pull` followed by the long `dotnet publish .....` command.
2. Edit appsettings.json BEFORE you run the `dotnet build/publish` commands. The file will automatically get copied to the `publish` folder
3. When the authentication token has been generated, it will be stored in the new appsettings.json file in the publish folder (which will be overwritten next time you build)

## TODO:
1. ~~Custom input on how you want the folder structure to be. Currently it's USERNAME\YEAR\MONTH\DAY\CLIP_TITLE (aka "slug")~~
2. Clean up some of the messy "if linux or osx then"-code
3. Make the -u argument optional, and introduce some kind of flag you can set, that downloads from a list of users in appsettings.json
4. Error handling (pff works on my machine)
5. DOCKER CONTAINER
6. ~~someone please see if it works on osx please~~ THANK YOU @mauran
7. ~~Check if file exists before overwriting it (waste of time)~~
8. Somehow allow the user to determine how many videos they want (filtering, basically). Not everyone wants every video
9. ~~Refactor to Helix API instead of Kraken, which apparently was newer~~