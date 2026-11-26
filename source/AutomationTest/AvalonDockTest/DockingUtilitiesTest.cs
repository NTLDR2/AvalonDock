namespace AvalonDockTest
{
	using System;
	using System.Windows.Controls;

	using NUnit.Framework;
	using NUnit.Framework.Legacy;

	using AvalonDock.Layout;

	[TestFixture]
	public sealed class DockingUtilitiesTest
	{
		[Test]
		public void CalculatedDockMinWidthHeightTest()
		{
			double defaultDockMinHeight = 25;
			double defaultDockMinWidth = 25;

			const double documentPaneDockMinHeight = 200;
			const double documentPaneDockMinWidth = 400;
			LayoutDocumentPane layoutDocumentPane = new LayoutDocumentPane { DockMinHeight = documentPaneDockMinHeight, DockMinWidth = documentPaneDockMinWidth };
			layoutDocumentPane.InsertChildAt(0, new LayoutDocument { ContentId = "Document" });

			LayoutDocumentPaneGroup layoutDocumentPaneGroup = new LayoutDocumentPaneGroup();
			layoutDocumentPaneGroup.InsertChildAt(0, layoutDocumentPane);

			const double anchorablePaneDockMinHeight = 80;
			const double anchorablePaneDockMinWidth = 160;
			LayoutAnchorablePane layoutAnchorablePane = new LayoutAnchorablePane { DockMinHeight = anchorablePaneDockMinHeight, DockMinWidth = anchorablePaneDockMinWidth };
			layoutAnchorablePane.InsertChildAt(0, new LayoutAnchorable { ContentId = "Anchorable" });

			LayoutAnchorablePaneGroup layoutAnchorablePaneGroup = new LayoutAnchorablePaneGroup();
			layoutAnchorablePaneGroup.InsertChildAt(0, layoutAnchorablePane);

			LayoutPanel layoutPanel = new LayoutPanel();
			layoutPanel.InsertChildAt(0, layoutDocumentPaneGroup);
			layoutPanel.InsertChildAt(1, layoutAnchorablePaneGroup);

			ClassicAssert.AreEqual(defaultDockMinWidth, layoutPanel.DockMinWidth);
			ClassicAssert.AreEqual(defaultDockMinHeight, layoutPanel.DockMinHeight);
			ClassicAssert.AreEqual(documentPaneDockMinWidth + anchorablePaneDockMinWidth, layoutPanel.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(Math.Max(documentPaneDockMinHeight, anchorablePaneDockMinHeight), layoutPanel.CalculatedDockMinHeight());

			ClassicAssert.AreEqual(documentPaneDockMinWidth, layoutDocumentPane.DockMinWidth);
			ClassicAssert.AreEqual(documentPaneDockMinHeight, layoutDocumentPane.DockMinHeight);
			ClassicAssert.AreEqual(layoutDocumentPane.DockMinWidth, layoutDocumentPane.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(layoutDocumentPane.DockMinHeight, layoutDocumentPane.CalculatedDockMinHeight());

			ClassicAssert.AreEqual(defaultDockMinWidth, layoutDocumentPaneGroup.DockMinWidth);
			ClassicAssert.AreEqual(defaultDockMinWidth, layoutDocumentPaneGroup.DockMinHeight);
			ClassicAssert.AreEqual(documentPaneDockMinWidth, layoutDocumentPaneGroup.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(documentPaneDockMinHeight, layoutDocumentPaneGroup.CalculatedDockMinHeight());

			ClassicAssert.AreEqual(anchorablePaneDockMinWidth, layoutAnchorablePane.DockMinWidth);
			ClassicAssert.AreEqual(anchorablePaneDockMinHeight, layoutAnchorablePane.DockMinHeight);
			ClassicAssert.AreEqual(layoutAnchorablePane.DockMinWidth, layoutAnchorablePane.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(layoutAnchorablePane.DockMinHeight, layoutAnchorablePane.CalculatedDockMinHeight());

			ClassicAssert.AreEqual(defaultDockMinWidth, layoutAnchorablePaneGroup.DockMinWidth);
			ClassicAssert.AreEqual(defaultDockMinWidth, layoutAnchorablePaneGroup.DockMinHeight);
			ClassicAssert.AreEqual(anchorablePaneDockMinWidth, layoutAnchorablePaneGroup.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(anchorablePaneDockMinHeight, layoutAnchorablePaneGroup.CalculatedDockMinHeight());

			layoutPanel.RemoveChild(layoutDocumentPaneGroup);
			ClassicAssert.AreEqual(anchorablePaneDockMinWidth, layoutPanel.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(anchorablePaneDockMinHeight, layoutPanel.CalculatedDockMinHeight());
		}

		[Test]
		public void UpdateDocMinWidthHeightTest()
		{
			double documentPaneDockMinHeight = 100;
			double documentPaneDockMinWidth = 101;
			LayoutDocumentPane layoutDocumentPane = new LayoutDocumentPane { DockMinHeight = documentPaneDockMinHeight, DockMinWidth = documentPaneDockMinWidth };
			layoutDocumentPane.InsertChildAt(0, new LayoutDocument { ContentId = "Document" });

			LayoutDocumentPaneGroup layoutDocumentPaneGroup = new LayoutDocumentPaneGroup();
			layoutDocumentPaneGroup.InsertChildAt(0, layoutDocumentPane);

			double anchorablePane1DockMinHeight = 150;
			double anchorablePane1DockMinWidth = 151;
			LayoutAnchorablePane layoutAnchorablePane1 = new LayoutAnchorablePane { DockMinHeight = anchorablePane1DockMinHeight, DockMinWidth = anchorablePane1DockMinWidth };
			layoutAnchorablePane1.InsertChildAt(0, new LayoutAnchorable { ContentId = "Anchorable1" });

			double anchorablePane2DockMinHeight = 200;
			double anchorablePane2DockMinWidth = 201;
			LayoutAnchorablePane layoutAnchorablePane2 = new LayoutAnchorablePane { DockMinHeight = anchorablePane2DockMinHeight, DockMinWidth = anchorablePane2DockMinWidth };
			layoutAnchorablePane2.InsertChildAt(0, new LayoutAnchorable { ContentId = "Anchorable2" });

			LayoutAnchorablePaneGroup layoutAnchorablePaneGroup = new LayoutAnchorablePaneGroup { Orientation = Orientation.Horizontal };
			layoutAnchorablePaneGroup.InsertChildAt(0, layoutAnchorablePane1);
			layoutAnchorablePaneGroup.InsertChildAt(0, layoutAnchorablePane2);

			LayoutPanel layoutPanel = new LayoutPanel { Orientation = Orientation.Vertical };
			layoutPanel.InsertChildAt(0, layoutDocumentPaneGroup);
			layoutPanel.InsertChildAt(1, layoutAnchorablePaneGroup);

			ClassicAssert.AreEqual(anchorablePane2DockMinWidth + anchorablePane1DockMinWidth, layoutAnchorablePaneGroup.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(Math.Max(anchorablePane2DockMinHeight, anchorablePane1DockMinHeight), layoutAnchorablePaneGroup.CalculatedDockMinHeight());

			ClassicAssert.AreEqual(documentPaneDockMinWidth, layoutDocumentPaneGroup.CalculatedDockMinWidth());
			ClassicAssert.AreEqual(documentPaneDockMinHeight, layoutDocumentPaneGroup.CalculatedDockMinHeight());

			ClassicAssert.AreEqual(
				Math.Max(anchorablePane1DockMinWidth + anchorablePane2DockMinWidth, documentPaneDockMinWidth),
				layoutPanel.CalculatedDockMinWidth());

			ClassicAssert.AreEqual(documentPaneDockMinHeight + anchorablePane2DockMinHeight, layoutPanel.CalculatedDockMinHeight());
		}
	}
}
