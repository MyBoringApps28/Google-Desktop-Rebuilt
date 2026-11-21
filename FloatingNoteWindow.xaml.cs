using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GoogleDesktopRemake;

public partial class FloatingNoteWindow : Window
{
    bool _deleted;
    bool _returned;
    public event Action<string>? ReturnedToSidebar;
    public event Action? Deleted;

    public FloatingNoteWindow(string text)
    {
        InitializeComponent();
        NoteBox.Text = text;
        Closed += (_, _) => { if (!_deleted && !_returned) ReturnedToSidebar?.Invoke(NoteBox.Text); };
    }

    public void BeginPinDrag() => Dispatcher.BeginInvoke(() => { DragMove(); TryReturnToSidebar(); }, DispatcherPriority.Input);

    void Delete_Click(object s, RoutedEventArgs e)
    {
        _deleted = true;
        Deleted?.Invoke();
        Close();
    }

    void Pin_MouseLeftButtonDown(object s, MouseButtonEventArgs e) { DragMove(); TryReturnToSidebar(); }

    void TryReturnToSidebar()
    {
        if (Owner == null) return;
        double x = Left + Width / 2, y = Top + Height / 2;
        if (x < Owner.Left || x > Owner.Left + Owner.Width || y < Owner.Top || y > Owner.Top + Owner.Height) return;
        _returned = true;
        ReturnedToSidebar?.Invoke(NoteBox.Text);
        Close();
    }
}
