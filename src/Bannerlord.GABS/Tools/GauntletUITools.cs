// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using Lib.GAB.Tools;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.ScreenSystem;

namespace Bannerlord.GABS.Tools;

public partial class GauntletUITools
{
    private static readonly AccessTools.FieldRef<object, IEnumerable>? MovieIdentifiersField = AccessTools2.FieldRefAccess<IEnumerable>(typeof(GauntletLayer), "_movieIdentifiers");

    // Note: HandleClick and EventFired are resolved at call time via GetType().GetMethod()
    // because ButtonWidget/Widget types may not be loaded during static initialization.

    /// <summary>
    /// Layers that contain map decorations (nameplates, trackers, etc.) — skip button enumeration
    /// to avoid thousands of settlement/party nameplate buttons bloating the output.
    /// </summary>
    private static readonly HashSet<string> SkipButtonLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MapNameplateLayer",
        "SceneLayer",
    };

    [Tool("ui/get_screen", Description = "Get the current GauntletUI screen: layers, movies, and clickable buttons. Returns a compact summary. Use this when other tools (menu, inquiry, conversation) return nothing.")]
    public partial Task<object> GetScreen(
        [ToolParameter(Description = "Optional: only return buttons from layers matching this name (case-insensitive substring match). Use to inspect a specific layer in detail.", Required = false)] string? layerFilter)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                var screen = ScreenManager.TopScreen;
                if (screen == null)
                    return new { error = "No active screen" };

                var layers = new List<object>();

                foreach (var layer in screen.Layers)
                {
                    var matchesFilter = layerFilter != null &&
                                        layer.Name != null &&
                                        layer.Name.IndexOf(layerFilter, StringComparison.OrdinalIgnoreCase) >= 0;

                    // Skip noisy map layers unless explicitly requested by filter
                    var skipButtons = !matchesFilter &&
                                      layer.Name != null &&
                                      SkipButtonLayers.Contains(layer.Name);

                    if (layer is GauntletLayer gauntletLayer)
                    {
                        var movies = GetMovies(gauntletLayer);
                        var movieInfos = new List<object>();

                        foreach (var movieId in movies)
                        {
                            if (skipButtons)
                            {
                                movieInfos.Add(new
                                {
                                    /// Movie name identifier
                                    movieName = movieId.MovieName,
                                    /// ViewModel class name bound to this movie
                                    dataSource = movieId.DataSource?.GetType().Name,
                                    /// Number of buttons found (null if skipped)
                                    buttonCount = (int?) null,
                                    /// Array of button objects (null if skipped)
                                    buttons = (List<object>?) null,
                                    /// Note explaining why buttons were skipped
                                    note = "skipped (map decoration layer)",
                                });
                                continue;
                            }

                            var buttons = new List<object>();
                            var rootWidget = movieId.RootWidget;
                            if (rootWidget != null)
                            {
                                CollectButtons(rootWidget, buttons, matchesFilter ? 20 : 8);
                            }

                            movieInfos.Add(new
                            {
                                /// Movie name identifier
                                movieName = movieId.MovieName,
                                /// ViewModel class name bound to this movie
                                dataSource = movieId.DataSource?.GetType().Name,
                                /// Number of buttons found
                                buttonCount = (int?) buttons.Count,
                                /// Array of button objects
                                buttons,
                                /// Note (null when buttons are included)
                                note = (string?) null,
                            });
                        }

                        layers.Add(new
                        {
                            /// Layer name
                            name = layer.Name,
                            /// Whether the layer is active
                            isActive = layer.IsActive,
                            /// Array of movie objects (null for non-Gauntlet layers)
                            movies = movieInfos,
                        });
                    }
                    else
                    {
                        layers.Add(new
                        {
                            /// Layer name
                            name = layer.Name,
                            /// Whether the layer is active
                            isActive = layer.IsActive,
                            /// Array of movie objects (null for non-Gauntlet layers)
                            movies = (object?) null,
                        });
                    }
                }

                return new
                {
                    /// Class name of the active screen (e.g. 'MapScreen', 'GauntletInitialScreen')
                    screenType = screen.GetType().Name,
                    /// Number of layers on the screen
                    layerCount = screen.Layers.Count,
                    /// Array of layer objects with name, isActive, and movies
                    layers,
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to read screen: {ex.Message}" };
            }
        });
    }

    [Tool("ui/click_widget", Description = "Click a UI widget (button) by Id, text content, or index. Use get_screen first to find widgets. Many buttons lack Ids but have text — use their text to find and click them. Pass '__index:N' as widgetId to click the Nth button (0-based). Disabled buttons are blocked by default.")]
    public partial Task<object> ClickWidget(
        [ToolParameter(Description = "The widget Id or text content of a button (e.g. 'Done', 'Continue Campaign'). Use '__index:N' (e.g. '__index:2') to click the Nth button on screen.")] string widgetId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                var screen = ScreenManager.TopScreen;
                if (screen == null)
                    return new { error = "No active screen" };

                var searchText = widgetId?.Trim();
                int buttonIndex = -1;

                // Parse __index:N syntax
                if (searchText != null && searchText.StartsWith("__index:", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(searchText.Substring(8), out var idx))
                        buttonIndex = idx;
                    else
                        return new { error = $"Invalid index format: '{searchText}'. Use '__index:N' where N is 0-based." };
                }

                // Index-based click: find the Nth button on screen
                if (buttonIndex >= 0)
                {
                    int currentIndex = 0;
                    foreach (var layer in screen.Layers)
                    {
                        if (layer is GauntletLayer gauntletLayer)
                        {
                            var movies = GetMovies(gauntletLayer);
                            foreach (var movieId in movies)
                            {
                                var rootWidget = movieId.RootWidget;
                                if (rootWidget != null)
                                {
                                    var buttons = new List<Widget>();
                                    CollectButtonWidgets(rootWidget, buttons, 12);
                                    foreach (var btn in buttons)
                                    {
                                        if (currentIndex == buttonIndex)
                                        {
                                            ClickWidgetCore(btn);
                                            var clickedText = FindTextInChildren(btn, 3) ?? btn.Id ?? $"[index:{buttonIndex}]";
                                            return (object) new
                                            {
                                                /// The Id or text that was searched for
                                                widgetId = clickedText,
                                                /// Type name of the widget that was clicked (e.g. 'ButtonWidget')
                                                widgetType = btn.GetType().Name,
                                                /// Layer name where the widget was found
                                                layer = layer.Name,
                                                /// Movie name where the widget was found
                                                movie = movieId.MovieName,
                                            };
                                        }
                                        currentIndex++;
                                    }
                                }
                            }
                        }
                    }
                    return new { error = $"Button index {buttonIndex} out of range (found {currentIndex} buttons)" };
                }

                if (string.IsNullOrEmpty(searchText))
                    return new { error = "Either widgetId (text/id) or buttonIndex must be provided" };

                Widget? found = null;
                string? foundLayer = null;
                string? foundMovie = null;

                // First pass: find by Id (exact match)
                foreach (var layer in screen.Layers)
                {
                    if (layer is GauntletLayer gauntletLayer)
                    {
                        var movies = GetMovies(gauntletLayer);
                        foreach (var movieId in movies)
                        {
                            var rootWidget = movieId.RootWidget;
                            if (rootWidget != null)
                            {
                                found = FindWidgetById(rootWidget, searchText);
                                if (found != null)
                                {
                                    foundLayer = layer.Name;
                                    foundMovie = movieId.MovieName;
                                    break;
                                }
                            }
                        }
                    }
                    if (found != null) break;
                }

                // Second pass: find button by text content (exact match, case-insensitive)
                if (found == null)
                {
                    foreach (var layer in screen.Layers)
                    {
                        if (layer is GauntletLayer gauntletLayer)
                        {
                            var movies = GetMovies(gauntletLayer);
                            foreach (var movieId in movies)
                            {
                                var rootWidget = movieId.RootWidget;
                                if (rootWidget != null)
                                {
                                    found = FindButtonByText(rootWidget, searchText);
                                    if (found != null)
                                    {
                                        foundLayer = layer.Name;
                                        foundMovie = movieId.MovieName;
                                        break;
                                    }
                                }
                            }
                        }
                        if (found != null) break;
                    }
                }

                // Third pass: find any clickable widget by text (not just ButtonWidget — handles subclasses the type check misses)
                if (found == null)
                {
                    foreach (var layer in screen.Layers)
                    {
                        if (layer is GauntletLayer gauntletLayer)
                        {
                            var movies = GetMovies(gauntletLayer);
                            foreach (var movieId in movies)
                            {
                                var rootWidget = movieId.RootWidget;
                                if (rootWidget != null)
                                {
                                    found = FindClickableWidgetByText(rootWidget, searchText);
                                    if (found != null)
                                    {
                                        foundLayer = layer.Name;
                                        foundMovie = movieId.MovieName;
                                        break;
                                    }
                                }
                            }
                        }
                        if (found != null) break;
                    }
                }

                if (found == null)
                {
                    // Collect available buttons for the error message to help debugging
                    var availableButtons = new List<string>();
                    foreach (var layer in screen.Layers)
                    {
                        if (layer is GauntletLayer gauntletLayer)
                        {
                            var movies = GetMovies(gauntletLayer);
                            foreach (var movieId in movies)
                            {
                                var rootWidget = movieId.RootWidget;
                                if (rootWidget != null)
                                {
                                    var buttons = new List<object>();
                                    CollectButtons(rootWidget, buttons, 12);
                                    foreach (var btn in buttons)
                                    {
                                        if (btn is { } obj)
                                        {
                                            var textProp = obj.GetType().GetProperty("text");
                                            var idProp = obj.GetType().GetProperty("id");
                                            var text = textProp?.GetValue(obj)?.ToString();
                                            var id = idProp?.GetValue(obj)?.ToString();
                                            availableButtons.Add(text ?? id ?? "?");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return new { error = $"Widget with Id or text '{searchText}' not found. Available buttons: [{string.Join(", ", availableButtons)}]" };
                }

                if (!found.IsEnabled)
                {
                    return new { error = $"Widget '{searchText}' is disabled (state: {found.CurrentState}) and cannot be clicked" };
                }

                ClickWidgetCore(found);

                return new
                {
                    /// The Id or text that was searched for
                    widgetId = searchText,
                    /// Type name of the widget that was clicked (e.g. 'ButtonWidget')
                    widgetType = found.GetType().Name,
                    /// Layer name where the widget was found
                    layer = foundLayer,
                    /// Movie name where the widget was found
                    movie = foundMovie,
                };

            }
            catch (Exception ex)
            {
                return new { error = $"Click failed: {ex.Message}" };
            }
        });
    }

    private static void ClickWidgetCore(Widget widget)
    {
        var widgetType = widget.GetType();
        var handleClick = widgetType.GetMethod("HandleClick",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (handleClick != null)
        {
            handleClick.Invoke(widget, null);
        }
        else
        {
            // Fallback: fire EventFired directly
            var eventFired = widgetType.GetMethod("EventFired",
                BindingFlags.Instance | BindingFlags.NonPublic);
            eventFired?.Invoke(widget, ["Click", Array.Empty<object>()]);
        }
    }

    [Tool("ui/call_viewmodel_method", Description = "Call a method on the ViewModel bound to a GauntletUI movie. Useful for triggering actions like 'ExecuteQuitAction' on ScoreboardBaseVM.")]
    public partial Task<object> CallViewModelMethod(
        [ToolParameter(Description = "Name of the layer (from get_screen)")] string layerName,
        [ToolParameter(Description = "Method name to call on the ViewModel (e.g. 'ExecuteQuitAction')")] string methodName)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                var screen = ScreenManager.TopScreen;
                if (screen == null)
                    return new { error = "No active screen" };

                foreach (var layer in screen.Layers)
                {
                    if (layer is GauntletLayer gauntletLayer &&
                        string.Equals(layer.Name, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        var movies = GetMovies(gauntletLayer);
                        foreach (var movieId in movies)
                        {
                            var dataSource = movieId.DataSource;
                            if (dataSource == null) continue;

                            var method = dataSource.GetType().GetMethod(methodName,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (method != null)
                            {
                                method.Invoke(dataSource, null);
                                return new
                                {
                                    /// ViewModel class name the method was called on
                                    viewModel = dataSource.GetType().Name,
                                    /// Method name that was invoked
                                    method = methodName,
                                    /// Layer name
                                    layer = layerName,
                                    /// Movie name
                                    movie = movieId.MovieName,
                                };
                            }
                        }

                        return new { error = $"Method '{methodName}' not found on any ViewModel in layer '{layerName}'" };
                    }
                }

                return new { error = $"Layer '{layerName}' not found" };
            }
            catch (Exception ex)
            {
                return new { error = $"ViewModel call failed: {ex.Message}" };
            }
        });
    }

    [Tool("ui/wait_for_screen", Description = "Wait until the active screen changes to the expected type. Use after clicking buttons that trigger screen transitions (e.g. 'Continue Campaign' → MapScreen, 'Done' on battle results → MapScreen). Pass timeout via games.call_tool timeout parameter.")]
    public partial Task<object> WaitForScreen(
        [ToolParameter(Description = "Expected screen type name (e.g. 'MapScreen', 'GauntletInitialScreen'). Case-insensitive substring match.")] string screenType,
        [ToolParameter(Description = "Optional: also wait for a specific layer to appear (e.g. 'MapNotification'). Case-insensitive substring match.", Required = false)] string? waitForLayer,
        [ToolParameter(Description = "Poll interval in milliseconds (default 500).", Required = false)] int pollIntervalMs = 500)
    {
        if (string.IsNullOrWhiteSpace(screenType))
            return Task.FromResult<object>(new { error = "screenType is required (e.g. 'MapScreen', 'GauntletInitialScreen')" });

        if (pollIntervalMs < 100) pollIntervalMs = 100;
        if (pollIntervalMs > 5000) pollIntervalMs = 5000;

        return Task.Run<object>(async () =>
        {
            var startTime = DateTime.UtcNow;
            // Poll until the GABP request timeout kills us (controlled by caller).
            // We use a generous local limit of 120s as a safety net.
            var maxWait = TimeSpan.FromSeconds(120);

            while (DateTime.UtcNow - startTime < maxWait)
            {
                // Check screen type on the main thread
                var check = await MainThreadDispatcher.EnqueueAsync(() =>
                {
                    var screen = ScreenManager.TopScreen;
                    if (screen == null)
                        return (matched: false, screenType: (string?) null, layerCount: 0);

                    var name = screen.GetType().Name;
                    var screenMatch = name.IndexOf(screenType, StringComparison.OrdinalIgnoreCase) >= 0;

                    var layerMatch = true;
                    if (waitForLayer != null && screenMatch)
                    {
                        layerMatch = false;
                        foreach (var layer in screen.Layers)
                        {
                            if (layer.Name != null &&
                                layer.Name.IndexOf(waitForLayer, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                layerMatch = true;
                                break;
                            }
                        }
                    }

                    return (matched: screenMatch && layerMatch, screenType: (string?) name, layerCount: screen.Layers.Count);
                });

                if (check.matched)
                {
                    return new
                    {
                        /// Whether the wait timed out
                        timedOut = false,
                        /// Current screen type name
                        screenType = check.screenType,
                        /// Number of layers on the matched screen
                        layerCount = check.layerCount,
                        /// Milliseconds spent waiting
                        waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
                    };
                }

                await Task.Delay(pollIntervalMs);
            }

            // Timeout — return current state
            var finalScreen = await MainThreadDispatcher.EnqueueAsync(() =>
                ScreenManager.TopScreen?.GetType().Name ?? "null");

            return new
            {
                /// Whether the wait timed out
                timedOut = true,
                /// Current screen type name
                screenType = finalScreen,
                /// Number of layers on the matched screen
                layerCount = 0,
                /// Milliseconds spent waiting
                waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
            };
        });
    }

    /// <summary>
    /// Traverse a dot-notation property path with optional array indexing.
    /// Supports: "Smelting.SmeltableItemList", "WeaponDesign.PieceLists[0].Pieces", "CurrentCraftingHero.CurrentStamina"
    /// </summary>
    private static (object? value, string viewModelName) TraversePropertyPath(object root, string path)
    {
        // Parse path into segments: "PieceLists[0].Pieces" → ["PieceLists", "[0]", "Pieces"]
        var segments = Regex.Split(path, @"(?=\[)|(?<=\])\.?|(?<!\])\.");
        object? current = root;
        string resolvedViewModel = root.GetType().Name;

        foreach (var rawSegment in segments)
        {
            if (current == null) break;
            var segment = rawSegment.Trim('.');
            if (string.IsNullOrEmpty(segment)) continue;

            // Array index: [N]
            if (segment.StartsWith("[") && segment.EndsWith("]"))
            {
                var indexStr = segment.Substring(1, segment.Length - 2);
                if (!int.TryParse(indexStr, out var idx))
                {
                    current = null;
                    break;
                }

                if (current is not IEnumerable enumerable)
                {
                    current = null;
                    break;
                }

                int i = 0;
                object? found = null;
                foreach (var item in enumerable)
                {
                    if (i == idx) { found = item; break; }
                    i++;
                }

                current = found;
                if (current != null)
                    resolvedViewModel = current.GetType().Name;
            }
            else
            {
                // Property access
                var prop = current.GetType().GetProperty(segment,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop == null)
                {
                    current = null;
                    break;
                }

                current = prop.GetValue(current);
                if (current != null)
                    resolvedViewModel = current.GetType().Name;
            }
        }

        return (current == root ? null : current, resolvedViewModel);
    }

    private readonly struct MovieEntry
    {
        public readonly string? MovieName;
        public readonly object? DataSource;
        public readonly Widget? RootWidget;

        public MovieEntry(string? movieName, object? dataSource, Widget? rootWidget)
        {
            MovieName = movieName;
            DataSource = dataSource;
            RootWidget = rootWidget;
        }
    }

    private static List<MovieEntry> GetMovies(GauntletLayer layer)
    {
        var result = new List<MovieEntry>();
#if v1313 || v1315
            if (MovieIdentifiersField?.Invoke(layer) is { } enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is GauntletMovieIdentifier id)
                        result.Add(new MovieEntry(id.MovieName, id.DataSource, id.Movie?.RootWidget));
                }
            }
#else
        foreach (var tuple in layer.MoviesAndDataSources)
        {
            var movie = tuple.Item1;
            var dataSource = tuple.Item2;
            result.Add(new MovieEntry(movie?.MovieName, dataSource, movie?.RootWidget));
        }
#endif
        return result;
    }

    private static Widget? FindButtonByText(Widget root, string text)
    {
        if (root is ButtonWidget && root is { IsVisible: true, IsHidden: false })
        {
            var buttonText = FindTextInChildren(root, 4);
            if (buttonText != null && buttonText.Trim().Equals(text.Trim(), StringComparison.OrdinalIgnoreCase))
                return root;
        }

        for (int i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindButtonByText(child, text);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Widget? FindWidgetById(Widget root, string id)
    {
        if (root.Id == id)
            return root;

        for (int i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindWidgetById(child, id);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Collect only visible ButtonWidget instances from the widget tree.
    /// Returns compact info: id, text, state. Limits depth to avoid huge output.
    /// </summary>
    private static void CollectButtons(Widget widget, List<object> result, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth > maxDepth) return;

        if (widget is ButtonWidget && widget is { IsVisible: true, IsHidden: false })
        {
            var id = widget.Id;
            var text = FindTextInChildren(widget, 3);

            // Only include buttons that have an id or text (skip decorative/icon-only buttons)
            if (!string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(text))
            {
                result.Add(new
                {
                    id = string.IsNullOrEmpty(id) ? (string?) null : id,
                    text,
                    state = widget.CurrentState,
                    enabled = widget.IsEnabled,
                });
            }
        }

        for (int i = 0; i < widget.ChildCount; i++)
        {
            CollectButtons(widget.GetChild(i), result, maxDepth, currentDepth + 1);
        }
    }

    [Tool("ui/get_viewmodel_property", Description = "Read a property from a ViewModel on the current screen. Supports dot-notation paths to traverse nested ViewModels (e.g. 'Smelting.SmeltableItemList'). Returns the value as a string, or for list properties returns a count. Use with subProperties to extract specific fields from list items.")]
    public partial Task<object> GetViewModelProperty(
        [ToolParameter(Description = "Property path on the ViewModel. Supports dot-notation for nested access (e.g. 'PlayerGold', 'Smelting.SmeltableItemList', 'LeftItemListVM')")] string propertyName,
        [ToolParameter(Description = "Name of the layer (from get_screen)")] string layerName,
        [ToolParameter(Description = "For list items: comma-separated sub-property names to extract (e.g. 'ItemDescription,ItemCost,ItemCount'). Omit for simple properties.", Required = false)] string? subProperties)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                var screen = ScreenManager.TopScreen;
                if (screen == null) return new { error = "No active screen" };

                foreach (var layer in screen.Layers)
                {
                    if (layer is GauntletLayer gauntletLayer &&
                        string.Equals(layer.Name, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        var movies = GetMovies(gauntletLayer);
                        foreach (var movieId in movies)
                        {
                            var dataSource = movieId.DataSource;
                            if (dataSource == null) continue;

                            var (traversed, resolvedViewModel) = TraversePropertyPath(dataSource, propertyName);
                            if (traversed == null && resolvedViewModel == dataSource.GetType().Name) continue;

                            var value = traversed;

                            if (value == null)
                            {
                                return new
                                {
                                    /// String representation of the property value, or item count for collections
                                    value = (string?) null,
                                    /// ViewModel class name that owns the property
                                    viewModel = resolvedViewModel,
                                    /// Number of items (for list properties)
                                    count = (int?) null,
                                    /// Extracted items with sub-properties (for list properties with subProperties param)
                                    items = (string?) null,
                                };
                            }

                            if (value is IEnumerable enumerable and not string)
                            {
                                var result = SerializeListProperty(enumerable, subProperties);
                                return new
                                {
                                    /// String representation of the property value, or item count for collections
                                    value = result.count.ToString(),
                                    /// ViewModel class name that owns the property
                                    viewModel = resolvedViewModel,
                                    /// Number of items (for list properties)
                                    count = (int?) result.count,
                                    /// Extracted items with sub-properties (for list properties with subProperties param)
                                    items = result.json,
                                };
                            }

                            return new
                            {
                                /// String representation of the property value, or item count for collections
                                value = value.ToString(),
                                /// ViewModel class name that owns the property
                                viewModel = resolvedViewModel,
                                /// Number of items (for list properties)
                                count = (int?) null,
                                /// Extracted items with sub-properties (for list properties with subProperties param)
                                items = (string?) null,
                            };
                        }
                    }
                }

                return new { error = $"Property '{propertyName}' not found in layer '{layerName}'" };
            }
            catch (Exception ex)
            {
                return new { error = $"GetViewModelProperty failed: {ex.Message}" };
            }
        });
    }

    private static (int count, string? json) SerializeListProperty(IEnumerable enumerable, string? subProperties)
    {
        var subPropNames = subProperties?.Split(',').Select(s => s.Trim()).ToArray();

        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        int index = 0;
        foreach (var item in enumerable)
        {
            if (item == null) continue;
            if (index > 0) sb.Append(',');

            var itemType = item.GetType();
            sb.Append('{');
            sb.Append($"\"index\":{index}");

            if (subPropNames != null && subPropNames.Length > 0)
            {
                foreach (var spName in subPropNames)
                {
                    var sp = itemType.GetProperty(spName, BindingFlags.Instance | BindingFlags.Public);
                    var spVal = sp?.GetValue(item)?.ToString()?.Replace("\"", "\\\"");
                    sb.Append($",\"{spName}\":\"{spVal}\"");
                }
            }
            else
            {
                var nameProp = itemType.GetProperty("ItemDescription") ?? itemType.GetProperty("Name");
                var itemName = nameProp?.GetValue(item)?.ToString()?.Replace("\"", "\\\"") ?? item.ToString();
                sb.Append($",\"name\":\"{itemName}\",\"type\":\"{itemType.Name}\"");
            }

            sb.Append('}');
            index++;
            if (index >= 100) break;
        }
        sb.Append(']');

        return (index, subProperties != null || index > 0 ? sb.ToString() : null);
    }

    [Tool("ui/call_viewmodel_method_at_index", Description = "Call a method on a specific item in a ViewModel list. Supports dot-notation paths for nested lists (e.g. 'Smelting.SmeltableItemList'). Use to trigger actions on individual items.")]
    public partial Task<object> CallViewModelMethodAtIndex(
        [ToolParameter(Description = "Name of the layer (from get_screen)")] string layerName,
        [ToolParameter(Description = "List property path on the ViewModel. Supports dot-notation (e.g. 'LeftItemListVM', 'Smelting.SmeltableItemList')")] string listPropertyName,
        [ToolParameter(Description = "Index of the item in the list")] int index,
        [ToolParameter(Description = "Method name to call on the item (e.g. 'ExecuteBuySingle', 'ExecuteSellSingle')")] string methodName)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                var screen = ScreenManager.TopScreen;
                if (screen == null) return new { error = "No active screen" };

                foreach (var layer in screen.Layers)
                {
                    if (layer is GauntletLayer gauntletLayer &&
                        string.Equals(layer.Name, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        var movies = GetMovies(gauntletLayer);
                        foreach (var movieId in movies)
                        {
                            var dataSource = movieId.DataSource;
                            if (dataSource == null) continue;

                            var (traversed, _) = TraversePropertyPath(dataSource, listPropertyName);
                            if (traversed == null) continue;

                            if (traversed is not IEnumerable list) continue;

                            int i = 0;
                            object? target = null;
                            foreach (var item in list)
                            {
                                if (i == index) { target = item; break; }
                                i++;
                            }

                            if (target == null) return new { error = $"Index {index} out of range" };

                            var method = target.GetType().GetMethod(methodName,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (method == null) return new { error = $"Method '{methodName}' not found on item type {target.GetType().Name}" };

                            method.Invoke(target, null);
                            return new
                            {
                                /// Type name of the item the method was called on
                                itemType = target.GetType().Name,
                                /// Method name that was invoked
                                method = methodName,
                                /// Index of the item in the list
                                index,
                            };
                        }
                    }
                }

                return new { error = $"List '{listPropertyName}' not found in layer '{layerName}'" };
            }
            catch (Exception ex)
            {
                return new { error = $"CallViewModelMethodAtIndex failed: {ex.Message}" };
            }
        });
    }

    /// <summary>
    /// Find any widget that has a HandleClick method and matching text in its children.
    /// Broader than FindButtonByText — catches custom widget types that aren't ButtonWidget subclasses.
    /// </summary>
    private static Widget? FindClickableWidgetByText(Widget root, string text)
    {
        if (root is { IsVisible: true, IsHidden: false })
        {
            var hasHandleClick = root.GetType().GetMethod("HandleClick",
                BindingFlags.Instance | BindingFlags.NonPublic) != null;
            if (hasHandleClick)
            {
                var widgetText = FindTextInChildren(root, 4);
                if (widgetText != null && widgetText.Trim().Equals(text.Trim(), StringComparison.OrdinalIgnoreCase))
                    return root;
            }
        }

        for (int i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindClickableWidgetByText(child, text);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Collect all visible ButtonWidget instances (flat list) for index-based clicking.
    /// </summary>
    private static void CollectButtonWidgets(Widget widget, List<Widget> result, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth > maxDepth) return;

        if (widget is ButtonWidget && widget is { IsVisible: true, IsHidden: false })
        {
            var id = widget.Id;
            var text = FindTextInChildren(widget, 3);
            if (!string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(text))
                result.Add(widget);
        }

        for (int i = 0; i < widget.ChildCount; i++)
        {
            CollectButtonWidgets(widget.GetChild(i), result, maxDepth, currentDepth + 1);
        }
    }

    private static string? FindTextInChildren(Widget widget, int maxDepth)
    {
        if (maxDepth <= 0) return null;

        for (int i = 0; i < widget.ChildCount; i++)
        {
            var child = widget.GetChild(i);
            var typeName = child.GetType().Name;

            if (typeName is "TextWidget" or "RichTextWidget")
            {
                try
                {
                    var textProp = child.GetType().GetProperty("Text");
                    var text = textProp?.GetValue(child) as string;
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
                catch { }
            }

            var found = FindTextInChildren(child, maxDepth - 1);
            if (found != null) return found;
        }
        return null;
    }
}