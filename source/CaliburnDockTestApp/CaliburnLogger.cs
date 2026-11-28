using Caliburn.Micro;
using System;
using System.Diagnostics;
using System.Globalization;

namespace CaliburnDockTestApp
{
	class CaliburnLogger : ILog
	{
		private readonly Type _type;

		public CaliburnLogger(Type type)
		{
			_type = type;
		}

		public void Error(Exception exception)
		{
			Trace.TraceError($"{_type.Name}: {exception}");
		}

		public void Info(string format, params object[] args)
		{
			Trace.TraceInformation($"{_type.Name}: {FormatMessage(format, args)}");
		}

		public void Warn(string format, params object[] args)
		{
			Trace.TraceWarning($"{_type.Name}: {FormatMessage(format, args)}");
		}

		private static string FormatMessage(string format, object[] args)
		{
			if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
				return format ?? string.Empty;

			try
			{
				return string.Format(CultureInfo.InvariantCulture, format, args);
			}
			catch (FormatException)
			{
				return format;
			}
		}
	}
}