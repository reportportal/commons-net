﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Internal.Logging;

namespace ReportPortal.Shared.Reporter
{
    public class TestReporter : ITestReporter
    {
        private readonly Service _service;

        private static ITraceLogger TraceLogger { get; } = TraceLogManager.GetLogger<TestReporter>();

        public TestReporter(Service service, ILaunchReporter launchReporter, ITestReporter parentTestReporter, StartTestItemRequest startTestItemRequest)
        {
            _service = service;
            LaunchReporter = launchReporter;
            ParentTestReporter = parentTestReporter;

            if (StartTask != null)
            {
                var message = "The test item is already scheduled for starting.";
                TraceLogger.Error(message);
                throw new InsufficientExecutionStackException(message);
            }

            var parentStartTask = ParentTestReporter?.StartTask ?? LaunchReporter.StartTask;

            StartTask = parentStartTask.ContinueWith(async pt =>
            {
                if (pt.IsFaulted)
                {
                    throw pt.Exception;
                }

                startTestItemRequest.LaunchId = LaunchReporter.LaunchInfo.Id;
                if (ParentTestReporter == null)
                {
                    if (startTestItemRequest.StartTime < LaunchReporter.LaunchInfo.StartTime)
                    {
                        startTestItemRequest.StartTime = LaunchReporter.LaunchInfo.StartTime;
                    }

                    var id = (await _service.StartTestItemAsync(startTestItemRequest)).Id;

                    TestInfo = new TestItem
                    {
                        Id = id
                    };
                }
                else
                {
                    if (startTestItemRequest.StartTime < ParentTestReporter.TestInfo.StartTime)
                    {
                        startTestItemRequest.StartTime = ParentTestReporter.TestInfo.StartTime;
                    }

                    var id = (await _service.StartTestItemAsync(ParentTestReporter.TestInfo.Id, startTestItemRequest)).Id;

                    TestInfo = new TestItem
                    {
                        Id = id
                    };
                }

                TestInfo.StartTime = startTestItemRequest.StartTime;
            }).Unwrap();

            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public TestItem TestInfo { get; private set; }

        public ILaunchReporter LaunchReporter { get; }

        public ITestReporter ParentTestReporter { get; }

        public Task StartTask { get; private set; }

        public Task FinishTask { get; private set; }

        public void Finish(FinishTestItemRequest request)
        {
            TraceLogger.Verbose("Scheduling request to finish new test item");

            if (StartTask == null)
            {
                var message = "The test item wasn't scheduled for starting to finish it properly.";
                TraceLogger.Error(message);
                throw new InsufficientExecutionStackException(message);
            }

            if (FinishTask != null)
            {
                var message = "The test item is already scheduled for finishing.";
                TraceLogger.Error(message);
                throw new InsufficientExecutionStackException(message);
            }

            var dependentTasks = new List<Task>();
            dependentTasks.Add(StartTask);
            if (_additionalTasks != null)
            {
                dependentTasks.AddRange(_additionalTasks);
            }
            if (ChildTestReporters != null)
            {
                dependentTasks.AddRange(ChildTestReporters.Select(tn => tn.FinishTask));
            }

            FinishTask = Task.Factory.ContinueWhenAll(dependentTasks.ToArray(), async a =>
            {
                try
                {
                    if (StartTask.IsFaulted)
                    {
                        var message = "Cannot finish test item due starting item failed.";
                        TraceLogger.Error(message);
                        throw new Exception(message, StartTask.Exception);
                    }

                    if (ChildTestReporters?.Any(ctr => ctr.FinishTask.IsFaulted) == true)
                    {
                        var message = "Cannot finish test item due finishing of child items failed.";
                        TraceLogger.Error(message);
                        throw new AggregateException(message, ChildTestReporters.Where(ctr => ctr.FinishTask.IsFaulted).Select(ctr => ctr.FinishTask.Exception).ToArray());
                    }

                    TestInfo.EndTime = request.EndTime;
                    TestInfo.Status = request.Status;

                    if (request.EndTime < TestInfo.StartTime)
                    {
                        request.EndTime = TestInfo.StartTime;
                    }

                    await _service.FinishTestItemAsync(TestInfo.Id, request);
                }
                finally
                {
                    // clean up childs
                    ChildTestReporters = null;

                    // clean up addition tasks
                    _additionalTasks = null;
                }
            }).Unwrap();
        }

        private ConcurrentBag<Task> _additionalTasks;

        public ConcurrentBag<ITestReporter> ChildTestReporters { get; private set; }

        public ITestReporter StartChildTestReporter(StartTestItemRequest request)
        {
            TraceLogger.Verbose("Scheduling request to start new test item");

            var newTestNode = new TestReporter(_service, LaunchReporter, this, request);

            if (ChildTestReporters == null)
            {
                ChildTestReporters = new ConcurrentBag<ITestReporter>();
            }
            ChildTestReporters.Add(newTestNode);

            (LaunchReporter as LaunchReporter).LastTestNode = newTestNode;

            return newTestNode;
        }

        public void Update(UpdateTestItemRequest request)
        {
            if (FinishTask == null || !FinishTask.IsCompleted)
            {
                if (_additionalTasks == null)
                {
                    _additionalTasks = new ConcurrentBag<Task>();
                }
                _additionalTasks.Add(StartTask.ContinueWith(async a =>
                {
                    await _service.UpdateTestItemAsync(TestInfo.Id, request);
                }).Unwrap());
            }
        }

        public void Log(AddLogItemRequest request)
        {
            if (StartTask == null)
            {
                var message = "The test item wasn't scheduled for starting to add log messages.";
                TraceLogger.Error(message);
                throw new InsufficientExecutionStackException(message);
            }

            if (StartTask.IsFaulted || StartTask.IsCanceled)
            {
                return;
            }

            if (FinishTask == null || !FinishTask.IsCompleted)
            {
                var parentTask = _additionalTasks?.Last() ?? StartTask;

                var task = parentTask.ContinueWith(async pt =>
                {
                    if (!StartTask.IsFaulted)
                    {
                        if (request.Time < TestInfo.StartTime)
                        {
                            request.Time = TestInfo.StartTime;
                        }

                        request.TestItemId = TestInfo.Id;

                        foreach (var formatter in Bridge.LogFormatterExtensions)
                        {
                            formatter.FormatLog(ref request);
                        }

                        await _service.AddLogItemAsync(request);
                    }
                }).Unwrap();

                if (_additionalTasks == null)
                {
                    _additionalTasks = new ConcurrentBag<Task>();
                }

                _additionalTasks.Add(task);
            }
        }

        // TODO: need remove (used by specflow only)
        public int ThreadId { get; set; }
    }

}
