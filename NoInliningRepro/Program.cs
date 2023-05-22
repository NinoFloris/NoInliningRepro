﻿using System.Runtime.CompilerServices;
using System;

void Create<T>(Func<PgConverterInfo, PgConverter> factory)
{
    factory.Invoke(new PgConverterInfo());
}

Create<int>(info => new PgConverter<int>(info.GetResolutionAsObject()));

Create<short>(info => new PgConverter<short>(info.GetResolutionAsObjectTiny()));

abstract class PgConverter
{
    public Type TypeToConvert => typeof(object);
}

class PgConverter<T> : PgConverter
{
    readonly PgConverterResolution _resolution;

    public PgConverter(PgConverterResolution resolution)
    {
        _resolution = resolution;
    }
}

abstract class PgConverterResolver
{
    internal abstract PgConverterResolution GetAsObject(object? value, PgTypeId? expectedPgTypeId, bool requirePortableIds);
}

readonly record struct PgTypeId
{
}

readonly struct PgConverterResolution
{
    readonly Type? _effectiveType;

    public PgConverterResolution(PgConverter converter, PgTypeId pgTypeId, Type? effectiveType = null)
    {
        Converter = converter;
        PgTypeId = pgTypeId;
        _effectiveType = effectiveType;
    }

    public PgConverter Converter { get; }
    public PgTypeId PgTypeId { get; }
    public Type EffectiveType => _effectiveType ?? Converter.TypeToConvert;
}

class PgConverterInfo
{
    Type Type => typeof(object);
    PgConverter? Converter { get; }
    PgTypeId? PgTypeId { get; }

    public PgConverterResolution GetResolutionAsObjectTiny(object? value = default, PgTypeId? expectedPgTypeId = null) => GetResolutionCoreTiny(value, expectedPgTypeId);

    PgConverterResolution GetResolutionCoreTiny(object? value = default, PgTypeId? expectedPgTypeId = null)
    {
        return default;
    }

    public PgConverterResolution GetResolutionAsObject(object? value = default, PgTypeId? expectedPgTypeId = null) => GetResolutionCore(value, expectedPgTypeId);

    [MethodImpl(MethodImplOptions.NoInlining)]
    PgConverterResolution GetResolutionCore(object? value = default, PgTypeId? expectedPgTypeId = null)
    {
        PgConverterResolution resolution;
        switch (this)
        {
            case { Converter: { } converter }:
                resolution = new(converter, PgTypeId.GetValueOrDefault(), Type);
                break;
            case PgConverterResolverInfo { ConverterResolver: { } resolver }:
                resolution = resolver.GetAsObject(value, expectedPgTypeId, true);
                ThrowIfInvalidEffectiveType(resolution.EffectiveType);
                break;
            default:
                ThrowNotSupportedType(Type);
                return default;
        }

        return resolution;
    }

    void ThrowIfInvalidEffectiveType(Type actual)
    {
        if (Type != actual)
            ThrowInvalidEffectiveType();

        [MethodImpl(MethodImplOptions.NoInlining)]
        void ThrowInvalidEffectiveType() => throw new InvalidOperationException($"{nameof(PgConverterResolution.EffectiveType)} for a boxing info can't be {actual} but must return {Type}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void ThrowNotSupportedType(Type type) => throw new NotSupportedException($"ConverterInfo only supports boxing conversions, call GetResolution<T> with {typeof(object)} instead of {type}.");

}

class PgConverterResolverInfo : PgConverterInfo
{
    public PgConverterResolver ConverterResolver { get; }
}
