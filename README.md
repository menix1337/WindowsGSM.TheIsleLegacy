# WindowsGSM.TheIsleLegacy

🧩WindowsGSM plugin that provides TheIsle Legacy Dedicated server support!

- A modified version of [@dkdue](https://www.github.com/dkdue)'s [Evrima](https://github.com/dkdue/WindowsGSM.TheIsle) version to benefit the good old Legacy Version, with some additions.

# This version adds in support for:

- Gamemode (Surival / Sandbox) selection
- Admin lists\*

\*Admin lists is a optional way for you to support having one or multiple text files (Admin lists are textfiles with lines of Steam IDs) and the ability to add one or multiple per server, leaving the server owners only having to update a a single text file - to update all the servers you want, with your admins

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

This plugin has the ability for the server hosters to specify one or multiple text files with lists of SteamIDs (Currently only tested with RAW Github Repo files) adding them automatically to the servers Game.ini file.
This means if you are a hoster having multiple servers, and you dislike having to spend hours on adjusting each Game.ini file for adding/removings - this might be an option for you.

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

# So how could a final Server Start Param look? (With and Without the usage of admin lists)

`game=Survival` or
`game=Survival;adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt` or
`game=Sandbox;adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlisttwo.txt`

**OBS: Remember if you use Admin Lists to adjust them into your own Steam IDs. The Steam IDs & lists provided in the examples are only for an example purpose**

# License

This project is licensed under the MIT License - see the <a href="https://github.com/menix1337/WindowsGSM.TheIsleLegacy/blob/main/LICENSE">LICENSE.md</a> file for details
