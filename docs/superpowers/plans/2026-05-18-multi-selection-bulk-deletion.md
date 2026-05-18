# Multi-Selection Bulk Deletion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a "Selection Mode" in the Device Inventory page to allow bulk deletion of multiple devices in one operation.

**Architecture:** Use a state-driven approach in `InventoryPage.cs` to toggle between single-device and multi-selection modes. The database will be updated with a bulk deletion method to ensure efficient removal.

**Tech Stack:** C#, Avalonia UI, LiteDB.

---

### Task 1: Data Layer - Bulk Deletion

**Files:**
- Modify: `Data/LocalDatabase.cs`

- [ ] **Step 1: Add DeleteDevices method to LocalDatabase**

```csharp
public int DeleteDevices(IEnumerable<string> macAddresses)
{
    var collection = _db.GetCollection<NetworkNode>("devices");
    int deletedCount = 0;
    foreach (var mac in macAddresses)
    {
        var existing = collection.FindOne(x => x.MacAddress == mac);
        if (existing != null) 
        { 
            collection.Delete(existing.Id); 
            deletedCount++;
        }
    }
    Log(LogLevel.Info, "Database", $"Bulk deleted {deletedCount} devices from database.");
    return deletedCount;
}
```

- [ ] **Step 2: Commit**

```bash
git add Data/LocalDatabase.cs
git commit -m "feat(data): add DeleteDevices method for bulk deletion"
```

### Task 2: UI State & Header Components

**Files:**
- Modify: `UI/InventoryPage.cs`

- [ ] **Step 1: Add selection state variables**

```csharp
// Inside InventoryPage class fields
private bool _isSelectionMode;
private readonly HashSet<string> _selectedMacs = new();
private readonly Button _selectModeBtn;
private readonly CheckBox _selectAllCheckbox;
```

- [ ] **Step 2: Initialize header buttons in constructor**

```csharp
// Inside InventoryPage constructor, near listHeader definition
_selectModeBtn = ThemeTokens.TertiaryButton("Select");
_selectModeBtn.Padding = new Thickness(8, 4);
_selectModeBtn.Click += (s, e) => ToggleSelectionMode();

_selectAllCheckbox = new CheckBox { IsVisible = false, Margin = new Thickness(4, 0, 0, 0) };
_selectAllCheckbox.IsCheckedChanged += (s, e) => ToggleSelectAll(_selectAllCheckbox.IsChecked == true);

// Add to listTitleRow
listTitleRow.Children.Add(_selectAllCheckbox);

// Update listHeader grid to include the select button
listHeader.ColumnDefinitions.Insert(1, new ColumnDefinition(GridLength.Auto));
Grid.SetColumn(_selectModeBtn, 1);
listHeader.Children.Add(_selectModeBtn);
```

- [ ] **Step 3: Commit**

```bash
git add UI/InventoryPage.cs
git commit -m "feat(ui): add selection state and header controls to InventoryPage"
```

### Task 3: Bulk Detail View (Right Panel)

**Files:**
- Modify: `UI/InventoryPage.cs`

- [ ] **Step 1: Define _bulkArea and its controls**

```csharp
// Class fields
private readonly Border _bulkArea;
private readonly StackPanel _selectedDeviceList;
private readonly Button _bulkDeleteBtn;
private readonly TextBlock _bulkTitle;
```

- [ ] **Step 2: Initialize bulk area in constructor**

```csharp
// Inside InventoryPage constructor
_bulkTitle = ThemeTokens.Headline("Bulk Actions", 22);
_selectedDeviceList = new StackPanel { Spacing = 4, Margin = new Thickness(0, 10) };
_bulkDeleteBtn = ThemeTokens.DangerButton("🗑 Delete Selected Devices");
_bulkDeleteBtn.Click += OnBulkDeleteClicked;

var cancelBulkBtn = ThemeTokens.SecondaryButton("Cancel Selection");
cancelBulkBtn.Click += (s, e) => ToggleSelectionMode();

var bulkContent = new StackPanel
{
    Padding = new Thickness(20),
    Children =
    {
        _bulkTitle,
        new ScrollViewer { Content = _selectedDeviceList, Height = 300, Margin = new Thickness(0, 10) },
        _bulkDeleteBtn,
        new Panel { Height = 10 },
        cancelBulkBtn
    }
};
_bulkArea = ThemeTokens.Card(bulkContent, ThemeTokens.SurfaceContainerHigh, 24);
_bulkArea.IsVisible = false;

// Add _bulkArea to the rightCol Grid alongside _emptyDetail and _detailArea
rightCol.Children.Add(_bulkArea);
```

- [ ] **Step 3: Commit**

```bash
git add UI/InventoryPage.cs
git commit -m "feat(ui): implement bulk actions panel in InventoryPage"
```

### Task 4: UI Logic & Event Handlers

**Files:**
- Modify: `UI/InventoryPage.cs`

