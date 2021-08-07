import unittest

import lookup

class TestLookups(unittest.TestCase):

    def test_coolectsroles_norolejson_createsnone(self):
        lk = lookup.Lookup()

        results_json = [[]]
        roles = lk.collect_roles_from_results(results_json)

        self.assertEqual(len(roles), 0, "Expected 0 roles")


    def test_coolectsroles_novalidrolejson_createsnone(self):
        
        lk = lookup.Lookup()

        results_json = [[{'id':'role-id', 'name':'Has Name', 'ability':'has no team'}]]
        roles = lk.collect_roles_from_results(results_json)

        self.assertEqual(len(roles), 0, "Expected 0 roles")


    def test_coolectsroles_onerole_createsone(self):
        
        lk = lookup.Lookup()

        results_json = [[{'id':'role-id', 'name':'Has Name', 'ability':'some ability', 'team':'Townsfolk'}]]
        roles = lk.collect_roles_from_results(results_json)

        self.assertEqual(len(roles), 1, "Expected 1 role")


    def test_coolectsroles_manyroles_createsall(self):
        
        lk = lookup.Lookup()

        results_json = [[
            {'id':'role-id-1', 'name':'Name 1', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id-2', 'name':'Name 2', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id-3', 'name':'Name 3', 'ability':'some ability', 'team':'Townsfolk'},
            ]]
        roles = lk.collect_roles_from_results(results_json)

        self.assertEqual(len(roles), 3, "Expected 3 roles")


    def test_coolectsroles_duperoles_createsone(self):
        
        lk = lookup.Lookup()

        results_json = [[
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk'},
            {'id':'role-id', 'name':'Name', 'ability':'some ability', 'team':'Townsfolk'},
            ]]
        roles = lk.collect_roles_from_results(results_json)

        self.assertEqual(len(roles), 1, "Expected 1 role")


if __name__ == '__main__':
    unittest.main()