import glob
import unittest

def create_test_suite():
    test_file_strings = glob.glob('test_*.py')
    module_strings = [str[:len(str)-3] for str in test_file_strings]
    suites = [unittest.defaultTestLoader.loadTestsFromName(name) \
              for name in module_strings]
    testSuite = unittest.TestSuite(suites)
    return testSuite

if __name__ == '__main__':
    test_suite = create_test_suite()
    unittest.TextTestRunner().run(test_suite)