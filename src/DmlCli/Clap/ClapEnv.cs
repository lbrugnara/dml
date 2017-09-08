// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;

namespace DmlCli.Clap
{
    public abstract class ClapEnv<TEnv>
        where TEnv : ClapEnv<TEnv>
    {
        private Parameters<TEnv> _parameters;
        public bool Error;
        public List<string> Errors = new List<string>();
        protected bool showHelp;

        protected bool disableValidation;

        public ClapEnv(Parameters<TEnv> parameters)
        {
            _parameters = parameters;
        }

        public bool Parse(string[] args)
        {
            bool parsingSucceeded = _parameters.Parse(this as TEnv, args);
            if (parsingSucceeded)
            {
                ValidateParameters();
            }
            return parsingSucceeded;
        }

        public string GetHelpMessage()
        {
            return _parameters.GetHelpMessage();
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
            string beforeerrors = OnBeforeErrorMessages();
            List<string> errors = new List<string>();
            foreach (string err in Errors)
            {
                string beforeerror = OnBeforeErrorMessage();
                string error = OnErrorMessage(err);
                string aftererror = OnAfterErrorMessage();
                errors.Add(beforeerror + error + aftererror);
            }
            string aftererrors = OnAfterErrorMessages();
            OnShowErrorMessage(beforeerrors + string.Join("\n", errors) + aftererrors);
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
            string beforehelp = OnBeforeHelpMessage();
            string help = OnHelpMessage(_parameters.GetHelpMessage());
            string afterhelp = OnAfterHelpMessage();
            OnShowHelpMessage(beforehelp + help + afterhelp);
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