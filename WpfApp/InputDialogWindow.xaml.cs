using System.Windows;
using System.Windows.Controls;

namespace WpfApp;

public partial class InputDialogWindow : Window
{
    public string InputText
    {
        get => InputTextBox.Text;
        set => InputTextBox.Text = value;
    }

    public InputDialogWindow(string title)
    {
        Title = title;
        Width = 400;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
        Background = System.Windows.Media.Brushes.White;

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var textBlock = new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = System.Windows.Media.Brushes.DarkGray,
            FontFamily = new System.Windows.Media.FontFamily("Times New Roman")
        };
        Grid.SetRow(textBlock, 0);
        grid.Children.Add(textBlock);

        InputTextBox = new TextBox
        {
            Height = 30,
            Margin = new Thickness(0, 0, 0, 15),
            FontFamily = new System.Windows.Media.FontFamily("Times New Roman"),
            FontSize = 14
        };
        Grid.SetRow(InputTextBox, 1);
        grid.Children.Add(InputTextBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var cancelButton = new Button
        {
            Content = "Отмена",
            Width = 100,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0),
            Background = System.Windows.Media.Brushes.LightGray,
            Foreground = System.Windows.Media.Brushes.DarkGray,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontFamily = new System.Windows.Media.FontFamily("Times New Roman")
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

        var okButton = new Button
        {
            Content = "OK",
            Width = 100,
            Height = 30,
            Background = System.Windows.Media.Brushes.LawnGreen,
            Foreground = System.Windows.Media.Brushes.DarkGray,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontFamily = new System.Windows.Media.FontFamily("Times New Roman")
        };
        okButton.Click += (s, e) => { DialogResult = true; Close(); };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);

        var buttonBorder = new Border { Child = buttonPanel };
        Grid.SetRow(buttonBorder, 2);
        grid.Children.Add(buttonBorder);

        Content = grid;

        InputTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        };
    }

    private readonly TextBox InputTextBox;
}
