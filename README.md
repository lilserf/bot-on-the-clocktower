# botc_mover
Discord bot for moving people to run a game of Blood on the Clocktower

## Inviting this bot

https://discord.com/api/oauth2/authorize?client_id=795055055509651456&permissions=285223936&scope=bot

## Assumptions

This bot is made for an extremely specific use case and makes assumptions:

1. Voice channel categories exist called "BotC - Daytime" and "BotC - Nighttime"
2. The Nighttime category should contain enough channels for each user in your game to get their own channel
3. A channel exists called "Town Square" (probably in the Daytime category but that's not required)
4. A role exists called "BotC Current Storyteller"
    * The intent here is that this role is the only one that can see the channels in the Nighttime category - players will be sent to a channel but not be able to see the others, so that they can't cheat and see where the storyteller is going

## Usage

### `!night`

Sends all users in the Town Square channel to individual channels within the Nighttime category. Also grants the `BotC Current Storyteller` role to the user who sent the command and removes it from all other users being moved.

### `!day`

Brings all users from the nighttime channels back to the Town Square.

### `!vote`

Brings all users from other daytime channels back to the Town Square for nominations to begin.

### `!currgame`

Sets the correct roles on all users currently in the BotC categories. Useful if a late arrival shows up and it's not time for `!night` (which also runs this) yet.

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