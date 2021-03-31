# Bot on the Clocktower (botc_mover)
Discord bot for moving people to run a game of Blood on the Clocktower

## Inviting this bot

https://discord.com/api/oauth2/authorize?client_id=795055055509651456&permissions=486550528&scope=bot

This bot requires the following permissions:
* View Channels - required for many operations
* Manage Roles - to grant the storyteller and villager roles
* Move Members - to move players to nighttime rooms or back to the Town Square
* Manage Nicknames - to add/remove '(ST) ' to/from the storyteller's nickname
* Change Nicknames - probably unneeded?
* Send Messages - required for many operations
* Manage Messages - to delete !evil commands so players can't see who's evil

## Setup

A general setup for using the bot is to have a set of categories, channels, and roles representing a "Town". This example will use "Ravenswood Bluff" as the name of the Town, but you can use whatever names you like.

* 2 server roles for the currently-running game
  * A "**Ravenswood Bluff Storyteller**" role
  * A "**Ravenswood Bluff Villager**" role
* 2 Categories:
  * A "**Ravenswood Bluff**" daytime category
    * Category permissions should be set up to be visible to "**Ravenswood Bluff Villager**", and allow **Bot on the Clocktower** to move members
    * The category should contain these channels:
      * A "**mover**" text channel. This is for interacting with the bot. This should be visible to the **Bot on the Clocktower** role, as well as any members who may want to be Storytellers. It can be hidden from members who don't intend to do any storytelling.
      * A "**Town Square**" voice channel. This is the main lobby for the game, and should be visible to anyone who wants to play.
      * A variety of other voice channels for private conversations, such as "Dark Alley" and "Graveyard". These can all inherit permissions from the category, so they will be visible to the "**Ravenswood Bluff Villager**" role
      * A single "game-chat" text channel, also inheriting category permissions. This is for the villagers to chat, especially during the night phase.
  * A "**Ravenswood Bluff - Night**" nighttime category
    * Permission should be set up to be visible to "**Ravenswood Bluff Storyteller**", and allow **Bot on the Clocktower** to move members
    * This category can contain a bunch of voice channels that inherit category permissions. We use 20 channels all named "Cottage"

Once all this is set up, you can run the `!addTown` command, telling it the name of your main channel, categories, and roles. For the above example, you would run:

> `!addtown mover "Town Square" "Ravenswood Bluff" "Ravenswood Bluff - Night" "Ravenswood Bluff Storyteller" "Ravenswood Bluff Villager"`

If that command works, you're ready to go!

## Usage

### `!night`

Sends all users in the Town Square channel to individual channels within the Nighttime category. Also grants the `BotC Current Storyteller` role to the user who sent the command and removes it from all other users being moved.

### `!day`

Brings all users from the nighttime channels back to the Town Square.

### `!vote`

Brings all users from other daytime channels back to the Town Square for nominations to begin.

### `!currgame`

Sets the correct roles on all users currently daytime channels. Useful if a late arrival shows up and it's not time for `!night` (which also runs this) yet.

### `!endgame`

Removes Storyteller and Villager roles, as well as `(ST) ` nickname prefix

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
