// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

namespace DmlCli.Tools
{
    public static class ToolFactory
    {
        public static ITool Create(ToolType tool)
        {
            switch (tool)
            {
                case ToolType.HTML:
                    return new Dml2Html();
                case ToolType.Markdown:
                    return new Dml2Md();
            }
            return null;
        }
    }
}