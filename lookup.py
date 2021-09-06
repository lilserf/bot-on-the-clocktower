import aiohttp
import asyncio
import datetime
from fuzzywuzzy import process
import json
import shlex
import discord
import urllib

class LookupRole:
    def matches_other(self, other):
        return self.name == other.name and self.team == other.team and self.ability == other.ability

    def __init__(self, name, ability, team, image, scriptInfo=None):
        self.name = name
        self.team = team
        self.ability = ability
        self.image = image
        self.scriptInfos = list()
        self.scriptInfos.append(scriptInfo)
        if self.image == None and scriptInfo.is_official:
            self.image = f'https://raw.githubusercontent.com/bra1n/townsquare/develop/src/assets/icons/{urllib.parse.quote(self.name.lower())}.png'

    def get_formatted_script_list(self):
        strs = map(lambda x: f'{x.tostring()}', self.scriptInfos)
        return ", ".join(strs)

    def has_script_info(self):
        return len(self.scriptInfos) > 0
        

class ScriptInfo:
    def __init__(self, name, author, img, is_official):
        self.name = name
        self.author = author
        self.image = img
        self.is_official = is_official

    def tostring(self):
        return f'{self.name} by {self.author}'

class LookupRoleParser:
    def is_valid_role_json(self, json):
        return json != None and isinstance(json, dict) and 'id' in json and json['id'] != '_meta' and 'name' in json and 'team' in json and 'ability' in json

    def create_role_from_json(self, json, scriptInfo):
        if self.is_valid_role_json(json):
            name = json['name']
            team = json['team']
            ability = json['ability']
            try:
                image = json['image']
            except KeyError:
                image = None
            return LookupRole(name, ability, team, image, scriptInfo)
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

    def collect_roles_for_script_json(self, script, roles, is_official):
        
        scriptInfo = None

        for json in script:
            if json["id"] == "_meta":
                scriptInfo = ScriptInfo('name' in json and json["name"], 'author' in json and json["author"], 'logo' in json and json["logo"], is_official)
                break

        for json in script:
            thisScriptInfo = scriptInfo
            if not thisScriptInfo and is_official:
                lookup = { "tb" : "Trouble Brewing (Official)", "bmr": "Bad Moon Rising (Official)", "snv": "Sects & Violets (Official)", "": "Experimental/Unreleased"}
                setname = lookup[json["edition"]]
                thisScriptInfo = ScriptInfo(setname, "Pandemonium Institute", None, True)

            role = self.create_role_from_json(json, thisScriptInfo)
            if role != None:
                 self.add_role(role, roles)

    def merge_roles_from_json(self, json, roles, are_official):
        for script in json:
            if isinstance(script, list):
                self.collect_roles_for_script_json(script, roles, are_official)

    def collect_roles_from_json(self, json, are_official):
        roles = {}
        self.merge_roles_from_json(json, roles, are_official)
        return roles


class ILookupRoleDownloader:
    async def collect_roles_from_urls(self, urls, are_official):
        pass

class LookupRoleDownloader(ILookupRoleDownloader):
    async def fetch_url(self, url, session):
        try:
            async with session.get(url) as response:
                data = await response.read()
            return json.loads(data)
        except Exception as ex:
            return []

    async def fetch_urls(self, urls):
        loop = asyncio.get_event_loop()
        async with aiohttp.ClientSession() as session:
            tasks = [loop.create_task(self.fetch_url(url, session)) for url in urls]
            return await asyncio.gather(*tasks)

    async def collect_roles_from_urls(self, urls, are_official):
        results = await self.fetch_urls(urls)
        parser = LookupRoleParser()
        return parser.collect_roles_from_json(results, are_official)


class LookupRoleServerData:
    def __init__(self, role_lookup):
        self.role_lookup = role_lookup
        self.last_refresh_time = datetime.datetime.now()

    def get_role_lookup(self):
        return self.role_lookup

    def get_matching_roles(self, role_name, official_roles):
        all_roles = set()
        all_roles.update(self.role_lookup.keys())
        all_roles.update(official_roles.keys())
        option = process.extractOne(role_name, all_roles, score_cutoff=80)
        if option != None:
            return self.get_roles_with_name(option[0], official_roles)
        return None

    def get_roles_with_name(self, name, official_roles):
        ret = []
        if name in official_roles:
            ret.extend(official_roles[name])
        if name in self.role_lookup:
            ret.extend(self.role_lookup[name])
        return ret


class ILookupRoleDatabase:
    def get_server_urls(self, server_token):
        return []

    def add_server_url(self, server_token, url):
        pass

    def remove_server_url(self, server_token, url):
        pass


class LookupRoleDatabase(ILookupRoleDatabase):
    def __init__(self, db):
        self.serverRoleUrls = db['ServerRoleUrls']

    def get_doc_internal(self, server_token):
        lookupQuery = { "server" : server_token }
        doc = self.serverRoleUrls.find_one(lookupQuery)
        if doc == None:
            doc = { "server" : server_token, "urls" : list() }
        return doc

    def update_doc_internal(self, server_token, doc):
        lookupQuery = { "server" : server_token }
        self.serverRoleUrls.replace_one(lookupQuery, doc, True)

    def get_server_urls(self, server_token):
        doc = self.get_doc_internal(server_token)
        return doc["urls"]

    def add_server_url(self, server_token, url):
        doc = self.get_doc_internal(server_token)
        if not url in doc["urls"]:
            doc["urls"].append(url)
        self.update_doc_internal(server_token, doc)

    def remove_server_url(self, server_token, url):
        doc = self.get_doc_internal(server_token)
        doc["urls"].remove(url)
        self.update_doc_internal(server_token, doc)

