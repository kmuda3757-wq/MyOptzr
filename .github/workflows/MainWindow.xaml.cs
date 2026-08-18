using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCOptimizerApp.Models;
using PCOptimizerApp.Services;

namespace PCOptimizerApp;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TweakCategory> _categories;
    private readonly ObservableCollection<string> _log = new();
    private string _activeCategoryId;
    private int _pendingCount;

    public MainWindow()
    {
        InitializeComponent();

        _categories = TweakCatalog.Build();
        _activeCategoryId = _categories.First().Id;

        LogList.ItemsSource = _log;
        AppendLog("Sistem siap. Pilih kategori lalu nyalakan tweak.");

        BuildSidebar();
        RenderCategory(_activeCategoryId);
    }

    // ---------- Sidebar ---------------------------------------------------

    private void BuildSidebar()
    {
        SidebarPanel.Children.Clear();
        foreach (var cat in _categories)
        {
            var radio = new RadioButton
            {
                Style = (Style)FindResource("SidebarButton"),
                GroupName = "sidebar",
                IsChecked = cat.Id == _activeCategoryId,
                Tag = cat.Id,
                Content = BuildSidebarLabel(cat),
                Margin = new Thickness(0, 0, 0, 2),
            };
            radio.Checked += (_, _) => RenderCategory((string)radio.Tag);
            SidebarPanel.Children.Add(radio);
        }
    }

    private UIElement BuildSidebarLabel(TweakCategory cat)
    {
        var panel = new DockPanel();
        var label = new TextBlock
        {
            Text = cat.Label,
            Foreground = cat.Id == _activeCategoryId
                ? new SolidColorBrush(Color.FromRgb(0x7F, 0xB0, 0xFF))
                : new SolidColorBrush(Color.FromRgb(0x9A, 0xA3, 0xB0)),
        };
        DockPanel.SetDock(label, Dock.Left);
        panel.Children.Add(label);

        int onCount = cat.Items.Count(i => i.IsOn);
        if (onCount > 0)
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x21, 0x29)),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(6, 1, 6, 1),
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = onCount.ToString(),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x82, 0x90)),
                },
            };
            panel.Children.Add(badge);
        }
        return panel;
    }

    // ---------- Tweak list --------------------------------------------------

    private void RenderCategory(string categoryId)
    {
        _activeCategoryId = categoryId;
        var cat = _categories.First(c => c.Id == categoryId);
        CategoryTitle.Text = $"{cat.Label}  ·  {cat.Items.Count(i => i.IsOn)}/{cat.Items.Count} aktif";

        var stack = new StackPanel();
        foreach (var item in cat.Items)
            stack.Children.Add(BuildTweakRow(item));

        TweakList.ItemsSource = null;
        TweakList.Items.Clear();
        TweakList.ItemsSource = new[] { stack };
    }

    private UIElement BuildTweakRow(TweakItem item)
    {
        var row = new Grid { Margin = new Thickness(4, 10, 4, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var toggle = new CheckBox
        {
            Style = (Style)FindResource("ToggleSwitch"),
            IsChecked = item.IsOn,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 12, 0),
        };
        toggle.Click += (_, _) => OnToggleClicked(item, toggle);
        Grid.SetColumn(toggle, 0);
        row.Children.Add(toggle);

        var textPanel = new StackPanel();
        var headerPanel = new WrapPanel();
        headerPanel.Children.Add(new TextBlock
        {
            Text = item.Name,
            FontSize = 13.5,
            FontWeight = FontWeights.Medium,
            Foreground = new SolidColorBrush(Color.FromRgb(0xED, 0xEF, 0xF2)),
            Margin = new Thickness(0, 0, 7, 0),
        });
        foreach (var tag in item.Tags)
        {
            headerPanel.Children.Add(new Border
            {
                Style = (Style)FindResource("TagBadge"),
                Child = new TextBlock { Text = tag, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x7F, 0xB0, 0xFF)) },
            });
        }
        if (item.Risky)
        {
            headerPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x1A, 0x1A)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x8C)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 1, 6, 1),
                Child = new TextBlock { Text = "Risiko", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x8C)) },
            });
        }
        textPanel.Children.Add(headerPanel);
        textPanel.Children.Add(new TextBlock
        {
            Text = item.Description,
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x74, 0x84)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0),
        });
        Grid.SetColumn(textPanel, 1);
        row.Children.Add(textPanel);

        return row;
    }

    // ---------- Toggle / apply logic ----------------------------------------

    private void OnToggleClicked(TweakItem item, CheckBox toggle)
    {
        bool next = toggle.IsChecked == true;

        if (next && item.Risky)
        {
            var confirm = MessageBox.Show(
                $"\"{item.Name}\" ditandai berisiko dan bisa memengaruhi stabilitas sistem.\n\nLanjutkan?",
                "Konfirmasi tweak berisiko", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                toggle.IsChecked = false;
                return;
            }
        }

        try
        {
            item.Apply(next);
            item.IsOn = next;
            _pendingCount++;
            PendingText.Text = $"{_pendingCount} pending";
            AppendLog($"{(next ? "ON " : "OFF")}  {item.Name}");
        }
        catch (Exception ex)
        {
            toggle.IsChecked = !next; // revert visual state
            AppendLog($"GAGAL  {item.Name} — {ex.Message}");
            MessageBox.Show(
                $"Tidak bisa menerapkan \"{item.Name}\".\n\n{ex.Message}\n\nPastikan aplikasi dijalankan sebagai Administrator.",
                "Gagal menerapkan tweak", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        RenderCategory(_activeCategoryId);
        BuildSidebar();
    }

    private void ApplyAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingCount == 0)
        {
            AppendLog("Tidak ada perubahan tertunda.");
            return;
        }
        AppendLog($"{_pendingCount} perubahan sudah diterapkan langsung saat toggle diklik.");
        AppendLog("Beberapa tweak butuh restart untuk aktif penuh.");
        _pendingCount = 0;
        PendingText.Text = "0 pending";
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "PC akan restart dalam 10 detik. Simpan pekerjaan Anda sekarang.\n\nLanjutkan?",
            "Konfirmasi Restart", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SystemActions.RestartComputer(10);
            AppendLog("Restart dijadwalkan dalam 10 detik. Ketik shutdown /a untuk membatalkan.");
        }
        catch (Exception ex)
        {
            AppendLog($"GAGAL restart — {ex.Message}");
        }
    }

    private void AppendLog(string text)
    {
        _log.Add($"[{DateTime.Now:HH:mm:ss}] {text}");
        Dispatcher.InvokeAsync(() => LogScroll.ScrollToEnd());
    }
}
