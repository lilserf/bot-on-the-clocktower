import json
import shlex
import aiohttp
import asyncio

from fuzzywuzzy import process

class LookupRole:
    def is_valid_role_json(json):
        return json != None and isinstance(json, dict) and 'id' in json and json['id'] != '_meta' and 'name' in json and 'team' in json and 'ability' in json

    def create_role_from_json(json):
        if LookupRole.is_valid_role_json(json):
            return LookupRole(json)
        return None

    def matches_other(self, other):
        return self.name == other.name and self.team == other.team and self.ability == other.ability

    def __init__(self, json):
        self.name = json['name']
        self.team = json['team']
        self.ability = json['ability']


class Lookup:
    def find_role_from_message_content(self, content):
        return " ".join(shlex.split(content)[1:])

    async def fetch_url(self, url, session):
        async with session.get(url) as response:
            try:
                return await response.json()
            except Exception:
                return []

    async def fetch_urls(self, urls):
        loop = asyncio.get_event_loop()
        async with aiohttp.ClientSession() as session:
            tasks = [loop.create_task(self.fetch_url(url, session)) for url in urls]
            return await asyncio.gather(*tasks)

    def combine_or_append_role(self, role, roleList):
        found = False
        for r in roleList:
            if role.matches_other(r):
                found = True
                break
        if not found:
            roleList.append(role)

    def add_role(self, role, roles):
        if role.name in roles:
            self.combine_or_append_role(role, roles[role.name])
        else:
            roles[role.name] = [ role ]

    def collect_roles_for_set(self, set, roles):
        for json in set:
            role = LookupRole.create_role_from_json(json)
            if role != None:
                 self.add_role(role, roles)

    def collect_roles_from_results(self, results):
        roles = {}
        for set in results:
            if isinstance(set, list):
                self.collect_roles_for_set(set, roles)
        return roles


    async def collect_all_roles(self):
        urls = [
            'https://www.bloodstar.xyz/p/MagRoader/StarWars/script.json',
            'https://www.bloodstar.xyz/p/morilac/Pandemonium/script.json',
        ]
        results = await self.fetch_urls(urls)
        return collect_roles_from_results(results)


    async def role_lookup(self, ctx):
        try:
            #info = ctx.bot.getTownInfo(ctx)

            roleToCheck = self.find_role_from_message_content(ctx.message.content)

            roleLookup = {}

            # TODO: collect roles (if needed), compare vs. request, output result

        except Exception as ex:
            await ctx.bot.sendErrorToAuthor(ctx)