# WindowsGSM.TheIsleLegacy

🧩WindowsGSM plugin that provides TheIsle Legacy Dedicated server support!

- A modified version of [@dkdue](https://www.github.com/dkdue)'s [Evrima](https://github.com/dkdue/WindowsGSM.TheIsle) version to benefit the good old Legacy Version, with some additions.

# This version adds in support for:

- Gamemode (Surival / Sandbox) selection
- Admin lists\*

\*Admin lists is a optional way for you to support having one or multiple text files (Admin lists are textfiles with lines of Steam IDs) OR fetch admins from an API endpoint. This allows you to add one or multiple admin lists per server, leaving the server owners only having to update a single text file or API - to update all the servers you want, with your admins

# The Game

https://store.steampowered.com/app/376210/The_Isle/ - Legacy Version (Not Evrima!)

# Requirements

WindowsGSM >= 1.21.0

# Installation

1. Download the latest release
2. Move TheIsleLegacy.cs folder to plugins folder
3. Click [RELOAD PLUGINS] button or restart WindowsGSM

# Map Selection (Legacy has multiple maps you can select)

Since Legacy has multiple maps you can select between, you can select by using any of the following as the specific Servers Map Parameter:

**Isle V3**: Isle V3, isle v3, v3, islev3

**Thenyaw**: Thenyaw, thenyaw, ThenyawIsland, Thenyaw Island

**DV_TestLevel**: testlevel, DV_TestLevel, dm, Test Level, Dev Map, Dev level

# Game Mode Selection

To select either Sandbox or Survival game mode, add in `game=Sandbox` or `game=Survival` as the **Server Start Param**

- If no game mode is specified, it will default to Survival

# Admin Lists (Not required!)

This plugin has the ability for the server hosters to specify one or multiple text files with lists of SteamIDs (Currently only tested with RAW Github Repo files) OR fetch admins from an API endpoint, adding them automatically to the servers Game.ini file.
This means if you are a hoster having multiple servers, and you dislike having to spend hours on adjusting each Game.ini file for adding/removings - this might be an option for you.

You can choose between two modes:
- **Text-based**: Download admin lists from text files (GitHub, etc.)
- **API-based**: Fetch admin lists from an API endpoint

1. In the servers Start Param option field add in the following (\*Examples with my test files, use your own!)

- `adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt`

You can add multiple lists by adding a semi-colon (`;`)with a new list entry such as:

- `adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlisttwo.txt`

-- And if you wish you can expand to this list with AdminListThree, AdminListFour - followed by the links as the examples above... etc (There should be no limit in theory)

## So what happens with these lists?

WindowsGSM will open the text file and merge each Steam ID on the list into a combined list & add `ServerAdmin=` in front & modify your Game.ini by adding them in there.
So make sure all admins you want to have as admin in game, are added to these lists, if you use decide it.

So lets say if you have 2 steamids in adminListOne and 1 steamid in adminListTwo, it will combine them into 3 Steam IDs, getting added as admin on your server.
(This gives you an option to add seperate lists for lets say';' Deathmatch, Event servers where you maybe need more people to be admin, that you don't want to have admin on the other servers)

So in theory adminListOne could be your main admins
adminListTwo could be trial admins
adminListThree could eventually be DM/Event related admins

- and combined they will make 1 admin list in your server.

**OBS: Currently only supporting text file, laying online in places such as GitHub etc. (Raw text files)**
**- In case your source for admin lists textfiles goes down, or you do not apply one - it will just keep using the Game.Ini you already have**

## API-Based Admin Lists (Alternative to Text Files)

Instead of using text files, you can fetch admin lists from an API endpoint. This is useful if you have a centralized admin management system.

### Setup

Add the following parameters to your **Server Start Param**:

- `listtype=api` - Enables API mode (required for API-based lists)
- `apiurl=...` - Your API base URL or full endpoint URL (required)
- `apibearertoken=...` - Bearer token for authentication (optional, only if your API requires it)

### API Endpoint

The plugin will call: `GET {apiurl}/your-endpoint-path` (or use the full URL if provided)

**Expected Response Format:**
```json
[
  {
    "steamId": "76561198000000000"
  }
]
```

**Note:** Only the `steamId` field is required and used. Your API response may contain other fields, but they will be ignored.

### Examples

**Basic API call:**
```
game=Survival;listtype=api;apibearertoken=your_token;apiurl=http://your-api-host:port
```

**Without Bearer token (if API doesn't require authentication):**
```
game=Survival;listtype=api;apiurl=http://your-api-host:port
```

**Note:** The port (`:port`) is optional - use it only if your API requires a specific port.

**Note:** By default, the system will only fetch admins with type "admin". The API response may contain other fields, but only the `steamId` field is used.

**OBS: If the API is unreachable or returns an error, the server will keep using the existing Game.ini without refreshing the admin list (ensuring admins remain even if the API is temporarily down)**

# So how could a final Server Start Param look? (With and Without the usage of admin lists)

**Without admin lists:**
```
game=Survival
```

**With text-based admin lists:**
```
game=Survival;adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt
```
or
```
game=Sandbox;adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlisttwo.txt
```

**With API-based admin lists:**
```
game=Survival;listtype=api;apibearertoken=your_token;apiurl=http://your-api-host:port
```

**OBS: Remember if you use Admin Lists to adjust them into your own Steam IDs. The Steam IDs & lists provided in the examples are only for an example purpose**

# License

This project is licensed under the MIT License - see the <a href="https://github.com/menix1337/WindowsGSM.TheIsleLegacy/blob/main/LICENSE">LICENSE.md</a> file for details
