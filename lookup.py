import aiohttp
import asyncio
import datetime
from fuzzywuzzy import process
import json
import shlex
import discord
import urllib

class BotcWiki:
    def create_wiki_url(name):
        words = name.split(' ')
        words = map(lambda x: x.capitalize(), words)
        final = "_".join(words)
        final = urllib.parse.quote(final)
        return f'https://wiki.bloodontheclocktower.com/{final}'

class LookupRole:
    def matches_other(self, other):
        return self.name == other.name and self.team == other.team and self.ability == other.ability

    def __init__(self, name, ability, team, image, flavor, roles_in_script_info=None):
        self.name = name
        self.team = team
        self.ability = ability
        self.image = image
        self.flavor = flavor
        self.roles_in_script_infos = list()
        if roles_in_script_info:
            self.roles_in_script_infos.append(roles_in_script_info)
            
    def clone(self):
        c = LookupRole(self.name, self.ability, self.team, self.image, self.flavor)
        c.roles_in_script_infos.extend(self.roles_in_script_infos)
        return c

    def get_formatted_script_list(self):
        scripts = list()
        for rsi in self.roles_in_script_infos:
            if rsi.script_info:
                val = f'{rsi.script_info.tostring()}'
                (label, link) = rsi.get_role_link(self.name)
                if link != None:
                    if len(val) > 0:
                        val += ' - '
                    val += f'[{label}]({link})'
                scripts.append(val)
        return "\n".join(scripts)

    def has_script_info(self):
        for rsi in self.roles_in_script_infos:
            if rsi.script_info:
                return True
        return False

    def merge_script_infos(self, other):
        self.roles_in_script_infos.extend(other.roles_in_script_infos)

class RoleInScriptInfo:
    def __init__(self, id, script_info):
        self.id = id
        self.script_info = script_info

    def get_role_link(self, role_name):
        if self.script_info.is_official:
            url = BotcWiki.create_wiki_url(role_name)
            return ('Wiki', url)
        elif self.script_info.almanac_link:
            # Bloodstar Clocktica Almanac - put #roleid after almanac link
            if self.script_info.almanac_link.startswith('https://www.bloodstar.xyz/'):
                return ('Almanac', f'{self.script_info.almanac_link}#{self.id}')
        return (None, None)


class ScriptInfo:
    def __init__(self, name, author, img, almanac_link, is_official):
        self.name = name
        self.author = author
        self.image = img
        self.almanac_link = almanac_link
        self.is_official = is_official

    def tostring(self):
        str = ''
        if self.name:
            (label, link) = self.get_almanac_link()
            if link:
                str += f'[{label}]({link})'
            else:
                str += self.name
            if self.author:
                str += ' '
        if self.author:
            str += f'by {self.author}'
        return str

    def get_almanac_link(self):
        if self.almanac_link:
            return (self.name, self.almanac_link)
        else:
            return (None, None)


class LookupRoleMerger:

    def add_to_merged_list(self, role, role_list):
        found = False
        for r in role_list:
            if role.matches_other(r):
                found = True
                r.merge_script_infos(role)
                break
        if not found:
            role_list.append(role.clone())

    def add_to_merged_dict(self, role, role_dict):
        if role.name in role_dict:
            self.add_to_merged_list(role, role_dict[role.name])
        else:
            role_dict[role.name] = [ role.clone() ]


