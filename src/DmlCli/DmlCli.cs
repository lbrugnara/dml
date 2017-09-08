// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using DmlCli.Clap;
using DmlCli.Tools;
using DmlCli.Tools.Envs;

namespace DmlCli
{
    class DmlCli
    {
        static void Main(string[] args)
        {
            Parameters<DmlCliEnv> p = new Parameters<DmlCliEnv>()
            {
                { "html",       null,      "Use it to translate from DML to HTML",
                    (e, moduleArgs) => { e.Tool = ToolType.HTML; e.Args = moduleArgs; },      
                    ParamAttrs.SubModule
                },
                
                { "markdown",   "md",      "Use it to translate from DML to Markdown",
                    (e, moduleArgs) => { e.Tool = ToolType.Markdown; e.Args = moduleArgs; },
                    ParamAttrs.SubModule
                },
                
                { "-h",         "--help",  "Show this message",
                    (e) => e.RequestHelpMessage(),
                    ParamAttrs.Optional 
                }
            };

            DmlCliEnv env = new DmlCliEnv(p);
            if (!env.ProcessEnvArguments(args))
            {
                return;
            }

            ITool tool = ToolFactory.Create(env.Tool.Value);
            if (!tool.ProcessArguments(env.Args))
            {
                return;
            }
            
            tool.Run();
        }
    }
}
