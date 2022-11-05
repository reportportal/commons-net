# Extensibility

Sometimes it would be great to have ability to explore what payload is sent to ReportPortal server and update it or add extra one based on your own conditions. `ReportPortal.Shared` package provides you this ability by implementing `IReportEventsObserver` interface.

## How to use own observers?

To be able to observe events, next conditions should be met:
1. Implement `IReportEventsObserver` interface
2. Name of an assembly with implementation from previous step should contain `ReportPortal`
3. The assembly should be in the same directory with `ReportPortal.Shared.dll`

## Examples

#### 1. Test names updating

Let's imagine that you use snake case for naming your tests, but would like to improve readability of test names on ReportPortal server by replacing each underscore by empty space.

Next code snippet shows how it can be done:

```cs
public class ReportPortalEventsObserver : IReportEventsObserver
{
    public void Initialize(IReportEventsSource reportEventsSource)
    {
        reportEventsSource.OnBeforeTestStarting += ReportEventsSource_OnBeforeTestStarting;
    }

    private void ReportEventsSource_OnBeforeTestStarting(ITestReporter testReporter, BeforeTestStartingEventArgs args)
    {
        args.StartTestItemRequest.Name = args.StartTestItemRequest.Name.Replace('_', ' ');
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

## What can be observed?
`IReportEventsObserver` interface allows to observe such events as:

- _before/after test starting_
- _before/after test finished_
- _before/after launch starting_
- _before/after launch finished_

Please, have a look the interface for understanding what actions also can be observed.


