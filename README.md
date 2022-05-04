# Bot on the Clocktower

A Discord bot to assist with running a game of Blood on the Clocktower on Discord

This bot handles setting up all the channels, roles, and permissions you need to play Blood on the Clocktower, as well as moving the players back and forth during the various phases of the game without needing to type in complex movement commands!

# Invite the Bot

First, you need to **\>\>** [invite the bot](https://discord.com/api/oauth2/authorize?client_id=795055055509651456&permissions=419441680&scope=bot) **\<\<**

For more information on the permissions it requests, see [Permission Details](#permission-details).

# Quick Start: `/createtown`

To quickly set up your town, simply send a command to the bot with the name of your town.
You can also specify whether you'd like to use the "Night Category" with Cottages via the `usenight` param - some players don't use it.

> `/createtown townname:"Ravenswood Bluff"`

This will create all the categories, channels, and roles needed by Ravenswood Bluff.

The bot supports more than 1 town per Discord server. With 2 differently-named towns, you can run 2 games at once on the same server.

## Explanation of the Setup

For more information on precisely what this command does (what categories, roles, and permissions are created), see the [Setup Details](SETUP.md) and [Command Details](COMMANDS.md) documentation.

# Gameplay

All players can gather in the Town Square channel while the Storyteller sets up the game on http://clocktower.online (or whatever mechanism your group uses).

When it's time for Night 1 to begin, the Storyteller should use the `/game` command to start a new game.

![image](https://user-images.githubusercontent.com/151635/162874601-a94936c7-de43-4c0b-ad08-6089f67f6dc3.png)

From here, they can hit the Night button to move into the Night phase of the game.

## Initial Evil Info
If desired, to distribute the Minion and Demon info (but not the Demon bluffs), the Storyteller can use the `/evil` command (see [Command Details](COMMANDS.md)) to quickly send messages to all the Evil players informing them of their teammates.

## Night Phase
During the Night, the Storyteller can visit Cottages as dictated by the night order (the players are alphabetized into the Cottages to make finding players easier). The permissions are set up to allow the Storyteller to screen-share with users in the Cottages (for instance, to show the Grimoire to the Spy or Widow).

Once the night phase is complete, the Storyteller presses the Day button (or uses the `/day` command) to bring the players back to the Town Square and begin the day.

## Day Phase
Players can switch to other Daytime channels to have semi-private conversations until the Storyteller is ready to open nominations. The Storyteller may use the Vote button (or `/vote` command) or set a Vote Timer using the dropdown (or `/voteTimer` command) to drag all the players back to the Town Square.

This cycle of night & day continues until there's a winner!

If you'd like to start a new game with a new Storyteller, the new Storyteller can run `/game`, `/night` or any other command when ready to take over Storytelling duties.

Once you're all done playing, a Storyteller can optionally push the End Game button (or run `/endgame`) to remove roles and nicknames, generally cleaning up the town (though the bot will run this automatically after a few hours).

If you have more than one Storyteller, check out the `/storytellers` command in the [Command Details](COMMANDS.md).

# A Word About Rate Limits

Discord sometimes limits how many commands a bot can execute in a given timeframe (for good reason).

This is most noticeable when you run `/night`, `/day`, or `/vote` in larger groups - frequently only some (usually about 10) of the players will initially be moved, and there will be a delay of several seconds before the rest are moved.

This is normal behavior and not something to worry about! Just be patient and everyone will move eventually. Make sure you wait for everyone to wake up from their cottages in the morning!

The bot randomizes the order people are moved in, so it won't end up leaving the whole evil team with 10 extra seconds to plot by coincidence.

If you run into much longer delays, failures to move, or other errors, do let us know, however.

# Command Details

For full details of all the supported commands, see the [Command Details](COMMANDS.md) documentation.

# Permission Details

The bot requests the following permissions:

| Permission | Why? |
| ---------- | ---- |
| Manage Channels | To create/destroy channels and categories with `/createtown` and `/destroytown` commands |
| Manage Roles | To grant/remove Storyteller and Villager roles<br/>To create/destroy roles with `/createtown` and `/destroytown` commands |
| Manage Nicknames | To add/remove **(ST)** for the Storyteller's nickname |
| Move Members | To move players to nighttime rooms or back to the Town Square |
| Manage Messages | To delete `/evil` command messages so players can't see who's evil |
| View Channels | Required for many operations |
| Send Messages | Required for many operations |

# Support

I can be contacted on Discord at lilserf#8712 with any issues or questions.
In general, please file a Github issue with lots of details if you run into problems.
Of course, we're just doing this in our spare time and the bot features have primarily been driven by what our local play group needs, so please be patient.
