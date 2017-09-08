// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.IO;
using DmlCli.Clap;

namespace DmlCli.Tools.Envs
{
    public class HtmlToolEnv : BaseEnv<HtmlToolEnv>
    {
        public List<string> InputFiles = new List<string>();
        public string OutputFile;
        public string TokensOutputFile;
        public bool JustBodyContent;
        public bool AppendOutput;
        public string Document;
        public int? Watch;
        public List<string> Styles = new List<string>();
        public List<string> Scripts = new List<string>();
        public List<string> Resources = new List<string>();

        public HtmlToolEnv(Parameters<HtmlToolEnv> parmeters)
            : base (parmeters)
        {
        }

        public void Reset()
        {
            showHelp = false;
            InputFiles.Clear();
            OutputFile = null;
            TokensOutputFile = null;
            AppendOutput = false;
            JustBodyContent = false;
            Document = null;
            Styles.Clear();
            Scripts.Clear();
            Resources.Clear();
        }

        public override void ValidateParameters()
        {
            if (InputFiles.Count == 0)
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
        }
    }
}