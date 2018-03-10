// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;

namespace DmlCli.Clap
{
    public abstract class ClapEnv<TEnv>
        where TEnv : ClapEnv<TEnv>
    {
        private Parameters<TEnv> parameters;
        protected bool showHelp;
        protected bool disableValidation;

        public bool Error { get; set; }
        public List<string> Errors { get; set; }

        public ClapEnv(Parameters<TEnv> parameters)
        {
            this.parameters = parameters;
            this.Errors = new List<string>();
        }

        public bool Parse(string[] args)
        {
            bool parsingSucceeded = parameters.Parse(this as TEnv, args);
            if (parsingSucceeded)
            {
                this.ValidateParameters();
            }
            return parsingSucceeded;
        }

        public string GetHelpMessage()
        {
            return parameters.GetHelpMessage();
        }

        public bool IsHelpMessageRequest()
        {
            return showHelp;
        }

        public void RequestHelpMessage()
        {
            showHelp = true;
        }

        public void DisableValidation()
        {
            disableValidation = true;
        }

        public void EnableValidation()
        {
            disableValidation = false;
        }

        // Error messages

        public void ShowErrorMessage()
        {
            List<string> errors = new List<string>();

            string beforeerrors = this.OnBeforeErrorMessages();

            foreach (string err in Errors)
            {
                string beforeerror = this.OnBeforeErrorMessage();
                string error = this.OnErrorMessage(err);
                string aftererror = this.OnAfterErrorMessage();

                errors.Add(beforeerror + error + aftererror);
            }

            string aftererrors = this.OnAfterErrorMessages();

            this.OnShowErrorMessage(beforeerrors + string.Join("\n", errors) + aftererrors);
        }

        protected virtual string OnBeforeErrorMessages()
        {
            return "";
        }

        protected virtual string OnBeforeErrorMessage()
        {
            return "";
        }

        protected virtual string OnErrorMessage(string error)
        {
            return error;
        }

        protected virtual string OnAfterErrorMessage()
        {
            return "";
        }

        protected virtual string OnAfterErrorMessages()
        {
            return "";
        }

        protected virtual void OnShowErrorMessage(string error)
        {
            Console.WriteLine(error);
        }

        // Help message
        public void ShowHelpMessage()
        {
            string beforehelp = this.OnBeforeHelpMessage();
            string help = this.OnHelpMessage(parameters.GetHelpMessage());
            string afterhelp = this.OnAfterHelpMessage();

            this.OnShowHelpMessage(beforehelp + help + afterhelp);
        }

        protected virtual string OnBeforeHelpMessage()
        {
            return "";
        }

        protected virtual string OnHelpMessage(string help)
        {
            return help;
        }

        protected virtual string OnAfterHelpMessage()
        {
            return "";
        }

        protected virtual void OnShowHelpMessage(string help)
        {
            Console.WriteLine(help);
        }

        public abstract void ValidateParameters();
    }
}