- [ ] **Step 1: Implement ToggleSelectionMode and ToggleSelectAll**

```csharp
private void ToggleSelectionMode()
{
    _isSelectionMode = !_isSelectionMode;
    _selectedMacs.Clear();
    _selectModeBtn.Content = _isSelectionMode ? "Exit" : "Select";
    _selectAllCheckbox.IsVisible = _isSelectionMode;
    _selectAllCheckbox.IsChecked = false;
    
    if (_isSelectionMode)
    {
        _detailArea.IsVisible = false;
        _emptyDetail.IsVisible = false;
        _bulkArea.IsVisible = true;
        UpdateBulkView();
    }
    else
    {
        _bulkArea.IsVisible = false;
        if (_currentNode != null) _detailArea.IsVisible = true;
        else _emptyDetail.IsVisible = true;
    }
    RefreshDeviceList();
}

private void ToggleSelectAll(bool selected)
{
    if (selected)
    {
        foreach (var node in _activeNodes) _selectedMacs.Add(node.MacAddress);
    }
    else
    {
        _selectedMacs.Clear();
    }
    UpdateBulkView();
    RefreshDeviceList();
}
```

- [ ] **Step 2: Implement UpdateBulkView and Bulk Delete logic**

```csharp
private void UpdateBulkView()
{
    _selectedDeviceList.Children.Clear();
    _bulkTitle.Text = $"Selected Devices ({_selectedMacs.Count})";
    _bulkDeleteBtn.Content = $"🗑 Delete {_selectedMacs.Count} Devices";
    _bulkDeleteBtn.IsEnabled = _selectedMacs.Count > 0;

    var selectedNodes = _activeNodes.Where(n => _selectedMacs.Contains(n.MacAddress)).ToList();
    foreach (var node in selectedNodes)
    {
        _selectedDeviceList.Children.Add(new Border {
            Padding = new Thickness(8, 4),
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock { Text = $"{node.DisplayName} ({node.IpAddress})", FontSize = 12 }
        });
    }
}

private void OnBulkDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (_selectedMacs.Count == 0) return;
    _bulkDeleteBtn.Content = "⚠ Confirm Bulk Delete?";
    _bulkDeleteBtn.Background = Brushes.DarkRed;
    _bulkDeleteBtn.Click -= OnBulkDeleteClicked;
    _bulkDeleteBtn.Click += DoActualBulkDelete;
    
    var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
    timer.Tick += (s, ev) => { 
        _bulkDeleteBtn.Content = $"🗑 Delete {_selectedMacs.Count} Devices";
        _bulkDeleteBtn.Background = ThemeTokens.ErrorContainer;
        _bulkDeleteBtn.Click -= DoActualBulkDelete;
        _bulkDeleteBtn.Click += OnBulkDeleteClicked;
        timer.Stop(); 
    };
    timer.Start();
}

private void DoActualBulkDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    var macsToDelete = _selectedMacs.ToList();
    _db.DeleteDevices(macsToDelete);
    foreach (var mac in macsToDelete) _monitor.RemoveDevice(mac);
    _activeNodes.RemoveAll(n => macsToDelete.Contains(n.MacAddress));
    
    ToggleSelectionMode();
    RefreshDeviceList();
}
```

- [ ] **Step 3: Modify BuildDeviceCard to handle Selection Mode**

```csharp
// Inside BuildDeviceCard, after nameText/subText/ipText setup
if (_isSelectionMode)
{
    var cardCheck = new CheckBox { IsChecked = _selectedMacs.Contains(node.MacAddress), Margin = new Thickness(0, 0, 8, 0), IsHitTestVisible = false };
    leftGroup.Children.Insert(0, cardCheck);
}

// Update the PointerPressed logic
card.PointerPressed += (s, e) => {
    if (_isSelectionMode)
    {
        if (_selectedMacs.Contains(node.MacAddress)) _selectedMacs.Remove(node.MacAddress);
        else _selectedMacs.Add(node.MacAddress);
        UpdateBulkView();
        RefreshDeviceList();
    }
    else ShowDevice(node);
};
```

- [ ] **Step 4: Commit**

```bash
git add UI/InventoryPage.cs
git commit -m "feat(ui): wire up bulk selection logic and card checkboxes"
```

### Task 5: Verification & Cleanup

- [ ] **Step 1: Verify multi-selection mode works**
    - Click "Select" button.
    - Checkboxes appear.
    - Right panel switches to Bulk Actions.
- [ ] **Step 2: Verify Select All**
    - Toggle "Select All" checkbox.
    - All devices should be highlighted and listed.
- [ ] **Step 3: Verify Bulk Deletion**
    - Select 2+ devices.
    - Click "Delete Selected".
    - Click confirmation.
    - Devices should be gone from list and database.
- [ ] **Step 4: Commit final changes**

```bash
git add .
git commit -m "feat: complete multi-selection bulk deletion feature"
```
