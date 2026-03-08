using System.Text.Json.Serialization;

namespace LeveLEO.Infrastructure.Common;

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>(T? value)
{
    public bool HasValue { get; } = true;
    public T? Value { get; } = value;

    public static implicit operator Optional<T>(T? value)
        => new(value);
}