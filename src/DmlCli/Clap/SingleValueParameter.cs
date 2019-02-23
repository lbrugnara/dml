﻿// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file
using System;

namespace DmlCli.Clap
{
    public class SingleValueParameter<TEnvironment> : Parameter<TEnvironment> where TEnvironment : ClapEnvironment<TEnvironment>
    {
        public override ParameterHandler<TEnvironment> Handler { get; }

        public SingleValueParameter(string shortopt, string longopt, string description, ParameterAttribute attributes, Action<TEnvironment, string> handler)
            : base(shortopt, longopt, description, attributes)
        {
            this.Handler = (env, args) => handler(env, args[0]);
        }
    }
}