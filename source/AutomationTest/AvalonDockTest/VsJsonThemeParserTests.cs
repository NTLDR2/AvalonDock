using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using AvalonDock.Themes.Type3New;
using NUnit.Framework;

namespace AvalonDockTest
{
	[TestFixture]
	public class VsJsonThemeParserTests
	{
		// The override sample from the VS blog "Make Visual Studio look the way you want".
		private const string CoolBreezeJson = @"[
  { ""Name"": ""EnvironmentHeader"", ""Category"": ""5af241b7-5627-4d12-bfb1-2b67d11127d7"", ""Background"": ""FFF5CC84"" },
  { ""Name"": ""EnvironmentTab"", ""Category"": ""5af241b7-5627-4d12-bfb1-2b67d11127d7"", ""Background"": ""FFF5CC84"" },
  { ""Name"": ""EnvironmentBody"", ""Category"": ""5af241b7-5627-4d12-bfb1-2b67d11127d7"", ""Background"": ""FF5D6B99"" },
  { ""Name"": ""EnvironmentBodyText"", ""Category"": ""5af241b7-5627-4d12-bfb1-2b67d11127d7"", ""Background"": ""E4FFFFFF"" },
  { ""Name"": ""EnvironmentBackground"", ""Category"": ""5af241b7-5627-4d12-bfb1-2b67d11127d7"", ""Background"": ""FFCCD5F0"" },
  { ""Name"": ""StatusBarBackgroundFillRest"", ""Category"": ""5af241b7-5627-4d12-bfb1-2b67d11127d7"", ""Background"": ""FF40508D"" }
]";

