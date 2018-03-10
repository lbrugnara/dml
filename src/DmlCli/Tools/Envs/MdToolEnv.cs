// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.IO;
using DmlCli.Clap;

namespace DmlCli.Tools.Envs
{
    public class MdToolEnv : BaseEnv<MdToolEnv>
    {
        public List<string> InputFiles = new List<string>();
        public string OutputFile;
        public string TokensOutputFile;
        public int? Watch;
        public bool Interactive;
        
        public MdToolEnv(Parameters<MdToolEnv> parmeters)
            : base (parmeters)
        {
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