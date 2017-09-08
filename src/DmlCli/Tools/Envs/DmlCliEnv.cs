// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlCli.Clap;

namespace DmlCli.Tools.Envs
{
    public class DmlCliEnv : BaseEnv<DmlCliEnv>
    {
        public ToolType? Tool;
        public string[] Args;

        public DmlCliEnv(Parameters<DmlCliEnv> parameters) 
            : base(parameters)
        {
        }

        public override void ValidateParameters()
        {
            if (!Tool.HasValue)
            {
                Error = true;
                Errors.Add("Please select the tool to generate the output.");
            }
        }

        protected override string OnBeforeHelpMessage()
        {
            return
            base.OnBeforeHelpMessage() + 
            "Usage: dml [html|md] [args]\n\n";
        }

        protected override string OnBeforeErrorMessages()
        {
            return
            base.OnBeforeErrorMessages() + 
            "Usage: dml [html|md] [args]\n\n";
        }
    }
}