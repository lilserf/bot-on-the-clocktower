# Bot on the Clocktower (botc_mover)
Discord bot for moving people to run a game of Blood on the Clocktower

## Inviting this bot

https://discord.com/api/oauth2/authorize?client_id=795055055509651456&permissions=486550528&scope=bot

This bot requires the following permissions:

| Permission | Why? |
| ---------- | ---- |
| View Channels | Required for many operations  |
| Send Messages | Required for many operations |
| Manage Roles | To grant the storyteller and villager roles |
| Move Members | To move players to nighttime rooms or back to the Town Square |
| Manage Messages | To delete !evil commands so players can't see who's evil |
| Manage Nicknames | To add/remove '(ST) ' to/from the storyteller's nickname |
| Change Nicknames | *Probably unneeded?* |


## Setup

A general setup for using the bot is to have a set of categories, channels, and roles representing a "Town".

This example will use "Ravenswood Bluff" as the name of the Town, but you can use whatever names you like.

* 2 server roles for the currently-running game
  * A "**Ravenswood Bluff Storyteller**" role
  * A "**Ravenswood Bluff Villager**" role
* A "**Ravenswood Bluff**" daytime category
  * Category permissions should be set up to be visible to "**Ravenswood Bluff Villager**", and allow **Bot on the Clocktower** to move members
  * The category should contain these channels:
    * A "**mover**" text channel. This is for interacting with the bot. Permissions should make this visible only to the **Bot on the Clocktower** role, as well as any members who may want to be Storytellers. It can be hidden from members who don't intend to do any storytelling, so you can remove "**Ravenswood Bluff Villager**" from the permissions set.
    * A "**Town Square**" voice channel. This is the main lobby for the game. Permissions should allow this to be visible to anyone who wants to play.
    * A variety of other voice channels for private conversations, such as "Dark Alley" and "Graveyard". These can all inherit permissions from the category.
    * A single "game-chat" text channel, also inheriting category permissions. This is for the villagers to chat, especially during the night phase.
* A "**Ravenswood Bluff - Night**" nighttime category
  * Permission should be set up to be visible to "**Ravenswood Bluff Storyteller**", and allow **Bot on the Clocktower** to move members
  * This category can contain a bunch of voice channels that inherit category permissions. We use 20 channels all named "Cottage"

Once all this is set up, you can run the `!addTown` command, telling it the name of your main channel, categories, and roles. For the above example, you would run:

> `!addtown mover "Town Square" "Ravenswood Bluff" "Ravenswood Bluff - Night" "Ravenswood Bluff Storyteller" "Ravenswood Bluff Villager"`

If that command works, you're ready to run a game!

**NOTE:** The bot supports more than 1 town per Discord server. Use the above setup for a new town with different category and role names, and you can run 2 games at once on the same server.

## Usage

### `!night`

Sends all members in the Town Square channel to individual channels within the Nighttime category. Also runs `!currgame`.

### `!day`

Brings all members from the Nighttime category channels back to the Town Square. Also runs `!currgame`.

### `!vote`

Brings all members from other Daytime category channels back to the Town Square for nominations to begin. Also runs `!currgame`.

### `!currgame`

Sets the correct roles on all members currently in the voice channels, and gives the Storyteller the '(ST) ' nickname prefix.

This logic is run by many other commands (`!night`, `!day`, etc.). This command is mostly useful if a Traveler enters the town.

### `!endgame`

Removes Storyteller and Villager roles, as well as `(ST) ` nickname prefix.

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
