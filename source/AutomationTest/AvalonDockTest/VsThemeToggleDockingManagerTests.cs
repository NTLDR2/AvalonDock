namespace AvalonDockTest
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Media;

	using NUnit.Framework;

	using AvalonDock;
	using AvalonDock.Controls;
	using AvalonDock.Themes;
	using AvalonDock.Themes.Type1Legacy;
	using AvalonDock.Themes.Type2Hybrid;
	using AvalonDock.Themes.Type3New;

	/// <summary>
	/// Regression tests for #683 and v5.1.0 theme invariants across <see cref="VS2010Theme"/> (<c>Type1Legacy</c>),
	/// <c>AvalonDock.Themes.Type2Hybrid</c> (VS2013), and <c>AvalonDock.Themes.Type3New</c> (VS2015, VS2022, VS2026):
	/// frozen palette brushes, <see cref="ToggleDockingManager"/> styles, detached window context-menu
	/// wiring, and leak-free runtime <see cref="DictionaryTheme"/> switching.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class VsThemeToggleDockingManagerTests
	{
		private static IEnumerable<TestCaseData> AllVsDictionaryThemes()
		{
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new Vs2013BlueTheme())).SetName("Vs2013Blue");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new Vs2013DarkTheme())).SetName("Vs2013Dark");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new Vs2013LightTheme())).SetName("Vs2013Light");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2015BlueTheme())).SetName("VS2015Blue");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2015DarkTheme())).SetName("VS2015Dark");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2015LightTheme())).SetName("VS2015Light");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2022BlueTheme())).SetName("VS2022Blue");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2022DarkTheme())).SetName("VS2022Dark");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2022LightTheme())).SetName("VS2022Light");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2026BlueTheme())).SetName("VS2026Blue");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2026DarkTheme())).SetName("VS2026Dark");
			yield return new TestCaseData((Func<DictionaryTheme>)(() => new VS2026LightTheme())).SetName("VS2026Light");
		}

		[TestCaseSource(nameof(AllVsDictionaryThemes))]
		public void Theme_ProvidesToggleDockingManagerStylesAndFrozenBrushes(Func<DictionaryTheme> createTheme)
		{
			_ = new Border();
			var theme = createTheme();
			var dict = theme.ThemeResourceDictionary;

			Assert.That(dict, Is.Not.Null);
			Assert.That(dict.Count, Is.GreaterThan(50));

			foreach (var value in dict.Values)
			{
				if (value is SolidColorBrush brush)
				{
					Assert.That(brush.IsFrozen, Is.True, $"Expected all SolidColorBrush values in {theme.Name} to be frozen.");
				}
			}

			Assert.That(dict["ToggleAnchorablePaneControlStyle"], Is.InstanceOf<Style>());
			Assert.That(dict[typeof(ToggleAnchorablePaneTitle)], Is.InstanceOf<Style>());
			Assert.That(dict[ToggleDockButton.ForegroundBrushKey], Is.InstanceOf<Brush>());
			Assert.That(dict[typeof(ToggleDockButton)], Is.InstanceOf<Style>());
			Assert.That(dict[typeof(ToggleDockingManager)], Is.InstanceOf<Style>());
		}

		[TestCaseSource(nameof(AllVsDictionaryThemes))]
		public void ToggleAnchorablePaneTitle_TemplateResolvesAllResources(Func<DictionaryTheme> createTheme)
		{
			var host = new Border();
			host.Resources.MergedDictionaries.Add(createTheme().ThemeResourceDictionary);
			var title = new ToggleAnchorablePaneTitle();
			host.Child = title;

			Assert.DoesNotThrow(() => title.ApplyTemplate());
			Assert.That(title.Template, Is.Not.Null);
			Assert.That(title.Template.FindName("PART_MinimizeButton", title), Is.InstanceOf<Button>());
		}

		[TestCaseSource(nameof(AllVsDictionaryThemes))]
		public void Theme_DockingManagerStyle_IncludesDetachToWindowCommandInAnchorableContextMenu(Func<DictionaryTheme> createTheme)
		{
			_ = new Border();
			var dict = createTheme().ThemeResourceDictionary;
			var managerStyle = dict[typeof(DockingManager)] as Style;
			Assert.That(managerStyle, Is.Not.Null);

			var contextMenuSetter = managerStyle!.Setters.OfType<Setter>()
				.SingleOrDefault(s => s.Property == DockingManager.AnchorableContextMenuProperty);
			Assert.That(contextMenuSetter?.Value, Is.InstanceOf<ContextMenu>());

			var menu = (ContextMenu)contextMenuSetter!.Value;
			var hasDetachItem = menu.Items.OfType<MenuItem>()
				.Any(item => BindingOperations.GetBinding(item, MenuItem.CommandProperty)?.Path?.Path == "DetachToWindowCommand");
			Assert.That(hasDetachItem, Is.True);
		}

		[Test]
		public void DockingManager_RuntimeThemeSwitching_AcrossDictionaryAndUriThemes_DoesNotLeakDictionaries()
		{
			var dockManager = new DockingManager();
			var sequence = new Theme[]
			{
				new VS2010Theme(),
				new Vs2013DarkTheme(),
				new Vs2013BlueTheme(),
				new VS2015DarkTheme(),
				new VS2022LightTheme(),
				new VS2026DarkTheme(),
				new GenericTheme(),
			};

			foreach (var theme in sequence)
			{
				dockManager.Theme = theme;
				Assert.That(dockManager.Resources.MergedDictionaries.Count, Is.EqualTo(1),
					$"Expected exactly 1 merged theme dictionary after applying {theme.Name}.");
			}

			dockManager.Theme = null;
			Assert.That(dockManager.Resources.MergedDictionaries.Count, Is.EqualTo(0));
		}

		[Test]
		public void VsTheme_FromJson_StreamWithBasePalette_BuildsValidFrozenTheme()
		{
			_ = new Border();
			var basePalette = new VsThemeColorPalette(new Dictionary<string, VsThemeColorEntry>
			{
				["EnvironmentBackground"] = new VsThemeColorEntry(Color.FromRgb(0x1E, 0x1E, 0x1E), null),
				["FileTabSelectedBorder"] = new VsThemeColorEntry(Color.FromRgb(0x00, 0x7A, 0xCC), null),
			});

			const string jsonOverride = @"[
  // Custom accent override
  { ""Name"": ""FileTabSelectedBorder"", ""Background"": ""FFE06C75"", }
]";

			using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonOverride));
			var customTheme = VsTheme.FromJson(stream, basePalette);

			Assert.That(customTheme.ThemeResourceDictionary, Is.Not.Null);
			var accentColor = (Color)customTheme.ThemeResourceDictionary[AvalonDock.Themes.Type3New.Themes.ResourceKeys.ControlAccentColorKey];
			Assert.That(accentColor, Is.EqualTo(Color.FromArgb(0xFF, 0xE0, 0x6C, 0x75)));

			var accentBrush = customTheme.ThemeResourceDictionary[AvalonDock.Themes.Type3New.Themes.ResourceKeys.ControlAccentBrushKey] as SolidColorBrush;
			Assert.That(accentBrush, Is.Not.Null);
			Assert.That(accentBrush!.IsFrozen, Is.True);
		}

		[Test]
		public void Type2Hybrid_Vs2013Themes_ExposeSingleMergedDictionaryAccentColorAndFrozenMenuKeys()
		{
			_ = new Border();
			var themes = new DictionaryTheme[]
			{
				new Vs2013BlueTheme(),
				new Vs2013DarkTheme(),
				new Vs2013LightTheme(),
			};

			var requiredMenuKeys = new[]
			{
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.MenuSeparatorBorderBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.SubmenuItemBackgroundKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.MenuItemHighlightedBackgroundKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.SubmenuItemBackgroundHighlightedKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.CheckMarkBackgroundBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.CheckMarkBorderBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.CheckMarkForegroundBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.DisabledSubMenuItemBackgroundBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.DisabledSubMenuItemBorderBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.TextBrushKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.ItemBackgroundSelectedKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.ItemTextDisabledKey,
				AvalonDock.Themes.Type2Hybrid.Themes.Menu.MenuKeys.ItemBackgroundHoverKey,
			};

			foreach (var theme in themes)
			{
				var dict = theme.ThemeResourceDictionary;
				Assert.That(dict.MergedDictionaries.Count, Is.EqualTo(1),
					$"{theme.Name} must merge only Generic.xaml without duplicate OverlayButtons.xaml.");

				var accentColorObj = dict[AvalonDock.Themes.Type2Hybrid.Themes.ResourceKeys.ControlAccentColorKey];
				Assert.That(accentColorObj, Is.TypeOf<Color>(), $"{theme.Name} must define ControlAccentColorKey.");
				var accentBrush = dict[AvalonDock.Themes.Type2Hybrid.Themes.ResourceKeys.ControlAccentBrushKey] as SolidColorBrush;
				Assert.That(accentBrush, Is.Not.Null);
				Assert.That(accentBrush!.IsFrozen, Is.True);
				Assert.That(accentBrush.Color, Is.EqualTo((Color)accentColorObj));

				foreach (var menuKey in requiredMenuKeys)
				{
					var brush = dict[menuKey] as SolidColorBrush;
					Assert.That(brush, Is.Not.Null, $"{theme.Name} is missing MenuKeys.{menuKey.ResourceId}.");
					Assert.That(brush!.IsFrozen, Is.True, $"{theme.Name} MenuKeys.{menuKey.ResourceId} must be frozen.");
				}
			}
		}

		[Test]
		public void VS2010Theme_ProvidesToggleDockingManagerStyles()
		{
			_ = new Border();
			var dict = new ResourceDictionary { Source = new VS2010Theme().GetResourceUri() };

			Assert.That(dict["ToggleAnchorablePaneControlStyle"], Is.InstanceOf<Style>());
			Assert.That(dict[ToggleDockButton.ForegroundBrushKey], Is.InstanceOf<Brush>());
			Assert.That(dict[typeof(ToggleDockButton)], Is.InstanceOf<Style>());
			Assert.That(dict[typeof(ToggleDockingManager)], Is.InstanceOf<Style>());
		}

		[Test]
		public void VS2010Theme_ToggleAnchorablePaneTitle_TemplateResolvesAllResources()
		{
			var host = new Border();
			host.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new VS2010Theme().GetResourceUri() });
			var title = new ToggleAnchorablePaneTitle();
			host.Child = title;

			Assert.DoesNotThrow(() => title.ApplyTemplate());
			Assert.That(title.Template, Is.Not.Null);
			Assert.That(title.Template.FindName("PART_MinimizeButton", title), Is.InstanceOf<Button>());
		}
	}
}
