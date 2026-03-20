# GauntletUI System Reference

Research findings from decompiling Bannerlord's GauntletUI framework DLLs.
Goal: determine whether an AI agent can programmatically "read" game UI state
(like a DOM) instead of relying on screenshots.

---

## 1. Architecture Overview

GauntletUI is a retained-mode UI framework with a clear separation between
**Widgets** (visual tree), **ViewModels** (data), and **Screens/Layers**
(lifecycle and stacking). The relationship maps roughly to:

```
ScreenManager (static, singleton)
  -> ScreenBase  (stack of screens; TopScreen is the active one)
       -> ScreenLayer / GauntletLayer  (ordered layers within a screen)
            -> UIContext  (owns the EventManager + root widget)
                 -> Widget tree  (the actual visual elements)
            -> GauntletMovieIdentifier[]  (loaded "movies" = UI templates)
                 -> IGauntletMovie  (binds a Widget tree to a ViewModel)
                      -> RootWidget   (Widget)
                      -> DataSource   (ViewModel)
```

### Key assemblies

| Assembly | Role |
|---|---|
| `TaleWorlds.ScreenSystem.dll` | `ScreenManager`, `ScreenBase`, `ScreenLayer` |
| `TaleWorlds.Engine.GauntletUI.dll` | `GauntletLayer` (concrete ScreenLayer that hosts Gauntlet UI) |
| `TaleWorlds.GauntletUI.dll` | `Widget`, `UIContext`, `EventManager`, all base widget types |
| `TaleWorlds.GauntletUI.Data.dll` | `GauntletMovie`, `GauntletView`, data-binding engine |
| `TaleWorlds.Library.dll` | `ViewModel` base class |
| `TaleWorlds.CampaignSystem.ViewModelCollection.dll` | Concrete campaign VMs (inventory, map, party, etc.) |
| `TaleWorlds.MountAndBlade.ViewModelCollection.dll` | Battle/mission VMs (HUD, orders, scoreboard, etc.) |

---

## 2. Screen & Layer System

### ScreenManager (`TaleWorlds.ScreenSystem.ScreenManager`)

Static class. The global entry point for all UI state.

| Member | Type | Description |
|---|---|---|
| `TopScreen` | `ScreenBase` (static property) | The currently active screen on top of the stack |
| `SortedLayers` | `List<ScreenLayer>` (static property) | All layers across all screens, sorted by render order |
| `FocusedLayer` | `ScreenLayer` (static property) | The layer currently receiving input focus |
| `FirstHitLayer` | `ScreenLayer` (static property) | The topmost layer hit by the mouse this frame |
| `PushScreen()` | method | Push a new screen onto the stack |
| `PopScreen()` | method | Pop the top screen |
| `OnPushScreen` | event | Fires when a screen is pushed |
| `OnPopScreen` | event | Fires when a screen is popped |

### ScreenBase (`TaleWorlds.ScreenSystem.ScreenBase`)

Abstract base for all game screens.

| Member | Type | Description |
|---|---|---|
| `Layers` | `MBReadOnlyList<ScreenLayer>` | All layers in this screen |
| `IsActive` | `bool` | Whether the screen is currently active |
| `FindLayer<T>()` | method | Find a layer by type |
| `FindLayer<T>(name)` | method | Find a layer by type and name |
| `AddLayer()` / `RemoveLayer()` | methods | Manage layers |

### GauntletLayer (`TaleWorlds.Engine.GauntletUI.GauntletLayer`)

Extends `ScreenLayer`. This is the concrete layer that hosts GauntletUI content.

| Member | Type | Description |
|---|---|---|
| `UIContext` | `UIContext` (public property) | The UI context that owns the widget tree for this layer |
| `LoadMovie(movieName, dataSource)` | method | Load a UI template ("movie") bound to a ViewModel; returns `GauntletMovieIdentifier` |
| `ReleaseMovie(identifier)` | method | Unload a movie |
| `GetMovieIdentifier(movieName)` | method | Find a loaded movie by name |
| `_movieIdentifiers` | `MBList<GauntletMovieIdentifier>` (private) | All currently loaded movies in this layer |

