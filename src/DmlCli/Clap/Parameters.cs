// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DmlCli.Clap
{
    public class Parameters<TEnv> : KeyedCollection<string, Parameter<TEnv>>
        where TEnv : ClapEnv<TEnv>
    {
        private List<Parameter<TEnv>> parameters;

        public Parameters()
        {
            parameters = new List<Parameter<TEnv>>();
        }

        public string GetHelpMessage()
        {
            return string.Join("\n", parameters.Select(p => {
                string name = string.Format("  {0}{1}{2}", p.ShortName, p.LongName != null ? "|":"", p.LongName);
                string desc = string.Format("\t{0}", p.GetFormattedDescription(name.Length), p.Attributes.ToString());
                string attrs = p.Attributes != ParamAttrs.None ? $" ({p.Attributes.ToString()})" : "";
                return name + desc + attrs + "\n";
            }));
        }

        public Parameter<TEnv> Add (string shortopt, string longopt, string description, Action<TEnv, string> handler, ParamAttrs attributes)
		{
            var p = new Parameter<TEnv>(shortopt, longopt, description, handler, attributes);
            parameters.Add(p);
            return p;
        }

        public Parameter<TEnv> Add (string shortopt, string longopt, string description, Action<TEnv, string[]> handler, ParamAttrs attributes)
		{
            var p = new Parameter<TEnv>(shortopt, longopt, description, handler, attributes);
            parameters.Add(p);
            return p;
        }

        public Parameter<TEnv> Add (string shortopt, string longopt, string description, Action<TEnv> action, ParamAttrs attributes)
		{
            var p = new Parameter<TEnv>(shortopt, longopt, description, action, attributes);
            parameters.Add(p);
            return p;
        }

        protected override string GetKeyForItem(Parameter<TEnv> item)
        {
            throw new NotImplementedException();
        }

        public bool Parse(TEnv env, string[] args)
        {
            List<Parameter<TEnv>> parsed = new List<Parameter<TEnv>>();
            List<Parameter<TEnv>> parameters = this.parameters.ToList();
            int index = 0;
            while (index < args.Length)
            {
                string argument = args.ElementAtOrDefault(index++);
                Parameter<TEnv> param = parameters.Where(p => p.ShortName == argument || p.LongName == argument).Select(p => p).FirstOrDefault();
                if (param == null)
                {
                    continue;
                }

                bool isSubModule = param.Attributes.HasFlag(ParamAttrs.SubModule);

                if (isSubModule)
                {
                    param.ParamsHandler(env, args.Skip(index).ToArray());
                }
                else if (param.Action != null)
                {
                    param.Action(env);
                }
                else if (param.Handler != null)
                {
                    string nextArg = args.ElementAtOrDefault(index);
                    bool isEnd = string.IsNullOrEmpty(nextArg);
                    bool isOtherParam = !isEnd && nextArg.StartsWith("-") && parameters.Any(p => p.ShortName == nextArg || p.LongName == nextArg);
                    bool isRequired = param.IsRequired();
                    bool hasOptionalValue = param.Attributes.HasFlag(ParamAttrs.OptionalValue);

                    if ((isEnd || isOtherParam) && !hasOptionalValue)
                    {
                        env.Errors.Add(string.Format("Parameter {0} is required", param.LongName ?? param.ShortName));
                        return false;
                    }

                    if (hasOptionalValue && (isEnd || isOtherParam))
                    {
                        index++;
                        nextArg = null;
                    }

                    param.Handler(env, nextArg);
                }
                else if (param.ParamsHandler != null && param.Attributes.HasFlag(ParamAttrs.Multiple))
                {
                    bool isRequiredParameter = param.IsRequired();
                    bool hasOptionalValue = param.Attributes.HasFlag(ParamAttrs.OptionalValue);
                    List<string> arguments = new List<string>(); 
                    bool isOtherParam = false;
                    bool isEnd = false;
                    do
                    {
                        string nextArg = args.ElementAtOrDefault(index);
                        isEnd = string.IsNullOrEmpty(nextArg);
                        isOtherParam = !isEnd && nextArg.StartsWith("-") && parameters.Any(p => p.ShortName == nextArg || p.LongName == nextArg);
                        if (!isOtherParam && !isEnd)
                        {
                            arguments.Add(nextArg);
                            index++;
                        }
                    } while (!isOtherParam && !isEnd);
                    
                    if (arguments.Count == 0 && (!hasOptionalValue || isRequiredParameter))
                    {
                        env.Errors.Add(string.Format("Parameter {0} is required", param.LongName ?? param.ShortName));
                        return false;
                    }

                    param.ParamsHandler(env, arguments.ToArray());
                }
                else
                {
                    throw new Exception("ClapEnv is misconfigured");
                }
                parameters.Remove(param);
                parsed.Add(param);
                if (isSubModule)
                    break;
            }

            env.Error = parameters.Any(p => p.IsRequired());
            parameters.Where(p => p.IsRequired()).ToList().ForEach(p => env.Errors.Add(string.Format("Parameter {0} is required", p.LongName ?? p.ShortName)));
            return !env.Error;
        }
    }
}