// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.IO;
using DmlCli.Clap;

namespace DmlCli.Tools.Envs
{
    public class MarkdownToolEnv : BaseEnv<MarkdownToolEnv>
    {
        public List<string> InputFiles { get; private set; }
        public string OutputFile { get; set; }
        public string TokensOutputFile { get; private set; }
        public int? Watch;
        public bool Interactive;
        
        public MarkdownToolEnv(Parameters<MarkdownToolEnv> parmeters)
            : base (parmeters)
        {
            this.InputFiles = new List<string>();
        }

        public void Reset()
        {
            showHelp = false;
            InputFiles = new List<string>();
            OutputFile = null;
            TokensOutputFile = null;
        }

        public override void ValidateParameters()
        {
            if (InputFiles.Count == 0 && !this.Interactive)
            {
                Error = true;
                Errors.Add(string.Format("No input files"));
                return;
            }

            foreach (var inputFile in InputFiles)
            {
                if (!File.Exists(inputFile))
                {
                    Error = true;
                    Errors.Add(string.Format("Input file '{0}' does not exist", inputFile));
                }
            }

            if (this.Watch.HasValue && this.Interactive)
            {
                this.Error = true;
                this.Errors.Add("Cannot use watch while in interactive mode");
            }
        }
    }
}