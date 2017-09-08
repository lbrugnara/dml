// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DmlCli.Clap;
using DmlCli.Observer;
using DmlCli.Tools.Envs;
using DmlLib.Core;
using DmlLib.Core.Formats;
using DmlLib.Core.Nodes;

namespace DmlCli.Tools
{
    public class Dml2Md : ITool
    {
        private static Parameters<MdToolEnv> Parameters = new Parameters<MdToolEnv>()
        {
            { "-i", "--input",      "Source file",          (e, p)  => e.InputFiles.AddRange(p), ParamAttrs.Multiple},
            
            { "-o", "--output",     "Destination file. If it is not specified the output will be sent " +
                                    "to stdout. If it includes paths, they will be created and the parent path " +
                                    "of the file will be considered the root directory of the 'project'.",
                                                            (e, p)  => e.OutputFile = p,                ParamAttrs.Optional        },

            { "-w", "--watch",      "Detects changes in the input file, scripts, styles and other resources to " +
                                    "to trigger the parsing process. If present, the watch will run every 1000ms. " +
                                    "If user provides a value it will be used",          
                                                            (e, p)  => {
                                                                int pt = 0;  
                                                                bool hasP = int.TryParse(p, out pt);
                                                                e.Watch = hasP ? pt : 1000;
                                                            },                                           ParamAttrs.Optional | ParamAttrs.OptionalValue  },

            { "-h", "--help",       "Show this message",    (e)     => e.RequestHelpMessage(),           ParamAttrs.Optional       }
        };

        private MdToolEnv env;

        public bool ProcessArguments(string[] args)
        {
            env = new MdToolEnv(Parameters);
            return env.ProcessEnvArguments(args);
        }

        public void Run()
        {
            if (env.Watch.HasValue)
            {
                List<string> observed = new List<string>(env.InputFiles);
                FileObserver.OnFileChangeDetect(observed, env.Watch.Value, () => Exec());
            }
            else
            {
                Exec();
            }
        }

        private void Exec()
        {
            string source = string.Join("\n\n", env.InputFiles.Select(f => File.ReadAllText(f)));
            // Parse the DML file
            Parser parser = new Parser();
            DmlDocument doc = parser.Parse(source);
            
            // Send to output file
            var mdctx = new MarkdownTranslationContext();
            mdctx.CodeBlockSupport = CodeBlockSupport.Full;
            string output = doc.Body.ToMarkdown(mdctx);

            if (env.OutputFile != null)
            {
                DirectoryInfo di = new FileInfo(env.OutputFile).Directory;
                if (!di.Exists)
                {
                    di.Create();
                }
                File.WriteAllText(env.OutputFile, output);
            }
            else
            {
                Console.WriteLine(output);
            }
        }
    }
}