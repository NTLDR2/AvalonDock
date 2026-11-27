using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AvalonDock.Controls;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Dual-mode WPF + Win32/DWM screenshot and multi-window composite stitcher writing atomic PNG and Markdown
	/// bundles into <see cref="AgentBridgePaths"/> for multimodal inspection by Antigravity.
	/// </summary>
	public sealed class WindowScreenshotService
	{
		private const int MaxDimensionPixels = 6144;
		private const double CompositePadding = 16.0;
		private const double PanelHeaderHeight = 28.0;

		private static readonly SolidColorBrush CanvasBackgroundBrush = CreateFrozenBrush(0x0F, 0x17, 0x2A);
		private static readonly SolidColorBrush PanelHeaderBrush = CreateFrozenBrush(0x1E, 0x29, 0x3B);
		private static readonly SolidColorBrush PanelBorderBrush = CreateFrozenBrush(0x47, 0x55, 0x69);
		private static readonly SolidColorBrush PanelTextBrush = CreateFrozenBrush(0xF8, 0xFA, 0xFC);
		private static readonly SolidColorBrush FallbackBackdropBrush = CreateFrozenBrush(0x1E, 0x1E, 0x22);
		private static readonly Pen PanelBorderPen = CreateFrozenPen(PanelBorderBrush, 1.0);
		private static readonly Typeface LabelTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

		private readonly AgentBridgePaths _paths;
		private readonly DiagnosticReportExporterService _reportExporter;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowScreenshotService"/> class.
		/// </summary>
		/// <param name="paths">The <c>AgentBridge</c> directory configuration.</param>
		/// <param name="reportExporter">The full Markdown diagnostic report exporter.</param>
		public WindowScreenshotService(AgentBridgePaths paths, DiagnosticReportExporterService reportExporter)
		{
			_paths = paths ?? throw new ArgumentNullException(nameof(paths));
			_reportExporter = reportExporter ?? throw new ArgumentNullException(nameof(reportExporter));
		}

		/// <summary>
		/// Renders <paramref name="element"/> via WPF's retained visual composition tree (<see cref="RenderTargetBitmap"/>),
		/// succeeding even when the window is occluded behind another application or off-screen.
		/// </summary>
		/// <param name="element">The visual element or window to render.</param>
		/// <returns>A frozen <see cref="BitmapSource"/>.</returns>
		public static BitmapSource CaptureWpfVisualBitmap(FrameworkElement element)
		{
			if (element == null) throw new ArgumentNullException(nameof(element));
			if (!element.CheckAccess())
				return element.Dispatcher.Invoke(() => CaptureWpfVisualBitmap(element));

			if ((element.ActualWidth < 1.0 || element.ActualHeight < 1.0) &&
				!double.IsNaN(element.Width) && element.Width > 0.0 &&
				!double.IsNaN(element.Height) && element.Height > 0.0)
			{
				var fallbackSize = new Size(element.Width, element.Height);
				element.Measure(fallbackSize);
				element.Arrange(new Rect(fallbackSize));
				element.UpdateLayout();
			}

			var widthDip = Math.Max(1.0, element.ActualWidth > 0.0 ? element.ActualWidth : (!double.IsNaN(element.Width) && element.Width > 0.0 ? element.Width : 320.0));
			var heightDip = Math.Max(1.0, element.ActualHeight > 0.0 ? element.ActualHeight : (!double.IsNaN(element.Height) && element.Height > 0.0 ? element.Height : 200.0));

			var dpi = VisualTreeHelper.GetDpi(element);
			var dpiX = dpi.PixelsPerInchX > 0 ? dpi.PixelsPerInchX : 96.0;
			var dpiY = dpi.PixelsPerInchY > 0 ? dpi.PixelsPerInchY : 96.0;
			var scaleX = dpiX / 96.0;
			var scaleY = dpiY / 96.0;

			var pixelWidth = Math.Min(MaxDimensionPixels, Math.Max(1, (int)Math.Ceiling(widthDip * scaleX)));
			var pixelHeight = Math.Min(MaxDimensionPixels, Math.Max(1, (int)Math.Ceiling(heightDip * scaleY)));

			// Draw through a VisualBrush at (0,0) so Window root offsets never shift the rendered client image.
			var drawingVisual = new DrawingVisual();
			using (var dc = drawingVisual.RenderOpen())
			{
				var targetRect = new Rect(0, 0, widthDip, heightDip);
				dc.DrawRectangle(FallbackBackdropBrush, null, targetRect);
				var visualBrush = new VisualBrush(element)
				{
					Stretch = Stretch.Fill,
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
				};
				dc.DrawRectangle(visualBrush, null, targetRect);
			}

			var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
			rtb.Render(drawingVisual);
			rtb.Freeze();
			return rtb;
		}

		/// <summary>
		/// Captures the full Win32 window including its native Windows OS DWM title bar and borders via <c>PrintWindow</c>
		/// (<c>PW_RENDERFULLCONTENT</c>). Returns <see langword="null"/> safely when the window has no HWND or is minimized.
		/// </summary>
		/// <param name="window">The top-level WPF window.</param>
		/// <returns>A frozen <see cref="BitmapSource"/>, or <see langword="null"/> if unavailable.</returns>
		public static BitmapSource? TryCaptureWin32DwmWindowBitmap(Window? window)
		{
			if (window == null || !window.CheckAccess())
				return null;

			var hwnd = new WindowInteropHelper(window).Handle;
			if (hwnd == IntPtr.Zero || NativeMethods.IsIconic(hwnd))
				return null;

			if (!NativeMethods.GetWindowRect(hwnd, out var rect))
				return null;

			var width = rect.Right - rect.Left;
			var height = rect.Bottom - rect.Top;
			if (width <= 2 || height <= 2 || width > MaxDimensionPixels || height > MaxDimensionPixels)
				return null;

			var windowDc = IntPtr.Zero;
			var memDc = IntPtr.Zero;
			var hBitmap = IntPtr.Zero;
			var oldObj = IntPtr.Zero;

			try
			{
				windowDc = NativeMethods.GetWindowDC(hwnd);
				if (windowDc == IntPtr.Zero)
					return null;

				memDc = NativeMethods.CreateCompatibleDC(windowDc);
				if (memDc == IntPtr.Zero)
					return null;

				hBitmap = NativeMethods.CreateCompatibleBitmap(windowDc, width, height);
				if (hBitmap == IntPtr.Zero)
					return null;

				oldObj = NativeMethods.SelectObject(memDc, hBitmap);

				var ok = NativeMethods.PrintWindow(hwnd, memDc, NativeMethods.PwRenderFullContent)
					|| NativeMethods.PrintWindow(hwnd, memDc, 0);
				if (!ok)
					return null;

				var source = Imaging.CreateBitmapSourceFromHBitmap(
					hBitmap,
					IntPtr.Zero,
					Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
				source.Freeze();
				return source;
			}
			finally
			{
				if (oldObj != IntPtr.Zero && memDc != IntPtr.Zero)
					_ = NativeMethods.SelectObject(memDc, oldObj);
				if (hBitmap != IntPtr.Zero)
					_ = NativeMethods.DeleteObject(hBitmap);
				if (memDc != IntPtr.Zero)
					_ = NativeMethods.DeleteDC(memDc);
				if (windowDc != IntPtr.Zero)
					_ = NativeMethods.ReleaseDC(hwnd, windowDc);
			}
		}

		/// <summary>
		/// Stitches one or more labeled screenshot panels vertically onto a single labeled dark-slate overview bitmap
		/// so Antigravity can inspect the main window, DWM chrome, and any floating/detached windows in one image call.
		/// </summary>
		/// <param name="panels">Ordered list of labeled bitmaps.</param>
		/// <returns>A frozen composite <see cref="BitmapSource"/>.</returns>
		public static BitmapSource BuildCompositeOverviewBitmap(IReadOnlyList<LabeledScreenshotPanel> panels)
		{
			if (panels == null || panels.Count == 0)
				throw new ArgumentException("At least one screenshot panel is required.", nameof(panels));

			var maxPanelWidth = Math.Min(
				MaxDimensionPixels - (int)(CompositePadding * 2),
				panels.Max(p => p.Bitmap.PixelWidth));

			var totalHeight = CompositePadding;
			var scaledHeights = new double[panels.Count];
			var scaledWidths = new double[panels.Count];

			for (var i = 0; i < panels.Count; i++)
			{
				var bmp = panels[i].Bitmap;
				var scale = bmp.PixelWidth > maxPanelWidth ? (double)maxPanelWidth / bmp.PixelWidth : 1.0;
				scaledWidths[i] = Math.Max(1.0, bmp.PixelWidth * scale);
				scaledHeights[i] = Math.Max(1.0, bmp.PixelHeight * scale);
				totalHeight += PanelHeaderHeight + scaledHeights[i] + CompositePadding;
			}

			var canvasWidth = Math.Min(MaxDimensionPixels, Math.Max(320, (int)Math.Ceiling(maxPanelWidth + (CompositePadding * 2))));
			var canvasHeight = Math.Min(MaxDimensionPixels, Math.Max(120, (int)Math.Ceiling(totalHeight)));

			var dv = new DrawingVisual();
			using (var dc = dv.RenderOpen())
			{
				dc.DrawRectangle(CanvasBackgroundBrush, null, new Rect(0, 0, canvasWidth, canvasHeight));

				var y = CompositePadding;
				for (var i = 0; i < panels.Count; i++)
				{
					var w = scaledWidths[i];
					var h = scaledHeights[i];
					if (y + PanelHeaderHeight + 10 > canvasHeight)
						break;

					var headerRect = new Rect(CompositePadding, y, Math.Max(220.0, w), PanelHeaderHeight);
					dc.DrawRectangle(PanelHeaderBrush, PanelBorderPen, headerRect);

					var ft = new FormattedText(
						$"[{i + 1}] {panels[i].Label} ({panels[i].Bitmap.PixelWidth}x{panels[i].Bitmap.PixelHeight}px)",
						CultureInfo.InvariantCulture,
						FlowDirection.LeftToRight,
						LabelTypeface,
						13.0,
						PanelTextBrush,
						pixelsPerDip: 1.0);
					dc.DrawText(ft, new Point(CompositePadding + 8.0, y + 6.0));

					var imgHeight = Math.Min(h, Math.Max(1.0, canvasHeight - (y + PanelHeaderHeight + CompositePadding)));
					var imgRect = new Rect(CompositePadding, y + PanelHeaderHeight, w, imgHeight);
					dc.DrawImage(panels[i].Bitmap, imgRect);
					dc.DrawRectangle(null, PanelBorderPen, imgRect);

					y += PanelHeaderHeight + imgHeight + CompositePadding;
				}
			}

			var composite = new RenderTargetBitmap(canvasWidth, canvasHeight, 96.0, 96.0, PixelFormats.Pbgra32);
			composite.Render(dv);
			composite.Freeze();
			return composite;
		}

		/// <summary>
		/// Writes a <see cref="BitmapSource"/> atomically to <paramref name="destinationPath"/> as a PNG file.
		/// </summary>
		/// <param name="bitmap">Source bitmap.</param>
		/// <param name="destinationPath">Target <c>.png</c> file path.</param>
		public static void SavePngAtomic(BitmapSource bitmap, string destinationPath)
		{
			if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

			var dir = Path.GetDirectoryName(destinationPath);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			var tempPath = destinationPath + ".tmp";
			using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				var encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bitmap));
				encoder.Save(fs);
			}

			MoveOrReplaceAtomic(tempPath, destinationPath);
		}

		/// <summary>
		/// Captures the main visual/window, optional Win32 DWM title bar, any open AvalonDock floating/detached windows,
		/// a stitched multi-window composite PNG, and the full Markdown diagnostic report into both <c>captures/</c> and <c>latest/</c>.
		/// </summary>
		/// <param name="mainElement">Main <c>ShellView</c> (or root <see cref="FrameworkElement"/> in tests).</param>
		/// <param name="themeSlug">Short identifier slug (e.g., <c>VS2022Dark</c>).</param>
		/// <param name="userNote">Optional user or agent annotation embedded at the top of <c>latest-context.md</c>.</param>
		/// <param name="includeDwmCapture">Whether to attempt Win32 <c>PrintWindow</c> DWM capture when <paramref name="mainElement"/> is a <see cref="Window"/>.</param>
		/// <param name="updateLatestFolder">Whether to also mirror outputs to <c>AgentBridge/latest/</c>.</param>
		/// <returns>The saved <see cref="ScreenshotCaptureBundle"/> metadata.</returns>
		public ScreenshotCaptureBundle CaptureSnapshotBundle(
			FrameworkElement mainElement,
			string? themeSlug = null,
			string? userNote = null,
			bool includeDwmCapture = true,
			bool updateLatestFolder = true)
		{
			if (mainElement == null) throw new ArgumentNullException(nameof(mainElement));
			if (!mainElement.CheckAccess())
				return mainElement.Dispatcher.Invoke(() => CaptureSnapshotBundle(mainElement, themeSlug, userNote, includeDwmCapture, updateLatestFolder));

			_paths.EnsureDirectoriesExist();

			var now = DateTimeOffset.Now;
			var safeSlug = SanitizeFileToken(string.IsNullOrWhiteSpace(themeSlug) ? "Snapshot" : themeSlug!);
			var captureId = string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd-HHmmssfff}-{1}", now, safeSlug);

			var panels = new List<LabeledScreenshotPanel>();

			// 1. Main WPF Visual Capture
			var mainWpfBitmap = CaptureWpfVisualBitmap(mainElement);
			var mainWpfPath = Path.Combine(_paths.CapturesDirectory, $"{captureId}-main-wpf.png");
			SavePngAtomic(mainWpfBitmap, mainWpfPath);
			panels.Add(new LabeledScreenshotPanel($"Main ShellView — WPF Visual ({safeSlug})", mainWpfBitmap));

			// 2. Main Win32 DWM Capture (includes native OS Dark/Light title bar & border)
			string? mainDwmPath = null;
			BitmapSource? mainDwmBitmap = null;
			if (includeDwmCapture && mainElement is Window mainWin)
			{
				mainDwmBitmap = TryCaptureWin32DwmWindowBitmap(mainWin);
				if (mainDwmBitmap != null)
				{
					mainDwmPath = Path.Combine(_paths.CapturesDirectory, $"{captureId}-main-dwm.png");
					SavePngAtomic(mainDwmBitmap, mainDwmPath);
					panels.Add(new LabeledScreenshotPanel($"Main ShellView — Win32 DWM Title Bar & Chrome ({safeSlug})", mainDwmBitmap));
				}
			}

			// 3. Open Floating & Detached Windows
			var secondaryPaths = new List<string>();
			var app = Application.Current;
			if (app != null && app.CheckAccess())
			{
				var secIndex = 1;
				foreach (Window win in app.Windows)
				{
					if (win == null || ReferenceEquals(win, mainElement) || !win.CheckAccess() || !win.IsVisible)
						continue;

					if (!(win is LayoutFloatingWindowControl) && !(win is DetachedAnchorableWindow) && !(win is NavigatorWindow))
						continue;

					var kindName = win.GetType().Name;
					var winTitle = string.IsNullOrWhiteSpace(win.Title) ? kindName : $"{kindName}: {win.Title}";

					var secWpf = CaptureWpfVisualBitmap(win);
					var secWpfPath = Path.Combine(_paths.CapturesDirectory, $"{captureId}-win{secIndex:D2}-wpf.png");
					SavePngAtomic(secWpf, secWpfPath);
					secondaryPaths.Add(secWpfPath);
					panels.Add(new LabeledScreenshotPanel($"{winTitle} (WPF)", secWpf));

					if (includeDwmCapture && win is DetachedAnchorableWindow)
					{
						var secDwm = TryCaptureWin32DwmWindowBitmap(win);
						if (secDwm != null)
						{
							var secDwmPath = Path.Combine(_paths.CapturesDirectory, $"{captureId}-win{secIndex:D2}-dwm.png");
							SavePngAtomic(secDwm, secDwmPath);
							secondaryPaths.Add(secDwmPath);
							panels.Add(new LabeledScreenshotPanel($"{winTitle} (Win32 DWM Title Bar)", secDwm));
						}
					}

					secIndex++;
				}
			}

			// 4. Stitched Multi-Window Overview Composite PNG
			var compositeBitmap = BuildCompositeOverviewBitmap(panels);
			var compositePath = Path.Combine(_paths.CapturesDirectory, $"{captureId}-composite.png");
			SavePngAtomic(compositeBitmap, compositePath);

			// 5. Context Markdown Report
			var contextMdPath = Path.Combine(_paths.CapturesDirectory, $"{captureId}-context.md");
			var markdown = BuildContextMarkdown(captureId, now, userNote, mainWpfPath, mainDwmPath, compositePath, secondaryPaths);
			SaveTextAtomic(markdown, contextMdPath);

			// 6. Mirror to latest/ for instant predictable Antigravity lookup
			if (updateLatestFolder)
			{
				SavePngAtomic(mainWpfBitmap, _paths.LatestMainWpfPngPath);
				if (mainDwmBitmap != null)
					SavePngAtomic(mainDwmBitmap, _paths.LatestMainDwmPngPath);
				else if (File.Exists(_paths.LatestMainDwmPngPath))
					File.Delete(_paths.LatestMainDwmPngPath);
				SavePngAtomic(compositeBitmap, _paths.LatestCompositePngPath);
				SaveTextAtomic(markdown, _paths.LatestContextMarkdownPath);
			}

			PruneArchivedCaptureSets();

			return new ScreenshotCaptureBundle(
				captureId,
				now,
				mainWpfPath,
				mainDwmPath,
				compositePath,
				contextMdPath,
				_paths.LatestCompositePngPath,
				_paths.LatestContextMarkdownPath,
				secondaryPaths);
		}

		/// <summary>
		/// Deletes oldest archived capture sets in <see cref="AgentBridgePaths.CapturesDirectory"/> exceeding <see cref="AgentBridgePaths.MaxRetainedCaptureSets"/>.
		/// </summary>
		/// <returns>Number of archived files deleted.</returns>
		public int PruneArchivedCaptureSets()
		{
			if (!Directory.Exists(_paths.CapturesDirectory))
				return 0;

			var files = new DirectoryInfo(_paths.CapturesDirectory)
				.GetFiles()
				.Where(f => !f.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
				.ToList();

			var groups = files
				.GroupBy(f => ExtractCapturePrefix(f.Name), StringComparer.OrdinalIgnoreCase)
				.OrderByDescending(g => g.Key, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (groups.Count <= _paths.MaxRetainedCaptureSets)
				return 0;

			var deleted = 0;
			foreach (var oldGroup in groups.Skip(_paths.MaxRetainedCaptureSets))
			{
				foreach (var file in oldGroup)
				{
					try
					{
						file.Delete();
						deleted++;
					}
					catch (IOException)
					{
					}
					catch (UnauthorizedAccessException)
					{
					}
				}
			}

			return deleted;
		}

		/// <summary>
		/// Deletes all archived files in <see cref="AgentBridgePaths.CapturesDirectory"/> on user request.
		/// </summary>
		/// <returns>Number of files deleted.</returns>
		public int CleanAllArchivedCaptures()
		{
			if (!Directory.Exists(_paths.CapturesDirectory))
				return 0;

			var deleted = 0;
			foreach (var file in Directory.GetFiles(_paths.CapturesDirectory))
			{
				try
				{
					File.Delete(file);
					deleted++;
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}

			return deleted;
		}

		internal static void SaveTextAtomic(string content, string destinationPath)
		{
			var dir = Path.GetDirectoryName(destinationPath);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			var tempPath = destinationPath + ".tmp";
			File.WriteAllText(tempPath, content.Replace("\r\n", "\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			MoveOrReplaceAtomic(tempPath, destinationPath);
		}

		internal static void MoveOrReplaceAtomic(string tempPath, string destinationPath)
		{
			if (File.Exists(destinationPath))
				File.Delete(destinationPath);
			File.Move(tempPath, destinationPath);
		}

		private static string ExtractCapturePrefix(string fileName)
		{
			string[] suffixes =
			{
				"-main-wpf.png",
				"-main-dwm.png",
				"-composite.png",
				"-context.md",
			};

			foreach (var suffix in suffixes)
			{
				if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
					return fileName.Substring(0, fileName.Length - suffix.Length);
			}

			var winIdx = fileName.IndexOf("-win", StringComparison.OrdinalIgnoreCase);
			if (winIdx > 0)
				return fileName.Substring(0, winIdx);

			return Path.GetFileNameWithoutExtension(fileName);
		}

		private static string SanitizeFileToken(string raw)
		{
			var sb = new StringBuilder(raw.Length);
			foreach (var ch in raw)
			{
				if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
					sb.Append(ch);
			}

			return sb.Length > 0 ? sb.ToString() : "Snapshot";
		}

		private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
		{
			var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
			brush.Freeze();
			return brush;
		}

		private static Pen CreateFrozenPen(Brush brush, double thickness)
		{
			var pen = new Pen(brush, thickness);
			pen.Freeze();
			return pen;
		}

		private string BuildContextMarkdown(
			string captureId,
			DateTimeOffset capturedAt,
			string? userNote,
			string mainWpfPath,
			string? mainDwmPath,
			string compositePath,
			IReadOnlyList<string> secondaryPaths)
		{
			var sb = new StringBuilder();
			sb.AppendLine("# Antigravity Snapshot Bundle — ThemeSuperTestApp");
			sb.AppendLine();
			sb.AppendLine($"- **Capture ID:** `{captureId}`");
			sb.AppendFormat(CultureInfo.InvariantCulture, "- **Captured At:** `{0:yyyy-MM-dd HH:mm:ss.fff zzz}`", capturedAt);
			sb.AppendLine();

			if (!string.IsNullOrWhiteSpace(userNote))
			{
				sb.AppendLine();
				sb.AppendLine("## User / Operator Note for Antigravity");
				sb.AppendLine();
				sb.AppendLine($"> {userNote!.Trim().Replace("\n", "\n> ")}");
			}

			sb.AppendLine();
			sb.AppendLine("## Captured Image Files (Inspect via `read_file`)");
			sb.AppendLine();
			sb.AppendLine($"- **Latest Composite Overview (All Panels):** `{_paths.LatestCompositePngPath}`");
			sb.AppendLine($"- **Latest Main WPF Visual:** `{_paths.LatestMainWpfPngPath}`");
			if (mainDwmPath != null)
				sb.AppendLine($"- **Latest Main Win32 DWM Window (OS Title Bar):** `{_paths.LatestMainDwmPngPath}`");
			sb.AppendLine($"- **Archived Composite:** `{compositePath}`");
			sb.AppendLine($"- **Archived Main WPF:** `{mainWpfPath}`");
			if (mainDwmPath != null)
				sb.AppendLine($"- **Archived Main DWM:** `{mainDwmPath}`");
			foreach (var sec in secondaryPaths)
				sb.AppendLine($"- **Secondary Window:** `{sec}`");

			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine(_reportExporter.BuildFullMarkdownReport().TrimEnd());

			return sb.ToString();
		}
	}
}