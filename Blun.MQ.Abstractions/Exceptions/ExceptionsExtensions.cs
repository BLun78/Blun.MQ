using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Exceptions
{
    public static class ExceptionsExtensions
    {
        public static string FlattenException(this Exception exception)
        {
            var st = new StackTrace(exception, true);
            var frames = st.GetFrames();
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("An error is thrown!");
            
            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("Stacktrace: ");
            foreach (var frame in frames)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                stringBuilder.Append("File: " + frame.GetFileName());
                stringBuilder.Append(", Method:" + frame.GetMethod().Name);
                stringBuilder.Append(value: $", LineNumber: {frame.GetFileLineNumber().ToString(CultureInfo.InvariantCulture)}" );
                stringBuilder.Append("  -->  ");
            }

            return stringBuilder.ToString();
        }
    }
}