class LookupRoleParser:
    def __init__(self):
        self.merger = LookupRoleMerger()

    def is_valid_role_json(self, json):
        return json != None and isinstance(json, dict) and 'id' in json and json['id'] != '_meta' and 'name' in json and 'team' in json and 'ability' in json

    def create_role_from_json(self, json, script_infos):
        if self.is_valid_role_json(json):
            name = 'name' in json and json['name'] or None
            team = 'team' in json and json['team'] or None
            ability = 'ability' in json and json['ability'] or None
            flavor = 'flavor' in json and json['flavor'] or None
            image = 'image' in json and urllib.parse.quote(json['image'], safe='/:') or None
            id = 'id' in json and json['id'] or None

            if not image and script_infos and len(script_infos) > 0 and script_infos[0].is_official:
                if id:
                    image = f'https://raw.githubusercontent.com/bra1n/townsquare/develop/src/assets/icons/{id}.png'
            
            ret = None
            if script_infos and len(script_infos) > 0:
                for si in script_infos:
                    this_role = LookupRole(name, ability, team, image, flavor, RoleInScriptInfo(id, si))
                    if ret:
                        ret.merge_script_infos(this_role)
                    else:
                        ret = this_role
            else:
                ret = LookupRole(name, ability, team, image, flavor, RoleInScriptInfo(id, None))
            return ret
        return None

    def collect_roles_for_script_json(self, script, roles, is_official, official_provider):
        
        scriptInfo = None

        for json in script:
            if 'id' in json and json['id'] == "_meta":
                scriptInfo = ScriptInfo(
                    'name' in json and json['name'] or None,
                    'author' in json and json['author'] or None,
                    'logo' in json and json['logo'] or None,
                    'almanac' in json and json['almanac'] or None,
                    is_official)
                break

        for json in script:
            all_script_infos = []
            if scriptInfo:
                all_script_infos = [scriptInfo]
            if not scriptInfo and is_official:
                official_editions = official_provider.get_official_editions()
                if 'edition' in json and len(json['edition']) > 0:
                    edition = json['edition']
                    if official_editions and edition in official_editions:
                        all_script_infos = [official_editions[edition]]
                else:
                    if 'id' in json:
                        id = json['id']
                        all_script_infos = official_provider.get_script_infos_for_role_id(id)
                    if not all_script_infos:
                        all_script_infos = [ScriptInfo(None, None, None, None, True)]

            role = None
            if len(all_script_infos) > 0:
                role = self.create_role_from_json(json, all_script_infos)
            else:
                role = self.create_role_from_json(json, None)
            if role != None:
                 self.merger.add_to_merged_dict(role, roles)

    def merge_roles_from_json(self, json, roles, are_official, official_provider):
        for script in json:
            if isinstance(script, list):
                self.collect_roles_for_script_json(script, roles, are_official, official_provider)

    # TODO: There is a gross circular dependency here between the collection and the offical provider.
    # Gotta figure out the right way to fix that up.
    def collect_roles_from_json(self, json, are_official, official_provider):
        roles = {}
        self.merge_roles_from_json(json, roles, are_official, official_provider)
        return roles


class ILookupRoleDownloader:
    async def fetch_urls(self, urls):
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


class LookupRoleServerData:
    def __init__(self, role_lookup, official_provider):
        self.role_lookup = role_lookup
        self.official_provider = official_provider
        self.last_refresh_time = datetime.datetime.now()
        self.merger = LookupRoleMerger()

    def get_matching_roles(self, role_name):
        official_roles = self.official_provider.get_official_roles()
        all_roles = set()
        if self.role_lookup:
            all_roles.update(self.role_lookup.keys())
        all_roles.update(official_roles.keys())
        option = process.extractOne(role_name, all_roles, score_cutoff=80)
        if option != None:
            return self.get_roles_with_name(option[0], official_roles)
        return None

    def get_roles_with_name(self, name, official_roles):
        ret = []
        if name in official_roles:
            for r in official_roles[name]:
                self.merger.add_to_merged_list(r, ret)
        if self.role_lookup and name in self.role_lookup:
            for r in self.role_lookup[name]:
                self.merger.add_to_merged_list(r, ret)
        return ret