**GauntletMovieIdentifier** holds:
- `MovieName` (string) -- the template name (e.g., "Inventory", "MapBar")
- `DataSource` (ViewModel) -- the bound ViewModel instance
- `Movie` (IGauntletMovie) -- the loaded movie with its `RootWidget`

---

## 3. Widget Tree

### Widget (`TaleWorlds.GauntletUI.BaseTypes.Widget`)

The base class for all visual elements. Forms a tree via parent/children.

#### Tree traversal

| Member | Signature | Description |
|---|---|---|
| `ParentWidget` | `Widget` | Parent in the tree |
| `Children` | `List<Widget>` | Direct children |
| `ChildCount` | `int` | Number of children |
| `GetChild(i)` | `Widget` | Get child by index |
| `FindChild(id, includeAllChildren)` | `Widget` | Find descendant by Id |
| `FindChildrenWithId(id, includeAllChildren)` | `List<Widget>` | Find all descendants with a given Id |
| `FindChildrenWithType<T>(includeAllChildren)` | `List<T>` | Find all descendants of a given type |
| `GetFirstInChildrenRecursive(predicate)` | `Widget` | First descendant matching a predicate |
| `GetAllChildrenRecursive(predicate)` | `List<Widget>` | All descendants matching a predicate |
| `GetAllChildrenAndThisRecursive()` | `List<Widget>` | Flat list of this widget + all descendants |
| `ApplyActionToAllChildrenRecursive(action)` | `void` | Walk entire subtree |
| `CheckIsMyChildRecursive(child)` | `bool` | Check ancestry |

#### Identity & visibility

| Member | Type | Description |
|---|---|---|
| `Id` | `string` | Widget identifier (set from XML templates) |
| `IsVisible` | `bool` | Whether the widget is visible (checks `!IsHidden`) |
| `IsHidden` | `bool` | Explicitly hidden |
| `IsRecursivelyVisible()` | `bool` | Walks parents to check full visibility chain |
| `CurrentState` | `string` | Current visual state (e.g. "Default", "Hovered", "Pressed", "Selected", "Disabled") |
| `Tag` | `object` | Arbitrary tag data |

#### Geometry & positioning

| Member | Type | Description |
|---|---|---|
| `GlobalPosition` | `Vector2` | Absolute position on screen |
| `LocalPosition` | `Vector2` | Position relative to parent |
| `Size` | `Vector2` | Current measured size |
| `MeasuredSize` | `Vector2` | Size after layout |
| `Left`, `Top`, `Right`, `Bottom` | `float` | Bounds |
| `MarginLeft/Top/Right/Bottom` | `float` | Margins |
| `SuggestedWidth`, `SuggestedHeight` | `float` | Requested dimensions |

#### State & interaction

| Member | Type | Description |
|---|---|---|
| `IsHovered` | `bool` | Mouse is over this widget |
| `IsPressed` | `bool` | Currently being pressed |
| `IsFocused` | `bool` | Has input focus |
| `IsDisabled` | `bool` | Disabled state |
| `IsEnabled` | `bool` | Inverse of IsDisabled |
| `CanAcceptEvents` | `bool` | Whether the widget receives events |
| `DoNotAcceptEvents` | `bool` | Event blocking flag |
| `Context` | `UIContext` | Reference back to the UIContext |
| `EventManager` | `EventManager` | Reference to the event manager |
| `ConnectedToRoot` | `bool` | Whether this widget is in the live tree |

#### Events

| Member | Signature | Description |
|---|---|---|
| `EventFire` | `event Action<Widget, string, object[]>` | Fires for all widget events (Click, DoubleClick, etc.) |
| `OnVisibilityChanged` | `event Action<Widget>` | Fires when visibility changes |

The `EventFired(eventName, args)` protected method dispatches to all `EventFire` subscribers. ButtonWidget fires `"Click"`, `"DoubleClick"`, and `"AlternateClick"` through this mechanism.

### Key Widget subclasses

