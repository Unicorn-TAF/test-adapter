using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using Unicorn.TestAdapter.Util;
using System.Reflection;

namespace Unicorn.TestAdapter
{
    public class TestCaseFilter
    {
        private const string DisplayName = "DisplayName";
        private const string FullyQualifiedName = "FullyQualifiedName";

        private readonly HashSet<string> _filterableTraits = 
            new HashSet<string>() { Constants.CategoryTrait, Constants.TagTrait, Constants.DisabledTrait };

        private readonly List<string> _supportedPropertyNames;
        private readonly ITestCaseFilterExpression _filterExpression;
        private readonly bool _successfullyGotFilter;
        private readonly bool _isDiscovery;

        public TestCaseFilter(IRunContext runContext, Logger logger, string assemblyFileName)
        {
            _supportedPropertyNames = GetSupportedPropertyNames();

            _successfullyGotFilter = GetTestCaseFilterExpression(
                runContext, logger, assemblyFileName, out _filterExpression);
        }

        public TestCaseFilter(IDiscoveryContext discoveryContext, Logger logger)
        {
            // Traits are not known at discovery time because we load them from tests
            _isDiscovery = true;
            _supportedPropertyNames = GetSupportedPropertyNames();
            
            _successfullyGotFilter = GetTestCaseFilterExpressionFromDiscoveryContext(
                discoveryContext, logger, out _filterExpression);
        }

        public bool MatchTestCase(TestCase testCase)
        {
            // Had an error while getting filter, match no testcase to ensure discovered test list is empty
            if (!_successfullyGotFilter)
            {
                return false;
            }

            // No filter specified, keep every testcase
            if (_filterExpression is null)
            {
                return true;
            }

            return _filterExpression.MatchTestCase(testCase, (p) => PropertyProvider(testCase, p));
        }

        public object PropertyProvider(TestCase testCase, string name)
        {
            if (string.Equals(name, FullyQualifiedName, StringComparison.OrdinalIgnoreCase))
            {
                return testCase.FullyQualifiedName;
            }
                
            if (string.Equals(name, DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                return testCase.DisplayName;
            }

            // Traits filtering
            if (_isDiscovery || _filterableTraits.Contains(name))
            {
                var result = new List<string>();

                foreach (var trait in GetTraits(testCase))
                {
                    if (string.Equals(trait.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(trait.Value);
                    }
                }

                if (result.Count > 0)
                {
                    return result.ToArray();
                }
            }

            return null;
        }

        private bool GetTestCaseFilterExpression(IRunContext runContext, Logger logger, string assemblyFileName, 
            out ITestCaseFilterExpression filter)
        {
            filter = null;

            try
            {
                filter = runContext.GetTestCaseFilter(_supportedPropertyNames, s => null);
                return true;
            }
            catch (TestPlatformFormatException e)
            {
                logger.Warn("{0}: Exception filtering tests: {1}", 
                    Path.GetFileNameWithoutExtension(assemblyFileName), e.Message);
                return false;
            }
        }

        private bool GetTestCaseFilterExpressionFromDiscoveryContext(IDiscoveryContext discoveryContext, Logger logger,
            out ITestCaseFilterExpression filter)
        {
            filter = null;

            if (discoveryContext is IRunContext runContext)
            {
                try
                {
                    filter = runContext.GetTestCaseFilter(_supportedPropertyNames, s => null);
                    return true;
                }
#if NET || NETCOREAPP
                catch (TestPlatformException e)
#else
                catch (Exception e)
#endif
                {
                    logger.Warn($"Exception filtering tests: {e.Message}");
                    return false;
                }
            }
            else
            {
                try
                {
                    TestProperty dummy(string name) => null;

                    // GetTestCaseFilter is present on DiscoveryContext but not in IDiscoveryContext interface
                    var method = discoveryContext.GetType().GetRuntimeMethod(
                        "GetTestCaseFilter", 
                        new Type[] { typeof(IEnumerable<string>), typeof(Func<string, TestProperty>) });
                    
                    filter = method?.Invoke(discoveryContext, new object[] { _supportedPropertyNames, dummy("") }) 
                        as ITestCaseFilterExpression;

                    return true;
                }
                catch (TargetInvocationException e)
                {
#if NET || NETCOREAPP
                    if (e.InnerException is TestPlatformException ex)
                    {
                        logger.Warn($"Exception filtering tests: {ex.InnerException?.Message}");
                        return false;
                    }
#endif

                    throw e.InnerException == null ? e : e.InnerException;
                }
            }
        }

        private List<string> GetSupportedPropertyNames() =>
            new List<string>(_filterableTraits)
            {
                DisplayName,
                FullyQualifiedName
            };

        private static IEnumerable<KeyValuePair<string, string>> GetTraits(TestCase testCase)
        {
            var traitProperty = TestProperty.Find("TestObject.Traits");

            if (traitProperty != null)
            {
                return testCase.GetPropertyValue(traitProperty, new KeyValuePair<string, string>[0]);
            }

            return new KeyValuePair<string, string>[0];
        }
    }
}
