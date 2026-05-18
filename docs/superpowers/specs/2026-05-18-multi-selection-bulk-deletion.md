# Design Spec: Multi-Selection for Bulk Device Deletion

**Date:** 2026-05-18
**Topic:** Adding multi-selection capabilities to the Device Inventory page to allow bulk deletion of devices.

## 1. Overview
The current "Device Inventory" page allows users to interact with only one device at a time. To delete multiple devices, the user must select each one individually and confirm deletion. This spec outlines a new "Selection Mode" that enables picking multiple devices from the list and deleting them all in a single batch action.

## 2. User Interface Changes

### 2.1 Left Panel (Device List)
- **Select Toggle:** A new "Select" button will be added to the list header (near the "Saved Devices" title).
- **Selection Mode UI:** When active:
    - Each device card will show a checkbox on its left side.
    - Clicking anywhere on the card will toggle the checkbox.
    - The standard "single-select" highlight will be replaced by a persistent "checked" state highlight.
- **Select All:** A "Select All" checkbox/button will appear in the header during selection mode.

### 2.2 Right Panel (Bulk Detail View)
- **Contextual Switch:** When selection mode is active, the individual device detail view is hidden.
- **Selection Summary:** A new view will display:
    - A count of selected devices.
    - A list of selected device names and IP addresses (for verification).
    - A "Delete [N] Selected" button (Danger style).
    - A "Cancel" button to exit selection mode and return to normal view.

### 2.3 Visual Consistency
- Checkboxes and list styling will match the `ThemeTokens` used throughout the application.
- The bulk delete button will follow the existing confirmation pattern (Click -> "⚠ Confirm Delete?").

## 3. Implementation Details

### 3.1 State Management (`InventoryPage.cs`)
- `bool _isSelectionMode`: Flag to toggle the UI state.
- `HashSet<string> _selectedMacs`: Set of MAC addresses identifying the currently selected devices.

### 3.2 UI Logic Updates
- **`BuildDeviceCard`**: Update to conditionally render a checkbox and change the `PointerPressed` behavior.
- **`RefreshDeviceList`**: Ensure the list reflects the selection state and checkboxes.
- **Header Actions**: Methods to handle "Enter Selection Mode," "Toggle All," and "Exit Selection Mode."

### 3.3 Data Layer (`LocalDatabase.cs`)
- **New Method:** `DeleteDevices(IEnumerable<string> macs)`
    - Iterates through the provided MAC addresses.
    - Completely removes each corresponding document from the `devices` collection in LiteDB.
    - Ensures all metadata associated with the device (registration info, history, etc.) is cleaned up if applicable.

## 4. Success Criteria
- Users can enter/exit selection mode without losing their current place in the inventory.
- Multiple devices can be selected and deleted in one go.
- Deleted devices are removed from the database, the active `_activeNodes` list, and the `ConnectivityMonitor`.
- The UI refreshes correctly after bulk deletion.
