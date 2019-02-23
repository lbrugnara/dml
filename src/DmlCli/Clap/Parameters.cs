// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DmlCli.Clap
{
    public class Parameters<TEnvironment> : KeyedCollection<string, Parameter<TEnvironment>> where TEnvironment : ClapEnvironment<TEnvironment>
    {
        /// <summary>
        /// List of environment's parameters
        /// </summary>
        private List<Parameter<TEnvironment>> EnvironmentParameters;

        public Parameters()
        {
            EnvironmentParameters = new List<Parameter<TEnvironment>>();
        }

        /// <summary>
        /// Returns the formatted help message
        /// </summary>
        /// <returns></returns>
        public string GetHelpMessage()
        {
            return string.Join("\n", EnvironmentParameters.Select(p => {
                string name = string.Format("  {0}{1}{2}", p.ShortName, p.LongName != null ? "|":"", p.LongName);
                string desc = string.Format("\t{0}", p.GetFormattedDescription(name.Length), p.Attributes.ToString());
                string attrs = p.Attributes != ParameterAttribute.None ? $" ({p.Attributes.ToString()})" : "";
                return name + desc + attrs + "\n";
            }));
        }

        public Parameter<TEnvironment> Add (string shortopt, string longopt, string description, Action<TEnvironment> handler, ParameterAttribute attributes)
		{
            var p = new VoidParameter<TEnvironment>(shortopt, longopt, description, attributes, handler);
            EnvironmentParameters.Add(p);
            return p;
        }

        public Parameter<TEnvironment> Add (string shortopt, string longopt, string description, Action<TEnvironment, string> handler, ParameterAttribute attributes)
		{
            var p = new SingleValueParameter<TEnvironment>(shortopt, longopt, description, attributes, handler);
            EnvironmentParameters.Add(p);
            return p;
        }

        public Parameter<TEnvironment> Add (string shortopt, string longopt, string description, Action<TEnvironment, string[]> action, ParameterAttribute attributes)
		{
            var p = new MultiValueParameter<TEnvironment>(shortopt, longopt, description, attributes, action);
            EnvironmentParameters.Add(p);
            return p;
        }

        protected override string GetKeyForItem(Parameter<TEnvironment> item)
        {
            return item.ShortName;
        }

        public bool Parse(TEnvironment env, string[] args)
        {
            // Create a copy of the list to track already parsed parameters
            var parameters = this.EnvironmentParameters.ToList();

            int index = 0;
            bool shouldBreak = false;
            while (!shouldBreak && index < args.Length)
            {
                string argument = args.ElementAtOrDefault(index++);

                // Get the parameter matching this argument
                var parameter = parameters.FirstOrDefault(p => p.ShortName == argument || p.LongName == argument);

                if (parameter == null)
                    continue;

                // Remove the parameter we found so we won't process it again
                parameters.Remove(parameter);

                switch (parameter)
                {
                    case VoidParameter<TEnvironment> sp:
                        this.ParseVoidParamater(sp, env);
                        break;

                    case SingleValueParameter<TEnvironment> svp:
                        this.ParseSingleValueParamater(svp, env, args, ref index);
                        break;

                    case MultiValueParameter<TEnvironment> mvp:
                        this.ParseMultiValueParameter(mvp, env, args, ref index);

                        // The submodule process the rest of the arguments, so we need to leave
                        if (mvp.Attributes.HasFlag(ParameterAttribute.SubModule))
                            shouldBreak = true;

                        break;

                    default:
                        throw new Exception("ClapEnv is misconfigured");
                }
            }

            env.Error = parameters.Any(p => p.IsRequired());
            parameters.Where(p => p.IsRequired()).ToList().ForEach(p => env.Errors.Add(string.Format("Parameter {0} is required", p.LongName ?? p.ShortName)));
            return !env.Error;
        }

        private void ParseVoidParamater(VoidParameter<TEnvironment> parameter, TEnvironment env)
        {
            parameter.Handler.Invoke(env);
        }

        private void ParseSingleValueParamater(SingleValueParameter<TEnvironment> parameter, TEnvironment env, string[] args, ref int index)
        {
            string nextArg = args.ElementAtOrDefault(index);
            bool isEnd = string.IsNullOrEmpty(nextArg);
            bool isOtherParam = !isEnd && nextArg.StartsWith("-") && this.EnvironmentParameters.Any(p => p.ShortName == nextArg || p.LongName == nextArg);
            bool isRequired = parameter.IsRequired();
            bool hasOptionalValue = parameter.Attributes.HasFlag(ParameterAttribute.OptionalValue);

            if ((isEnd || isOtherParam) && !hasOptionalValue)
            {
                env.Errors.Add(string.Format("Parameter {0} is required", parameter.LongName ?? parameter.ShortName));
                return;
            }

            if (hasOptionalValue && (isEnd || isOtherParam))
            {
                index++;
                nextArg = null;
            }

            parameter.Handler.Invoke(env, nextArg);
        }

        private void ParseMultiValueParameter(MultiValueParameter<TEnvironment> parameter, TEnvironment env, string[] args, ref int index)
        {
            if (parameter.Attributes.HasFlag(ParameterAttribute.SubModule))
            {
                this.ParseMultiValueAsSubModule(parameter, env, args.Skip(index).ToArray());
            }
            else if (parameter.Attributes.HasFlag(ParameterAttribute.Multiple))
            {
                this.ParseMultiValueAsParameter(parameter, env, args, ref index);
            }
        }

        private void ParseMultiValueAsSubModule(MultiValueParameter<TEnvironment> parameter, TEnvironment env, string[] args)
        {
            parameter.Handler.Invoke(env, args);
        }

        private void ParseMultiValueAsParameter(MultiValueParameter<TEnvironment> parameter, TEnvironment env, string[] args, ref int index)
        {
            bool isRequiredParameter = parameter.IsRequired();
            bool hasOptionalValue = parameter.Attributes.HasFlag(ParameterAttribute.OptionalValue);
            List<string> arguments = new List<string>();
            bool isOtherParam = false;
            bool isEnd = false;
            do
            {
                string nextArg = args.ElementAtOrDefault(index);
                isEnd = string.IsNullOrEmpty(nextArg);
                isOtherParam = !isEnd && nextArg.StartsWith("-") && this.EnvironmentParameters.Any(p => p.ShortName == nextArg || p.LongName == nextArg);
                if (!isOtherParam && !isEnd)
                {
                    arguments.Add(nextArg);
                    index++;
                }
            } while (!isOtherParam && !isEnd);

            if (arguments.Count == 0 && (!hasOptionalValue || isRequiredParameter))
            {
                env.Errors.Add(string.Format("Parameter {0} is required", parameter.LongName ?? parameter.ShortName));
                return;
            }

            parameter.Handler.Invoke(env, arguments.ToArray());
        }
    }
}