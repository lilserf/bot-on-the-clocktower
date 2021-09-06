import unittest

import lookup

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
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('foo', {})

        self.assertIsNone(result, 'Expected no results')


    def test_serverdatahasroles_findsperfectmatch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk', 'SomeImage')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('Some Role', {})

        self.assertEqual(result[0], role, 'Expected matching role')


    def test_serverdatahasroles_findsclosematch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk', 'SomeImage')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('Som', {})

        self.assertEqual(result[0], role, 'Expected matching role')


    def test_serverdatahasroles_doesntfindnomatch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk', 'SomeImage')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('bwuh', {})
        
        self.assertIsNone(result, 'Expected no results')


    def test_serverdatahasroles_matchesrealisticmistake(self):
        role = lookup.LookupRole('Boom-dandy', 'Some Ability', 'Townsfolk', 'SomeImage')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('bomdand', {})
        
        self.assertEqual(result[0], role, 'Expected matching role')

class TestLookupImpl(unittest.IsolatedAsyncioTestCase):

    async def test_lookup_impl(self):

        class TestDb(lookup.ILookupRoleDatabase):
            def __init__(self):
                self.urls = [];

            def get_script_urls(self, server_token):
                return self.urls;

            def add_server_url(self, server_token, url):
                self.urls.append(url)

            def remove_server_url(self, server_token, url):
                self.urls.remove(url)

        class TestDownloader(lookup.ILookupRoleDownloader):
            async def collect_roles_from_urls(self, urls, is_official):
                pass

        tdb = TestDb()
        tdl = TestDownloader()

        token = "thistokenisfake"

        looker = lookup.LookupImpl(tdb, tdl)
        await looker.refresh_roles_for_server(token)



if __name__ == '__main__':
    unittest.main()