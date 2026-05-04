using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace FactorioModManager.App.Services;

public sealed class DialogService : IDialogService
{
    private readonly Window _owner;
    private static readonly SolidColorBrush SurfaceBrush = Brush("#1A1814");
    private static readonly SolidColorBrush SurfaceRaisedBrush = Brush("#221D18");
    private static readonly SolidColorBrush InputBrush = Brush("#15110D");
    private static readonly SolidColorBrush TextBrush = Brush("#E8DFCF");
    private static readonly SolidColorBrush MutedBrush = Brush("#8A7D65");
    private static readonly SolidColorBrush BorderBrush = Brush("#3A342C");
    private static readonly SolidColorBrush AccentBrush = Brush("#D97A2C");
    private static readonly SolidColorBrush AccentBorderBrush = Brush("#F0A455");
    private static readonly SolidColorBrush DangerBrush = Brush("#2A1510");
    private static readonly SolidColorBrush DangerBorderBrush = Brush("#5A2820");
    private static readonly SolidColorBrush DangerTextBrush = Brush("#E89089");

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await _owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PromptAsync(string title, string message, string? initialValue = null)
    {
        var input = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 360,
            MinHeight = 31,
            Background = InputBrush,
            Foreground = TextBrush,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(9, 5),
            FontSize = 12
        };

        var window = CreateDialogWindow(title);
        var okButton = CreateButton("Save", DialogButtonKind.Primary);
        var cancelButton = CreateButton("Cancel", DialogButtonKind.Secondary);

        okButton.Click += (_, _) => window.Close(input.Text);
        cancelButton.Click += (_, _) => window.Close(null);

        window.Content = CreateDialogContent(
            title,
            message,
            input,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    cancelButton,
                    okButton
                }
            });

        return await window.ShowDialog<string?>(_owner);
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var window = CreateDialogWindow(title);
        var cancelButton = CreateButton("Cancel", DialogButtonKind.Secondary);
        var confirmButton = CreateButton(
            confirmText,
            string.Equals(confirmText, "Delete", StringComparison.OrdinalIgnoreCase)
                ? DialogButtonKind.Danger
                : DialogButtonKind.Primary);

        cancelButton.Click += (_, _) => window.Close(false);
        confirmButton.Click += (_, _) => window.Close(true);

        window.Content = CreateDialogContent(
            title,
            message,
            null,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    cancelButton,
                    confirmButton
                }
            });

        return await window.ShowDialog<bool>(_owner);
    }

    public Task ShowMessageAsync(string title, string message)
    {
        return ShowNoticeAsync(title, message, "OK");
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return ShowNoticeAsync(title, message, "OK");
    }

    private async Task ShowNoticeAsync(string title, string message, string buttonText)
    {
        var window = CreateDialogWindow(title);
        var okButton = CreateButton(buttonText, DialogButtonKind.Primary);

        okButton.Click += (_, _) => window.Close();
        window.Content = CreateDialogContent(
            title,
            message,
            null,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children =
                {
                    okButton
                }
            });

        await window.ShowDialog(_owner);
    }

    private static Window CreateDialogWindow(string title)
    {
        return new Window
        {
            Title = title,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = SurfaceBrush,
            SystemDecorations = SystemDecorations.None
        };
    }

    private static Control CreateDialogContent(string title, string message, Control? body, Control actions)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 14
        };

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextBrush
        });

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 19,
            Foreground = MutedBrush
        });

        if (body is not null)
        {
            panel.Children.Add(body);
        }

        panel.Children.Add(actions);
        return new Border
        {
            Background = SurfaceBrush,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = panel
        };
    }

    private static Button CreateButton(string text, DialogButtonKind kind)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 88,
            MinHeight = 30,
            Padding = new Thickness(11, 5),
            CornerRadius = new CornerRadius(5),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12,
            FontWeight = FontWeight.Medium
        };

        switch (kind)
        {
            case DialogButtonKind.Primary:
                button.Background = AccentBrush;
                button.BorderBrush = AccentBorderBrush;
                button.Foreground = InputBrush;
                button.FontWeight = FontWeight.SemiBold;
                break;
            case DialogButtonKind.Danger:
                button.Background = DangerBrush;
                button.BorderBrush = DangerBorderBrush;
                button.Foreground = DangerTextBrush;
                break;
            default:
                button.Background = SurfaceRaisedBrush;
                button.BorderBrush = BorderBrush;
                button.Foreground = TextBrush;
                break;
        }

        button.BorderThickness = new Thickness(1);
        return button;
    }

    private static SolidColorBrush Brush(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }

    private enum DialogButtonKind
    {
        Secondary,
        Primary,
        Danger
    }
}