| Type | Namespace | Key properties |
|---|---|---|
| `TextWidget` | `BaseTypes` | `Text` (string), `IntText` (int), `FloatText` (float) |
| `RichTextWidget` | `BaseTypes` | `Text` (string, supports markup) |
| `EditableTextWidget` | `BaseTypes` | `Text` (string), `RealText` (string) |
| `ButtonWidget` | `BaseTypes` | `IsSelected` (bool), `ButtonType`, `ClickEventHandlers` |
| `ImageWidget` | `BaseTypes` | Inherits from `BrushWidget` |
| `BrushWidget` | `BaseTypes` | Adds brush/styling support |
| `SliderWidget` | `BaseTypes` | Slider control |
| `ScrollbarWidget` | `BaseTypes` | Scrollbar |
| `DropdownWidget` | `BaseTypes` | Dropdown selector |
| `GridWidget` | `BaseTypes` | Grid layout |
| `TabToggleWidget` | `BaseTypes` | Tab switching |

---

## 4. ViewModel System

### ViewModel (`TaleWorlds.Library.ViewModel`)

Abstract base class. Uses MVVM data binding pattern.

| Member | Signature | Description |
|---|---|---|
| `GetPropertyValue(name)` | `object` | Read any property by name (reflection-based) |
| `SetPropertyValue(name, value)` | `void` | Write any property by name |
| `GetPropertyType(name)` | `Type` | Get the type of a named property |
| `GetViewModelAtPath(path)` | `object` | Navigate the VM tree via binding paths |
| `ExecuteCommand(commandName, params)` | `void` | Invoke a command method on the VM |
| `RefreshValues()` | `void` (virtual) | Re-push all property values to the UI |
| `OnPropertyChanged(propertyName)` | `void` | Notify UI of property change |
| `PropertyChanged` | `event` | Standard INotifyPropertyChanged event |
| `PropertyChangedWithValue` | `event` | Typed change notification |

Concrete ViewModels expose all their data as public properties. For example,
`SPInventoryVM` exposes:
- `CharacterHelmSlot`, `CharacterTorsoSlot`, etc. (equipped items)
- `CharacterList` (SelectorVM for party member selection)
- `CurrentCharacterHeadArmor`, `CurrentCharacterBodyArmor`, etc. (armor values)
- `CostText`, `CancelLbl` (UI text)
- `CompanionExists` (bool flags)
- `ActiveFilterIndex` (current tab)

### Data Binding (GauntletView)

`GauntletView` (`TaleWorlds.GauntletUI.Data`) connects Widgets to ViewModels.

| Member | Type | Description |
|---|---|---|
| `ViewModelPath` | `BindingPath` | The path in the ViewModel tree this view binds to |
| `ViewModelPathString` | `string` | String representation of the path |
| `DisplayName` | `string` | Display name for debugging |
| `Parent` | `GauntletView` | Parent in the view hierarchy |
| `GauntletMovie` | `GauntletMovie` | Reference to the owning movie |
| `BindData(property, path)` | method | Bind a widget property to a VM path |
| `RefreshBindingWithChildren()` | method | Refresh all bindings recursively |

### GauntletMovie (`TaleWorlds.GauntletUI.Data.GauntletMovie`)

Represents a loaded UI template with its widget tree and ViewModel binding.

| Member | Type | Description |
|---|---|---|
| `RootWidget` | `Widget` | Root of the widget tree for this movie |
| `RootView` | `GauntletView` | Root of the view binding tree |
| `ViewModel` | `IViewModel` | The bound ViewModel |
| `MovieName` | `string` | Template name |
| `FindViewOf(widget)` | `GauntletView` | Find the GauntletView associated with a widget |

---

## 5. EventManager (`TaleWorlds.GauntletUI.EventManager`)

Manages input state and widget interaction within a UIContext.

| Member | Type | Description |
|---|---|---|
| `Root` | `Widget` | Root widget of the context |
| `HoveredWidget` | `Widget` | Widget currently under the mouse |
| `FocusedWidget` | `Widget` | Widget with input focus |
| `MouseOveredWidgets` | `List<Widget>` | All widgets under mouse (z-ordered) |
| `DraggedWidget` | `Widget` | Widget being dragged |
| `LatestMouseDownWidget` | `Widget` | Last widget that received mouse down |
| `LatestMouseUpWidget` | `Widget` | Last widget that received mouse up |
| `MousePosition` | `Vector2` | Current mouse position |
| `PageSize` | `Vector2` | Page/viewport size |
| `UIEventManager` | `EventManager` (static) | Global event manager |
| `OnDragStarted` / `OnDragEnded` | events | Drag lifecycle |
| `OnFocusedWidgetChanged` | event | Focus change notification |

