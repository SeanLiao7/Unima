﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Anotar.Log4Net;
using Cama.Core.Execution.Result;
using NUnit;
using NUnit.Engine;
using NUnit.Engine.Services;
using ITestRunner = Cama.Core.Execution.Runners.ITestRunner;
using TestSuiteResult = Cama.Core.Execution.Result.TestSuiteResult;

namespace Cama.TestRunner.NUnit
{
    public class NUnitTestRunner : ITestRunner
    {
        public Task<TestSuiteResult> RunTestsAsync(string dllPath, TimeSpan maxTime)
        {
            return Task.Run(() =>
            {
                var package = new TestPackage(dllPath);

                using (var engine = new TestEngine())
                {
                    engine.Services.Add(new SettingsService(false));
                    engine.Services.Add(new ExtensionService());
                    engine.Services.Add(new DriverService());
                    engine.Services.Add(new InProcessTestRunnerFactory());
                    engine.Services.Add(new ProjectService()); // +
                    engine.Services.Add(new RuntimeFrameworkService()); // +
                    engine.Services.Add(new TestFilterService()); // +
                    engine.Services.Add(new DomainManager()); // -
                    engine.Services.Add(new ResultService());
                    engine.Services.ServiceManager.StartServices();

                    using (global::NUnit.Engine.ITestRunner runner = engine.GetRunner(package))
                    {
                        try
                        {
                            var filter = TestFilter.Empty;
                            var runComplete = new ManualResetEvent(false);
                            var listener = new NUnitTestRunFinishedTestEventListener(runComplete);

                            var testRun = runner.RunAsync(listener, filter);
                            runComplete.WaitOne(maxTime);

                            if (!listener.TestRunFinished)
                            {
                                LogTo.Info("Test canceled. The mutation probably created an infinite loop.");
                                TryStopRunner(runner);
                                return new TestSuiteResult("TIMEOUT", new List<TestResult>(), "NULL", maxTime);
                            }

                            var result = TryGetTestResult(testRun);

                            if (result == null)
                            {
                                return new TestSuiteResult("ERROR", new List<TestResult>(), "NULL", TimeSpan.Zero);
                            }

                            if (result.InnerText.Contains(
                                "System.IO.FileLoadException : Could not load file or assembly "))
                            {
                                LogTo.Error(
                                    $"Problem with loading file or assembly formation. Make sure that we have all required dependencies: \"{result.InnerText}\"");
                            }

                            runner.Unload();
                            var parsedResults = CreateResult(result);

                            if (!parsedResults.TestResults.Any())
                            {
                                LogTo.Info("Didn't run any test. Please check the inner xml: ");
                                LogTo.Info(result.InnerXml);
                                LogTo.Info(result.InnerText);
                            }

                            return parsedResults;
                        }
                        catch (Exception ex)
                        {
                            LogTo.ErrorException("Uknown exception when running nunit", ex);
                            TryStopRunner(runner);
                        }
                    }

                    return new TestSuiteResult("ERROR", new List<TestResult>(), "NULL", TimeSpan.Zero);
                }
            });
        }

        private TestSuiteResult CreateResult(XmlNode result)
        {
            var nUnitTestCaseResultMaker = new NUnitTestCaseResultMaker();
            if (result.Name != "test-run")
            {
                throw new InvalidOperationException("Expected <test-run> as top-level element but was <" + result.Name + ">");
            }

            var name = result.GetAttribute("name");
            var testCaseResults = nUnitTestCaseResultMaker.CreateTestCaseResult(result);
            var duration = double.Parse(result.GetAttribute("duration"), CultureInfo.InvariantCulture);

            return new TestSuiteResult(name, testCaseResults, result.InnerXml, TimeSpan.FromSeconds(duration));
        }

        private XmlNode TryGetTestResult(ITestRun testRun)
        {
            // Really hacky but sometimes it fail..
            for (int n = 0; n < 10; n++)
            {
                try
                {
                    return testRun.Result;
                }
                catch (InvalidOperationException)
                {
                    Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            throw new Exception("Could not get test results");
        }

        private void TryStopRunner(global::NUnit.Engine.ITestRunner testRunner)
        {
            // Really hacky but sometimes it fail..
            for (int n = 0; n < 3; n++)
            {
                try
                {
                    testRunner.StopRun(true);
                    testRunner.Unload();

                    LogTo.Info("Stopped testrunner");
                }
                catch (Exception ex)
                {
                    LogTo.WarnException("Could not stop testrunner, trying again", ex);
                    Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            LogTo.Error("Failed to stop testrunner");
        }
    }
}
