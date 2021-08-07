import aiohttp
import asyncio
import datetime
from fuzzywuzzy import process
import json
import shlex


class LookupRole:
    def matches_other(self, other):
        return self.name == other.name and self.team == other.team and self.ability == other.ability

    def __init__(self, name, ability, team):
        self.name = name
        self.team = ability
        self.ability = team


class LookupRoleParser:
    def is_valid_role_json(self, json):
        return json != None and isinstance(json, dict) and 'id' in json and json['id'] != '_meta' and 'name' in json and 'team' in json and 'ability' in json

    def create_role_from_json(self, json):
        if self.is_valid_role_json(json):
            name = json['name']
            team = json['team']
            ability = json['ability']
            return LookupRole(name, ability, team)
        return None

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

    def collect_roles_for_set_json(self, set, roles):
        for json in set:
            role = self.create_role_from_json(json)
            if role != None:
                 self.add_role(role, roles)

    def collect_roles_from_json(self, results):
        roles = {}
        for set in results:
            if isinstance(set, list):
                self.collect_roles_for_set_json(set, roles)
        return roles


class LookupRoleDownloader:
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


    async def collect_roles_from_urls(self, urls):
        results = await self.fetch_urls(urls)
        parser = LookupRoleParser()
        return parser.collect_roles_from_json(results)


class LookupRoleServerData:
    def __init__(self, role_lookup):
        self.role_lookup = role_lookup
        self.last_refresh_time = datetime.datetime.now()

    def get_matching_roles(self, role_name):
        option = process.extractOne(role_name, self.role_lookup.keys(), score_cutoff=80)
        if option != None:
            return self.role_lookup[option[0]]
        return None


class LookupRoleDatabase:
    def __init__(self):
        self.server_data = {}

    def update_server_data(self, server_token, role_lookup):
        self.server_data[server_token] = LookupRoleServerData(role_lookup)

    def server_needs_update(self, server_token):
        return (not server_token in self.server_data) or self.server_data[server_token].last_refresh_time == None or (datetime.datetime.now() - self.server_data[server_token].last_refresh_time) > datetime.timedelta(days=1)


class Lookup:
        #urls = [
        #    'https://www.bloodstar.xyz/p/MagRoader/StarWars/script.json',
        #    'https://www.bloodstar.xyz/p/morilac/Pandemonium/script.json',
        #]


    async def role_lookup(self, ctx):
        try:
            #info = ctx.bot.getTownInfo(ctx)

            roleToCheck = self.find_role_from_message_content(ctx.message.content)

            roleLookup = {}

            # TODO: collect roles (if needed), compare vs. request, output result

        except Exception as ex:
            await ctx.bot.sendErrorToAuthor(ctx)