---

## 6. How to Access Current UI State Programmatically

### Step 1: Get the active screen and its layers

```csharp
ScreenBase topScreen = ScreenManager.TopScreen;
// topScreen.GetType().Name tells you which screen (e.g., "MapScreen", "InventoryScreen")

foreach (ScreenLayer layer in topScreen.Layers)
{
    if (layer is GauntletLayer gauntletLayer)
    {
        // Each GauntletLayer has a UIContext with a widget tree
        UIContext ctx = gauntletLayer.UIContext;
        Widget root = ctx.Root;
    }
}
```

### Step 2: Access loaded movies (templates) and their ViewModels

`GauntletLayer._movieIdentifiers` is private, but can be accessed via reflection
or by using `GetMovieIdentifier(movieName)` if you know the movie name.

Each `GauntletMovieIdentifier` gives you:
- `.MovieName` -- the template (e.g., "MapBar", "Inventory")
- `.DataSource` -- the ViewModel (cast to concrete type to read properties)
- `.Movie.RootWidget` -- the root of that movie's widget tree

### Step 3: Traverse the widget tree (like a DOM)

```csharp
void WalkWidgetTree(Widget widget, int depth = 0)
{
    string indent = new string(' ', depth * 2);
    string text = "";

    if (widget is TextWidget tw) text = tw.Text;
    else if (widget is RichTextWidget rtw) text = rtw.Text;
    else if (widget is EditableTextWidget etw) text = etw.Text;

    // Log: type, id, visibility, position, size, text
    // widget.GetType().Name, widget.Id, widget.IsVisible,
    // widget.GlobalPosition, widget.Size, text

    foreach (Widget child in widget.Children)
    {
        WalkWidgetTree(child, depth + 1);
    }
}
```

### Step 4: Read ViewModel data directly

```csharp
// Example: reading inventory state
if (dataSource is SPInventoryVM inventoryVM)
{
    string helmName = inventoryVM.CharacterHelmSlot?.ToString();
    float headArmor = inventoryVM.CurrentCharacterHeadArmor;
    int activeFilter = inventoryVM.ActiveFilterIndex;
}

// Or generically via reflection:
object value = viewModel.GetPropertyValue("SomePropertyName");
```

### Step 5: Simulate interaction

```csharp
// For button clicks, the HandleClick() method is protected virtual.
// The easiest approach is to call ExecuteCommand on the ViewModel:
viewModel.ExecuteCommand("ExecuteDone", new object[0]);

// Or subscribe to widget events:
widget.EventFire += (w, eventName, args) => {
    // eventName: "Click", "DoubleClick", "AlternateClick", etc.
};

// For ButtonWidget specifically, ClickEventHandlers is a public List:
if (widget is ButtonWidget button)
{
    button.ClickEventHandlers.Add(w => { /* handle */ });
}
```

---

## 7. Feasibility Assessment for AI Bridge Integration

### What CAN be done (High confidence)

1. **Read the active screen type** -- `ScreenManager.TopScreen.GetType().Name` tells you
   exactly what screen is showing (MapScreen, InventoryScreen, etc.).

2. **Enumerate all layers and movies** -- Walk `TopScreen.Layers`, filter for
   `GauntletLayer`, access their movies to know exactly which UI panels are loaded.

3. **Traverse the full widget tree** -- The `Widget` class exposes `Children`,
   `FindChild`, `GetAllChildrenRecursive`, etc. This gives you a complete DOM-like
   traversal of every visible UI element.

4. **Read all displayed text** -- `TextWidget.Text`, `RichTextWidget.Text`, and
   `EditableTextWidget.Text` expose the actual displayed strings. Walking the tree
   and collecting all text widgets gives you everything the player can read.

