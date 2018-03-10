// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DmlCli.Clap;
using DmlCli.Observer;
using DmlCli.Tools.Envs;
using DmlLib.Core;
using DmlLib.Core.Nodes;

namespace DmlCli.Tools
{
    public class Dml2Html : ITool
    {
        private static Parameters<HtmlToolEnv> Parameters = new Parameters<HtmlToolEnv>()
        {
            { "-i", "--input",      "Source file",          (e, p)  => e.InputFiles.AddRange(p), ParamAttrs.Optional | ParamAttrs.Multiple},

            { "-it", "--interactive", "Interactive mode",   (e) => e.Interactive = true, ParamAttrs.Optional },

            { "-o", "--output",     "Destination file. If it is not specified the output will be sent " +
                                    "to stdout. If it includes paths, they will be created and the parent path " +
                                    "of the file will be considered the root directory of the 'project'.",
                                                            (e, p)  => e.OutputFile = p,                ParamAttrs.Optional        },

            { "-s", "--styles",     "Comma-separated list of CSS files to be linked to the document. "+
                                    "If at the end of each file the characters :i are appended, the style will " +
                                    "be included in an style tag, if not, it will be used with a link tag and the " +
                                    ".css files will be copied to the [css] directory.",
                                                            (e, p)  => e.Styles.AddRange(p.Split(',')), ParamAttrs.Optional        },

            { "-js", "--scripts",   "Comma-separated list of JS files to be linked to the document. They will " +
                                    "be copied to the [js] folder.",
                                                            (e, p)  => e.Scripts.AddRange(p.Split(',')), ParamAttrs.Optional       },
            
            { "-c", "--content",    "Comma-separated list of resources files and directories to be copied to the " +
                                    "[content] folder",
                                                            (e, p)  => e.Resources.AddRange(p.Split(',')), ParamAttrs.Optional     },

            { "-a", "--append",     "If destination file exists, the output is appended to it, " +
                                    "if not the output file will be created",
                                                                (e)     => e.AppendOutput = true,          ParamAttrs.Optional     },

            { "-b", "--body",       "If present, the output is just the body inner xml (without body tags)",
                                                                (e)     => e.JustBodyContent = true,      ParamAttrs.Optional      },
            
            /*{ "-d", "--document",   "HTML file to load and to replace its body with the result of the parsing. " +
                                    "If --append is present, the output will be appended to the document body.",        
                                                                (e)      => e.Document = p,              false   },*/

            { "-t", "--tokens",     "Saves the tokenization phase in the file specified by this parameter",    
                                                            (e, p)  => e.TokensOutputFile = p,           ParamAttrs.Optional       },

            { "-w", "--watch",      "Detects changes in the input file, scripts, styles and other resources " +
                                    "to trigger the parsing process. If present, the watch will run every 1000ms. " +
                                    "If user provides a value it will be used instead",          
                                                            (e, p)  =>  e.Watch = int.TryParse(p, out int pt) ? pt : 1000,
                                                                                                         ParamAttrs.Optional | ParamAttrs.OptionalValue  },

            { "-h", "--help",       "Show this message",    (e)     => e.RequestHelpMessage(),           ParamAttrs.Optional       }
        };

        private HtmlToolEnv env;

        public bool ProcessArguments(string[] args)
        {
            env = new HtmlToolEnv(Parameters);
            return env.ProcessEnvArguments(args);
        }

        public void Run()
        {
            if (env.Watch.HasValue)
            {
                List<string> observed = new List<string>(env.InputFiles);
                observed.AddRange(env.Styles.Where(s => File.Exists(s)).ToList());
                observed.AddRange(env.Scripts.Where(s => File.Exists(s)).ToList());
                FileObserver.OnFileChangeDetect(observed, env.Watch.Value, () => Exec(string.Join("\n\n", env.InputFiles.Select(f => File.ReadAllText(f)))));
            }
            else if (env.Interactive)
            {
                Console.WriteLine("DML CLI (Interactive mode)");
                Console.WriteLine("Copyright (c) Leo Brugnara\n");
                Console.WriteLine("Enter an EOF string:");
                string eof = Console.ReadLine();

                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    Console.WriteLine("Input:");
                    do
                    {
                        string input = Console.ReadLine();

                        if (input == eof)
                            break;

                        sb.AppendLine(input);
                    } while (true);

                    Console.WriteLine("Output:");
                    this.Exec(sb.ToString());

                    sb.Clear();
                }
            }
            else
            {
                Exec(string.Join("\n\n", env.InputFiles.Select(f => File.ReadAllText(f))));
            }
        }

