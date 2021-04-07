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

This bot requires the following permissions:

| Permission | Why? |
| ---------- | ---- |
| Manage Roles | To grant the Storyteller and villager roles, and to create/destroy roles with `!createTown` and `!destroyTown` commands |
| Manage Channels | For creating/destroy channels and categories with `!createTown` and `!destroyTown` commands  |
| Manage Nicknames | To add/remove **(ST)** to/from the Storyteller's nickname |
| View Channels | Required for many operations  |
| Send Messages | Required for many operations |
| Manage Messages | To delete `!evil` commands so players can't see who's evil |
| Move Members | To move players to nighttime rooms or back to the Town Square |

In addition, it is recommended (but optional) that you have already created:
* A role for server members who like to be Storytellers. Example role name: **BotC Storyteller**
* A role for server members who play the game. Example role name: **BotC Player** (if your server is entirely based around playing Blood on the Clocktower, this is totally unnecessary)

You will of course want to grant this role to the appropriate members.

#### A note on Server Ownership

Bot on the Clocktower works best by hiding nighttime channels from members. Unfortunately, the owner of the server can see all channels no matter what. In addition, the bot cannot change the nickname of the server owner. For these reasons, if the server owner wants to play too, it is recommended that they create a separate Discord account to act as the actual owner, and use a personal non-owner account to play Blood on the Clocktower.

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

All players can gather in the Town Square channel while the Storyteller sets up the game on http://clocktower.online or whatever other mechanism your group uses.

When it's time for Night 1 to begin, the Storyteller can use `!night` to send all players to their individual Cottages.

Each player has permissions only for the Cottage they're placed in, so they can't see each other at all, but the Storyteller can see all the Cottages.

At this point the Storyteller can visit Cottages as dictated by the night order (the players are even alphabetized into the Cottages to make finding the player you need easier!). The permissions will even allow the storyteller to screen-share with users in the Cottages (for instance, to show the Grimoire to the Spy).

Optionally, to distribute the Minion and Demon info (but not the demon bluffs), the Storyteller can use the `!evil` command (see this and `!lunatic` below) to quickly send messages to all the Evil players informing them of their teammates.

Once the night phase is complete, the Storyteller uses the `!day` command to bring the players back to the Town Square, tell them who died, and start discussion.

The players can jump out to other Daytime channels to have semi-private conversations until the Storyteller is ready to open nominations - they can use the `!vote` command to drag all the players back to the Town Square from any other Daytime channels.

This cycle of night & day continues until there's a winner!

If you'd like to start a new game with a new Storyteller, the new Storyteller can run `!night` when ready to take over Storytelling duties.

If you have more than one Storyteller, check out the `!setStorytellers` command reference below.

Once you're all done playing, a Storyteller can optionally run `!endgame` to remove roles and nicknames, generally cleaning up the town (though the bot will run this automatically after a few hours).

---

## Gameplay Command Details

### `!night`

Sends all members in the Town Square channel to individual channels within the Nighttime category. Also runs `!currgame`.

### `!day`

Brings all members from the Nighttime category channels back to the Town Square. Also runs `!currgame`.

### `!vote`

Brings all members from other Daytime category channels back to the Town Square for nominations to begin. Also runs `!currgame`.

### `!currGame`

Sets the correct roles on all members currently in the voice channels, and gives the Storyteller the **(ST)** nickname prefix.

This logic is run by `!night` already, so this command is mostly useful if a Traveler enters the town midday.

### `!endGame`

Removes Storyteller and Villager roles, as well as the **(ST)** nickname prefix. Automatically run on the town after a few hours of inactivity.

### `!setStorytellers <name> <name>`

Used to specify multiple Storytellers for a game. Will remove the Storyteller role and **(ST)** nickname prefix from any previous Storyteller(s).

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

### `!lunatic <demon> <minion> <minion> <minion>...`

Sends a DM to the Lunatic identical to those sent by `!evil` telling them who their fake minions are.

---

## Setup Command Details

### `!createTown <townName> [serverStorytellerRole] [serverPlayerRole]`

Creates an entire town from nothing, including all of its categories, channels, and roles.

The optional `serverStorytellerRole` is an already-created server role for members of your server who wish to be Storytellers. They will be given access to a channel to control Bot on the Clocktower. If not provided, everyone on the server will see this channel.

The optional `serverPlayerRole` is an already-created server role for members of your server who wish to play Blood on the Clocktower. They will be granted access to see the Town Square when a game is not in progress. If not provided, everyone on the server will see the Town Square.

For more information about precisely what this sets up (in case you wanted to do it all yourself manually for some reason), see the `!addTown` command reference below.

### `!destroyTown <townName>`

Destroys all the channels, categories, and roles created via the `!createTown` command.

If there are extra channels that the bot does not expect, it will leave them alone and warn you about them. Simply clean them up and run this command again to finish town destruction.

### `!townInfo`

When run in a control channel for a town, reports all the details stored by `!addTown` or `!createTown` - the channel & role names the bot is expecting.

### `!addTown <controlChannel> <townSquareChannel> <dayCategory> <nightCategory> <currentStorytellerRole> <currentVillageRole>`

`!addTown` tells the bot about all the roles, categories, and channels it needs to know about to do its job. It expects these things are all already created; if they are not, use `!createTown` and it will handle all of this.

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
    * A single "game-chat" text channel, also inheriting category permissions. This is for the villagers to chat, especially during the night phase.
* A "**Ravenswood Bluff - Night**" nighttime category
  * Permission should be set up to be visible to "**Ravenswood Bluff Storyteller**", and allow **Bot on the Clocktower** to move members
  * This category can contain a bunch of voice channels that inherit category permissions. Common setup is to use 20 channels all named "Cottage"

Once all this is set up, you can run the `!addTown` command, telling it the name of your main channel, categories, and roles. For the above example, you would run:

> `!addtown control "Town Square" "Ravenswood Bluff" "Ravenswood Bluff - Night" "Ravenswood Bluff Storyteller" "Ravenswood Bluff Villager"`

If that command works, you're ready to run a game!

### `!removeTown`

The opposite of `!addTown` - when run in the control channel for a town, removes registration of this town from the bot. The channels and roles will still exist and are not touched.

---

## Support

Please file a Github issue with lots of details if you run into problems.
Of course, we're just doing this in our spare time and the bot features have primarily been driven by what our local play group needs, so please be patient.
