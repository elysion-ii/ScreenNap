using System.Globalization;
using System.Runtime.CompilerServices;

namespace ScreenNap.Logging;

// Formats every interpolated value of a log message with the invariant culture, so a log
// line reads the same whatever the user's culture (specification L10N-5)
[InterpolatedStringHandler]
internal ref struct InvariantLogMessage
{
    private DefaultInterpolatedStringHandler _inner;

    internal InvariantLogMessage(int literalLength, int formattedCount)
    {
        _inner = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture);
    }

    internal void AppendLiteral(string value) => _inner.AppendLiteral(value);

    internal void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);

    internal void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);

    internal string ToStringAndClear() => _inner.ToStringAndClear();
}
