using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Single labeled bitmap panel included in a multi-window composite overview image.
	/// </summary>
	public sealed class LabeledScreenshotPanel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledScreenshotPanel"/> class.
		/// </summary>
		/// <param name="label">Caption rendered above the bitmap panel.</param>
		/// <param name="bitmap">Frozen captured bitmap.</param>
		public LabeledScreenshotPanel(string label, BitmapSource bitmap)
		{
			Label = label ?? throw new ArgumentNullException(nameof(label));
			Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
		}

		/// <summary>Gets the panel caption.</summary>
		public string Label { get; }

		/// <summary>Gets the frozen bitmap image.</summary>
		public BitmapSource Bitmap { get; }
	}

	/// <summary>
	/// Metadata and file paths produced by a dual-mode <c>ThemeSuperTestApp</c> screenshot + Markdown context snapshot.
	/// </summary>
	public sealed class ScreenshotCaptureBundle
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenshotCaptureBundle"/> class.
		/// </summary>
		/// <param name="captureId">Unique timestamp/request prefix.</param>
		/// <param name="capturedAt">Capture timestamp.</param>
		/// <param name="mainWpfPngPath">Archived main WPF visual PNG path.</param>
		/// <param name="mainDwmPngPath">Archived Win32 DWM window PNG path, or <see langword="null"/> when off-screen/minimized.</param>
		/// <param name="compositePngPath">Archived multi-window composite overview PNG path.</param>
		/// <param name="contextMarkdownPath">Archived context Markdown report path.</param>
		/// <param name="latestCompositePngPath">Fixed <c>latest/latest-composite.png</c> path.</param>
		/// <param name="latestContextMarkdownPath">Fixed <c>latest/latest-context.md</c> path.</param>
		/// <param name="secondaryWindowPngPaths">Archived PNG paths of any open floating or detached windows.</param>
		public ScreenshotCaptureBundle(
			string captureId,
			DateTimeOffset capturedAt,
			string mainWpfPngPath,
			string? mainDwmPngPath,
			string compositePngPath,
			string contextMarkdownPath,
			string latestCompositePngPath,
			string latestContextMarkdownPath,
			IReadOnlyList<string> secondaryWindowPngPaths)
		{
			CaptureId = captureId ?? throw new ArgumentNullException(nameof(captureId));
			CapturedAt = capturedAt;
			MainWpfPngPath = mainWpfPngPath ?? throw new ArgumentNullException(nameof(mainWpfPngPath));
			MainDwmPngPath = mainDwmPngPath;
			CompositePngPath = compositePngPath ?? throw new ArgumentNullException(nameof(compositePngPath));
			ContextMarkdownPath = contextMarkdownPath ?? throw new ArgumentNullException(nameof(contextMarkdownPath));
			LatestCompositePngPath = latestCompositePngPath ?? throw new ArgumentNullException(nameof(latestCompositePngPath));
			LatestContextMarkdownPath = latestContextMarkdownPath ?? throw new ArgumentNullException(nameof(latestContextMarkdownPath));
			SecondaryWindowPngPaths = secondaryWindowPngPaths ?? Array.Empty<string>();
		}

		/// <summary>Gets the unique capture identifier.</summary>
		public string CaptureId { get; }

		/// <summary>Gets when the snapshot was captured.</summary>
		public DateTimeOffset CapturedAt { get; }

		/// <summary>Gets the archived WPF <c>RenderTargetBitmap</c> PNG path for the main shell.</summary>
		public string MainWpfPngPath { get; }

		/// <summary>Gets the archived Win32 <c>PrintWindow</c> (DWM title-bar) PNG path, if captured.</summary>
		public string? MainDwmPngPath { get; }

		/// <summary>Gets the archived stitched composite PNG path.</summary>
		public string CompositePngPath { get; }

		/// <summary>Gets the archived context Markdown file path.</summary>
		public string ContextMarkdownPath { get; }

		/// <summary>Gets the fixed <c>latest/latest-composite.png</c> path for Antigravity vision inspection.</summary>
		public string LatestCompositePngPath { get; }

		/// <summary>Gets the fixed <c>latest/latest-context.md</c> path for Antigravity telemetry inspection.</summary>
		public string LatestContextMarkdownPath { get; }

		/// <summary>Gets the archived PNG paths of any open floating or detached windows.</summary>
		public IReadOnlyList<string> SecondaryWindowPngPaths { get; }
	}
}