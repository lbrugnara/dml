// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;

namespace DmlCli.Clap
{
    [Flags]
    public enum ParameterAttribute
    {
        None = 0,
        Optional = 1,
        OptionalValue = 2,
        SubModule = 4,
        Multiple = 8
    }
}
