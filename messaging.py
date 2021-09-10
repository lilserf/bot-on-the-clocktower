'''Various gameplay messaging features like sending !evil etc to the evil team'''

import shlex
import discord
from discord.ext import commands
import discordhelper

from botctypes import TownInfo

class RoleMessagerImpl:
    '''Class for sending messages to players about their role'''

    async def send_group_message(self, group:list[discord.Member], msg:str) -> None:
        '''Send a message to every member of the group, formatting in their name and the list of other users'''
        for mem in group:
            others = discordhelper.user_names(group)
            others.remove(discordhelper.get_user_name(mem))

            others_msg = ', '.join(others)
            formatted_msg = msg.format(discordhelper.get_user_name(mem), others_msg)
            await mem.send(formatted_msg)

    async def send_demon_message(self, demon_user:discord.Member, minion_users:list[discord.Member]) -> None:
        '''Send a message to the demon about their minions'''
        demon_msg = f"{discordhelper.get_user_name(demon_user)}: You are the **demon**. Your minions are: "
        minion_names = discordhelper.user_names(minion_users)
        demon_msg += ', '.join(minion_names)
        await demon_user.send(demon_msg)

    async def send_minion_message(self, demon_user:discord.Member, minion_users:list[discord.Member]) -> None:
        '''Send a message to the minions about their demon and fellow minions'''
        minion_msg = "{}: You are a **minion**. Your demon is: " + discordhelper.get_user_name(demon_user) + "."

        if len(minion_users) > 1:
            minion_msg += " Your fellow minions are: {}."

        await self.send_group_message(minion_users, minion_msg)


    async def process_minion_message(self, info:TownInfo, msg:str, ctx:commands.Context) -> tuple[bool, discord.Member, list[discord.Member] ]:
        '''Given a string of format <demon, minion, minion, etc> find the demon and minion users and return a tuple with them'''
        # Split the message allowing quoted substrings
        params = shlex.split(msg)
        users = info.activePlayers

        # Grab the demon and list of minions
        demon = params[1]
        minions = params[2:]

        if len(minions) == 0:
            await discordhelper.send_error_to_author(ctx, "It seems you forgot to specify any minions!")
            return (False, None, None)

        # Get the users from the names
        demon_user = discordhelper.get_closest_user(users, demon)
        minion_users = list(map(lambda x: discordhelper.get_closest_user(users, x), minions))

        categories = [info.dayCategory.name]
        if info.nightCategory:
            categories.append(info.nightCategory.name)
        cat_string = ', '.join(categories)

        # Error messages for users not found
        if demon_user is None:
            await discordhelper.send_error_to_author(ctx, f"Couldn't find user **{demon}** in these categories: {cat_string}.")
            return (False, None, None)

        for (index, minion) in enumerate(minion_users):
            if minion is None:
                await discordhelper.send_error_to_author(ctx, f"Couldn't find user **{minions[index]}** in these categories: {cat_string}.")
                return (False, None, None)

        return (True, demon_user, minion_users)

    async def inform_evil(self, info:TownInfo, message:discord.Message, ctx:commands.Context) -> None:
        '''Given a message of format !evil foo bar baz, inform the evil team who each other are'''
        content = message.content
        await message.delete()
        (success, demon_user, minion_users) = await self.process_minion_message(info, content, ctx)

        if not success:
            return

        await self.send_demon_message(demon_user, minion_users)
        await self.send_minion_message(demon_user, minion_users)

    async def inform_lunatic(self, info:TownInfo, message:discord.Message, ctx:commands.Context) -> None:
        '''Inform the Lunatic of their fake minions as if they were really a demon'''
        content = message.content
        await message.delete()
        (success, demon_user, minion_users) = await self.process_minion_message(info, content, ctx)

        if not success:
            return

        await self.send_demon_message(demon_user, minion_users)

    async def inform_legion(self, info:TownInfo, message:discord.Message, ctx:commands.Context) -> None:
        '''Inform all Legion players of the situation'''
        content = message.content
        await message.delete()
        (success, demon_user, minion_users) = await self.process_minion_message(info, content, ctx)

        if not success:
            return

        minion_users.append(demon_user)
        await self.send_group_message(minion_users, '{}: You are **Legion**, along with {}.')

