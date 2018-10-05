using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ReportPortal.Client;
using ReportPortal.Client.Requests;

namespace ReportPortal.Shared
{
    public class LaunchReporter
    {
        private readonly Service _service;

        public LaunchReporter(Service service)
        {
            _service = service;

            TestNodes = new ConcurrentBag<TestReporter>();
        }

        public LaunchReporter(Service service, string launchId, DateTime startTime)
        {
            _service = service;

            LaunchId = launchId;
            StartTime = startTime;

            TestNodes = new ConcurrentBag<TestReporter>();

            StartTask = Task.FromResult(0);
        }

        public string LaunchId;

        public Task StartTask;
        public DateTime StartTime;

        public void Start(StartLaunchRequest request)
        {
            StartTask = Task.Factory.StartNew(async () =>
            {
                LaunchId = (await _service.StartLaunchAsync(request)).Id;
                StartTime = request.StartTime;
            }).Unwrap();
        }

        public Task FinishTask;

        public void Finish(FinishLaunchRequest request, bool force = false)
        {
            Finish(request, force, true);
        }

        public void Finish(FinishLaunchRequest request, bool force, bool finishLaunch)
        {
            var dependentTasks = TestNodes.Select(tn => tn.FinishTask).Concat(new[] { StartTask }).ToArray();

            FinishTask = Task.Factory.ContinueWhenAll(dependentTasks, async _ =>
            {
                if (!force)
                {
                    try
                    {
                        Task.WaitAll(TestNodes.Select(tn => tn.FinishTask).ToArray());
                    }
                    catch (Exception exp)
                    {
                        throw new Exception("Cannot finish launch due inner items failed to finish.", exp);
                    }

                    if (request.EndTime < StartTime)
                    {
                        request.EndTime = StartTime;
                    }
                }

                if (finishLaunch)
                {
                    await _service.FinishLaunchAsync(LaunchId, request, force);
                }

            }).Unwrap();
        }

        public ConcurrentBag<TestReporter> TestNodes { get; set; }

        public TestReporter StartNewTestNode(StartTestItemRequest request)
        {
            var newTestNode = new TestReporter(_service, this, null);
            newTestNode.Start(request);
            TestNodes.Add(newTestNode);

            LastTestNode = newTestNode;

            return newTestNode;
        }

        public TestReporter LastTestNode { get; set; }
    }
}
