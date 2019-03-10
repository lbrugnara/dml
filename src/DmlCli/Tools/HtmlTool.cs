// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CmdOpt.Options;
using DmlCli.Observer;
using DmlCli.Tools.Envs;
using DmlLib.Semantic;
using DmlLib.Nodes;

namespace DmlCli.Tools
{
    public class HtmlTool : ITool
    {
        private static readonly HtmlToolEnv Env = new HtmlToolEnv()
        {
            { "-i", "--input",      "Source file",          (e, p)  => e.InputFiles.AddRange(p), OptionAttributes.Optional | OptionAttributes.MultiValue},

            { "-it", "--interactive", "Interactive mode",   (e) => e.Interactive = true, OptionAttributes.Optional },

            { "-o", "--output",     "Destination file. If it is not specified the output will be sent " +
                                    "to stdout. If it includes paths, they will be created and the parent path " +
                                    "of the file will be considered the root directory of the 'project'.",
                                                            (e, p)  => e.OutputFile = p,                OptionAttributes.Optional        },

            { "-s", "--styles",     "Comma-separated list of CSS files to be linked to the document. "+
                                    "If at the end of each file the characters :i are appended, the style will " +
                                    "be included in an style tag, if not, it will be used with a link tag and the " +
                                    ".css files will be copied to the [css] directory.",
                                                            (e, p)  => e.Styles.AddRange(p.Split(',')), OptionAttributes.Optional        },

            { "-js", "--scripts",   "Comma-separated list of JS files to be linked to the document. They will " +
                                    "be copied to the [js] folder.",
                                                            (e, p)  => e.Scripts.AddRange(p.Split(',')), OptionAttributes.Optional       },

            { "-c", "--content",    "Comma-separated list of resources files and directories to be copied to the " +
                                    "[content] folder",
                                                            (e, p)  => e.Resources.AddRange(p.Split(',')), OptionAttributes.Optional     },

            { "-a", "--append",     "If destination file exists, the output is appended to it, " +
                                    "if not the output file will be created",
                                                                (e)     => e.AppendOutput = true,          OptionAttributes.Optional     },

            { "-b", "--body",       "If present, the output is just the body inner xml (without body tags)",
                                                                (e)     => e.JustBodyContent = true,      OptionAttributes.Optional      },
            
            /*{ "-d", "--document",   "HTML file to load and to replace its body with the result of the parsing. " +
                                    "If --append is present, the output will be appended to the document body.",        
                                                                (e)      => e.Document = p,              false   },*/

            { "-t", "--tokens",     "Saves the tokenization phase in the file specified by this parameter",
                                                            (e, p)  => e.TokensOutputFile = p,           OptionAttributes.Optional       },

            { "-w", "--watch",      "Detects changes in the input file, scripts, styles and other resources " +
                                    "to trigger the parsing process. If present, the watch will run every 1000ms. " +
                                    "If user provides a value it will be used instead",
                                                            (e, p)  =>  e.Watch = int.TryParse(p, out int pt) ? pt : 1000,
                                                                                                            OptionAttributes.Optional | OptionAttributes.OptionalValue  },

            { "-h", "--help",       "Show this message",    (e)     => e.RequestHelpMessage(),           OptionAttributes.Optional       }
        };

        public bool ProcessArguments(string[] args) => Env.ProcessEnvArguments(args);

        public void Run()
        {
            if (Env.Watch.HasValue)
            {
                List<string> observed = new List<string>(Env.InputFiles);
                observed.AddRange(Env.Styles.Where(s => File.Exists(s)).ToList());
                observed.AddRange(Env.Scripts.Where(s => File.Exists(s)).ToList());
                FileObserver.OnFileChangeDetect(observed, Env.Watch.Value, () => Exec(string.Join("\n\n", Env.InputFiles.Select(f => File.ReadAllText(f)))));
            }
            else if (Env.Interactive)
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
                Exec(string.Join("\n\n", Env.InputFiles.Select(f => File.ReadAllText(f))));
            }
        }

        private void Exec(string source)
        {
            // Parse the DML file
            Parser parser = new Parser();
            DmlDocument doc = parser.Parse(source);

            if (!Env.JustBodyContent)
            {
                doc.Head.AddChild(new CustomNode("meta", new Dictionary<string, string>(){
                    { "charset", "utf8"}
                }));

                Env.Styles.ForEach(s => {
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

                Env.Scripts.ForEach(s => {
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
            string output = Env.JustBodyContent ? doc.Body.InnerXml : doc.OuterXml;
            if (Env.OutputFile != null)
            {
                DirectoryInfo di = new FileInfo(Env.OutputFile).Directory;
                if (!di.Exists)
                {
                    di.Create();
                }

                File.WriteAllText(Env.OutputFile, output);

                if (!Env.JustBodyContent)
                {
                    // Copy CSS
                    DirectoryInfo cssDir = new DirectoryInfo(di.FullName + Path.DirectorySeparatorChar + "css");
                    if (!cssDir.Exists)
                    {
                        cssDir.Create();
                    }
                    Env.Styles.Where(f => !f.EndsWith(":i")).ToList().ForEach(filename => {
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
                    Env.Scripts.Where(f => !f.EndsWith(":i")).ToList().ForEach(filename => {
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

                    Env.Resources.ForEach(copy);
                }
            }
            else
            {
                Console.WriteLine(output);
            }

            if (Env.TokensOutputFile != null)
            {
                DirectoryInfo di = new FileInfo(Env.TokensOutputFile).Directory;
                if (!di.Exists)
                {
                    di.Create();
                }
                File.WriteAllText(Env.TokensOutputFile, string.Join("\n", new Lexer(source).Tokenize().Select(t => $"[{t.Type}, '{t.Value}'")));
            }
        }
    }
}