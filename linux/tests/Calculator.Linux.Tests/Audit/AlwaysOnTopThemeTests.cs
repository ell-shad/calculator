// AUDIT — regression tests for the two features the previous release shipped
// as dead code paths:
//   AUD-NEW-3  Always-on-top existed only as resource strings; no Topmost logic,
//              no button. The reference has EnterAlwaysOnTopButton /
//              ExitAlwaysOnTopButton with F8 / Alt+Up / Alt+Down shortcuts
//              (src/Calculator/Views/TitleBar.xaml + Resources.resw @ d125246a).
//   AUD-NEW-4  No theme selection or persistence. The reference persists
//              SelectedAppThemeKey = Light/Dark/Default via ThemeHelper and
//              applies it live.
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Calculator.Linux.Common;
using Calculator.Linux.Services;
using Xunit;

namespace Calculator.Linux.Tests.Audit;

public sealed class AlwaysOnTopThemeTests : IDisposable
{
    private readonly MainWindow _window;
    private readonly string _dir;

    public AlwaysOnTopThemeTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "aud-aot-" + Guid.NewGuid().ToString("N"));
        _window = new MainWindow(new LinuxSettings(_dir)) { Width = 1100, Height = 900 };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // ---- AUD-NEW-3 -----------------------------------------------------------

    [AvaloniaFact]
    public void AlwaysOnTopToggle_Exists_And_Drives_Topmost()
    {
        var toggle = Assert.IsType<ToggleButton>(
            _window.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "AlwaysOnTopToggle"));

        Assert.False(_window.Topmost);
        Assert.True(!toggle.IsChecked);

        // Mouse-equivalent activation: set IsChecked and raise Click like the
        // real toggle would.
        toggle.IsChecked = true;
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(_window.Topmost);

        toggle.IsChecked = false;
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.False(_window.Topmost);
    }

    [AvaloniaFact]
    public void ProgrammaticApi_Syncs_Toggle_State()
    {
        _window.IsAlwaysOnTop = true;
        Dispatcher.UIThread.RunJobs();

        var toggle = _window.GetLogicalDescendants().OfType<ToggleButton>()
            .First(c => c.Name == "AlwaysOnTopToggle");
        Assert.True(toggle.IsChecked);

        _window.ToggleAlwaysOnTop();
        Assert.False(_window.Topmost);
        Assert.False(toggle.IsChecked);
    }

    [AvaloniaFact]
    public void Shortcuts_F8_AltUp_AltDown_Match_Reference()
    {
        Send(Key.F8, RawInputModifiers.None);
        Assert.True(_window.Topmost);

        Send(Key.Down, RawInputModifiers.Alt);
        Assert.False(_window.Topmost);

        Send(Key.Up, RawInputModifiers.Alt);
        Assert.True(_window.Topmost);

        Send(Key.Down, RawInputModifiers.Alt);
        Assert.False(_window.Topmost);
    }

    private void Send(Key key, RawInputModifiers mods)
    {
        _window.Focus();
        _window.KeyPress(key, mods, PhysicalKey.None, key.ToString());
        _window.KeyRelease(key, mods, PhysicalKey.None, key.ToString());
        Dispatcher.UIThread.RunJobs();
    }

    // ---- AUD-NEW-4 -----------------------------------------------------------

    [AvaloniaFact]
    public void ThemeSelector_Defaults_To_Persisted_Value()
    {
        var combo = Assert.IsType<ComboBox>(
            _window.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "ThemeSelector"));

        // Fresh store -> Default (follow system), same default as the reference.
        var item = Assert.IsType<ComboBoxItem>(combo.SelectedItem);
        Assert.Equal("Default", item.Tag);
    }

    [AvaloniaFact]
    public void SetTheme_Persists_Applies_And_Restores()
    {
        Application app = Application.Current!;
        try
        {
            _window.SetTheme("Dark");
            Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
            Assert.Equal("Dark", MainWindow.ReadPersistedTheme(new LinuxSettings(_dir)));

            // Live switch back to light without restart.
            _window.SetTheme("Light");
            Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);

            // Invalid values normalize to Default (never persist garbage).
            _window.SetTheme("Holographic");
            Assert.Equal("Default", MainWindow.ReadPersistedTheme(new LinuxSettings(_dir)));
            Assert.Equal(ThemeVariant.Default, app.RequestedThemeVariant);
        }
        finally
        {
            app.RequestedThemeVariant = ThemeVariant.Default;
        }

        // A second window over the same store restores the last choice.
        var w2 = new MainWindow(new LinuxSettings(_dir));
        try
        {
            var combo = w2.GetLogicalDescendants().OfType<ComboBox>().First(c => c.Name == "ThemeSelector");
            var item = Assert.IsType<ComboBoxItem>(combo.SelectedItem);
            Assert.Equal("Default", item.Tag);
        }
        finally
        {
            w2.Close();
        }
    }

    [AvaloniaFact]
    public void ThemeSelector_Selection_Changes_App_Theme()
    {
        Application app = Application.Current!;
        try
        {
            var combo = _window.GetLogicalDescendants().OfType<ComboBox>().First(c => c.Name == "ThemeSelector");
            ComboBoxItem dark = combo.Items.OfType<ComboBoxItem>().First(i => (string?)i.Tag == "Dark");

            combo.SelectedItem = dark; // user picks from the dropdown
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
            Assert.Equal("Dark", MainWindow.ReadPersistedTheme(new LinuxSettings(_dir)));
        }
        finally
        {
            app.RequestedThemeVariant = ThemeVariant.Default;
        }
    }

    [AvaloniaFact]
    public void LastUsedMode_Persists_And_Restores()
    {
        _window.PersistMode(ViewMode.Programmer);
        Assert.Equal(ViewMode.Programmer, MainWindow.ReadPersistedMode(new LinuxSettings(_dir)));

        var w2 = new MainWindow(new LinuxSettings(_dir));
        try
        {
            w2.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("Programmer", w2.Title);
        }
        finally
        {
            w2.Close();
        }
    }
}
