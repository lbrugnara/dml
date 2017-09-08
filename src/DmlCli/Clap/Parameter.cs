// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;

namespace DmlCli.Clap
{
    [Flags]
    public enum ParamAttrs
    {
        None = 0,
        Optional = 1,
        OptionalValue = 2,
        SubModule = 4,
        Multiple = 8
    }

    public class Parameter<TEnv>
        where TEnv : ClapEnv<TEnv>
    {
        private string _shortName;
        private string _longName;
        private string _description;
        private Action<TEnv, string> _paramHandler;
        private Action<TEnv, string[]> _paramsHandler;
        private Action<TEnv> _action;
        private ParamAttrs _attributes;
        
        public Parameter(string shortopt, string longopt, string description, Action<TEnv, string> handler, ParamAttrs attributes)
        {
            _shortName = shortopt;
            _longName = longopt;
            _description = description;
            _paramHandler = handler;
            _action = null;
            _attributes = attributes;
        }

        public Parameter(string shortopt, string longopt, string description, Action<TEnv, string[]> subModuleArgs, ParamAttrs attributes)
        {
            _shortName = shortopt;
            _longName = longopt;
            _description = description;
            _paramsHandler = subModuleArgs;
            _action = null;
            _attributes = attributes;
        }

        public Parameter(string shortopt, string longopt, string description, Action<TEnv> action, ParamAttrs attributes)
        {
            _shortName = shortopt;
            _longName = longopt;
            _description = description;
            _paramHandler = null;
            _action = action;
            _attributes = attributes;
        }

        public string ShortName { get => _shortName; }
        public string LongName { get => _longName; }
        public string Description { get => _description; }
        public Action<TEnv, string> Handler { get => _paramHandler; }
        public Action<TEnv, string[]> ParamsHandler { get => _paramsHandler; }
        public Action<TEnv> Action { get => _action; }
        public ParamAttrs Attributes {get => _attributes; }

        public bool IsRequired()
        {
            return !Attributes.HasFlag(ParamAttrs.Optional) && !Attributes.HasFlag(ParamAttrs.SubModule);
        }

        public string GetFormattedDescription(int neededpad)
        {
            if (_description == null)
                return "\t";
            string desc = "\t";
            bool needsBreak = false;
            for (int i=0; i < _description.Length; i++)
            {
                if (i > 0 && i % 120 == 0)
                {
                    if (_description[i] == ' ')
                    {
                        desc += "\n\t".PadRight(neededpad, ' ') + "\t";
                    }
                    else
                    {
                        needsBreak = true;
                    }
                }
                else if (needsBreak && _description[i-1] == ' ')
                {
                    needsBreak = false;
                    desc += "\n\t".PadRight(neededpad, ' ') + "\t";
                }
                desc += _description[i];
            }
            return desc;
        }
    }
}
