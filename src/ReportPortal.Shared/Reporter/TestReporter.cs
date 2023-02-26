﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Extensibility.ReportEvents.EventArgs;
using ReportPortal.Shared.Internal.Delegating;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter.Statistics;

namespace ReportPortal.Shared.Reporter
{
    public class TestReporter : ITestReporter
    {
        private readonly IClientService _service;
        private readonly IConfiguration _configuration;
        private readonly IRequestExecuter _requestExecuter;
        private readonly IExtensionManager _extensionManager;

        private LogsReporter _logsReporter;

        private readonly ReportEventsSource _reportEventsSource;

        private static ITraceLogger TraceLogger { get; } = TraceLogManager.Instance.GetLogger<TestReporter>();

        private readonly object _lockObj = new object();

        public TestReporter(IClientService service, IConfiguration configuration, ILaunchReporter launchReporter,
                            ITestReporter parentTestReporter, IRequestExecuter requestExecuter,
                            IExtensionManager extensionManager, ReportEventsSource reportEventNotifier)
        {
            _service = service;
            _configuration = configuration;
            _requestExecuter = requestExecuter;
            _extensionManager = extensionManager;
            _reportEventsSource = reportEventNotifier;
            LaunchReporter = launchReporter;
            ParentTestReporter = parentTestReporter;
        }

        private TestInfo _testInfo;
        public ITestReporterInfo Info => _testInfo;

        public ILaunchReporter LaunchReporter { get; }

        public ITestReporter ParentTestReporter { get; }

        public Task StartTask { get; private set; }

        public void Start(StartTestItemRequest request)
        {
            RequestPreprocessor.Preprocess(request);

            if (StartTask != null)
            {
                var exp = new InsufficientExecutionStackException("The test item is already scheduled for starting.");
                TraceLogger.Error(exp.ToString());
                throw exp;
            }

            var parentStartTask = ParentTestReporter?.StartTask ?? LaunchReporter.StartTask;

            StartTask = parentStartTask.ContinueWith(async pt =>
            {
                if (pt.IsFaulted || pt.IsCanceled)
                {
                    var exp = new Exception("Cannot start test item due parent failed to start.", pt.Exception);

                    if (pt.IsCanceled)
                    {
                        exp = new Exception($"Cannot start test item due timeout while starting parent.");
                    }

                    TraceLogger.Error(exp.ToString());
                    throw exp;
                }

                request.LaunchUuid = LaunchReporter.Info.Uuid;
                if (ParentTestReporter == null)
                {
                    if (request.StartTime < LaunchReporter.Info.StartTime)
                    {
                        request.StartTime = LaunchReporter.Info.StartTime;
                    }

                    NotifyStarting(request);

                    var testModel = await _requestExecuter.ExecuteAsync(() => _service.TestItem.StartAsync(request), null, LaunchReporter.StatisticsCounter.StartTestItemStatisticsCounter).ConfigureAwait(false);

                    _testInfo = new TestInfo
                    {
                        Uuid = testModel.Uuid,
                        Name = request.Name,
                        StartTime = request.StartTime
                    };

                    NotifyStarted();
                }
                else
                {
                    if (request.StartTime < ParentTestReporter.Info.StartTime)
                    {
                        request.StartTime = ParentTestReporter.Info.StartTime;
                    }

                    NotifyStarting(request);

                    var testModel = await _requestExecuter.ExecuteAsync(() => _service.TestItem.StartAsync(ParentTestReporter.Info.Uuid, request), null, LaunchReporter.StatisticsCounter.StartTestItemStatisticsCounter).ConfigureAwait(false);

                    _testInfo = new TestInfo
                    {
                        Uuid = testModel.Uuid,
                        Name = request.Name,
                        StartTime = request.StartTime
                    };

                    NotifyStarted();
                }

                _testInfo.StartTime = request.StartTime;
            }, TaskContinuationOptions.PreferFairness).Unwrap();
        }

        public Task FinishTask { get; private set; }

