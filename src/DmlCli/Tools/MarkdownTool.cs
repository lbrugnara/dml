// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CmdOpt.Options;
using DmlCli.Observer;
using DmlCli.Tools.Envs;
using DmlLib.Core;
using DmlLib.Core.Formats;
using DmlLib.Core.Nodes;

namespace DmlCli.Tools
{
    public class MarkdownTool : ITool
    {
        private static readonly MarkdownToolEnv Env = new MarkdownToolEnv()
        {
            { "-i", "--input",      "Source file",          (e, p)  => e.InputFiles.AddRange(p), OptionAttributes.Optional | OptionAttributes.MultiValue},

            { "-it", "--interactive", "Interactive mode",   (e) => e.Interactive = true, OptionAttributes.Optional },

            { "-o", "--output",     "Destination file. If it is not specified the output will be sent " +
                                    "to stdout. If it includes paths, they will be created and the parent path " +
                                    "of the file will be considered the root directory of the 'project'.",
                                                            (e, p)  => e.OutputFile = p,                OptionAttributes.Optional        },

            { "-w", "--watch",      "Detects changes in the input file, scripts, styles and other resources " +
                                    "to trigger the parsing process. If present, the watch will run every 1000ms. " +
                                    "If user provides a value it will be used instead",
                                                            (e, p)  => e.Watch = int.TryParse(p, out int pt) ? pt : 1000,
                                                                                                        OptionAttributes.Optional | OptionAttributes.OptionalValue  },

            { "-h", "--help",       "Show this message",    (e)     => e.RequestHelpMessage(),           OptionAttributes.Optional       }
        };

        public bool ProcessArguments(string[] args) => Env.ProcessEnvArguments(args);

        public void Run()
        {
            if (Env.Watch.HasValue)
            {
                List<string> observed = new List<string>(Env.InputFiles);
                FileObserver.OnFileChangeDetect(observed, Env.Watch.Value, () => Exec(string.Join("\n\n", Env.InputFiles.Select(f => File.ReadAllText(f)))));
            }
            else if (Env.Interactive)
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
                Exec(string.Join("\n\n", Env.InputFiles.Select(f => File.ReadAllText(f))));
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

            if (Env.OutputFile != null)
            {
                DirectoryInfo di = new FileInfo(Env.OutputFile).Directory;
                if (!di.Exists)
                {
                    di.Create();
                }
                File.WriteAllText(Env.OutputFile, output);
            }
            else
            {
                Console.WriteLine(output);
            }
        }
    }
}