using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace NodeRadarPro.UI.Controls.Inventory;

/// <summary>
/// Composite filter bar for Inventory: search box with debounce, filter chips, and device count actions.
/// </summary>
public class InventoryFilterBar : StackPanel
{
    private readonly TextBox _searchBox;
    private readonly TextBlock _deviceCountText;
    private readonly Button _selectModeBtn;
    private readonly CheckBox _selectAllCheckbox;
    private readonly WrapPanel _chipPanel;
    private readonly DispatcherTimer _searchDebounceTimer;
    private string _activeFilter = "all";

    public event Action<string>? FilterChanged;
    public event Action<string>? SearchDebounced;
    public event Action? SelectModeToggled;
    public event Action<bool>? SelectAllToggled;
    public event Action? AddDeviceClicked;

    internal TextBox SearchBox => _searchBox;
    internal TextBlock DeviceCountText => _deviceCountText;
    internal Button SelectModeButton => _selectModeBtn;
    internal CheckBox SelectAllCheckBox => _selectAllCheckbox;
    internal WrapPanel ChipPanel => _chipPanel;
    internal string ActiveFilter => _activeFilter;

    public InventoryFilterBar()
    {
        Spacing = 0;

        // Search Input
        _searchBox = new TextBox
        {
            PlaceholderText = "Search devices...",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = 13,
            FontFamily = ThemeTokens.DefaultFont,
            Foreground = ThemeTokens.OnSurface,
            VerticalAlignment = VerticalAlignment.Center
        };

        var searchIcon = ThemeTokens.VectorIcon(ThemeTokens.SvgSearch, 14, ThemeTokens.OnSurfaceVariant);
        searchIcon.VerticalAlignment = VerticalAlignment.Center;
        searchIcon.Margin = new Thickness(0, 0, 8, 0);

        var clearSearchBtn = new Button
        {
            Content = ThemeTokens.VectorIcon(ThemeTokens.SvgClose, 10, ThemeTokens.OnSurfaceVariant),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            Cursor = ThemeTokens.HandCursor,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };
        clearSearchBtn.Click += (s, e) => _searchBox.Text = "";

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchDebounceTimer.Tick += (s, e) =>
        {
            _searchDebounceTimer.Stop();
            SearchDebounced?.Invoke(_searchBox.Text?.Trim() ?? "");
        };

        _searchBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(TextBox.Text))
            {
                clearSearchBtn.IsVisible = !string.IsNullOrEmpty(_searchBox.Text);
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
        };

        ThemeTokens.SetToolTip(_searchBox, "Filter the device list by IP, MAC, Vendor, or Custom Name.");

        var searchWrap = new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(ThemeTokens.InputRadius),
            Padding = new Thickness(14, 0, 8, 0),
            Margin = new Thickness(12, 12, 12, 8),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(GridLength.Auto)
                },
                Children =
                {
                    searchIcon,
                    _searchBox,
                    clearSearchBtn
                }
            }
        };
        Grid.SetColumn(searchIcon, 0);
        Grid.SetColumn(_searchBox, 1);
        Grid.SetColumn(clearSearchBtn, 2);

        // Filter chips
        _chipPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 0, 12, 6)
        };
        UpdateFilterChips();

        // Header with counts and actions
        _deviceCountText = ThemeTokens.Body("(0)", 14);
        _deviceCountText.Margin = new Thickness(6, 0, 0, 0);

        _selectModeBtn = ThemeTokens.TertiaryButton("Select");
        _selectModeBtn.Padding = new Thickness(8, 4);
        _selectModeBtn.Click += (s, e) => SelectModeToggled?.Invoke();

        _selectAllCheckbox = new CheckBox { IsVisible = false, Margin = new Thickness(4, 0, 0, 0) };
        _selectAllCheckbox.IsCheckedChanged += (s, e) => SelectAllToggled?.Invoke(_selectAllCheckbox.IsChecked == true);

        var listTitleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                ThemeTokens.Headline("Saved Devices", 18),
                _deviceCountText,
                _selectAllCheckbox
            }
        };

        var addBtn = new Button
        {
            Content = "+",
            Background = Brushes.Transparent,
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0)
        };
        addBtn.Click += (s, e) => AddDeviceClicked?.Invoke();
        ThemeTokens.SetToolTip(addBtn, "Manually register a new static device in the database.");

        var listHeader = new Grid { Margin = new Thickness(16, 8, 16, 8) };
        listHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        listHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        listHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        Grid.SetColumn(listTitleRow, 0);
        Grid.SetColumn(_selectModeBtn, 1);
        Grid.SetColumn(addBtn, 2);

        listHeader.Children.Add(listTitleRow);
        listHeader.Children.Add(_selectModeBtn);
        listHeader.Children.Add(addBtn);

        Children.Add(searchWrap);
        Children.Add(_chipPanel);
        Children.Add(listHeader);
    }

    public void SetDeviceCount(int count)
    {
        _deviceCountText.Text = $"({count})";
    }

    public void SetActiveFilter(string filter)
    {
        _activeFilter = filter;
        UpdateFilterChips();
        FilterChanged?.Invoke(_activeFilter);
    }

    public void SetSelectionModeUI(bool isSelectionMode)
    {
        _selectModeBtn.Content = isSelectionMode ? "Exit" : "Select";
        _selectAllCheckbox.IsVisible = isSelectionMode;
        if (!isSelectionMode)
        {
            _selectAllCheckbox.IsChecked = false;
        }
    }

    public void UpdateFilterChips()
    {
        _chipPanel.Children.Clear();
        _chipPanel.Children.Add(MakeFilterChip("All", "all", _activeFilter == "all"));
        _chipPanel.Children.Add(MakeFilterChip("Online", "online", _activeFilter == "online"));
        _chipPanel.Children.Add(MakeFilterChip("Offline", "offline", _activeFilter == "offline"));
        _chipPanel.Children.Add(MakeFilterChip("Critical", "critical", _activeFilter == "critical"));
        _chipPanel.Children.Add(MakeFilterChip("Servers", "servers", _activeFilter == "servers"));
        _chipPanel.Children.Add(MakeFilterChip("IoT", "iot", _activeFilter == "iot"));
    }

    private Border MakeFilterChip(string text, string filterKey, bool active)
    {
        var chip = new Border
        {
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 0, 6, 6),
            Background = active ? ThemeTokens.PrimaryContainer : Brushes.Transparent,
            BorderBrush = active ? Brushes.Transparent : ThemeTokens.GhostBorder30,
            BorderThickness = new Thickness(1),
            Cursor = ThemeTokens.HandCursor,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Foreground = active ? ThemeTokens.OnPrimaryContainer : ThemeTokens.OnSurfaceVariant,
                FontFamily = ThemeTokens.DefaultFont,
                FontWeight = FontWeight.Medium
            }
        };
        chip.PointerPressed += (s, e) => SetActiveFilter(filterKey);
        return chip;
    }
}
