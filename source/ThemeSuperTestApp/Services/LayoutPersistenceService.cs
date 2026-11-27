using System;
using System.IO;
using System.Xml;
using AvalonDock.Core;
using AvalonDock.Serializer.Xml;
using Caliburn.Micro;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Saves, restores, and resets the AvalonDock layout with <see cref="XmlLayoutSerializer"/>.
	/// </summary>
	public sealed class LayoutPersistenceService
	{
		private IDockingManager? _manager;
		private string? _defaultLayout;

		/// <summary>
		/// Initializes a new instance of the <see cref="LayoutPersistenceService"/> class using
		/// <c>%LOCALAPPDATA%\ThemeSuperTestApp\layout.xml</c>.
		/// </summary>
		public LayoutPersistenceService()
			: this(Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ThemeSuperTestApp",
				"layout.xml"))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LayoutPersistenceService"/> class.
		/// </summary>
		/// <param name="layoutFilePath">The file path the layout is saved to.</param>
		public LayoutPersistenceService(string layoutFilePath)
		{
			LayoutFilePath = layoutFilePath ?? throw new ArgumentNullException(nameof(layoutFilePath));
		}

		/// <summary>Gets the file path the layout is saved to.</summary>
		public string LayoutFilePath { get; }

		/// <summary>Gets a value indicating whether a docking manager is attached.</summary>
		public bool IsAttached => _manager != null;

		/// <summary>Gets a value indicating whether a saved layout exists.</summary>
		public bool HasSavedLayout => File.Exists(LayoutFilePath);

		/// <summary>
		/// Attaches the docking manager whose layout is persisted.
		/// </summary>
		/// <param name="manager">The docking manager.</param>
		public void Attach(IDockingManager manager)
		{
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
		}

		/// <summary>
		/// Captures the current layout in memory as the factory default used by <see cref="TryReset"/>.
		/// </summary>
		public void CaptureDefaultLayout()
		{
			if (_manager == null)
				return;

			using var writer = new StringWriter();
			new XmlLayoutSerializer(_manager).Serialize(writer);
			_defaultLayout = writer.ToString();
		}

		/// <summary>
		/// Saves the current layout to <see cref="LayoutFilePath"/>.
		/// </summary>
		/// <returns><see langword="true"/> on success.</returns>
		public bool Save()
		{
			if (_manager == null)
				return false;

			try
			{
				var directory = Path.GetDirectoryName(LayoutFilePath);
				if (!string.IsNullOrEmpty(directory))
					Directory.CreateDirectory(directory);

				new XmlLayoutSerializer(_manager).Serialize(LayoutFilePath);
				return true;
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				LogManager.GetLog(typeof(LayoutPersistenceService)).Error(ex);
				return false;
			}
		}

		/// <summary>
		/// Restores the layout saved in <see cref="LayoutFilePath"/>.
		/// </summary>
		/// <param name="resolveContent">Returns the ViewModel for an unresolved <c>ContentId</c>, or <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if a layout was restored.</returns>
		public bool TryLoad(Func<string, object?> resolveContent)
		{
			if (_manager == null || !HasSavedLayout)
				return false;

			try
			{
				using var reader = new StreamReader(LayoutFilePath);
				CreateSerializer(_manager, resolveContent).Deserialize(reader);
				return true;
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException || ex is InvalidOperationException)
			{
				LogManager.GetLog(typeof(LayoutPersistenceService)).Error(ex);
				return false;
			}
		}

		/// <summary>
		/// Restores the layout captured by <see cref="CaptureDefaultLayout"/>.
		/// </summary>
		/// <param name="resolveContent">Returns the ViewModel for an unresolved <c>ContentId</c>, or <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the default layout was restored.</returns>
		public bool TryReset(Func<string, object?> resolveContent)
		{
			if (_manager == null || _defaultLayout == null)
				return false;

			using var reader = new StringReader(_defaultLayout);
			CreateSerializer(_manager, resolveContent).Deserialize(reader);
			return true;
		}

		private static XmlLayoutSerializer CreateSerializer(IDockingManager manager, Func<string, object?> resolveContent)
		{
			if (resolveContent == null) throw new ArgumentNullException(nameof(resolveContent));

			var serializer = new XmlLayoutSerializer(manager);
			serializer.LayoutSerializationCallback += (_, args) =>
			{
				if (args.Content == null && !string.IsNullOrEmpty(args.Model.ContentId))
					args.Content = resolveContent(args.Model.ContentId);
			};
			return serializer;
		}
	}
}