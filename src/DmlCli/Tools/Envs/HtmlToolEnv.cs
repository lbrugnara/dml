// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System.Collections.Generic;
using System.IO;
using DmlCli.Clap;

namespace DmlCli.Tools.Envs
{
    public class HtmlToolEnv : BaseEnv<HtmlToolEnv>
    {
        public HtmlToolEnv(Parameters<HtmlToolEnv> parmeters)
            : base (parmeters)
        {
            this.InputFiles = new List<string>();
            this.Styles = new List<string>();
            this.Scripts = new List<string>();
            this.Resources = new List<string>();
        }

        /// <summary>
        /// Paths of the source dml files
        /// </summary>
        public List<string> InputFiles { get; private set; }

        /// <summary>
        /// Output file's path
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Path to the file that -if provided- will contain the tokenization process information
        /// </summary>
        public string TokensOutputFile { get; set; }

        /// <summary>
        /// If true, the output to the <see cref="OutputFile"/> will contain just the HTML document's body
        /// </summary>
        public bool JustBodyContent { get; set; }

        /// <summary>
        /// If true, the content is appended to the <see cref="OutputFile"/> instead of replacing the content
        /// </summary>
        public bool AppendOutput { get; set; }

        /// <summary>
        /// If provided, it will be used as the HTML "template" file. The output will replace the <see cref="Document"/>'s body content
        /// and if the <see cref="AppendOutput"/> flag is enabled, the content will be added to the body
        /// </summary>
        public string Document { get; set; }

        /// <summary>
        /// If present, the process will check files for changes every <see cref="Watch"/> seconds
        /// </summary>
        public int? Watch { get; set; }

        /// <summary>
        /// If true, provides an interactive interface to the tool through the command line interface
        /// </summary>
        public bool Interactive { get; set; }

        /// <summary>
        /// List of styles to add to the document
        /// </summary>
        public List<string> Styles { get; private set; }

        /// <summary>
        /// List of scripts to add to the document
        /// </summary>
        public List<string> Scripts { get; private set; }

        /// <summary>
        /// List of resources to add to the document
        /// </summary>
        public List<string> Resources { get; private set; }

        /// <summary>
        /// Restores the environment's default values
        /// </summary>
        public void Reset()
        {
            showHelp = false;
            this.InputFiles.Clear();
            this.OutputFile = null;
            this.TokensOutputFile = null;
            this.AppendOutput = false;
            this.JustBodyContent = false;
            this.Document = null;
            this.Styles.Clear();
            this.Scripts.Clear();
            this.Resources.Clear();
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