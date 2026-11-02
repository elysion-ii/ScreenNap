using ScreenNap.Core;

namespace ScreenNap.Tests.TestDoubles;

internal sealed class FakeUserNotifier : IUserNotifier
{
    internal List<string> Messages { get; } = [];

    public void Notify(string message) => Messages.Add(message);
}
