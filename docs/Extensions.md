# Extension

Sometimes it would be great to have ability to explore what payload is sent to ReportPortal server and update it or add extra one based on your own conditions. `ReportPortal.Shared` package provides you this ability by implementing `IReportEventsObserver` interface.

## How to use own observers?

To be able to observe events, next conditions should be met:
1. Implement `IReportEventsObserver` interface
2. Name of an assembly with implementation from previous step should contain `ReportPortal`
3. The assembly should be in the same directory with `ReportPortal.Shared.dll`

## Examples

#### 1. Issue linking

Let's imagine that you have implemented an `IssueAttribute` class which stores information about an issue in some Bug Tracking System. It would be great to automatically assign this issue to test in ReportPortal launch.

Next code snippet shows how it can be done:

```cs
public class ReportPortalEventsObserver : IReportEventsObserver
{
    public void Initialize(IReportEventsSource reportEventsSource)
    {
        reportEventsSource.OnBeforeTestFinishing += ReportEventsSource_OnBeforeTestFinishing;
    }

    private void ReportEventsSource_OnBeforeTestFinishing(ITestReporter testReporter, BeforeTestFinishingEventArgs args)
    {
        var methodInfo = TestExecutionContext.CurrentContext.CurrentTest.Method;

        var issueAttribute = methodInfo.GetCustomAttributes<IssueAttribute>(false).SingleOrDefault();

        if (issueAttribute != null)
        {
            args.FinishTestItemRequest.Issue = new Issue
            {
                AutoAnalyzed = false,
                Type = WellKnownIssueType.ProductBug,
                ExternalSystemIssues = new List<ExternalSystemIssue>
                {
                    new ExternalSystemIssue
                    {
                        BtsProject = issueAttribute.Project,
                        BtsUrl = issueAttribute.ProjectUrl,
                        TicketId = issueAttribute.Id,
                        Url = issueAttribute.IssueUrl,
                    }
                }
            };
        }
    }
}
```

#### 2. Adding dynamic information

Let's imagine that you have to add some dynamic information to ReportPortal launch. _Build number_, for instance. 

Next code snippet shows how it can be done:

```cs
public class ReportPortalEventsObserver : IReportEventsObserver
{
    public void Initialize(IReportEventsSource reportEventsSource)
    {
        reportEventsSource.OnBeforeLaunchStarting += ReportEventsSource_OnBeforeLaunchStarting;
    }

    private void ReportEventsSource_OnBeforeLaunchStarting(ILaunchReporter launchReporter, BeforeLaunchStartingEventArgs args)
    {
        args.StartLaunchRequest.Attributes = args.StartLaunchRequest.Attributes ?? new List<ItemAttribute>();

        args.StartLaunchRequest.Attributes.Add(new ItemAttribute
        {
            Key = nameof(Configuration.BuildNumber),
            Value = Configuration.BuildNumber
        });
    }
}
```

### What can be observed?
`IReportEventsObserver` interface allows to observe next events:

- <span style="font-family: 'Courier New', monospace">Before launch starting</span>
- <span style="font-family: 'Courier New', monospace">Launch initializing</span>
- <span style="font-family: 'Courier New', monospace">After launch starting</span>
- <span style="font-family: 'Courier New', monospace">Before test starting</span>
- <span style="font-family: 'Courier New', monospace">After test started</span>
- <span style="font-family: 'Courier New', monospace">Before logs sending</span>
- <span style="font-family: 'Courier New', monospace">After logs sent</span>
- <span style="font-family: 'Courier New', monospace">Before test finishing</span>
- <span style="font-family: 'Courier New', monospace">After test finished</span>
- <span style="font-family: 'Courier New', monospace">Before launch finished</span>
- <span style="font-family: 'Courier New', monospace">After launch finished</span>
