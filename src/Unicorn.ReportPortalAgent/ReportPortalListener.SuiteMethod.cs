﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Abstractions.Responses;
using Unicorn.Taf.Core.Testing;
using ULogging = Unicorn.Taf.Core.Logging;
using UTesting = Unicorn.Taf.Core.Testing;

namespace Unicorn.ReportPortalAgent
{
    /// <summary>
    /// Report portal listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class ReportPortalListener
    {
        private const string AuthorAttribute = "author";
        private const string MachineAttribute = "machine";
        private const string CategoryAttribute = "category";

        private readonly Dictionary<SuiteMethodType, TestItemType> _itemTypes =
            new Dictionary<SuiteMethodType, TestItemType>
        {
            { SuiteMethodType.BeforeSuite, TestItemType.BeforeClass },
            { SuiteMethodType.BeforeTest, TestItemType.BeforeMethod },
            { SuiteMethodType.AfterTest, TestItemType.AfterMethod },
            { SuiteMethodType.AfterSuite, TestItemType.AfterClass },
            { SuiteMethodType.Test, TestItemType.Step },
        };

        internal string SkippedTestDefectType { get; set; } = "ND001";

        internal void StartSuiteMethod(SuiteMethod suiteMethod)
        {
            try
            {
                var id = suiteMethod.Outcome.Id;
                var parentId = suiteMethod.Outcome.ParentId;
                var name = suiteMethod.Outcome.Title;

                _currentTests.TryAdd(id, suiteMethod);

                var startTestRequest = new StartTestItemRequest
                {
                    StartTime = DateTime.UtcNow,
                    Name = name,
                    Type = _itemTypes[suiteMethod.MethodType],
                    TestCaseId = suiteMethod.Outcome.Id.ToString(),
                    CodeReference = suiteMethod.Outcome.FullMethodName
                };

                startTestRequest.Attributes = new List<ItemAttribute>
                {
                    GetAttribute(MachineAttribute, Environment.MachineName)
                };

                var testVal = _suitesFlow[parentId].StartChildTestReporter(startTestRequest);
                _testFlowIds[id] = testVal;
            }
            catch (Exception exception)
            {
                ULogging.Logger.Instance.Log(
                    ULogging.LogLevel.Warning,
                    Prefix + BaseMessage + Environment.NewLine + exception);
            }
        }

        internal void FinishSuiteMethod(SuiteMethod suiteMethod)
        {
            try
            {
                var id = suiteMethod.Outcome.Id;
                var result = suiteMethod.Outcome.Result;

                _currentTests.TryRemove(id, out var res);

                if (!_testFlowIds.ContainsKey(id))
                {
                    return;
                }

                // adding categories to test
                var attributes = new List<ItemAttribute>
                {
                    GetAttribute(AuthorAttribute, suiteMethod.Outcome.Author),
                    GetAttribute(MachineAttribute, Environment.MachineName)
                };

                if (suiteMethod.MethodType.Equals(SuiteMethodType.Test))
                {
                    foreach (var category in (suiteMethod as Test).Categories)
                    {
                        attributes.Add(GetAttribute(CategoryAttribute, category));
                    }
                }

                // adding description to test
                var description =
                    suiteMethod.Outcome.Result == UTesting.Status.Failed ?
                    suiteMethod.Outcome.Exception.Message :
                    string.Empty;

                // adding failure items
                if (suiteMethod.Outcome.Result == UTesting.Status.Failed)
                {
                    var text = suiteMethod.Outcome.Exception.Message + Environment.NewLine + 
                        suiteMethod.Outcome.Exception.StackTrace;

                    AddLog(id, LogLevel.Error, text);

                    if (!string.IsNullOrEmpty(suiteMethod.Outcome.Output))
                    {
                        byte[] outputBytes = Encoding.ASCII.GetBytes(suiteMethod.Outcome.Output);
                        AddAttachment(id, LogLevel.Error, string.Empty, "Execution log", "text/plain", outputBytes);
                    }

                    if (suiteMethod.Outcome.Attachments.Any())
                    {
                        suiteMethod.Outcome.Attachments
                            .ForEach(a =>
                            AddAttachment(id, LogLevel.Error, string.Empty, a.Name, a.MimeType, a.GetBytes()));
                    }
                }

                var finishTestRequest = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Description = description,
                    Attributes = attributes,
                    Status = _statusMap[result]
                };

                // adding issue to finish test if failed test has a defect
                if (suiteMethod.Outcome.Result == UTesting.Status.Failed && suiteMethod.Outcome.Defect != null)
                {
                    finishTestRequest.Issue = new Issue
                    {
                        Type = suiteMethod.Outcome.Defect.DefectType,
                        Comment = suiteMethod.Outcome.Defect.Comment
                    };
                }

                // finishing test
                _testFlowIds[id].Finish(finishTestRequest);
                _testFlowIds.Remove(id);
            }
            catch (Exception exception)
            {
                ULogging.Logger.Instance.Log(
                    ULogging.LogLevel.Warning,
                    Prefix + BaseMessage + Environment.NewLine + exception);
            }
        }

        internal void SkipSuiteMethod(SuiteMethod suiteMethod)
        {
            try
            {
                var id = suiteMethod.Outcome.Id;
                var parentId = suiteMethod.Outcome.ParentId;
                var name = suiteMethod.Outcome.Title;
                var result = suiteMethod.Outcome.Result;

                var startTestRequest = new StartTestItemRequest
                {
                    StartTime = DateTime.UtcNow,
                    Name = name,
                    Type = _itemTypes[suiteMethod.MethodType]
                };

                startTestRequest.Attributes = new List<ItemAttribute>
                {
                    GetAttribute(AuthorAttribute, suiteMethod.Outcome.Author),
                    GetAttribute(MachineAttribute, Environment.MachineName)
                };

                if (suiteMethod.MethodType.Equals(SuiteMethodType.Test))
                {
                    foreach (var category in (suiteMethod as Test).Categories)
                    {
                        startTestRequest.Attributes.Add(GetAttribute(CategoryAttribute, category));
                    }
                }

                var testVal = _suitesFlow[parentId].StartChildTestReporter(startTestRequest);
                _testFlowIds[id] = testVal;

                var finishTestRequest = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = _statusMap[result],
                    Issue = new Issue
                    {
                        Type = SkippedTestDefectType,
                        Comment = "The test is skipped, check if dependent test or BeforeSuite failed"
                    }
                };

                // finishing test
                _testFlowIds[id].Finish(finishTestRequest);
            }
            catch (Exception exception)
            {
                ULogging.Logger.Instance.Log(
                    ULogging.LogLevel.Warning,
                    Prefix + BaseMessage + Environment.NewLine + exception);
            }
        }

        private ItemAttribute GetAttribute(string value)
        {
            if (value.Contains(":"))
            {
                var pair = value.Split(':');
                return GetAttribute(pair[0], pair[1]);
            }
            else
            {
                return new ItemAttribute
                {
                    Value = CheckForEmptyAttribute(value)
                };
            }
        }

        private ItemAttribute GetAttribute(string key, string value) =>
            new ItemAttribute
            {
                Key = key,
                Value = CheckForEmptyAttribute(value)
            };

        private string CheckForEmptyAttribute(string value) =>
            string.IsNullOrEmpty(value.Trim()) ?
            "#err" :
            value;
    }
}