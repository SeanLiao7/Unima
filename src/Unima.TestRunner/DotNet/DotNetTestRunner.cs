﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Medallion.Shell;
using Medallion.Shell.Streams;
using Unima.Core.Execution.Report.Trx;
using Unima.Core.Execution.Result;
using Unima.Core.Execution.Runners;

namespace Unima.TestRunner.DotNet
{
    public class DotNetTestRunner : ITestRunner
    {
        private readonly string _resultId;
        private readonly string _dotNetPath;
        private readonly TimeSpan _maxTime;

        public DotNetTestRunner(string dotNetPath, TimeSpan maxTime)
        {
            _dotNetPath = dotNetPath;
            _maxTime = maxTime;
            _resultId = Guid.NewGuid().ToString();
        }

        public Task<TestSuiteResult> RunTestsAsync(string dllPath)
        {
            var directoryPath = Path.GetDirectoryName(dllPath);

            return Task.Run(() =>
            {
                using (var command = Command.Run(
                     GetDotNetExe(),
                    new[] { "vstest", dllPath, $"--logger:trx;LogFileName={_resultId}", $"--ResultsDirectory:{directoryPath}" },
                    o =>
                    {
                        o.StartInfo(si =>
                        {
                            si.CreateNoWindow = true;
                            si.UseShellExecute = false;
                            si.RedirectStandardError = true;
                            si.RedirectStandardInput = true;
                            si.RedirectStandardOutput = true;
                        });
                        o.DisposeOnExit();
                    }))
                {
                    var success = ReadToEnd(command.StandardError, out var error);

                    if (!success)
                    {
                        command.Kill();
                    }

                    if (!success || (!command.Result.Success && !error.ToLower().Contains("test run failed")))
                    {
                        return TestSuiteResult.Error(error, TimeSpan.Zero);
                    }

                    return CreateResult(Path.GetFileNameWithoutExtension(dllPath), directoryPath);
                }
            });
        }

        private TestSuiteResult CreateResult(string name, string directoryPath)
        {
            var serializer = new XmlSerializer(typeof(TestRunType));
            var resultPath = Directory.GetFiles(directoryPath).First(f => f.Contains(_resultId));

            using (var fileStream = new FileStream(resultPath, FileMode.Open))
            {
                var testRun = (TestRunType)serializer.Deserialize(fileStream);
                var time = (TestRunTypeTimes)testRun.Items[0];

                var results = testRun.Items[2] as ResultsType;
                var tests = results.Items.Select(i => i as UnitTestResultType);

                var testDefinitions = testRun.Items[3] as TestDefinitionType;
                var testDefinitionItems = testDefinitions.Items.Select(i => i as UnitTestType);

                var testResults = new List<TestResult>();
                foreach (var unitTestResultType in tests)
                {
                    testResults.Add(new TestResult
                    {
                        FullName = $"{testDefinitionItems.First(t => t.id == unitTestResultType.testId).TestMethod.className}.{unitTestResultType.testName}",
                        IsSuccess = unitTestResultType.outcome.Equals("passed", StringComparison.InvariantCultureIgnoreCase) || unitTestResultType.outcome.Equals("NotExecuted", StringComparison.InvariantCultureIgnoreCase),
                        Name = unitTestResultType.testName,
                        InnerText = TryGetMessage(unitTestResultType)
                    });
                }

                return new TestSuiteResult
                {
                    ExecutionTime = DateTime.Parse(time.finish) - DateTime.Parse(time.start),
                    IsSuccess = testResults.All(t => t.IsSuccess),
                    Name = name,
                    TestResults = testResults
                };
            }
        }

        private string TryGetMessage(UnitTestResultType unitTestResultType)
        {
            if (unitTestResultType.Items == null || !unitTestResultType.Items.Any())
            {
                return string.Empty;
            }

            var item = unitTestResultType.Items.First() as OutputType;
            var message = item?.ErrorInfo?.Message as XmlNode[];

            if (message == null || !message.Any())
            {
                return string.Empty;
            }

            return message[0].Value ?? string.Empty;
        }

        private bool ReadToEnd(ProcessStreamReader processStream, out string message)
        {
            var readStreamTask = Task.Run(() => processStream.ReadToEnd());
            var successful = readStreamTask.Wait(_maxTime);

            message = successful ? readStreamTask.Result : "Error reading from stream";
            return successful;
        }

        private string GetDotNetExe()
        {
            return string.IsNullOrEmpty(_dotNetPath) ? "dotnet.exe" : _dotNetPath;
        }
    }
}
