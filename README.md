# Bot on the Clocktower
Discord bot to assist with running a game of Blood on the Clocktower on Discord

## Introduction: What does this bot do?

To easily play Blood on the Clocktower via Discord voice channels, you need:
* A place for all players and the Storyteller to discuss openly (the Town Square channel)
* Smaller gathering places for players to congregate for smaller semi-private conversations (other daytime channels)
* Individual places for each player to go during the night, where the Storyteller can visit them privately (night "Cottage" channels)

All of this can be set up manually, and general-purpose bots can be used to move players around, but it can be awkward to manage.

This bot handles setting up the channels, roles, and permissions automatically, as well as moving the players back and forth during the various phases of the game without needing to type in complex movement commands!

---

## Setup

The expected setup for using the bot is to have a set of categories, channels, and roles representing a "Town".

For these setup examples, we will use a town named "Ravenswood Bluff", but you can use whatever town name you like.

### Prerequisites

First, you need to **\>\>** [invite the bot](https://discord.com/api/oauth2/authorize?client_id=795055055509651456&permissions=419441680&scope=bot) **\<\<**

This bot requests the following permissions:

| Permission | Why? |
| ---------- | ---- |
| Manage Channels | To create/destroy channels and categories with `!createTown` and `!destroyTown` commands |
| Manage Roles | To grant/remove Storyteller and Villager roles<br/>To create/destroy roles with `!createTown` and `!destroyTown` commands |
| Manage Nicknames | To add/remove **(ST)** for the Storyteller's nickname |
| Move Members | To move players to nighttime rooms or back to the Town Square |
| Manage Messages | To delete `!evil` command messages so players can't see who's evil |
| View Channels | Required for many operations |
| Send Messages | Required for many operations |

In addition, it is recommended (but optional) that your server has the following roles:
* A role for server members who like to be Storytellers. Example role name: **BotC Storyteller**
* A role for server members who play the game. Example role name: **BotC Player**
  * If your server is entirely based around playing Blood on the Clocktower, this is unnecessary.

You should grant this role to appropriate server members, as the bot will not grant these roles for you.

#### A note on Server Administrators

Bot on the Clocktower works best by hiding nighttime channels from members. Unfortunately, server Administrators (including the server owner) can always see all channels. In addition, the bot cannot change the nickname of Administrators. For these reasons, if an Administrator wants to play too, it is recommended that they create a separate Discord account to act as the actual Administrator / server owner, and use a non-Administrator account to play instead.

### Quick Setup: `!createTown`

To quickly set up your town, simply send a command to the bot with the name of your town and - optionally - the roles mentioned above.

> `!createTown "Ravenswood Bluff" "BotC Storyteller" "BotC Player"`

This will create all the categories, channels, and roles needed by Ravenswood Bluff.

The bot supports more than 1 town per Discord server. With 2 differently-named towns, you can run 2 games at once on the same server.

**Note:** This command will create neither the **BotC Storyteller** nor **BotC Player** roles. It is expected you create them yourself if you need them.

#### Explanation of the Setup

For more information on precisely what this setup does (what categories, roles, and permissions are created), see the `!addTown` command reference, below.

---

## Gameplay

All players can gather in the Town Square channel while the Storyteller sets up the game on http://clocktower.online (or whatever mechanism your group uses).

When it's time for Night 1 to begin, the Storyteller uses `!night` to send all players to their individual Cottages. Each player has permissions only for the Cottage they're placed in, so they can't see each other at all, but the Storyteller can see all the Cottages.

Optionally, to distribute the Minion and Demon info (but not the Demon bluffs), the Storyteller can use the `!evil` command (see this and `!lunatic` below) to quickly send messages to all the Evil players informing them of their teammates.

At this point, the Storyteller can visit Cottages as dictated by the night order (the players are alphabetized into the Cottages to make finding players easier). The permissions are set up to allow the Storyteller to screen-share with users in the Cottages (for instance, to show the Grimoire to the Spy or Widow).

Once the night phase is complete, the Storyteller uses the `!day` command to bring the players back to the Town Square and begin the day.

Players can switch to other Daytime channels to have semi-private conversations until the Storyteller is ready to open nominations. The Storyteller may use the `!vote` or `!voteTimer` commands to drag all the players back to the Town Square.

This cycle of night & day continues until there's a winner!

If you'd like to start a new game with a new Storyteller, the new Storyteller can run `!night` when ready to take over Storytelling duties.

Once you're all done playing, a Storyteller can optionally run `!endgame` to remove roles and nicknames, generally cleaning up the town (though the bot will run this automatically after a few hours).

If you have more than one Storyteller, check out the `!setStorytellers` command reference below. This would be run before starting the first night.

---

## A Word About Rate Limits

Discord sometimes limits how many commands a bot can execute in a given timeframe (for good reason).

This is most noticeable when you run `!night`, `!day`, or `!vote` in larger groups - frequently only some (usually about 10) of the players will initially be moved, and there will be a delay of several seconds before the rest are moved.

This is normal behavior and not something to worry about! Just be patient and everyone will move eventually. Make sure you wait for everyone to wake up from their cottages in the morning!

The bot randomizes the order people are moved in, so it won't end up leaving the whole evil team with 10 extra seconds to plot by coincidence.

If you run into much longer delays, failures to move, or other errors, do let us know, however.

---

## Gameplay Command Details

### `!night`

Sends all members in the Town Square channel to individual channels within the Nighttime category. Also runs `!currGame`.

### `!day`

Brings all members from the Nighttime category channels back to the Town Square.

### `!vote`

Brings all members from other Daytime category channels back to the Town Square for nominations to begin.

### `!voteTimer <time>`

Runs `!vote` after the specified amount of time. Valid times range from 15 seconds to 20 minutes.

### `!stopVoteTimer`

Cancels an existing timer created by `!voteTimer`.

### `!currGame`

Sets the correct roles on all members currently in the voice channels, and gives the Storyteller the **(ST)** nickname prefix.

This logic is run by the `!night` command already, so this command is mostly useful if a Traveler enters the town midday, or if you do not plan on using the `!night` command.

### `!endGame`

Removes Storyteller and Villager roles, as well as the **(ST)** nickname prefix. Automatically run on the town after a few hours of inactivity.

### `!setStorytellers <name> <name>`

Used to specify multiple Storytellers for a game. Will remove the Storyteller role and **(ST)** nickname prefix from any previous Storyteller(s).

Any of these Storytellers may run the various gameplay commands. They will also be grouped together into the first Cottage during the night.

Example usage:

> `!setStorytellers Alice Bob`

> `!setStorytellers Alice "Bob G"`

### `!evil <demon> <minion> <minion> <minion>...`

Sends DMs to the evil players informing them of their teammates.

Example usage:

> `!evil Alice Bob Carol`

> `!evil Alice "Bob G" "Carol Anne Smith"`

The demon gets a message reading:

> Alice: You are the **demon**. Your minions are: Bob, Carol

Minions get a message reading:

> Bob: You are a **minion**. Your demon is: Alice. Your fellow minions are: Carol

### `!lunatic <lunatic> <fake minion> <fake minion> <fake minion>...`

Sends a DM to the Lunatic identical to those sent by `!evil` telling them who their fake minions are.

---

## Setup Command Details

### `!createTown <townName> [serverStorytellerRole] [serverPlayerRole] [noNight]`

Creates an entire town from nothing, including all of its categories, channels, and roles.

The optional `serverStorytellerRole` is an already-created server role for members of your server who wish to be Storytellers. They will be given access to a channel to control Bot on the Clocktower. If not provided, everyone on the server will see this channel.

The optional `serverPlayerRole` is an already-created server role for members of your server who wish to play Blood on the Clocktower. They will be granted access to see the Town Square when a game is not in progress. If not provided, everyone on the server will see the Town Square.

If `noNight` is provided, the Nighttime category and its Cottages will not be created. This will disable the `!day` and `!night` commands.

For more information about precisely what this sets up (in case you wanted to do it all yourself manually for some reason), see the `!addTown` command reference below.

### `!destroyTown <townName>`

Destroys all the channels, categories, and roles created via the `!createTown` command.

If there are extra channels that the bot does not expect, it will leave them alone and warn you about them. Simply clean them up and run this command again to finish town destruction.

### `!townInfo`

When run in a control channel for a town, reports all the details stored by `!addTown` or `!createTown` - the channel & role names the bot is expecting.

### `!addTown <controlChannel> <townSquareChannel> <dayCategory> <nightCategory> <currentStorytellerRole> <currentVillageRole>`

`!addTown` tells the bot about all the roles, categories, and channels it needs to know about to do its job. It expects these things are all already created; if they are not, use `!createTown` and it will handle all of this.

Alternative usage:

```!addTown control=<control channel> townSquare=<town square channel> dayCategory=<day category> nightCategory=<night category> stRole=<storyteller role> villagerRole=<villager role>```

**NOTE:** It is recommended that you create a town using `!createTown` above instead of using `!addTown`. But, if you've already got a setup that works for you (or you want your roles and channels to be named differently than what `!createTown` assumes), then `!addTown` might be preferred.

Here is what the bot expects to exist. Note that we are using "Ravenswood Bluff" for the example town name.

* 2 server roles for the currently-running game
  * A "**Ravenswood Bluff Storyteller**" role
  * A "**Ravenswood Bluff Villager**" role
* A "**Ravenswood Bluff**" daytime category
  * Category permissions should be set up to be visible to "**Ravenswood Bluff Villager**", and allow **Bot on the Clocktower** to move members
  * The category should contain these channels:
    * A "**control**" text channel. This is for interacting with the bot. Permissions should make this visible only to the **Bot on the Clocktower** role, as well as any members who may want to be Storytellers. It can be hidden from members who don't intend be a Storyteller, so you can remove "**Ravenswood Bluff Villager**" from the permissions set.
    * A "**Town Square**" voice channel. This is the main lobby for the game. Permissions should allow this to be visible to anyone who wants to play.
    * A variety of other voice channels for private conversations, such as "Dark Alley" and "Graveyard". These can all inherit permissions from the category.
    * A single "game-chat" text channel, also inheriting category permissions. This is for the Villagers to chat, especially during the night phase.
* A "**Ravenswood Bluff - Night**" nighttime category
  * **NOTE**: The nighttime category is optional. With the "Alternative Usage" of the command, you may leave the Night category out. This will disable the `!night` and `!day` commands.
  * Permission should be set up to be visible to "**Ravenswood Bluff Storyteller**", and allow **Bot on the Clocktower** to move members
  * This category can contain a bunch of voice channels that inherit category permissions. Common setup is to use 20 channels all named "Cottage"

Once all this is set up, you can run the `!addTown` command, telling it the name of your main channel, categories, and roles. For the above example, you would run:

> `!addtown control "Town Square" "Ravenswood Bluff" "Ravenswood Bluff - Night" "Ravenswood Bluff Storyteller" "Ravenswood Bluff Villager"`

If that command works, you're ready to run a game!

### `!removeTown`

The opposite of `!addTown` - when run in the control channel for a town, removes registration of this town from the bot. The channels and roles will still exist and are not touched.

### `!setChatChannel <chat channel name>`

Informs the bot about what your chat channel is. This is needed to use the `!voteTimer` command, as the bot periodically informs villagers how much time they have before the vote.


---

## Lookup Command Details

### `!character <character name>`

Looks up a character by name. Official characters provided by https://clocktower.online/ are supported.

If custom characters are desired, see the `!addScript` command.

### `!addScript <script json url>`

Informs the bot about a custom script using its json, collecting any custom characters in it.
The script is only used by your Discord server; other servers will not see your custom characters.

Some extra features are available if they are provided your script json.
* If `_meta` section has an `almanac` property, a link to the script almanac will be provided.
* If the character json has a `flavor` property, this will be included.

These features are all supported by script publishing from https://www.bloodstar.xyz/

### `!removeScript <script json url>`

Tells the bot to forget about a custom script url.

### `listScripts`

Lists all scripts the bot knows about for your server.

### `refreshScripts`

Forces a refresh on all the custom scripts known. This is useful if you publish a new script and want to see the changes immediately. Otherwise, the bot will automatically refresh daily. 

---

## Support

Please file a Github issue with lots of details if you run into problems.
Of course, we're just doing this in our spare time and the bot features have primarily been driven by what our local play group needs, so please be patient.