5. **Read widget state** -- `IsVisible`, `IsHovered`, `IsPressed`, `IsSelected`,
   `IsDisabled`, `CurrentState` all expose interaction state.

6. **Read widget geometry** -- `GlobalPosition`, `Size`, `MeasuredSize` give you
   exact pixel positions and bounds of every element.

7. **Read ViewModel data directly** -- `ViewModel.GetPropertyValue(name)` or casting
   to the concrete VM type gives access to all game data behind the UI, often in
   a richer form than what's displayed (e.g., exact numeric values, item objects, etc.).

8. **Listen to events** -- `Widget.EventFire`, `ScreenManager.OnPushScreen/OnPopScreen`,
   `EventManager.OnFocusedWidgetChanged` allow monitoring UI changes in real time.

9. **Listen to ViewModel changes** -- `ViewModel.PropertyChanged` and the typed variants
   fire whenever data changes, enabling reactive state tracking.

### What CAN be done with more effort (Medium confidence)

1. **Simulate button clicks** -- `ViewModel.ExecuteCommand()` can invoke command methods.
   `ButtonWidget.ClickEventHandlers` is a public list. Both allow programmatic clicks
   without simulating mouse input.

2. **Set ViewModel properties** -- `ViewModel.SetPropertyValue(name, value)` can modify
   UI-bound state, though side effects depend on the specific VM implementation.

3. **Access private fields** -- `GauntletLayer._movieIdentifiers` is private but can
   be accessed via Harmony or reflection to enumerate all loaded movies without
   knowing their names in advance.

### What would be DIFFICULT

1. **Simulating complex drag-and-drop** -- The drag system involves multiple coordinated
   state changes across `EventManager.DraggedWidget`, `DragHoveredWidget`, etc. and
   is tightly coupled to per-frame mouse input processing.

2. **Text input into EditableTextWidget** -- Would require simulating keyboard events
   through the `HandleInput(IReadOnlyList<int> lastKeysPressed)` path, or using the
   on-screen keyboard callbacks.

### Architecture advantages for AI bridge

- **Structural, not pixel-based**: The widget tree is a true structural representation,
  like the browser DOM. An AI agent can work with semantically meaningful elements
  (buttons, text fields, lists) rather than raw pixels.

- **Two complementary views**: The Widget tree shows what's rendered; the ViewModel
  shows the underlying data. Together they give complete context.

- **Event system**: The `EventFire` event on every Widget plus `ScreenManager` events
  enable building a reactive state tracker without polling.

- **Deterministic IDs**: Widgets have string `Id` values from the XML templates,
  making it possible to locate specific UI elements by stable identifiers.

---

## 8. Concrete Example: Reading What's Currently on Screen

Here is the conceptual approach a bridge mod would use to generate a structured
UI snapshot for an AI agent:

```csharp
public UISnapshot CaptureCurrentState()
{
    var snapshot = new UISnapshot();
    ScreenBase screen = ScreenManager.TopScreen;
    if (screen == null) return snapshot;

    snapshot.ScreenType = screen.GetType().Name;
    snapshot.IsActive = screen.IsActive;

    foreach (ScreenLayer layer in screen.Layers)
    {
        if (layer is GauntletLayer gl && layer.IsActive)
        {
            var layerInfo = new LayerSnapshot {
                Name = layer.Name,
                IsActive = layer.IsActive,
            };

            // Access movies via reflection on _movieIdentifiers
            // or use known movie names per screen type
            var movies = GetMovieIdentifiers(gl); // reflection helper
            foreach (var movie in movies)
            {
                var movieInfo = new MovieSnapshot {
                    MovieName = movie.MovieName,
                    ViewModelType = movie.DataSource?.GetType().Name,
                };

                // Walk the widget tree
                if (movie.Movie?.RootWidget != null)
                {
                    movieInfo.WidgetTree = SerializeWidgetTree(movie.Movie.RootWidget);
                }

                // Serialize key ViewModel properties
                if (movie.DataSource != null)
                {
                    movieInfo.ViewModelData = SerializeViewModel(movie.DataSource);
                }

                layerInfo.Movies.Add(movieInfo);
            }
            snapshot.Layers.Add(layerInfo);
        }
    }
    return snapshot;
}

WidgetNode SerializeWidgetTree(Widget widget)
{
    if (!widget.IsVisible) return null; // skip hidden widgets

    var node = new WidgetNode {
        Type = widget.GetType().Name,
        Id = widget.Id,
        Position = widget.GlobalPosition,
        Size = widget.Size,
        State = widget.CurrentState,
        IsHovered = widget.IsHovered,
        IsPressed = widget.IsPressed,
    };

    // Extract text content
    if (widget is TextWidget tw && !string.IsNullOrEmpty(tw.Text))
        node.Text = tw.Text;
    else if (widget is RichTextWidget rtw && !string.IsNullOrEmpty(rtw.Text))
        node.Text = rtw.Text;

    // Extract button state
    if (widget is ButtonWidget btn)
    {
        node.IsSelected = btn.IsSelected;
        node.IsToggle = btn.IsToggle;
    }

    // Recurse into children
    foreach (Widget child in widget.Children)
    {
        var childNode = SerializeWidgetTree(child);
        if (childNode != null)
            node.Children.Add(childNode);
    }
    return node;
}
```