        private void Exec(string source)
        {
            // Parse the DML file
            Parser parser = new Parser();
            DmlDocument doc = parser.Parse(source);

            if (!env.JustBodyContent)
            {
                doc.Head.AddChild(new CustomNode("meta", new Dictionary<string, string>(){
                    { "charset", "utf8"}
                }));

                env.Styles.ForEach(s => {
                    string filename = s;
                    bool inc = false;
                    if (filename.EndsWith(":i"))
                    {
                        inc = true;
                        filename = filename.Substring(0, s.Length-2);
                    }
                    
                    CustomNode cssNode = null;
                    if (inc)
                    {
                        cssNode = new CustomNode("style");
                        cssNode.AddChild(new TextNode(File.ReadAllText(filename)));
                    }
                    else
                    {
                        cssNode = new CustomNode("link");
                        cssNode.Attributes["rel"] = "stylesheet";
                        cssNode.Attributes["href"] = string.Join("/", new [] { ".", "css",  new FileInfo(filename).Name });
                    }
                    doc.Head.AddChild(cssNode);
                });

                env.Scripts.ForEach(s => {
                    string filename = s;
                    bool inc = false;
                    if (filename.EndsWith(":i"))
                    {
                        inc = true;
                        filename = filename.Substring(0, s.Length-2);
                    }
                    
                    CustomNode script = new CustomNode("script");
                    script.Attributes["type"] = "text/javascript";
                    if (inc)
                    {
                        script.AddChild(new TextNode(File.ReadAllText(filename)));
                    }
                    else
                    {
                        script.Attributes["src"] = string.Join("/", new [] { ".", "js",  new FileInfo(filename).Name });
                    }
                    doc.Head.AddChild(script);
                });
            }

            // Send to output file
            string output = env.JustBodyContent ? doc.Body.InnerXml : doc.OuterXml;
            if (env.OutputFile != null)
            {
                DirectoryInfo di = new FileInfo(env.OutputFile).Directory;
                if (!di.Exists)
                {
                    di.Create();
                }

                File.WriteAllText(env.OutputFile, output);

                if (!env.JustBodyContent)
                {
                    // Copy CSS
                    DirectoryInfo cssDir = new DirectoryInfo(di.FullName + Path.DirectorySeparatorChar + "css");
                    if (!cssDir.Exists)
                    {
                        cssDir.Create();
                    }
                    env.Styles.Where(f => !f.EndsWith(":i")).ToList().ForEach(filename => {
                        FileInfo srcfile = new FileInfo(filename);
                        FileInfo dst = new FileInfo(cssDir.FullName + Path.DirectorySeparatorChar + srcfile.Name);
                        //File.WriteAllText(dst.FullName, File.ReadAllText(filename));
                        File.Copy(srcfile.FullName, dst.FullName, true);
                    });

                    // Copy JS
                    DirectoryInfo jsDir = new DirectoryInfo(di.FullName + Path.DirectorySeparatorChar + "js");
                    if (!jsDir.Exists)
                    {
                        jsDir.Create();
                    }
                    env.Scripts.Where(f => !f.EndsWith(":i")).ToList().ForEach(filename => {
                        FileInfo srcfile = new FileInfo(filename);
                        FileInfo dst = new FileInfo(jsDir.FullName + Path.DirectorySeparatorChar + srcfile.Name);
                        //File.WriteAllText(dst.FullName, File.ReadAllText(filename));
                        File.Copy(srcfile.Name, dst.FullName, true);
                    });

                    // Copy Resources
                    DirectoryInfo contentDir = new DirectoryInfo(di.FullName + Path.DirectorySeparatorChar + "content");
                    if (!contentDir.Exists)
                    {
                        contentDir.Create();
                    }
                    Action<string, string> copyFile = (srcfile, dstpath) => {
                        FileInfo dst = new FileInfo(dstpath + Path.DirectorySeparatorChar + srcfile);
                        //File.WriteAllText(dst.FullName, File.ReadAllText(srcfile));
                        File.Copy(srcfile, dst.FullName, true);
                    };

                    Action<string> copyDir = (srcdir) => {
                        foreach (string childDir in Directory.GetDirectories(srcdir, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(childDir.Replace(srcdir, contentDir.FullName));
                        }

                        foreach (string childFile in Directory.GetFiles(srcdir, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(childFile, childFile.Replace(srcdir, contentDir.FullName), true);
                        }
                    };

                    Action<string> copy = (file) => {
                        FileAttributes attrs = File.GetAttributes(file);
                        if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            copyDir(file);
                        }
                        else
                        {
                            copyFile(file, contentDir.FullName);
                        }
                    };

                    env.Resources.ForEach(copy);
                }
            }
            else
            {
                Console.WriteLine(output);
            }

            if (env.TokensOutputFile != null)
            {
                DirectoryInfo di = new FileInfo(env.TokensOutputFile).Directory;
                if (!di.Exists)
                {
                    di.Create();
                }
                File.WriteAllText(env.TokensOutputFile, string.Join("\n", new Lexer(source).Tokenize().Select(t => $"[{t.Type}, '{t.Value}'")));
            }
        }
    }
}