// Copyright (c) Leonardo Brugnara
// Full copyright and license information in LICENSE file

using System;

namespace DmlCli.Observer
{
    public class ObservedFile
    {
        public string FileName;
        public DateTime? LastModification;

        public ObservedFile(string f, DateTime? lastModif = null)
        {
            FileName = f;
            LastModification = lastModif;
        }
    }
}