class ILookupRoleDatabase:
    def get_script_urls(self, server_token):
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

    def get_script_urls(self, server_token):
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
    def __init__(self, db, official_provider):
        self.server_role_data = {}
        self.db = db
        self.official_provider = official_provider

    def add_script(self, server_token, url):
        self.db.add_server_url(server_token, url)

    def remove_script(self, server_token, url):
        self.db.remove_server_url(server_token, url)

    def get_script_urls(self, server_token):
        return self.db.get_script_urls(server_token)

    def get_script_count(self, server_token):
        return len(self.get_script_urls(server_token))

    def get_server_role_data(self, server_token):
        return server_token in self.server_role_data and self.server_role_data[server_token] or {}

    def update_server_role_data(self, server_token, role_lookup):
        self.server_role_data[server_token] = LookupRoleServerData(role_lookup, self.official_provider)

    def server_roles_need_update(self, server_token):
        return (not server_token in self.server_role_data) or self.server_role_data[server_token].last_refresh_time == None or (datetime.datetime.now() - self.server_role_data[server_token].last_refresh_time) > datetime.timedelta(days=1)

class IOfficialEditionProvider:
    async def refresh_official_roles_if_needed(self):
        pass

    def get_official_roles(self):
        return {}

    def get_official_editions(self):
        return {}

    def official_roles_need_refresh(self):
        return False

    def get_script_infos_for_role_id(self, role_id):
        return []


class OfficialEditionProvider:
    def __init__(self, downloader, role_parser):
        self.downloader = downloader
        self.role_parser = role_parser
        self.last_official_refresh_time = None
        self.official_editions = None
        self.official_edition_urls = ['https://raw.githubusercontent.com/bra1n/townsquare/develop/src/editions.json']
        self.official_roles = None
        self.official_role_urls = [
            'https://raw.githubusercontent.com/bra1n/townsquare/develop/src/roles.json',
            'https://raw.githubusercontent.com/bra1n/townsquare/develop/src/fabled.json',
            ]
        self.role_to_script_info_dict = {}

    async def refresh_official_roles_if_needed(self):
        if self.official_roles_need_refresh():
            await self.refresh_official_roles()

    def get_official_roles(self):
        return self.official_roles

    def get_official_editions(self):
        return self.official_editions

    def get_script_infos_for_role_id(self, role_id):
        if role_id in self.role_to_script_info_dict:
            return self.role_to_script_info_dict[role_id]
        return []

    def official_roles_need_refresh(self):
        return not self.official_editions or not self.official_roles or not self.last_official_refresh_time or (datetime.datetime.now() - self.last_official_refresh_time) > datetime.timedelta(days=1)

    async def refresh_official_roles(self):
        self.last_official_refresh_time = datetime.datetime.now()
        await self.refresh_official_editions()
        download_results = await self.downloader.fetch_urls(self.official_role_urls)
        self.official_roles = self.role_parser.collect_roles_from_json(download_results, True, self)

    async def refresh_official_editions(self):
        edition_jsons = await self.downloader.fetch_urls(self.official_edition_urls)
        self.official_editions = {}
        self.official_roles = {}
        self.role_to_script_info_dict = {}
        for json in edition_jsons:
            if isinstance(json, list):
                self.add_list_to_official_editions(json)

    def add_list_to_official_editions(self, json):
        for ed_json in json:
            if isinstance(ed_json, dict):
                self.add_to_official_editions(ed_json)

    def add_to_official_editions(self, ed_json):
        if 'id' in ed_json and 'name' in ed_json and 'author' in ed_json and 'isOfficial' in ed_json:
            id = ed_json['id']
            name = ed_json['name']
            author = ed_json['author']
            is_official = ed_json['isOfficial']
            almanac_link = BotcWiki.create_wiki_url(name)
            script_info = ScriptInfo(name, author, None, almanac_link, is_official)
            self.official_editions[id] = script_info

            if 'roles' in ed_json:
                roles = ed_json['roles']
                for role_id in roles:
                    if role_id in self.role_to_script_info_dict:
                        self.role_to_script_info_dict[role_id].append(script_info)
                    else:
                        self.role_to_script_info_dict[role_id] = [script_info]