		[Test]
		public void Parse_ReadsBackgroundColors()
		{
			var palette = VsJsonThemeParser.Parse(CoolBreezeJson);

			Assert.That(palette.GetBackground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0xF5, 0xCC, 0x84)));
			Assert.That(palette.GetBackground("EnvironmentBackground"), Is.EqualTo(Color.FromArgb(0xFF, 0xCC, 0xD5, 0xF0)));
		}

		[Test]
		public void Parse_HonorsAlphaChannel()
		{
			var palette = VsJsonThemeParser.Parse(CoolBreezeJson);

			Assert.That(palette.GetBackground("EnvironmentBodyText"), Is.EqualTo(Color.FromArgb(0xE4, 0xFF, 0xFF, 0xFF)));
		}

		[Test]
		public void Parse_MissingTokenReturnsNull()
		{
			var palette = VsJsonThemeParser.Parse(CoolBreezeJson);

			Assert.That(palette.GetBackground("NonExistent"), Is.Null);
		}

		[Test]
		public void Parse_EntryWithoutBackgroundHasNullBackground()
		{
			var palette = VsJsonThemeParser.Parse(@"[ { ""Name"": ""Foo"", ""Category"": ""x"" } ]");

			Assert.That(palette.Contains("Foo"), Is.True);
			Assert.That(palette.GetBackground("Foo"), Is.Null);
		}

		[Test]
		public void Parse_IsCaseInsensitiveOnPropertyNames()
		{
			var palette = VsJsonThemeParser.Parse(@"[ { ""name"": ""Foo"", ""background"": ""FF112233"", ""fOrEgRoUnD"": ""AA445566"" } ]");

			Assert.That(palette.GetBackground("Foo"), Is.EqualTo(Color.FromArgb(0xFF, 0x11, 0x22, 0x33)));
			Assert.That(palette.GetForeground("Foo"), Is.EqualTo(Color.FromArgb(0xAA, 0x44, 0x55, 0x66)));
		}

		[Test]
		public void Parse_SupportsSingleLineAndMultiLineComments()
		{
			const string jsonWithComments = @"// Leading single-line comment
/* Multi-line block comment
   with * asterisks * inside */
[
  // Comment before entry
  {
    ""Name"": ""EnvironmentHeader"", /* inline block comment */ ""Background"": ""FF112233"" // end-of-line comment
  }
] // EOF comment without trailing newline";

			var palette = VsJsonThemeParser.Parse(jsonWithComments);

			Assert.That(palette.GetBackground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0x11, 0x22, 0x33)));
		}

		[Test]
		public void Parse_SupportsTrailingCommasInObjectsAndRootArray()
		{
			const string json = @"[
  {
    ""Name"": ""EnvironmentTab"",
    ""Background"": ""FF252526"",
  },
]";

			var palette = VsJsonThemeParser.Parse(json);

			Assert.That(palette.GetBackground("EnvironmentTab"), Is.EqualTo(Color.FromArgb(0xFF, 0x25, 0x25, 0x26)));
		}

		[Test]
		public void Parse_HandlesLeadingUtf8Bom()
		{
			var json = "\uFEFF[ { \"Name\": \"FileTabSelectedBorder\", \"Background\": \"FF0E639C\" } ]";

			var palette = VsJsonThemeParser.Parse(json);

			Assert.That(palette.GetBackground("FileTabSelectedBorder"), Is.EqualTo(Color.FromArgb(0xFF, 0x0E, 0x63, 0x9C)));
		}

		[Test]
		public void Parse_DecodesStandardAndUnicodeStringEscapes()
		{
			const string json = @"[
  {
    ""Name"": ""Environment\u0048eader"",
    ""Category"": ""escaped \"" \\ \/ \b \f \n \r \t \u003A"",
    ""Background"": ""FF\u0032\u00442D30""
  }
]";

			var palette = VsJsonThemeParser.Parse(json);

			Assert.That(palette.GetBackground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0x2D, 0x2D, 0x30)));
		}

		[Test]
		public void Parse_SkipsUnknownNestedObjectsArraysPrimitivesAndNullEntries()
		{
			const string json = @"[
  null,
  {},
  {
    ""Name"": ""EnvironmentHeader"",
    ""Background"": ""FF2D2D30"",
    ""Foreground"": null,
    ""Version"": 2,
    ""Opacity"": -0.75e+1,
    ""IsHighContrast"": false,
    ""IsDefault"": true,
    ""ExtraNull"": null,
    ""Metadata"": {
      ""Author"": ""Contoso"",
      ""Tags"": [ ""dark"", ""fluent"", 42, { ""Deep"": [ true, false, null ], }, ],
    }
  }
]";

			var palette = VsJsonThemeParser.Parse(json);

			Assert.That(palette.Count, Is.EqualTo(1));
			Assert.That(palette.GetBackground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0x2D, 0x2D, 0x30)));
			Assert.That(palette.GetForeground("EnvironmentHeader"), Is.Null);
		}

		[Test]
		public void Parse_DuplicateTokenName_LastEntryWins()
		{
			const string json = @"[
  { ""Name"": ""EnvironmentHeader"", ""Background"": ""FF111111"", ""Foreground"": ""FF222222"" },
  { ""Name"": ""EnvironmentHeader"", ""Background"": ""FF333333"", ""Foreground"": ""FF444444"" }
]";

			var palette = VsJsonThemeParser.Parse(json);

			Assert.That(palette.Count, Is.EqualTo(1));
			Assert.That(palette.GetBackground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0x33, 0x33, 0x33)));
			Assert.That(palette.GetForeground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0x44, 0x44, 0x44)));
		}

		[Test]
		public void Parse_NullLiteralAndEmptyArrayRoot_ReturnEmptyPalette()
		{
			var fromNull = VsJsonThemeParser.Parse(" /* comment */ null ");
			var fromEmpty = VsJsonThemeParser.Parse(" [ /* empty */ ] ");

			Assert.That(fromNull.Count, Is.EqualTo(0));
			Assert.That(fromEmpty.Count, Is.EqualTo(0));
		}

		[Test]
		public void Parse_StreamAndParseFile_ReadUtf8Content()
		{
			const string json = @"[ { ""Name"": ""EnvironmentTab"", ""Background"": ""FF252526"" } ]";

			// Stream with UTF-8 BOM
			var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
			var preamble = utf8WithBom.GetPreamble();
			var body = utf8WithBom.GetBytes(json);
			var combined = new byte[preamble.Length + body.Length];
			Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
			Buffer.BlockCopy(body, 0, combined, preamble.Length, body.Length);

			using (var stream = new MemoryStream(combined))
			{
				var streamPalette = VsJsonThemeParser.Parse(stream);
				Assert.That(streamPalette.GetBackground("EnvironmentTab"), Is.EqualTo(Color.FromArgb(0xFF, 0x25, 0x25, 0x26)));
			}

			var tempFile = Path.Combine(Path.GetTempPath(), $"avalondock-vsjson-{Guid.NewGuid():N}.json");
			try
			{
				File.WriteAllText(tempFile, json, utf8WithBom);
				var filePalette = VsJsonThemeParser.ParseFile(tempFile);
				Assert.That(filePalette.GetBackground("EnvironmentTab"), Is.EqualTo(Color.FromArgb(0xFF, 0x25, 0x25, 0x26)));
			}
			finally
			{
				if (File.Exists(tempFile))
				{
					File.Delete(tempFile);
				}
			}
		}

		[Test]
		public void Parse_NullOrEmptyArguments_ThrowArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => { _ = VsJsonThemeParser.Parse((string)null); });
			Assert.Throws<ArgumentNullException>(() => { _ = VsJsonThemeParser.Parse((Stream)null); });
			Assert.Throws<ArgumentNullException>(() => { _ = VsJsonThemeParser.ParseFile(null); });
			Assert.Throws<ArgumentNullException>(() => { _ = VsJsonThemeParser.ParseFile(string.Empty); });
		}

		[TestCase("")]
		[TestCase("   ")]
		[TestCase("{}")]
		[TestCase("123")]
		[TestCase("\"not-an-array\"")]
		[TestCase("[,]")]
		[TestCase("[{\"Name\":\"A\",,}]")]
		[TestCase("[{\"Name\":123}]")]
		[TestCase("[{\"Background\":true}]")]
		[TestCase("[{\"Name\":\"unterminated]")]
		[TestCase("[{\"Name\":\"bad\\q\"}]")]
		[TestCase("[{\"Name\":\"bad\\u12G4\"}]")]
		[TestCase("[{\"Name\":\"bad\\u12\"}]")]
		[TestCase("[{\"Name\":\"raw\nnewline\"}]")]
		[TestCase("/* unclosed block comment")]
		[TestCase("/ invalid-slash")]
		[TestCase("[] trailing")]
		[TestCase("[{\"Num\":01}]")]
		[TestCase("[{\"Num\":1.}]")]
		[TestCase("[{\"Num\":1e}]")]
		[TestCase("[{\"Lit\":truX}]")]
		public void Parse_MalformedJson_ThrowsFormatExceptionWithPosition(string invalidJson)
		{
			var ex = Assert.Throws<FormatException>(() => { _ = VsJsonThemeParser.Parse(invalidJson); });

			Assert.That(ex, Is.Not.Null);
			Assert.That(ex!.Message, Does.Match(@"\(Line \d+, Column \d+\)"));
		}

		[Test]
		public void Parse_MultilineMalformedJson_ReportsAccurateLineNumber()
		{
			const string multilineInvalid = "[\r\n  { \"Name\": \"Ok\", \"Background\": \"FF000000\" },\r\n  { \"Name\": 42 }\r\n]";

			var ex = Assert.Throws<FormatException>(() => { _ = VsJsonThemeParser.Parse(multilineInvalid); });

			Assert.That(ex, Is.Not.Null);
			Assert.That(ex!.Message, Does.Contain("Line 3,"));
		}

		[Test]
		public void Merge_OverridesReplaceBaseEntries()
		{
			var basePalette = MakePalette(
				("EnvironmentBackground", Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E)),
				("FileTabSelectedBorder", Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC)));

			var overrides = VsJsonThemeParser.Parse(CoolBreezeJson);
			var merged = basePalette.Merge(overrides);

			// Overridden token takes the new value.
			Assert.That(merged.GetBackground("EnvironmentBackground"), Is.EqualTo(Color.FromArgb(0xFF, 0xCC, 0xD5, 0xF0)));
			// Untouched base token is preserved.
			Assert.That(merged.GetBackground("FileTabSelectedBorder"), Is.EqualTo(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC)));
			// New token introduced by the override is added.
			Assert.That(merged.GetBackground("EnvironmentHeader"), Is.EqualTo(Color.FromArgb(0xFF, 0xF5, 0xCC, 0x84)));
		}

		[Test]
		public void Merge_PreservesBaseForegroundWhenOverrideOnlySetsBackground()
		{
			var baseColors = new Dictionary<string, VsThemeColorEntry>
			{
				["TitleBarActiveText"] = new VsThemeColorEntry(
					Color.FromArgb(0xFF, 0x00, 0x00, 0x00),
					Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)),
			};
			var basePalette = new VsThemeColorPalette(baseColors);

			var overrides = VsJsonThemeParser.Parse(@"[ { ""Name"": ""TitleBarActiveText"", ""Background"": ""FF112233"" } ]");
			var merged = basePalette.Merge(overrides);

			Assert.That(merged.GetBackground("TitleBarActiveText"), Is.EqualTo(Color.FromArgb(0xFF, 0x11, 0x22, 0x33)));
			Assert.That(merged.GetForeground("TitleBarActiveText"), Is.EqualTo(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)));
		}

		[Test]
		public void Merge_DoesNotModifyInputs()
		{
			var basePalette = MakePalette(("EnvironmentBackground", Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E)));
			var overrides = MakePalette(("EnvironmentBackground", Color.FromArgb(0xFF, 0xCC, 0xD5, 0xF0)));

			basePalette.Merge(overrides);

			Assert.That(basePalette.GetBackground("EnvironmentBackground"), Is.EqualTo(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E)));
			Assert.That(basePalette.Count, Is.EqualTo(1));
		}

		private static VsThemeColorPalette MakePalette(params (string Name, Color Background)[] entries)
		{
			var colors = new Dictionary<string, VsThemeColorEntry>();
			foreach (var (name, background) in entries)
			{
				colors[name] = new VsThemeColorEntry(background, null);
			}

			return new VsThemeColorPalette(colors);
		}
	}
}
