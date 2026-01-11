using System;
using Avalonia.Threading;
using SITLLauncher.Core.Services;

namespace SITLLauncher.Desktop.Services;

/// <summary>
/// Avalonia implementation of IUiDispatcher using Dispatcher.UIThread.
/// </summary>
public class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }
}
