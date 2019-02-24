// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using CmdOpt.Options;
using DmlCli.Tools;
using DmlCli.Tools.Envs;

namespace DmlCli
{
    class DmlCli
    {
        private static DmlCliEnv Env = new DmlCliEnv()
        {
            { "html", null, "Use it to translate from DML to HTML", (env, arguments) => { env.Tool = ToolType.HTML; env.Args = arguments; }, OptionAttributes.SubModule },

            { "markdown", "md", "Use it to translate from DML to Markdown", (env, arguments) => { env.Tool = ToolType.Markdown; env.Args = arguments; }, OptionAttributes.SubModule },

            { "-h", "--help",  "Show this message", env => env.RequestHelpMessage(), OptionAttributes.Optional }
        };

        static void Main(string[] args)
        {
            if (!Env.ProcessEnvArguments(args))
                return;


            var tool = ToolFactory.Create(Env.Tool.Value);

            if (!tool.ProcessArguments(Env.Args))
                return;
            
            tool.Run();
        }
    }
}
