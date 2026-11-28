using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AvalonDock.Themes.Type3New
{
	/// <summary>
	/// Parses Visual Studio 2026 JSON theme files and extracts color definitions
	/// without external JSON serializer dependencies.
	/// </summary>
	/// <remarks>
	/// Visual Studio 2026 replaced the XML <c>.vstheme</c> format with a JSON based
	/// override format. A file is a flat array of color entries, for example:
	/// <code>
	/// [
	///   { "Name": "EnvironmentBackground", "Category": "5af241b7-...", "Background": "FFCCD5F0" },
	///   { "Name": "EnvironmentHeader",     "Category": "5af241b7-...", "Background": "FFF5CC84" }
	/// ]
	/// </code>
	/// Each entry carries the classic VS color token name, an (ignored) category GUID,
	/// and an 8-digit AARRGGBB color value. Such files are <em>override only</em>: they
	/// list only the tokens that were changed, so they are normally layered on top of a
	/// base palette via <see cref="VsThemeColorPalette.Merge"/>.
	/// </remarks>
	public static class VsJsonThemeParser
	{
		/// <summary>
		/// Parses a VS2026 JSON theme from a string and returns the color palette.
		/// </summary>
		/// <param name="json">The JSON content.</param>
		/// <returns>A palette mapping color token names to their resolved colors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
		/// <exception cref="FormatException">Thrown when <paramref name="json"/> contains invalid JSON syntax or is not a JSON array.</exception>
		public static VsThemeColorPalette Parse(string json)
		{
			if (json == null)
			{
				throw new ArgumentNullException(nameof(json));
			}

			var reader = new JsonThemeReader(json);
			var entries = reader.ParseRoot();
			return Build(entries);
		}

		/// <summary>
		/// Parses a VS2026 JSON theme from a stream and returns the color palette.
		/// </summary>
		/// <param name="stream">The stream containing the JSON content.</param>
		/// <returns>A palette mapping color token names to their resolved colors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
		/// <exception cref="FormatException">Thrown when the stream contains invalid JSON syntax or is not a JSON array.</exception>
		public static VsThemeColorPalette Parse(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
			{
				return Parse(reader.ReadToEnd());
			}
		}

		/// <summary>
		/// Parses a VS2026 JSON theme from a file path and returns the color palette.
		/// </summary>
		/// <param name="filePath">The path to the JSON theme file.</param>
		/// <returns>A palette mapping color token names to their resolved colors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <see langword="null"/> or empty.</exception>
		/// <exception cref="FormatException">Thrown when the file contains invalid JSON syntax or is not a JSON array.</exception>
		public static VsThemeColorPalette ParseFile(string filePath)
		{
			if (string.IsNullOrEmpty(filePath))
			{
				throw new ArgumentNullException(nameof(filePath));
			}

			return Parse(File.ReadAllText(filePath, Encoding.UTF8));
		}

		private static VsThemeColorPalette Build(List<JsonColorEntry> entries)
		{
			var colors = new Dictionary<string, VsThemeColorEntry>();
			if (entries == null)
			{
				return new VsThemeColorPalette(colors);
			}

			foreach (var entry in entries)
			{
				if (entry == null || string.IsNullOrEmpty(entry.Name))
				{
					continue;
				}

				var bg = VsColorParser.FromArgbHex(entry.Background);
				var fg = VsColorParser.FromArgbHex(entry.Foreground);

				// Last entry wins for a duplicated token name.
				colors[entry.Name] = new VsThemeColorEntry(bg, fg);
			}

			return new VsThemeColorPalette(colors);
		}

		private sealed class JsonColorEntry
		{
			public string Name { get; set; }

			public string Background { get; set; }

			public string Foreground { get; set; }
		}

		private sealed class JsonThemeReader
		{
			private const int MaxNestingDepth = 64;

			private readonly string _json;
			private int _position;
			private int _depth;

			public JsonThemeReader(string json)
			{
				_json = json;
				if (_json.Length > 0 && _json[0] == '\uFEFF')
				{
					_position = 1;
				}
			}

			private bool IsAtEnd => _position >= _json.Length;

			public List<JsonColorEntry> ParseRoot()
			{
				SkipTrivia();
				if (IsAtEnd)
				{
					throw CreateFormatException("JSON theme content is empty.");
				}

				if (Peek() == 'n')
				{
					ConsumeLiteral("null");
					EnsureEndOfContent();
					return null;
				}

				ExpectChar('[');
				var entries = ReadRootArrayElements();
				EnsureEndOfContent();
				return entries;
			}

			private void EnsureEndOfContent()
			{
				SkipTrivia();
				if (!IsAtEnd)
				{
					throw CreateFormatException($"Unexpected character '{Peek()}' after root JSON value.");
				}
			}

			private List<JsonColorEntry> ReadRootArrayElements()
			{
				var list = new List<JsonColorEntry>();
				SkipTrivia();
				if (TryConsumeChar(']'))
				{
					return list;
				}

				while (true)
				{
					list.Add(ReadRootElement());
					SkipTrivia();
					if (TryConsumeChar(']'))
					{
						return list;
					}

					ExpectChar(',');
					SkipTrivia();
					if (TryConsumeChar(']'))
					{
						return list;
					}
				}
			}

			private JsonColorEntry ReadRootElement()
			{
				if (IsAtEnd)
				{
					throw CreateFormatException("Unexpected end of JSON inside root array.");
				}

				var ch = Peek();
				if (ch == 'n')
				{
					ConsumeLiteral("null");
					return null;
				}

				if (ch == '{')
				{
					return ReadEntryObject();
				}

				throw CreateFormatException($"Expected JSON object '{{' in theme array, but found '{ch}'.");
			}

			private JsonColorEntry ReadEntryObject()
			{
				ExpectChar('{');
				var entry = new JsonColorEntry();
				SkipTrivia();
				if (TryConsumeChar('}'))
				{
					return entry;
				}

				while (true)
				{
					ReadEntryProperty(entry);
					SkipTrivia();
					if (TryConsumeChar('}'))
					{
						return entry;
					}

					ExpectChar(',');
					SkipTrivia();
					if (TryConsumeChar('}'))
					{
						return entry;
					}
				}
			}

			private void ReadEntryProperty(JsonColorEntry entry)
			{
				var propertyName = ReadString();
				SkipTrivia();
				ExpectChar(':');
				SkipTrivia();

				if (string.Equals(propertyName, "Name", StringComparison.OrdinalIgnoreCase))
				{
					entry.Name = ReadNullableString();
				}
				else if (string.Equals(propertyName, "Background", StringComparison.OrdinalIgnoreCase))
				{
					entry.Background = ReadNullableString();
				}
				else if (string.Equals(propertyName, "Foreground", StringComparison.OrdinalIgnoreCase))
				{
					entry.Foreground = ReadNullableString();
				}
				else
				{
					SkipValue();
				}
			}

			private string ReadNullableString()
			{
				if (!IsAtEnd && Peek() == 'n')
				{
					ConsumeLiteral("null");
					return null;
				}

				return ReadString();
			}

			private string ReadString()
			{
				ExpectChar('"');
				var start = _position;

				while (!IsAtEnd)
				{
					var ch = _json[_position];
					if (ch == '"')
					{
						var value = _json.Substring(start, _position - start);
						_position++;
						return value;
					}

					if (ch == '\\')
					{
						return ReadEscapedString(start);
					}

					if (ch < ' ')
					{
						throw CreateFormatException("Unescaped control character in JSON string.");
					}

					_position++;
				}

				throw CreateFormatException("Unterminated JSON string.");
			}

			private string ReadEscapedString(int unescapedStart)
			{
				var sb = new StringBuilder();
				if (_position > unescapedStart)
				{
					sb.Append(_json, unescapedStart, _position - unescapedStart);
				}

				while (!IsAtEnd)
				{
					var ch = _json[_position++];
					if (ch == '"')
					{
						return sb.ToString();
					}

					if (ch == '\\')
					{
						sb.Append(ReadEscapeSequence());
					}
					else if (ch < ' ')
					{
						throw CreateFormatException("Unescaped control character in JSON string.");
					}
					else
					{
						sb.Append(ch);
					}
				}

				throw CreateFormatException("Unterminated JSON string.");
			}

			private char ReadEscapeSequence()
			{
				if (IsAtEnd)
				{
					throw CreateFormatException("Unterminated escape sequence in JSON string.");
				}

				var escaped = _json[_position++];
				switch (escaped)
				{
					case '"':
						return '"';
					case '\\':
						return '\\';
					case '/':
						return '/';
					case 'b':
						return '\b';
					case 'f':
						return '\f';
					case 'n':
						return '\n';
					case 'r':
						return '\r';
					case 't':
						return '\t';
					case 'u':
						return ReadUnicodeEscape();
					default:
						throw CreateFormatException($"Invalid JSON escape sequence '\\{escaped}'.");
				}
			}

			private char ReadUnicodeEscape()
			{
				if (_position + 4 > _json.Length)
				{
					throw CreateFormatException("Incomplete '\\uXXXX' Unicode escape sequence.");
				}

				var codeUnit = 0;
				for (var i = 0; i < 4; i++)
				{
					var digit = ParseHexDigit(_json[_position + i]);
					if (digit < 0)
					{
						throw CreateFormatException("Invalid hex digit in '\\uXXXX' Unicode escape sequence.");
					}

					codeUnit = (codeUnit << 4) | digit;
				}

				_position += 4;
				return (char)codeUnit;
			}

			private static int ParseHexDigit(char ch)
			{
				if (ch >= '0' && ch <= '9')
				{
					return ch - '0';
				}

				if (ch >= 'a' && ch <= 'f')
				{
					return ch - 'a' + 10;
				}

				if (ch >= 'A' && ch <= 'F')
				{
					return ch - 'A' + 10;
				}

				return -1;
			}

			private void SkipValue()
			{
				if (IsAtEnd)
				{
					throw CreateFormatException("Unexpected end of JSON while reading property value.");
				}

				var ch = Peek();
				if (ch == '"')
				{
					_ = ReadString();
				}
				else if (ch == '{')
				{
					SkipNestedObject();
				}
				else if (ch == '[')
				{
					SkipNestedArray();
				}
				else if (ch == 't')
				{
					ConsumeLiteral("true");
				}
				else if (ch == 'f')
				{
					ConsumeLiteral("false");
				}
				else if (ch == 'n')
				{
					ConsumeLiteral("null");
				}
				else if (ch == '-' || IsAsciiDigit(ch))
				{
					SkipNumber();
				}
				else
				{
					throw CreateFormatException($"Unexpected token '{ch}' in JSON value.");
				}
			}

			private void SkipNestedObject()
			{
				EnterDepth();
				try
				{
					ExpectChar('{');
					SkipTrivia();
					if (TryConsumeChar('}'))
					{
						return;
					}

					while (true)
					{
						_ = ReadString();
						SkipTrivia();
						ExpectChar(':');
						SkipTrivia();
						SkipValue();
						SkipTrivia();
						if (TryConsumeChar('}'))
						{
							return;
						}

						ExpectChar(',');
						SkipTrivia();
						if (TryConsumeChar('}'))
						{
							return;
						}
					}
				}
				finally
				{
					_depth--;
				}
			}

			private void SkipNestedArray()
			{
				EnterDepth();
				try
				{
					ExpectChar('[');
					SkipTrivia();
					if (TryConsumeChar(']'))
					{
						return;
					}

					while (true)
					{
						SkipValue();
						SkipTrivia();
						if (TryConsumeChar(']'))
						{
							return;
						}

						ExpectChar(',');
						SkipTrivia();
						if (TryConsumeChar(']'))
						{
							return;
						}
					}
				}
				finally
				{
					_depth--;
				}
			}

			private void EnterDepth()
			{
				_depth++;
				if (_depth > MaxNestingDepth)
				{
					throw CreateFormatException("JSON maximum nesting depth exceeded.");
				}
			}

			private void SkipNumber()
			{
				if (Peek() == '-')
				{
					_position++;
				}

				if (IsAtEnd || !IsAsciiDigit(Peek()))
				{
					throw CreateFormatException("Invalid JSON number.");
				}

				if (Peek() == '0')
				{
					_position++;
					if (!IsAtEnd && IsAsciiDigit(Peek()))
					{
						throw CreateFormatException("Leading zeros are not permitted in JSON numbers.");
					}
				}
				else
				{
					ConsumeDigits();
				}

				if (!IsAtEnd && Peek() == '.')
				{
					_position++;
					if (IsAtEnd || !IsAsciiDigit(Peek()))
					{
						throw CreateFormatException("Invalid fractional part in JSON number.");
					}

					ConsumeDigits();
				}

				SkipExponentPart();
			}

			private void SkipExponentPart()
			{
				if (IsAtEnd || (Peek() != 'e' && Peek() != 'E'))
				{
					return;
				}

				_position++;
				if (!IsAtEnd && (Peek() == '+' || Peek() == '-'))
				{
					_position++;
				}

				if (IsAtEnd || !IsAsciiDigit(Peek()))
				{
					throw CreateFormatException("Invalid exponent in JSON number.");
				}

				ConsumeDigits();
			}

			private void ConsumeDigits()
			{
				while (!IsAtEnd && IsAsciiDigit(_json[_position]))
				{
					_position++;
				}
			}

			private static bool IsAsciiDigit(char ch) => ch >= '0' && ch <= '9';

			private void ConsumeLiteral(string literal)
			{
				if (_position + literal.Length > _json.Length ||
					string.CompareOrdinal(_json, _position, literal, 0, literal.Length) != 0)
				{
					throw CreateFormatException($"Expected JSON literal '{literal}'.");
				}

				var nextIndex = _position + literal.Length;
				if (nextIndex < _json.Length && IsIdentifierContinuation(_json[nextIndex]))
				{
					throw CreateFormatException($"Invalid token following JSON literal '{literal}'.");
				}

				_position = nextIndex;
			}

			private static bool IsIdentifierContinuation(char ch)
			{
				return (ch >= 'a' && ch <= 'z') ||
					(ch >= 'A' && ch <= 'Z') ||
					IsAsciiDigit(ch) ||
					ch == '_';
			}

			private void SkipTrivia()
			{
				while (!IsAtEnd)
				{
					var ch = _json[_position];
					if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
					{
						_position++;
					}
					else if (ch == '/')
					{
						SkipComment();
					}
					else
					{
						return;
					}
				}
			}

			private void SkipComment()
			{
				if (_position + 1 >= _json.Length)
				{
					throw CreateFormatException("Unexpected '/' character.");
				}

				var next = _json[_position + 1];
				if (next == '/')
				{
					_position += 2;
					while (!IsAtEnd && _json[_position] != '\n' && _json[_position] != '\r')
					{
						_position++;
					}
				}
				else if (next == '*')
				{
					_position += 2;
					while (_position + 1 < _json.Length)
					{
						if (_json[_position] == '*' && _json[_position + 1] == '/')
						{
							_position += 2;
							return;
						}

						_position++;
					}

					throw CreateFormatException("Unterminated JSON block comment.");
				}
				else
				{
					throw CreateFormatException("Unexpected '/' character.");
				}
			}

			private char Peek() => _json[_position];

			private bool TryConsumeChar(char expected)
			{
				if (!IsAtEnd && _json[_position] == expected)
				{
					_position++;
					return true;
				}

				return false;
			}

			private void ExpectChar(char expected)
			{
				if (IsAtEnd)
				{
					throw CreateFormatException($"Unexpected end of JSON; expected '{expected}'.");
				}

				if (_json[_position] != expected)
				{
					throw CreateFormatException($"Expected '{expected}', but found '{_json[_position]}'.");
				}

				_position++;
			}

			private FormatException CreateFormatException(string message)
			{
				var limit = Math.Min(_position, _json.Length);
				var line = 1;
				var column = 1;

				for (var i = 0; i < limit; i++)
				{
					var ch = _json[i];
					if (ch == '\r')
					{
						line++;
						column = 1;
						if (i + 1 < limit && _json[i + 1] == '\n')
						{
							i++;
						}
					}
					else if (ch == '\n')
					{
						line++;
						column = 1;
					}
					else
					{
						column++;
					}
				}

				return new FormatException($"{message} (Line {line}, Column {column}).");
			}
		}
	}
}