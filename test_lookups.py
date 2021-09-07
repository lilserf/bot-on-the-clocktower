import unittest

import lookup



class MockDb(lookup.ILookupRoleDatabase):
    def __init__(self):
        self.urls = [];

    def get_script_urls(self, server_token):
        return self.urls;

    def add_server_url(self, server_token, url):
        self.urls.append(url)

    def remove_server_url(self, server_token, url):
        self.urls.remove(url)

class MockDownloader(lookup.ILookupRoleDownloader):
    async def collect_roles_from_urls(self, urls, is_official):
        pass

class MockOfficialProvider(lookup.IOfficialEditionProvider):
    def __init__(self):
        self.official_roles = {}

    async def refresh_official_roles_if_needed(self):
        pass

    def get_official_roles(self):
        return self.official_roles

    def official_roles_need_refresh(self):
        return False

class TestLookups(unittest.TestCase):

    def test_collectsroles_norolejson_createsnone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[]]
        roles = parser.collect_roles_from_json(results_json, False)

        self.assertEqual(len(roles), 0, 'Expected 0 roles')


    def test_collectsroles_novalidrolejson_createsnone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[{'id':'role-id', 'name':'Has Name', 'ability':'has no team', 'image':'some image'}]]
        roles = parser.collect_roles_from_json(results_json, False)

        self.assertEqual(len(roles), 0, 'Expected 0 roles')


    def test_collectsroles_onerole_createsone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[{'id':'role-id', 'name':'Has Name', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'}]]
        roles = parser.collect_roles_from_json(results_json, False)

        self.assertEqual(len(roles), 1, 'Expected 1 role')


    def test_collectsroles_manyroles_createsall(self):
        parser = lookup.LookupRoleParser()

        results_json = [[
            {'id':'role-id-1', 'name':'Name 1', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'},
            {'id':'role-id-2', 'name':'Name 2', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'},
            {'id':'role-id-3', 'name':'Name 3', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'},
            ]]
        roles = parser.collect_roles_from_json(results_json, False)

        self.assertEqual(len(roles), 3, 'Expected 3 roles')


    def test_collectsroles_duperoles_createsone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'},
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'},
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk', 'image':'some image'},
            ]]
        roles = parser.collect_roles_from_json(results_json, False)

        self.assertEqual(len(roles), 1, 'Expected 1 role')


    def test_serverdatahasnothing_cantfindrole(self):
        roles = {}
        db = lookup.LookupRoleServerData(roles, MockOfficialProvider())

        result = db.get_matching_roles('foo')

        self.assertIsNone(result, 'Expected no results')


    def test_serverdatahasroles_findsperfectmatch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk', 'SomeImage', 'flavor')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles, MockOfficialProvider())

        result = db.get_matching_roles('Some Role')

        self.assertTrue(result[0].matches_other(role), 'Expected matching role')


    def test_serverdatahasroles_findsclosematch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk', 'SomeImage', 'flavor')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles, MockOfficialProvider())

        result = db.get_matching_roles('Som')

        self.assertTrue(result[0].matches_other(role), 'Expected matching role')


    def test_serverdatahasroles_doesntfindnomatch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk', 'SomeImage', 'flavor')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles, MockOfficialProvider())

        result = db.get_matching_roles('bwuh')
        
        self.assertIsNone(result, 'Expected no results')


    def test_serverdatahasroles_matchesrealisticmistake(self):
        role = lookup.LookupRole('Boom-dandy', 'Some Ability', 'Townsfolk', 'SomeImage', 'flavor')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles, MockOfficialProvider())

        result = db.get_matching_roles('bomdand')
        
        self.assertTrue(result[0].matches_other(role), 'Expected matching role')


    def test_lookuprole_merging(self):
        merger = lookup.LookupRoleMerger()

        role1 = lookup.LookupRole('somename', 'ability1', 'townsfolk', 'image1', 'flavor1', lookup.RoleInScriptInfo('id1', lookup.ScriptInfo('script1', 'author1', 'scriptimage1', None, False)))
        role2 = lookup.LookupRole('somename', 'ability1', 'townsfolk', 'image2', 'flavor2', lookup.RoleInScriptInfo('id2', lookup.ScriptInfo('script2', 'author2', 'scriptimage2', None, False)))

        merged = {}
        merger.add_to_merged_dict(role1, merged)
        merger.add_to_merged_dict(role2, merged)

        self.assertEqual(1, len(merged.keys()))
        self.assertEqual(1, len(merged[role1.name]))
        self.assertEqual(2, len(merged[role1.name][0].roles_in_script_infos))
        self.assertNotEqual(role1, merged[role1.name][0], 'after merging, LookupRole should be unique object to not pollute main data')
        self.assertNotEqual(role2, merged[role1.name][0], 'after merging, LookupRole should be unique object to not pollute main data')
        
    def test_serverdata_merge_with_official(self):
        merger = lookup.LookupRoleMerger()


        off_merged = {}
        role_official = lookup.LookupRole('somename', 'ability', 'townsfolk', 'official_role_image', 'flavor', lookup.RoleInScriptInfo('ido', lookup.ScriptInfo('official', 'official_author', 'official_image', None, True)))
        merger.add_to_merged_dict(role_official, off_merged)

        op = MockOfficialProvider()
        op.official_roles = off_merged

        unoff_merged = {}
        role_unofficial_1 = lookup.LookupRole('somename', 'ability', 'townsfolk', 'unofficial_role_image_1', 'flavor1', lookup.RoleInScriptInfo('idu1', lookup.ScriptInfo('unofficial_1', 'unofficial_author_1', 'unofficial_image_1', None, False)))
        role_unofficial_2 = lookup.LookupRole('somename', 'ability', 'townsfolk', 'unofficial_role_image_2', 'flavor2', lookup.RoleInScriptInfo('idu2', lookup.ScriptInfo('unofficial_2', 'unofficial_author_2', 'unofficial_image_2', None, False)))
        merger.add_to_merged_dict(role_unofficial_1, unoff_merged)
        merger.add_to_merged_dict(role_unofficial_2, unoff_merged)

        server_data = lookup.LookupRoleServerData(unoff_merged, op)
        ret = server_data.get_matching_roles('somename')

        self.assertEqual(1, len(ret))
        found_main_role = ret[0]
        self.assertEqual(3, len(found_main_role.roles_in_script_infos))
        self.assertEqual('somename', found_main_role.name)
        self.assertEqual('ability', found_main_role.ability)
        self.assertEqual('townsfolk', found_main_role.team)
        self.assertEqual('official_role_image', found_main_role.image)
        self.assertEqual('ido', found_main_role.roles_in_script_infos[0].id)
        self.assertEqual('official', found_main_role.roles_in_script_infos[0].script_info.name)
        self.assertEqual('official_author', found_main_role.roles_in_script_infos[0].script_info.author)
        self.assertEqual('official_image', found_main_role.roles_in_script_infos[0].script_info.image)
        self.assertEqual('idu1', found_main_role.roles_in_script_infos[1].id)
        self.assertEqual('unofficial_1', found_main_role.roles_in_script_infos[1].script_info.name)
        self.assertEqual('unofficial_author_1', found_main_role.roles_in_script_infos[1].script_info.author)
        self.assertEqual('unofficial_image_1', found_main_role.roles_in_script_infos[1].script_info.image)
        self.assertEqual('idu2', found_main_role.roles_in_script_infos[2].id)
        self.assertEqual('unofficial_2', found_main_role.roles_in_script_infos[2].script_info.name)
        self.assertEqual('unofficial_author_2', found_main_role.roles_in_script_infos[2].script_info.author)
        self.assertEqual('unofficial_image_2', found_main_role.roles_in_script_infos[2].script_info.image)


class TestLookupImpl(unittest.IsolatedAsyncioTestCase):

    async def test_lookup_impl(self):
            
        tdl = MockDownloader()
        top = MockOfficialProvider()
        lrd = lookup.LookupRoleData(MockDb(), top)

        token = "thistokenisfake"

        looker = lookup.LookupImpl(lrd, top, tdl)
        await looker.refresh_roles_for_server(token)



if __name__ == '__main__':
    unittest.main()