class LookupRoleData:
    def __init__(self, db):
        self.server_role_data = {}
        self.db = db

    def add_script(self, server_token, url):
        self.db.add_server_url(server_token, url)

    def remove_script(self, server_token, url):
        self.db.remove_server_url(server_token, url)

    def get_server_urls(self, server_token):
        return self.db.get_server_urls(server_token)

    def get_script_count(self, server_token):
        return len(self.get_server_urls(server_token))

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
        self.last_official_refresh_time = None
        self.official_roles = None
        self.official_role_urls = ['https://raw.githubusercontent.com/bra1n/townsquare/develop/src/roles.json']

    async def refresh_roles_for_server(self, server_token):
        urls = self.data.get_server_urls(server_token)
        #TODO: exception(?) leading to message if roles empty - it can be returned
        roles = await self.downloader.collect_roles_from_urls(urls, False)
        self.data.update_server_role_data(server_token, roles)

    async def refresh_roles_for_server_if_needed(self, server_token):
        if self.data.server_roles_need_update(server_token):
            await self.refresh_roles_for_server(server_token)

    async def role_lookup(self, server_token, role_to_check):
        if self.official_roles_need_refresh():
            await self.refresh_official_roles()

        await self.refresh_roles_for_server_if_needed(server_token)
        role_data = self.data.get_server_role_data(server_token)
        role_found = role_data.get_matching_roles(role_to_check, self.official_roles)
        return role_found

    async def add_script(self, server_token, url):
        self.data.add_script(server_token, url)
        return await self.refresh_roles_for_server(server_token) # could just add the new one instead of a full refresh

    async def remove_script(self, server_token, url):
        self.data.remove_script(server_token, url)
        return await self.refresh_roles_for_server(server_token)

    def get_script_count(self, server_token):
        return self.data.get_script_count(server_token)

    def official_roles_need_refresh(self):
        return not self.official_roles or not self.last_official_refresh_time or (datetime.datetime.now() - self.last_official_refresh_time) > datetime.timedelta(days=1)

    async def refresh_official_roles(self):
        self.last_official_refresh_time = datetime.datetime.now()
        self.official_roles = await self.downloader.collect_roles_from_urls(self.official_role_urls, True)


# Concrete class for use by the Cog
class Lookup:
    def __init__(self, db):
        self.impl = LookupImpl(LookupRoleDatabase(db), LookupRoleDownloader())

    def find_role_from_message_content(self, content):
        return " ".join(shlex.split(content)[1:])

    async def role_lookup(self, ctx):
        server_token = ctx.guild.id
        role = await self.impl.role_lookup(server_token, self.find_role_from_message_content(ctx.message.content))
        await self.send_role(ctx, role)

    async def add_script(self, ctx):
        server_token = ctx.guild.id
        params = shlex.split(ctx.message.content)
        if len(params) == 2:
            message = await self.impl.add_script(server_token, params[1])
            if message:
                return message
            return f'Added script at URL: {params[1]}\nI now know about {self.impl.get_script_count(server_token)} scripts.'
        else:
            return 'Incorrect usage. Please pass the URL of a script to add.'

    async def remove_script(self, ctx):
        server_token = ctx.guild.id
        params = shlex.split(ctx.message.content)
        if len(params) == 2:
            message = await self.impl.remove_script(server_token, params[1])
            message = await self.impl.add_script(server_token, params[1])
            if message:
                return message
            return f'Removed script at URL: {params[1]}\nI now know about {self.impl.get_script_count(server_token)} scripts.'
        else:
            return 'Incorrect usage. Please pass the URL of a script to remove.'

    async def refresh_scripts(self, ctx):
        server_token = ctx.guild.id
        await self.impl.refresh_roles_for_server(server_token)
        return f'{self.impl.get_script_count(server_token)} scripts refreshed to latest versions.'

    async def send_role(self, ctx, roles):
        if roles == None:
            await ctx.send("No matching roles found!")
            return

        for role in roles:
            color = discord.Color.from_rgb(32, 100, 252)
            if role.team == 'outsider':
                color = discord.Color.from_rgb(69, 209, 251)
            if role.team == 'minion':
                color = discord.Color.from_rgb(251, 103, 0)
            if role.team == 'demon':
                color = discord.Color.from_rgb(203, 1, 0)

            embed = discord.Embed(title=f'{role.name}', description=f'{role.ability}', color=color)
            footer = f'{role.team.capitalize()}'
            if role.has_script_info():
                footer += " - " + role.get_formatted_script_list()
            embed.set_footer(text=footer)
            if role.image != None:
                embed.set_thumbnail(url=role.image)

            try:
                await ctx.send(embed=embed)
            except Exception:
                print("Bad embed!")

