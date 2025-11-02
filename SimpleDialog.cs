using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

public class SimpleDialog : Window
{
    public SimpleDialog(string message, string title = "Quiz")
    {
        Title = title;
        Width = 350;
        Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var textBlock = new TextBlock
        {
            Text = message,
            Margin = new Thickness(16),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        okButton.Click += (_, __) => Close();

        Content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                textBlock,
                okButton
            }
        };
    }

    public static async Task Show(Window owner, string message, string title = "Quiz")
    {
        var dialog = new SimpleDialog(message, title);
        await dialog.ShowDialog(owner);
    }
}