class LookupImpl:
    def __init__(self, lookup_role_data, official_provider, downloader, role_parser):
        self.data = lookup_role_data
        self.official_provider = official_provider
        self.downloader = downloader
        self.role_parser = role_parser

    async def refresh_roles_for_server(self, server_token):
        urls = self.data.get_script_urls(server_token)
        #TODO: exception(?) leading to message if roles empty - it can be returned
        download_results = await self.downloader.fetch_urls(urls)
        roles = None
        if download_results:
            roles = self.role_parser.collect_roles_from_json(download_results, False, self.official_provider)
        self.data.update_server_role_data(server_token, roles)

    async def refresh_roles_for_server_if_needed(self, server_token):
        if self.data.server_roles_need_update(server_token):
            await self.refresh_roles_for_server(server_token)

    async def role_lookup(self, server_token, role_to_check):
        await self.official_provider.refresh_official_roles_if_needed()

        await self.refresh_roles_for_server_if_needed(server_token)
        role_data = self.data.get_server_role_data(server_token)
        role_found = role_data.get_matching_roles(role_to_check)
        return role_found

    async def add_script(self, server_token, url):
        self.data.add_script(server_token, url)
        return await self.refresh_roles_for_server(server_token) # could just add the new one instead of a full refresh

    async def remove_script(self, server_token, url):
        self.data.remove_script(server_token, url)
        return await self.refresh_roles_for_server(server_token)

    def get_script_count(self, server_token):
        return self.data.get_script_count(server_token)

    def get_script_urls(self, server_token):
        return self.data.get_script_urls(server_token)


# Concrete class for use by the Cog
class Lookup:
    def __init__(self, db):
        role_parser = LookupRoleParser()
        downloader = LookupRoleDownloader()
        op = OfficialEditionProvider(downloader, role_parser)
        lrdb = LookupRoleDatabase(db)
        lrd = LookupRoleData(lrdb, op)
        self.impl = LookupImpl(lrd, op, downloader, role_parser)

    def find_role_from_message_content(self, content):
        sanitized = content.replace('\'', '').replace('"', '')
        return ' '.join(shlex.split(sanitized)[1:])

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
            if message:
                return message
            return f'Removed script at URL: {params[1]}\nI now know about {self.impl.get_script_count(server_token)} scripts.'
        else:
            return 'Incorrect usage. Please pass the URL of a script to remove.'

    async def refresh_scripts(self, ctx):
        server_token = ctx.guild.id
        await self.impl.refresh_roles_for_server(server_token)
        return f'{self.impl.get_script_count(server_token)} scripts refreshed to latest versions.'

    async def list_scripts(self, ctx):
        server_token = ctx.guild.id
        scripts = self.impl.get_script_urls(server_token)
        if scripts and len(scripts) > 0:
            scripts_str = "\n \u2022 ".join(scripts)
            return f'The following scripts are known for this server:\n \u2022 {scripts_str}'
        return 'No scripts are known. Try adding some!'

    async def send_role(self, ctx, roles):
        if roles == None:
            await ctx.send("No matching roles found!")
            return

        for role in roles:
            color = discord.Color.from_rgb(32, 100, 252)
            if role.team == 'outsider':
                color = discord.Color.from_rgb(69, 209, 251)
            elif role.team == 'minion':
                color = discord.Color.from_rgb(251, 103, 0)
            elif role.team == 'demon':
                color = discord.Color.from_rgb(203, 1, 0)
            elif role.team == 'traveler':
                color = discord.Color.purple()
            elif role.team == 'fabled':
                color = discord.Color.from_rgb(248, 227, 30)

            team = f'{role.team.capitalize()}'
            embed = discord.Embed(title=f'{role.name}', description=team, color=color)

            embed.add_field(name='Ability', value=role.ability, inline=False)

            if role.flavor:
                embed.set_footer(text=role.flavor)

            if role.has_script_info():
                scripts = role.get_formatted_script_list()
                embed.add_field(name='Found In', value=scripts, inline=False)
            
            if role.image != None:
                embed.set_thumbnail(url=role.image)

            try:
                await ctx.send(embed=embed)
            except Exception as ex:
                print(f"Bad embed! {ex}")

