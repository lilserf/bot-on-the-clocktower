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

    def merge_roles_from_json(self, json, roles):
        for set in json:
            if isinstance(set, list):
                self.collect_roles_for_set_json(set, roles)

    def collect_roles_from_json(self, json):
        roles = {}
        self.merge_roles_from_json(json, roles)
        return roles


class LookupRoleDownloaderVirtual:
    async def collect_roles_from_urls(self, urls):
        pass

class LookupRoleDownloaderConcrete(LookupRoleDownloaderVirtual):
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

    def get_roll_lookup(self):
        return self.role_lookup

    def get_matching_roles(self, role_name):
        option = process.extractOne(role_name, self.role_lookup.keys(), score_cutoff=80)
        if option != None:
            return self.role_lookup[option[0]]
        return None


class LookupRoleDatabaseVirtual:
    def get_server_urls(self, server_token):
        return []

    def add_server_url(self, server_token, url):
        pass

    def remove_server_url(self, server_token, url):
        pass


class LookupRoleDatabaseConcrete(LookupRoleDatabaseVirtual):
    def get_server_urls(self, server_token):
        #TODO query
        return []

    def add_server_url(self, server_token, url):
        #TODO query
        pass

    def remove_server_url(self, server_token, url):
        #TODO query
        pass


class LookupRoleData:
    def __init__(self, db):
        self.server_role_data = {}
        self.db = db

    def add_set(self, server_token, url):
        self.db.add_server_url(server_token, url)

    def remove_set(self, server_token, url):
        self.db.remove_server_url(server_token, url)

    def get_server_urls(self, server_token):
        return self.db.get_server_urls(server_token)

    def get_server_role_data(self, server_token):
        return server_token in self.server_role_data and self.server_role_data[server_token] or {}

    def update_server_role_data(self, server_token, role_lookup):
        self.server_role_data[server_token] = LookupRoleServerData(role_lookup)

    def server_roles_need_update(self, server_token):
        return (not server_token in self.server_role_data) or self.server_role_data[server_token].last_refresh_time == None or (datetime.datetime.now() - self.server_role_data[server_token].last_refresh_time) > datetime.timedelta(days=1)


class LookupImpl:    
    def __init__(self, db, downloader):
        self.data = LookupRoleData(db)
        self.downloader = downloader

    async def refresh_roles_for_server(self, server_token):
        urls = self.data.get_server_urls(server_token)
        #TODO: exception(?) leading to message if roles empty
        roles = await self.downloader.collect_roles_from_urls(urls)
        self.data.update_server_role_data(server_token, roles)

    async def refresh_roles_for_server_if_needed(self, server_token):
        if self.data.server_roles_need_update(server_token):
            await self.refresh_roles_for_server(server_token)
            return True
        return False

    async def role_lookup(self, server_token, role_to_check):
        await self.refresh_roles_for_server_if_needed(server_token)
        role_data = self.data.get_server_role_data(server_token)
        role_found = role_data.get_matching_roles(role_to_check)
        # TODO: output role info to user somehow (note that this class does not know about ctx, this function may need to be passed a virtual object

    async def add_set(self, server_token, url):
        self.data.add_set(server_token, url)
        await self.refresh_roles_for_server(server_token) # could just add the new one

    async def remove_set(self, server_token, url):
        self.data.remove_set(server_token, url)
        await self.refresh_roles_for_server(server_token)


# Concrete class for use by the Cog
class Lookup:
    def __init__(self):
        self.impl = LookupImpl(LookupRoleDatabaseConcrete(), LookupRoleDownloaderConcrete())

    def find_role_from_message_content(self, content):
        return " ".join(shlex.split(content)[1:])

    async def role_lookup(self, ctx):
        server_token = ctx.guild.id
        await self.impl.role_lookup(server_token, self.find_role_from_message_content(ctx.message.content))

    async def add_set(self, ctx):
        server_token = ctx.guild.id
        params = shlex.split(ctx.message.content)
        if len(params) == 2:
            await self.impl.add_set(server_token, params[1])
        else:
            #TODO error message
            pass

    async def remove_set(self, ctx):
        server_token = ctx.guild.id
        params = shlex.split(ctx.message.content)
        if len(params) == 2:
            await self.impl.remove_set(server_token, params[1])
        else:
            #TODO error message
            pass
