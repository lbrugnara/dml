// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using DmlCli.Clap;

namespace DmlCli.Tools.Envs
{
    public class BaseEnv<T> : ClapEnv<T>
        where T : ClapEnv<T>
    {
        public BaseEnv(Parameters<T> parameters) 
            : base(parameters)
        {
        }

        protected override string OnBeforeHelpMessage()
        {
            return
            "DML CLI:\n" +
            "Copyright (c) Leo Brugnara\n\n";
        }

        protected override string OnBeforeErrorMessages()
        {
            return
            "DML CLI:\n" +
            "Copyright (c) Leo Brugnara\n\n";
        }

        public override void ValidateParameters()
        {
            throw new NotImplementedException();
        }

        public bool ProcessEnvArguments(string[] args)
        {
            Parse(args);
            if (Error && !IsHelpMessageRequest())
            {
                ShowErrorMessage();
                return false;
            }
            
            if (IsHelpMessageRequest())
            {
                ShowHelpMessage();
                return false;
            }

            if (Error)
            {
                ShowErrorMessage();
                return false;
            }

            return true;
        }
    }
}