This would produce a JSON-serializable tree structure that an AI agent can
parse and reason about, much like a web browser's accessibility tree.

---

## 9. Key Type Quick Reference

### Namespaces

| Namespace | Contents |
|---|---|
| `TaleWorlds.ScreenSystem` | `ScreenManager`, `ScreenBase`, `ScreenLayer` |
| `TaleWorlds.Engine.GauntletUI` | `GauntletLayer`, `GauntletMovieIdentifier` |
| `TaleWorlds.GauntletUI` | `UIContext`, `EventManager`, `UIResourceManager` |
| `TaleWorlds.GauntletUI.BaseTypes` | `Widget`, `TextWidget`, `ButtonWidget`, `RichTextWidget`, etc. |
| `TaleWorlds.GauntletUI.Data` | `GauntletMovie`, `GauntletView`, `IGauntletMovie` |
| `TaleWorlds.Library` | `ViewModel`, `IViewModel` |
| `TaleWorlds.CampaignSystem.ViewModelCollection.*` | All campaign screen VMs |
| `TaleWorlds.MountAndBlade.ViewModelCollection.*` | All battle/mission VMs |

### Campaign ViewModel namespaces (52 namespaces, 757 types)

Key areas: `Inventory`, `Party`, `Map`, `Map.MapBar`, `Conversation`,
`Encyclopedia`, `KingdomManagement`, `ClanManagement`, `CharacterDeveloper`,
`GameMenu`, `Quests`, `WeaponCrafting`, `Barter`, `ArmyManagement`

### MountAndBlade ViewModel namespaces (32 namespaces, 284 types)

Key areas: `HUD`, `HUD.Compass`, `HUD.KillFeed`, `Order`, `OrderOfBattle`,
`Scoreboard`, `EscapeMenu`, `GameOptions`, `FaceGenerator`, `Multiplayer`

---

## 10. UIExtenderEx — Runtime UI Modification Framework

