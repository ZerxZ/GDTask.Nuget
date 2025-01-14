# GDTask.Nuget
[![GitHub Release](https://img.shields.io/github/v/release/Delsin-Yu/GDTask.Nuget)](https://github.com/Delsin-Yu/GDTask.Nuget/releases/latest)
[![NuGet Version](https://img.shields.io/nuget/v/GDTask)](https://www.nuget.org/packages/GDTask)
![NuGet Downloads](https://img.shields.io/nuget/dt/GDtask)
[![Stars](https://img.shields.io/github/stars/Delsin-Yu/GDTask.Nuget?color=brightgreen)](https://github.com/Delsin-Yu/GDTask.Nuget/stargazers)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/Delsin-Yu/GDTask.Nuget/blob/main/LICENSE)

- Ported and Tested in Godot 4.2 with .Net module.
- This is the Nuget Package version based on code from:
  - **[Atlinx's GDTask addon for Godot](https://github.com/Fractural/GDTask)**
  - **[Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)**

## Abstract

> Clarification: Contents in the abstract section are mostly migrated from the [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)

### Efficient allocation free async/await integration for Godot

- Struct-based `GDTask<T>` and custom AsyncMethodBuilder to achieve zero allocation.
- Provides `awaitable` functionality for certain Engine event functions.
- Runs completely on Godot PlayerLoop so doesn't use threads.
- Highly compatible behavior with Task/ValueTask/IValueTaskSource.

### GDTask Under the hood

- Based on the [task-like custom async method builder feature.](https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md) of C# 7.0, GDTask does not use [Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/using-threads-and-threading), [SynchronizationContext](https://learn.microsoft.com/en-us/dotnet/api/system.threading.synchronizationcontext), or [ExecutionContext](https://learn.microsoft.com/en-us/dotnet/api/system.threading.executioncontext). Instead, it dispatches the asynchronous tasks onto a standalone singleton node [GDTaskPlayerLoopRunner](https://github.com/Delsin-Yu/GDTask.Nuget/blob/main/GDTask/src/Autoload/GDTaskPlayerLoopRunner.cs), which results in better performance, and lower allocation.

## Installation via Nuget

For .Net CLI
```
dotnet add package GDTask
```

For Package Manager Console:
```
NuGet\Install-Package GDTask
```

## Basic API usage

For more detailed usage, see **[Unit Tests](https://github.com/Delsin-Yu/GDTask.Nuget/tree/main/GDTask.Tests/test)**.

```csharp
using GodotTasks.Tasks;

// Use GDTaskVoid if this task is only used with ApiUsage().Forget();
public async GDTask ApiUsage()
{
    // Delay the execution after frame(s).
    await GDTask.DelayFrame(100); 
    
    // Delay the execution after delayTimeSpan.
    await GDTask.Delay(TimeSpan.FromSeconds(10), DelayType.Realtime);

    // Delay the execution until the next Process.
    await GDTask.Yield(PlayerLoopTiming.Process);

    // Delay the execution until the next PhysicsProcess.
    await GDTask.WaitForPhysicsProcess();

    // Creates a task that will complete at the next provided PlayerLoopTiming when the supplied predicate evaluates to true
    await GDTask.WaitUntil(() => Time.GetTimeDictFromSystem()["minute"].AsInt32() % 2 == 0);
    
    // Creates a task that will complete at the next provided PlayerLoopTiming when the provided monitorFunction returns a different value.
    await GDTask.WaitUntilValueChanged(Time.Singleton, timeInstance => timeInstance.GetTimeDictFromSystem()["minute"]);
    
    // Creates an awaitable that asynchronously yields to ThreadPool when awaited.
    await GDTask.SwitchToThreadPool();
    
    /* Threaded work */
    
    // Creates an awaitable that asynchronously yields back to the next Process from the main thread when awaited.
    await GDTask.SwitchToMainThread();
    
    /* Main thread work */
    
    await GDTask.NextFrame();

    // Creates a continuation that executes when the target GDTask completes.
    int taskResult = await GDTask.Delay(10).ContinueWith(() => 5);
    
    GDTask<int> task1 = GDTask.Delay(10).ContinueWith(() => 5);
    GDTask<string> task2 = GDTask.Delay(20).ContinueWith(() => "Task Result");
    GDTask<bool> task3 = GDTask.Delay(30).ContinueWith(() => true);

    // Creates a task that will complete when all of the supplied tasks have completed.
    var (task1Result, task2Result, task3Result) = await GDTask.WhenAll(task1, task2, task3);

    try
    {
        // Creates a GDTask that has completed with the specified exception.
        await GDTask.FromException(new ExpectedException());
    }
    catch (ExpectedException) { }
    
    try
    {
        // Creates a GDTask that has completed due to cancellation with the specified cancellation token.
        await GDTask.FromCanceled(CancellationToken.None);
    }
    catch (TaskCanceledException) { }
    
    try
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(100);
        // Creates a task that never completes, with specified CancellationToken.
        await GDTask.Never(source.Token);
    }
    catch (TaskCanceledException) { }
    
    // Queues the specified work to run on the ThreadPool and returns a GDTask handle for that work.
    await GDTask.RunOnThreadPool(
        () => GD.Print(Environment.CurrentManagedThreadId.ToString()),
        configureAwait: true,
        cancellationToken: CancellationToken.None
    );

    // Create a GDTask that wraps around this task.
    await Task.Delay(5).AsGDTask(useCurrentSynchronizationContext: true);

    // Associate a time out to the current GDTask.
    await GDTask.Never(CancellationToken.None).Timeout(TimeSpan.FromMilliseconds(5));

    var node = new Node();
    ((SceneTree)Engine.GetMainLoop()).Root.CallDeferred(Node.MethodName.AddChild, node);
    
    // Creates a task that will complete when the _EnterTree() is called.
    await node.OnEnterTreeAsync();
    
    // Creates a task that will complete when the _Ready() is called.
    await node.OnReadyAsync();
    
    // Gets an instance of IAsyncProcessHandler for making repeatedly calls on OnProcessAsync().
    var processTrigger = node.GetAsyncProcessTrigger();
    
    // Creates a task that will complete when the next _Process(double) is called.
    await processTrigger.OnProcessAsync();
    await processTrigger.OnProcessAsync();
    await processTrigger.OnProcessAsync();
    await processTrigger.OnProcessAsync();
    
    // Gets an instance of IAsyncPhysicsProcessHandler for making repeatedly calls on OnPhysicsProcessAsync().
    var physicsProcessTrigger = node.GetAsyncPhysicsProcessTrigger();
    await physicsProcessTrigger.OnPhysicsProcessAsync();
    await physicsProcessTrigger.OnPhysicsProcessAsync();
    await physicsProcessTrigger.OnPhysicsProcessAsync();
    await physicsProcessTrigger.OnPhysicsProcessAsync();    
    
    node.QueueFree();
    
    // Creates a task that will complete when the Node is receiving NotificationPredelete.
    await node.OnPredeleteAsync();
}
```

## Task Profiling

> Clarification: Contents in the task profiling section are mostly migrated from the [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)

When calling `TaskTracker.ShowTrackerWindow()` in your code base, the GDTask system will create(or reuse) a `GDTask Tracker` window for inspecting/diagnosing (leaked) `GDTasks`.

![Image](https://github.com/Delsin-Yu/GDTask.Nuget/blob/main/Readme-Images/GDTaskTracker.png)

| Name              | Description                                                                                                                                                                                                              |
|:------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Enable Tracking   | Enable the tracking system for collecting status for future started `GDTasks`, this is on by default when calling `TaskTracker.ShowTrackerWindow()`, you may also alter this value through `TaskTracker.EnableTracking`. | 
| Enable StackTrace | Records and show stack traces for the active `GDTasks`, you may also alter this value through `TaskTracker.EnableStackTrace`.                                                                                            |
| GC Collect        | Invokes `GC.Collect()` manually.                                                                                                                                                                                         | 

> - Do keep in mind this feature is for debugging purposes and it has performance penalties, so stay cautious when calling `TaskTracker.ShowTrackerWindow()` under the production environment.
> - The background status collection system does not start if you have never called `TaskTracker.ShowTrackerWindow()`.
> - Closing an active `GDTask Tracker` window does not stop the background status collection system, remember to toggle off `Enable Tracking` or set `TaskTracker.EnableTracking` to `false` in your code.
> - Godot Games embeds sub-windows by default, you can disable the `Embed Subwindows` option located in `ProjectSettings (Advanced Settings enabled)Display/Window/Subwindows/Embed Subwindows` for them to become Standalone Windows.
> - This window reacts to the `window closing command` (`NotificationWMCloseRequest`) correctly so it closes itself when you click the close button, to relaunch this window simply call `TaskTracker.ShowTrackerWindow()` again.


## Compare with Standard .Net Task API

> Clarification: Contents in the compare section are mostly migrated from the [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask)

Same as the `Standard .Net Task APIs`, `CancellationToken` and `CancellationTokenSource` are widely used by the GDTask APIs as well.<br>
Otherwise, the following table shows the GDTask APIs provided that are meant to replace the usage of standard .Net Task APIs.

| .NET Type                                    | GDTask Type                                                      |
|----------------------------------------------|------------------------------------------------------------------|
| `Task`/`ValueTask`                           | `GDTask`                                                         |
| `Task<T>`/`ValueTask<T>`                     | `GDTask<T>`                                                      |
| `async void`                                 | `async GDTaskVoid`                                               |
| `+= async () => { }`                         | `GDTask.Void`, `GDTask.Action`                                   |
| ---                                          | `GDTaskCompletionSource`                                         |
| `TaskCompletionSource<T>`                    | `GDTaskCompletionSource<T>`/`AutoResetGDTaskCompletionSource<T>` |
| `ManualResetValueTaskSourceCore<T>`          | `GDTaskCompletionSourceCore<T>`                                  |
| `IValueTaskSource`                           | `IGDTaskSource`                                                  |
| `IValueTaskSource<T>`                        | `IGDTaskSource<T>`                                               |
| `ValueTask.IsCompleted`                      | `GDTask.Status.IsCompleted()`                                    |
| `ValueTask<T>.IsCompleted`                   | `GDTask<T>.Status.IsCompleted()`                                 |
| `CancellationToken.Register(UnsafeRegister)` | `CancellationToken.RegisterWithoutCaptureExecutionContext`       |
| `CancellationTokenSource.CancelAfter`        | `CancellationTokenSource.CancelAfterSlim`                        |
| `Task.Delay`                                 | `GDTask.Delay`                                                   |
| `Task.Yield`                                 | `GDTask.Yield`                                                   |
| `Task.Run`                                   | `GDTask.RunOnThreadPool`                                         |
| `Task.WhenAll`                               | `GDTask.WhenAll`                                                 |
| `Task.WhenAny`                               | `GDTask.WhenAny`                                                 |
| `Task.CompletedTask`                         | `GDTask.CompletedTask`                                           |
| `Task.FromException`                         | `GDTask.FromException`                                           |
| `Task.FromResult`                            | `GDTask.FromResult`                                              |
| `Task.FromCanceled`                          | `GDTask.FromCanceled`                                            |
| `Task.ContinueWith`                          | `GDTask.ContinueWith`                                            |
| `TaskScheduler.UnobservedTaskException`      | `GDTaskExceptionHandler.UnobservedTaskException`                 |
