using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.GABS;

/// <summary>
/// Dispatches actions to the game's main thread.
/// GABP tool handlers run on background TCP threads, but Bannerlord game API
/// calls must execute on the main thread. Queue work here and it will be
/// processed during OnApplicationTick.
/// </summary>
public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> ExecutionQueue = new();

    /// <summary>
    /// Enqueue a fire-and-forget action on the main thread.
    /// </summary>
    public static void Enqueue(Action action)
    {
        ExecutionQueue.Enqueue(action);
    }

    /// <summary>
    /// Enqueue work on the main thread and return a Task that completes with the result.
    /// Use this from async tool handlers that need to read game state.
    /// </summary>
    public static Task<T> EnqueueAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        ExecutionQueue.Enqueue(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Enqueue a void action on the main thread and return a Task that completes when done.
    /// </summary>
    public static Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        ExecutionQueue.Enqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    private static volatile string? _pendingSaveName;

    /// <summary>
    /// Schedule a save to run on the main thread OUTSIDE the dispatcher queue.
    /// SaveAs blocks the main thread and deadlocks if called inside ProcessQueue.
    /// </summary>
    public static void ScheduleSave(string saveName)
    {
        _pendingSaveName = saveName;
    }

    /// <summary>
    /// Process all queued actions. Called from SubModule.OnApplicationTick.
    /// </summary>
    internal static void ProcessQueue()
    {
        while (ExecutionQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception)
            {
                // Swallow exceptions from queued actions to avoid crashing the game tick.
                // Individual TaskCompletionSource callers will receive their exceptions.
            }
        }
    }

    /// <summary>
    /// Run a pending save AFTER ProcessQueue completes.
    /// Called from SubModule.OnApplicationTick, outside the queue loop.
    /// </summary>
    internal static void ProcessPendingSave()
    {
        var saveName = _pendingSaveName;
        if (saveName != null)
        {
            _pendingSaveName = null;
            try
            {
                Campaign.Current?.SaveHandler.SaveAs(saveName);
            }
            catch (Exception)
            {
                // Save failed — nothing to do since we already returned the response.
            }
        }
    }
}