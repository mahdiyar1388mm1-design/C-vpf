using System.Windows;
namespace NexusControlCenter.Services;
public sealed class NotificationService
{
    public event EventHandler<string>? NotificationRaised;
    public void Show(string message) => Application.Current.Dispatcher.InvokeAsync(() => NotificationRaised?.Invoke(this, message));
}
