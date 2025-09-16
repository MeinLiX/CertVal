using System.Text.Json;
using System.Text.Json.Serialization;

namespace CertVal.Infrastructure.Converters;

/// <summary>
/// Custom JSON converter that supports both string and numeric enum values
/// </summary>
/// <typeparam name="T">The enum type</typeparam>
public class FlexibleEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                ObjectDisposedException.ThrowIf(string.IsNullOrEmpty(stringValue), new JsonException($"Empty string cannot be converted to {typeof(T).Name}"));

                if (Enum.TryParse<T>(stringValue, ignoreCase: true, out var enumFromString))
                    return enumFromString;

                throw new JsonException($"String '{stringValue}' cannot be converted to {typeof(T).Name}");

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                {
                    if (Enum.IsDefined(typeof(T), intValue))
                        return (T)Enum.ToObject(typeof(T), intValue);

                    throw new JsonException($"Number {intValue} is not a valid value for {typeof(T).Name}");
                }
                throw new JsonException($"Cannot convert number to {typeof(T).Name}");

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when parsing {typeof(T).Name}");
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Converter for nullable enums
/// </summary>
/// <typeparam name="T">The enum type</typeparam>
public class NullableFlexibleEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
{
    private readonly FlexibleEnumConverter<T> _baseConverter = new();

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return _baseConverter.Read(ref reader, typeof(T), options);
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            _baseConverter.Write(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Factory for creating flexible enum converters
/// </summary>
public class FlexibleEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum ||
               (typeToConvert.IsGenericType &&
                typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type enumType;
        bool isNullable = false;

        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            enumType = Nullable.GetUnderlyingType(typeToConvert)!;
            isNullable = true;
        }
        else
        {
            enumType = typeToConvert;
        }

        var converterType = isNullable
            ? typeof(NullableFlexibleEnumConverter<>).MakeGenericType(enumType)
            : typeof(FlexibleEnumConverter<>).MakeGenericType(enumType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}