        public void Finish(FinishTestItemRequest request)
        {
            RequestPreprocessor.Preprocess(request);

            TraceLogger.Verbose($"Scheduling request to finish test item in {GetHashCode()} proxy instance");

            if (StartTask == null)
            {
                var exp = new InsufficientExecutionStackException("The test item wasn't scheduled for starting to finish it properly.");
                TraceLogger.Error(exp.ToString());
                throw exp;
            }

            if (FinishTask != null)
            {
                var exp = new InsufficientExecutionStackException("The test item is already scheduled for finishing.");
                TraceLogger.Error(exp.ToString());
                throw exp;
            }

            var dependentTasks = new List<Task>
            {
                StartTask
            };

            if (_logsReporter != null)
            {
                dependentTasks.Add(_logsReporter.ProcessingTask);
            }

            if (ChildTestReporters != null)
            {
                var childTestReporterFinishTasks = ChildTestReporters.Select(tn => tn.FinishTask);
                if (childTestReporterFinishTasks.Contains(null))
                {
                    throw new InsufficientExecutionStackException("Some of child test item(s) are not scheduled to finish yet.");
                }

                dependentTasks.AddRange(childTestReporterFinishTasks);
            }

            FinishTask = Task.Factory.ContinueWhenAll(dependentTasks.ToArray(), async a =>
            {
                try
                {
                    if (StartTask.IsFaulted || StartTask.IsCanceled)
                    {
                        var exp = new Exception("Cannot finish test item due starting item failed.", StartTask.Exception);

                        if (StartTask.IsCanceled)
                        {
                            exp = new Exception($"Cannot finish test item due timeout while starting it.");
                        }

                        TraceLogger.Error(exp.ToString());
                        throw exp;
                    }

                    if (ChildTestReporters != null)
                    {
                        var failedChildTestReporters = ChildTestReporters.Where(ctr => ctr.FinishTask.IsFaulted || ctr.FinishTask.IsCanceled);
                        if (failedChildTestReporters.Any())
                        {
                            var errors = new List<Exception>();
                            foreach (var failedChildTestReporter in failedChildTestReporters)
                            {
                                if (failedChildTestReporter.FinishTask.IsFaulted)
                                {
                                    errors.Add(failedChildTestReporter.FinishTask.Exception);
                                }
                                else if (failedChildTestReporter.FinishTask.IsCanceled)
                                {
                                    errors.Add(new Exception($"Timeout while finishing child test item."));
                                }
                            }

                            var exp = new AggregateException("Cannot finish test item due finishing of child items failed.", errors);
                            TraceLogger.Error(exp.ToString());
                            throw exp;
                        }
                    }

                    _testInfo.FinishTime = request.EndTime;
                    _testInfo.Status = request.Status;

                    if (request.EndTime < Info.StartTime)
                    {
                        request.EndTime = Info.StartTime;
                    }

                    NotifyFinishing(request);

                    await _requestExecuter.ExecuteAsync(() => _service.TestItem.FinishAsync(Info.Uuid, request), null, LaunchReporter.StatisticsCounter.FinishTestItemStatisticsCounter).ConfigureAwait(false);

                    NotifyFinished();
                }
                finally
                {
                    // clean up childs
                    //ChildTestReporters = null;
                }
            }, TaskContinuationOptions.PreferFairness).Unwrap();
        }

        public IList<ITestReporter> ChildTestReporters { get; private set; }

        public ILaunchStatisticsCounter StatisticsCounter => LaunchReporter.StatisticsCounter;

        public ITestReporter StartChildTestReporter(StartTestItemRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            TraceLogger.Verbose($"Scheduling request to start new '{request.Name}' test item in {GetHashCode()} proxy instance");

            var newTestNode = new TestReporter(_service, _configuration, LaunchReporter, this, _requestExecuter, _extensionManager, _reportEventsSource);
            newTestNode.Start(request);

            lock (_lockObj)
            {
                if (ChildTestReporters == null)
                {
                    lock (_lockObj)
                    {
                        ChildTestReporters = new List<ITestReporter>();
                    }
                }
                ChildTestReporters.Add(newTestNode);
            }

            return newTestNode;
        }

        public void Log(CreateLogItemRequest request)
        {
            if (StartTask == null)
            {
                var exp = new InsufficientExecutionStackException("The test item wasn't scheduled for starting to add log messages.");
                TraceLogger.Error(exp.ToString());
                throw (exp);
            }

            if (StartTask.IsFaulted || StartTask.IsCanceled)
            {
                return;
            }

            if (FinishTask == null)
            {
                lock (_lockObj)
                {
                    if (_logsReporter == null)
                    {
                        var logRequestAmender = new TestLogRequestAmender(this);

                        var logsBatchCapacity = _configuration.GetValue<int>(ConfigurationPath.LogsBatchCapacity, 20);

                        _logsReporter = new LogsReporter(this, _service, _configuration, _extensionManager, _requestExecuter, logRequestAmender, _reportEventsSource, logsBatchCapacity);
                    }
                }

                _logsReporter.Log(request);
            }
        }

        public void Sync()
        {
            StartTask?.GetAwaiter().GetResult();

            FinishTask?.GetAwaiter().GetResult();
        }

        private BeforeTestStartingEventArgs NotifyStarting(StartTestItemRequest request)
        {
            var args = new BeforeTestStartingEventArgs(_service, _configuration, request);
            ReportEventsSource.RaiseBeforeTestStarting(_reportEventsSource, this, args);
            return args;
        }

        private AfterTestStartedEventArgs NotifyStarted()
        {
            var args = new AfterTestStartedEventArgs(_service, _configuration);
            ReportEventsSource.RaiseAfterTestStarted(_reportEventsSource, this, args);
            return args;
        }

        private BeforeTestFinishingEventArgs NotifyFinishing(FinishTestItemRequest request)
        {
            var args = new BeforeTestFinishingEventArgs(_service, _configuration, request);
            ReportEventsSource.RaiseBeforeTestFinishing(_reportEventsSource, this, args);
            return args;
        }

        private AfterTestFinishedEventArgs NotifyFinished()
        {
            var args = new AfterTestFinishedEventArgs(_service, _configuration);
            ReportEventsSource.RaiseAfterTestFinished(_reportEventsSource, this, args);
            return args;
        }
    }
}
