// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DmlCli.Observer
{
    public static class FileObserver
    {
        public static void OnFileChangeDetect(List<string> filepaths, int sleep, Action action, Func<bool> doWhile = null)
        {
            if (doWhile == null)
            {
                doWhile = () => true;
            }
            List<ObservedFile> observed = new List<ObservedFile>();
            filepaths.ForEach(f => observed.Add(new ObservedFile(f)));
            while (doWhile.Invoke())
            {
                foreach (ObservedFile target in observed)
                {
                    try
                    {
                        DateTime ft = File.GetLastWriteTime(target.FileName);
                        if (!target.LastModification.HasValue || target.LastModification < ft)
                        {
                            action.Invoke();
                            target.LastModification = ft;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e.Message}", ConsoleColor.Red);
                    }
                }
                Thread.Sleep(sleep);
            }
        }
    }
}