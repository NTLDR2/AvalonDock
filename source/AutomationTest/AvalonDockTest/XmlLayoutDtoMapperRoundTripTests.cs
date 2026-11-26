using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Serializer.Xml;
using NUnit.Framework;

namespace AvalonDockTest
{
	/// <summary>
	/// End-to-end round-trip tests for <see cref="XmlLayoutSerializer"/> paired with the real
	/// <see cref="DockingManager"/> DTO mapper (<c>LayoutDtoMapper</c>).
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="XmlLayoutSerializerTests"/> (which uses a fake DTO mapper), these tests
	/// exercise the real <c>LayoutRoot</c> ↔ <c>LayoutRootDto</c> mapping, including conditional
	/// <c>ShouldSerialize*</c> default suppression, hidden tool windows, and auto-hide minimums.
	/// </remarks>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class XmlLayoutDtoMapperRoundTripTests
	{
		/// <summary>
		/// Builds a layout touching both content types, both pane types, nesting, an auto-hide side,
		/// the hidden list, and a floating window.
		/// </summary>
		/// <returns>The layout root.</returns>
		private static LayoutRoot BuildLayout()
		{
			var documentPane = new LayoutDocumentPane { ShowHeader = false };
			((ILayoutPaneSerializable)documentPane).Id = "docPane";
			documentPane.Children.Add(new LayoutDocument
			{
				Title = "Readme",
				ContentId = "readme",
				Description = "Docs",
				CanClose = false,
				CanMove = false,
				IsSelected = true,
			});

			var toolPane = new LayoutAnchorablePane { Name = "ToolsPane", DockWidth = new System.Windows.GridLength(220) };
			((ILayoutPaneSerializable)toolPane).Id = "toolsPane";
			toolPane.Children.Add(new LayoutAnchorable
			{
				Title = "Explorer",
				ContentId = "explorer",
				CanHide = false,
				CanClose = true,
				CanFloat = false,
				CanAutoHide = false,
				CanMove = false,
				CanShowOnHover = false,
				CanDockAsTabbedDocument = false,
			});

			var panel = new LayoutPanel(new LayoutDocumentPaneGroup(documentPane));
			panel.Children.Add(new LayoutAnchorablePaneGroup(toolPane));

			var layout = new LayoutRoot { RootPanel = panel };

			var group = new LayoutAnchorGroup();
			group.Children.Add(new LayoutAnchorable { Title = "Output", ContentId = "output" });
			layout.LeftSide.Children.Add(group);

			layout.Hidden.Add(new LayoutAnchorable { Title = "Hidden Tool", ContentId = "hiddenTool" });

			var floatingPane = new LayoutAnchorablePane();
			floatingPane.Children.Add(new LayoutAnchorable { Title = "Watch", ContentId = "watch" });
			layout.FloatingWindows.Add(new LayoutAnchorableFloatingWindow
			{
				RootPanel = new LayoutAnchorablePaneGroup(floatingPane),
			});

			return layout;
		}

		/// <summary>Round-trips a layout through the XML serializer and real DTO mapper.</summary>
		/// <param name="layout">The layout to store.</param>
		/// <returns>The restored layout.</returns>
		private static LayoutRoot RoundTripXml(LayoutRoot layout)
		{
			var source = new DockingManager { Layout = layout };
			using var stream = new MemoryStream();
			new XmlLayoutSerializer(source).Serialize(stream);

			var target = new DockingManager();
			var serializer = new XmlLayoutSerializer(target);
			serializer.LayoutSerializationCallback += (s, e) => e.Content = new object();

			using var input = new MemoryStream(stream.ToArray());
			serializer.Deserialize(input);
			return target.Layout;
		}

		/// <summary>
		/// Describes a layout as a flat list of lines so restored structure can be verified
		/// element by element.
		/// </summary>
		/// <param name="layout">The layout to describe.</param>
		/// <returns>One line per element.</returns>
		private static List<string> Describe(LayoutRoot layout)
		{
			var lines = new List<string>();

			foreach (var element in layout.Descendents())
			{
				switch (element)
				{
					case LayoutAnchorable anchorable:
						lines.Add(
							$"Anchorable id={anchorable.ContentId} title={anchorable.Title} "
							+ $"canHide={anchorable.CanHide} canClose={anchorable.CanClose} "
							+ $"canFloat={anchorable.CanFloat} canAutoHide={anchorable.CanAutoHide} "
							+ $"canMove={anchorable.CanMove} canShowOnHover={anchorable.CanShowOnHover} "
							+ $"canDockAsTabbedDocument={anchorable.CanDockAsTabbedDocument} "
							+ $"autoHideMinWidth={anchorable.AutoHideMinWidth} hidden={anchorable.IsHidden}");
						break;

					case LayoutDocument document:
						lines.Add(
							$"Document id={document.ContentId} title={document.Title} "
							+ $"description={document.Description} canClose={document.CanClose} "
							+ $"canMove={document.CanMove} selected={document.IsSelected}");
						break;

					case LayoutAnchorablePane anchorablePane:
						lines.Add(
							$"AnchorablePane id={((ILayoutPaneSerializable)anchorablePane).Id} "
							+ $"name={anchorablePane.Name} widthStar={anchorablePane.DockWidth.IsStar} "
							+ $"width={anchorablePane.DockWidth.Value} minWidth={anchorablePane.DockMinWidth}");
						break;

					case LayoutDocumentPane documentPane:
						lines.Add(
							$"DocumentPane id={((ILayoutPaneSerializable)documentPane).Id} "
							+ $"showHeader={documentPane.ShowHeader}");
						break;

					case LayoutAnchorablePaneGroup anchorableGroup:
						lines.Add($"AnchorablePaneGroup orientation={anchorableGroup.Orientation}");
						break;

					case LayoutDocumentPaneGroup documentGroup:
						lines.Add($"DocumentPaneGroup orientation={documentGroup.Orientation}");
						break;

					case LayoutAnchorGroup anchorGroup:
						lines.Add($"AnchorGroup children={anchorGroup.Children.Count}");
						break;

					case LayoutAnchorSide anchorSide:
						lines.Add($"AnchorSide {anchorSide.Side} groups={anchorSide.Children.Count}");
						break;

					case LayoutPanel panelElement:
						lines.Add($"Panel orientation={panelElement.Orientation}");
						break;

					case LayoutAnchorableFloatingWindow anchorableFloating:
						lines.Add($"AnchorableFloatingWindow valid={anchorableFloating.IsValid}");
						break;

					case LayoutDocumentFloatingWindow documentFloating:
						lines.Add($"DocumentFloatingWindow valid={documentFloating.IsValid}");
						break;
				}
			}

			return lines;
		}

		/// <summary>Repeated XML round-trips must restore the exact same layout tree.</summary>
		[Test]
		public void XmlRoundTrip_RestoresIdempotentLayoutTree()
		{
			var firstPass = RoundTripXml(BuildLayout());
			var secondPass = RoundTripXml(firstPass);

			Assert.That(Describe(secondPass), Is.EqualTo(Describe(firstPass)));
		}

		/// <summary>XML round-trip must preserve tool windows parked in the hidden list.</summary>
		[Test]
		public void XmlRoundTrip_KeepsHiddenList()
		{
			var viaXml = RoundTripXml(BuildLayout()).Hidden.Select(a => a.ContentId).OrderBy(id => id).ToArray();

			Assert.That(viaXml, Is.EqualTo(new[] { "hiddenTool" }));
		}

		/// <summary>
		/// Flags switched off from their defaults must survive XML round-tripping through ShouldSerialize rules.
		/// </summary>
		[Test]
		public void XmlRoundTrip_KeepsSwitchedOffFlags()
		{
			var fromXml = RoundTripXml(BuildLayout()).Descendents()
				.OfType<LayoutAnchorable>().Single(a => a.ContentId == "explorer");

			Assert.That(
				Flags(fromXml),
				Is.EqualTo(new[] { false, false, false, false, false, false, true }),
				"CanHide, CanFloat, CanAutoHide, CanMove, CanShowOnHover and "
				+ "CanDockAsTabbedDocument were stored off and CanClose on.");
		}

		/// <summary>
		/// Auto-hide minimums must restore both the default (omitted by ShouldSerialize) and custom values.
		/// </summary>
		/// <param name="stored">The value stored on the anchorable.</param>
		[TestCase(100.0)]
		[TestCase(240.0)]
		public void XmlRoundTrip_RestoresAutoHideMinimums(double stored)
		{
			var layout = BuildLayout();
			var anchorable = layout.Descendents().OfType<LayoutAnchorable>().Single(a => a.ContentId == "explorer");
			anchorable.AutoHideMinWidth = stored;
			anchorable.AutoHideMinHeight = stored;

			var restored = RoundTripXml(layout).Descendents()
				.OfType<LayoutAnchorable>().Single(a => a.ContentId == "explorer");

			Assert.That(new[] { restored.AutoHideMinWidth, restored.AutoHideMinHeight }, Is.EqualTo(new[] { stored, stored }));
		}

		private static bool[] Flags(LayoutAnchorable anchorable) => new[]
		{
			anchorable.CanHide,
			anchorable.CanFloat,
			anchorable.CanAutoHide,
			anchorable.CanMove,
			anchorable.CanShowOnHover,
			anchorable.CanDockAsTabbedDocument,
			anchorable.CanClose,
		};
	}
}
