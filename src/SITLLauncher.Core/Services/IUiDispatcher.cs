using System;

namespace SITLLauncher.Core.Services;

/// <summary>
/// Abstraction for dispatching actions to the UI thread.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Posts an action to be executed on the UI thread.
    /// </summary>
    void Post(Action action);
}
