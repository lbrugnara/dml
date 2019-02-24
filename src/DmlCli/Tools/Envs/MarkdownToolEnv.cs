// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.IO;

namespace DmlCli.Tools.Envs
{
    public class MarkdownToolEnv : BaseEnv<MarkdownToolEnv>
    {
        public List<string> InputFiles { get; private set; }
        public string OutputFile { get; set; }
        public string TokensOutputFile { get; private set; }
        public int? Watch;
        public bool Interactive;
        
        public MarkdownToolEnv()
            : base ()
        {
            this.InputFiles = new List<string>();
        }

        public void Reset()
        {
            ShowHelp = false;
            InputFiles = new List<string>();
            OutputFile = null;
            TokensOutputFile = null;
        }

        public override void ValidateOptions()
        {
            if (InputFiles.Count == 0 && !this.Interactive)
            {
                Errors.Add(string.Format("No input files"));
                return;
            }

            foreach (var inputFile in InputFiles)
            {
                if (!File.Exists(inputFile))
                    Errors.Add(string.Format("Input file '{0}' does not exist", inputFile));
            }

            if (this.Watch.HasValue && this.Interactive)
                this.Errors.Add("Cannot use watch while in interactive mode");
        }
    }
}