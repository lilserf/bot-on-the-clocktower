import unittest

import lookup

class TestLookups(unittest.TestCase):

    def test_coolectsroles_norolejson_createsnone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[]]
        roles = parser.collect_roles_from_json(results_json)

        self.assertEqual(len(roles), 0, 'Expected 0 roles')


    def test_coolectsroles_novalidrolejson_createsnone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[{'id':'role-id', 'name':'Has Name', 'ability':'has no team'}]]
        roles = parser.collect_roles_from_json(results_json)

        self.assertEqual(len(roles), 0, 'Expected 0 roles')


    def test_coolectsroles_onerole_createsone(self):        
        parser = lookup.LookupRoleParser()

        results_json = [[{'id':'role-id', 'name':'Has Name', 'ability':'some ability', 'team':'Townsfolk'}]]
        roles = parser.collect_roles_from_json(results_json)

        self.assertEqual(len(roles), 1, 'Expected 1 role')


    def test_coolectsroles_manyroles_createsall(self):
        parser = lookup.LookupRoleParser()

        results_json = [[
            {'id':'role-id-1', 'name':'Name 1', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id-2', 'name':'Name 2', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id-3', 'name':'Name 3', 'ability':'some ability', 'team':'Townsfolk'},
            ]]
        roles = parser.collect_roles_from_json(results_json)

        self.assertEqual(len(roles), 3, 'Expected 3 roles')


    def test_coolectsroles_duperoles_createsone(self):
        parser = lookup.LookupRoleParser()

        results_json = [[
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk'},
            ]]
        roles = parser.collect_roles_from_json(results_json)

        self.assertEqual(len(roles), 1, 'Expected 1 role')


    def test_serverdatahasnothing_cantfindrole(self):
        roles = {}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('foo')

        self.assertIsNone(result, 'Expected no results')


    def test_serverdatahasroles_findsperfectmatch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('Some Role')

        self.assertEqual(result[0], role, 'Expected matching role')


    def test_serverdatahasroles_findsclosematch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('Som')

        self.assertEqual(result[0], role, 'Expected matching role')


    def test_serverdatahasroles_doesntfindnomatch(self):
        role = lookup.LookupRole('Some Role', 'Some Ability', 'Townsfolk')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('bwuh')
        
        self.assertIsNone(result, 'Expected no results')


    def test_serverdatahasroles_matchesrealisticmistake(self):
        role = lookup.LookupRole('Boom-dandy', 'Some Ability', 'Townsfolk')
        roles = {role.name : [role]}
        db = lookup.LookupRoleServerData(roles)

        result = db.get_matching_roles('bomdand')
        
        self.assertEqual(result[0], role, 'Expected matching role')


if __name__ == '__main__':
    unittest.main()