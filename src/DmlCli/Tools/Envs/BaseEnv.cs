// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using CmdOpt.Environment;

namespace DmlCli.Tools.Envs
{
    public class BaseEnv<T> : Environment<T> where T : Environment<T>
    {

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

        public override void ValidateOptions()
        {
            throw new NotImplementedException();
        }

        public bool ProcessEnvArguments(string[] args)
        {
            this.Parse(args);

            if (this.Error && !this.IsHelpMessageRequest())
            {
                this.ShowErrorMessage();
                return false;
            }
            
            if (this.IsHelpMessageRequest())
            {
                this.ShowHelpMessage();
                return false;
            }

            if (this.Error)
            {
                this.ShowErrorMessage();
                return false;
            }

            return true;
        }
    }
}