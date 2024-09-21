// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE-command-line-api.md file in the project root for full license information.

using System;
using System.CommandLine;
using System.Reflection;

internal static class ArgumentBuilder
{
    private static readonly ConstructorInfo _ctor;

    static ArgumentBuilder()
    {
        _ctor = typeof(CliArgument<string>).GetConstructor(new[] { typeof(string) })!;
    }

    public static CliArgument CreateArgument(Type valueType, string name = "value")
    {
        var argumentType = typeof(CliArgument<>).MakeGenericType(valueType);

#if NET6_0_OR_GREATER
        var ctor = (ConstructorInfo)argumentType.GetMemberWithSameMetadataDefinitionAs(_ctor);
#else
        var ctor = argumentType.GetConstructor(new[] { typeof(string) });
#endif

        return (CliArgument)ctor.Invoke(new object[] { name });
    }

    internal static CliArgument CreateArgument(string name, ParameterInfo argsParam)
    {
        if (!argsParam.HasDefaultValue)
        {
            return CreateArgument(argsParam.ParameterType, name);
        }

        var argumentType = typeof(Bridge<>).MakeGenericType(argsParam.ParameterType);

        var ctor = argumentType.GetConstructor(new[] { typeof(string), argsParam.ParameterType })!;

        return (CliArgument)ctor.Invoke(new object[] { name, argsParam.DefaultValue! });
    }

    private sealed class Bridge<T> : CliArgument<T>
    {
        public Bridge(string name, T defaultValue)
            : base(name)
        {
            // this type exists only for an easy T => Func<ArgumentResult, T> transformation
            DefaultValueFactory = (_) => defaultValue;
        }
    }
}