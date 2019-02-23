// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlCli.Clap
{
    public delegate void ParameterHandler<TEnvironment>(TEnvironment env, params string[] arguments);

    public abstract class Parameter<TEnvironment> where TEnvironment : ClapEnvironment<TEnvironment>
    {
        public Parameter(string shortopt, string longopt, string description, ParameterAttribute attributes)
        {
            ShortName = shortopt;
            LongName = longopt;
            Description = description;
            Attributes = attributes;
        }

        /// <summary>
        /// Parameter's short name
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Parameter's long name
        /// </summary>
        public string LongName { get; }

        /// <summary>
        /// Parameter description
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Parameter's attributes that determine the behavior
        /// </summary>
        public ParameterAttribute Attributes { get; }

        /// <summary>
        /// Returns true if the parameter is required by checking if the <see cref="ParameterAttribute.Optional"/> is missing and
        /// the parameter is not a <see cref="ParameterAttribute.SubModule"/>
        /// </summary>
        /// <returns>True if the parameter is required</returns>
        public bool IsRequired()
        {
            return !Attributes.HasFlag(ParameterAttribute.Optional) && !Attributes.HasFlag(ParameterAttribute.SubModule);
        }

        /// <summary>
        /// The delegate handler that will be called once the parameter has 
        /// been parsed in order to update the TEnvironment
        /// </summary>
        public abstract ParameterHandler<TEnvironment> Handler { get; }

        /// <summary>
        /// Returns the parameter description formatted to show in the
        /// help message
        /// </summary>
        /// <param name="neededpad"></param>
        /// <returns></returns>
        public string GetFormattedDescription(int neededpad)
        {
            if (Description == null)
                return "\t";
            string desc = "\t";
            bool needsBreak = false;
            for (int i=0; i < Description.Length; i++)
            {
                if (i > 0 && i % 120 == 0)
                {
                    if (Description[i] == ' ')
                    {
                        desc += "\n\t".PadRight(neededpad, ' ') + "\t";
                    }
                    else
                    {
                        needsBreak = true;
                    }
                }
                else if (needsBreak && Description[i-1] == ' ')
                {
                    needsBreak = false;
                    desc += "\n\t".PadRight(neededpad, ' ') + "\t";
                }
                desc += Description[i];
            }
            return desc;
        }
    }
}
