// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DmlCli.Clap;
using DmlCli.Observer;
using DmlCli.Tools.Envs;
using DmlLib.Core;
using DmlLib.Core.Formats;
using DmlLib.Core.Nodes;

namespace DmlCli.Tools
{
    public class MarkdownTool : ITool
    {
        private static Parameters<MarkdownToolEnv> Parameters = new Parameters<MarkdownToolEnv>()
        {
            { "-i", "--input",      "Source file",          (e, p)  => e.InputFiles.AddRange(p), ParameterAttribute.Optional | ParameterAttribute.Multiple},

            { "-it", "--interactive", "Interactive mode",   (e) => e.Interactive = true, ParameterAttribute.Optional },

            { "-o", "--output",     "Destination file. If it is not specified the output will be sent " +
                                    "to stdout. If it includes paths, they will be created and the parent path " +
                                    "of the file will be considered the root directory of the 'project'.",
                                                            (e, p)  => e.OutputFile = p,                ParameterAttribute.Optional        },

            { "-w", "--watch",      "Detects changes in the input file, scripts, styles and other resources " +
                                    "to trigger the parsing process. If present, the watch will run every 1000ms. " +
                                    "If user provides a value it will be used instead",
                                                            (e, p)  => e.Watch = int.TryParse(p, out int pt) ? pt : 1000,
                                                                                                        ParameterAttribute.Optional | ParameterAttribute.OptionalValue  },

            { "-h", "--help",       "Show this message",    (e)     => e.RequestHelpMessage(),           ParameterAttribute.Optional       }
        };

        private MarkdownToolEnv env;

        public bool ProcessArguments(string[] args)
        {
            env = new MarkdownToolEnv(Parameters);
            return env.ProcessEnvArguments(args);
        }

        public void Run()
        {
            if (env.Watch.HasValue)
            {
                List<string> observed = new List<string>(env.InputFiles);
                FileObserver.OnFileChangeDetect(observed, env.Watch.Value, () => Exec(string.Join("\n\n", env.InputFiles.Select(f => File.ReadAllText(f)))));
            }
            else if (env.Interactive)
            {
                Console.WriteLine("DML CLI (Interactive mode)");
                Console.WriteLine("Copyright (c) Leo Brugnara\n");
                Console.WriteLine("Enter an EOF string:");
                string eof = Console.ReadLine();

                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    Console.WriteLine("Input:");
                    do
                    {
                        string input = Console.ReadLine();

                        if (input == eof)
                            break;

                        sb.AppendLine(input);
                    } while (true);

                    Console.WriteLine("Output:");
                    this.Exec(sb.ToString());

                    sb.Clear();
                }
            }
            else
            {
                Exec(string.Join("\n\n", env.InputFiles.Select(f => File.ReadAllText(f))));
            }
        }

        private void Exec(string source)
        {
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