**Assembly:** `Bannerlord.UIExtenderEx.dll` (v2.13.2)
**Module:** `Bannerlord.UIExtenderEx` (Community mod, available on [NexusMods #2102](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102))
**Dependency:** `Bannerlord.Harmony` (v2.2.2+)
**Source:** BUTR project (https://github.com/BUTR/Bannerlord.UIExtenderEx)

UIExtenderEx is the standard community framework for modifying Bannerlord's GauntletUI at runtime. It uses Harmony patches under the hood to intercept the UI system, allowing mods to:
1. **Extend ViewModels** with new properties/methods (ViewModel Mixins)
2. **Patch XML prefabs** to add/modify/remove UI elements (Prefab Extensions)

### 10.1 Core API — UIExtender Class

```csharp
// In your SubModule's OnSubModuleLoad:
var extender = UIExtender.Create("YourModuleName");
extender.Register(typeof(YourSubModule).Assembly); // scans for attributed types
extender.Enable();

// On unload:
extender.Deregister();
```

| Method | Description |
|--------|-------------|
| `UIExtender.Create(moduleName)` | Factory method, creates a new UIExtender instance |
| `Register(Assembly)` | Scans assembly for types with `BaseUIExtenderAttribute` subclasses |
| `Register(IEnumerable<Type>)` | Register specific types |
| `Enable()` / `Disable()` | Activate/deactivate all registered extensions |
| `Enable(Type)` / `Disable(Type)` | Enable/disable a specific extension type |
| `Deregister()` | Remove all extensions for this module |
| `GetUIExtenderFor(moduleName)` | Static lookup of a registered UIExtender by module name |

### 10.2 Attributes

Three key attributes drive the system, all in `Bannerlord.UIExtenderEx.Attributes`:

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[ViewModelMixin]` | Class | Marks a class as a ViewModel mixin (extends an existing VM) |
| `[PrefabExtension(movie, xpath)]` | Class | Marks a class as a prefab patch (modifies UI XML) |
| `[DataSourceMethod]` | Method | Marks a method in a mixin as a command callable from UI bindings |

#### `[ViewModelMixin]`

```csharp
[ViewModelMixin]                          // basic - auto-detect refresh
[ViewModelMixin("RefreshValues")]         // specify which VM method triggers OnRefresh
[ViewModelMixin(handleDerived: true)]     // also apply to derived VM types
[ViewModelMixin("RefreshValues", false)]  // both options
```

#### `[PrefabExtension]`

```csharp
[PrefabExtension("Inventory", "descendant::Widget[@Id='RightPanel']")]
// movie = "Inventory" (the XML template name)
// xpath = XPath to the target node in the prefab XML
```

Can be applied multiple times to the same class (`AllowMultiple = true`).

### 10.3 ViewModel Mixins

Extend an existing game ViewModel by inheriting `BaseViewModelMixin<TViewModel>`:

```csharp
[ViewModelMixin("RefreshValues")]
public class InventoryMixin : BaseViewModelMixin<SPInventoryVM>
{
    private string _customLabel;

    public InventoryMixin(SPInventoryVM vm) : base(vm) { }

    // New property — accessible from UI XML bindings
    [DataSourceProperty]
    public string CustomLabel
    {
        get => _customLabel;
        set => SetField(ref _customLabel, value, nameof(CustomLabel));
    }

    // New command — callable from UI XML via Command.ExecuteMyAction
    [DataSourceMethod]
    public void ExecuteMyAction()
    {
        // Access the original VM via this.ViewModel
        var vm = ViewModel;
        // ... do something
    }

    // Called when the target VM's RefreshValues() is called
    public override void OnRefresh()
    {
        CustomLabel = "Updated!";
    }

    public override void OnFinalize() { /* cleanup */ }
}
```

**Key members of `BaseViewModelMixin<T>`:**

| Member | Description |
|--------|-------------|
| `ViewModel` | WeakReference to the target VM (type `T?`) |
| `OnRefresh()` | Virtual, called when the VM's refresh method fires |
| `OnFinalize()` | Virtual, called when the VM is finalized |
| `OnPropertyChanged(name)` | Notify the VM of property changes |
| `OnPropertyChangedWithValue(value, name)` | Typed change notification |
| `GetPrivate<T>(name)` | Read private fields of the target VM via reflection |
| `SetPrivate<T>(name, value)` | Write private fields of the target VM |
| `SetField<T>(ref field, value, name)` | Set field + fire PropertyChanged |

### 10.4 Prefab Extensions (XML Patching)

Two generations of API exist. **Prefabs2** is the current recommended API.

#### Prefabs2 — Insert Patch

Inherit `PrefabExtensionInsertPatch` and provide content via attributed methods/properties:

```csharp
[PrefabExtension("Inventory", "descendant::Widget[@Id='RightPanel']")]
public class MyInsertPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Child; // where to insert

    // Content provider — return XML string
    [PrefabExtensionText]
    public string GetPrefabExtension()
    {
        return "<TextWidget Text='@CustomLabel' />";
    }
}
```

**InsertType enum:**

| Value | Description |
|-------|-------------|
| `Prepend` | Insert before the target node |
| `Append` | Insert after the target node |
| `Child` | Insert as a child of the target node |
| `Replace` | Replace the target node entirely |
| `ReplaceKeepChildren` | Replace but keep original children |
| `Remove` | Remove the target node |

**Content provider attributes** (on methods/properties of the patch class):

| Attribute | Returns | Description |
|-----------|---------|-------------|
| `[PrefabExtensionText]` | `string` | Raw XML text |
| `[PrefabExtensionFileName]` | `string` | Path to an XML file |
| `[PrefabExtensionXmlNode]` | `XmlNode` | An `XmlNode` object |
| `[PrefabExtensionXmlNodes]` | `IEnumerable<XmlNode>` | Multiple XML nodes |
| `[PrefabExtensionXmlDocument]` | `XmlDocument` | A full XML document |

Single-content attributes have `RemoveRootNode` option (strips the root wrapper element).

#### Prefabs2 — Set Attribute Patch

Modify attributes on existing XML nodes:

```csharp
[PrefabExtension("Inventory", "descendant::Widget[@Id='RightPanel']")]
public class MyAttributePatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("IsVisible", "true"),
        new Attribute("SuggestedWidth", "500"),
    };
}
```

#### Legacy Prefabs Namespace (older API, still functional)

| Class | Description |
|-------|-------------|
| `InsertPatch` | Basic XML insertion |
| `PrefabExtensionInsertPatch` | Insert as child of target |
| `PrefabExtensionInsertAsSiblingPatch` | Insert as sibling (with `InsertType.Prepend`/`Append`) |
| `PrefabExtensionReplacePatch` | Replace target node |
| `PrefabExtensionSetAttributePatch` | Modify target attributes |
| `CustomPatch` | Full custom XmlDocument manipulation |
| `ModulePrefabExtensionInsertPatch` | Insert from a module file |
| `EmbedPrefabExtensionInsertPatch` | Insert from embedded resource |

### 10.5 Internals — How It Works

UIExtenderEx uses Harmony to patch three key game systems at static init time:

1. **`ViewModelPatch`** — Patches ViewModel constructors and methods. When a registered VM type is instantiated, the corresponding mixin is created and attached. When the VM's refresh method fires, `OnRefresh()` is called on all mixins. New `[DataSourceProperty]` and `[DataSourceMethod]` members are injected via Harmony so the UI data-binding engine sees them.

2. **`WidgetPrefabPatch`** — Patches the prefab/movie loading pipeline (`WidgetFactory`). When a movie XML is loaded, all registered `PrefabExtension` patches targeting that movie name are applied, modifying the XML DOM before the widget tree is created.

3. **`UIConfigPatch`** / **`BrushFactoryManager`** / **`WidgetFactoryManager`** — Patch brush and widget factories to support custom brushes and widget types from mods.

### 10.6 Relevance for the AI Bridge

**For reading UI (primary bridge use case):**
UIExtenderEx is NOT needed for reading the existing UI — the native GauntletUI APIs (Sections 2-8 above) are sufficient. The bridge mod can traverse widget trees and read ViewModels without UIExtenderEx.

**For modifying UI (potential future use):**
UIExtenderEx would be valuable if the bridge wants to:
- Add an **overlay/HUD panel** showing AI agent status, connection state, or action queue
- Add **custom buttons** to existing screens (e.g., "AI Control" toggle on the campaign map)
- Inject **new ViewModel properties** that expose bridge-specific data to the UI
- Create a **debug panel** showing GABP protocol traffic or tool call history

**Practical recommendation:**
- Phase 1-3: Not needed. Focus on game API tools.
- Phase 4+: Consider using UIExtenderEx to add an AI status indicator to the map HUD — this would require a `[ViewModelMixin]` on `MapInfoVM` or similar, plus a `[PrefabExtension]` on the `MapBar` movie to add a visual indicator.

**If we depend on UIExtenderEx:**
- Add to `SubModule.xml`: `<DependedModule Id="Bannerlord.UIExtenderEx" />`
- Reference `Bannerlord.UIExtenderEx.dll` in the project
- Users would need to install UIExtenderEx (very common — most Bannerlord mods depend on it)
