// <copyright file="Program.cs" company="http://rikker.ru">
// Copyright (c) Rikker Serg 2012 All Right Reserved
// </copyright>
// <author>Rikker Serg</author>
// <email>serg@rikker.ru</email>
// <summary>Simple console app to combine multiple XAML resource dictionaries in one.</summary>
namespace XamlCombine
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Represents simple console app to combine multiple XAML resource dictionaries in one.
    /// Command line syntaxis:
    /// XamlCombine.exe [list-of-xamls.txt] [result-xaml.xaml]
    /// All paths must be relative to XamlCombine.exe location.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main function.
        /// </summary>
        /// <param name="args">Command line args.</param>
        private static int Main(string[] args)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // TODO: Add flags for some parameters.
                if (args.Length == 2)
                {
                    var sourceFile = args[0];
                    var resultFile = args[1];

                    using (var mutex = Lock(resultFile))
                    {
                        try
                        {
                            var combiner = new Combiner();
                            combiner.Combine(sourceFile, resultFile);
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }
                    }
                }

                // TODO: Add help output.

                stopwatch.Stop();
                Trace.WriteLine(string.Format("Combine time: {0}", stopwatch.Elapsed));

                if (Debugger.IsAttached)
                {
                    Console.ReadLine();
                }

                return 0;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);
                return 1;
            }
        }

        private static Mutex Lock(string file)
        {
            var appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value;
            var mutexName = $"Local\\{appGuid}_{GetMD5Hash(file)}";

            var mutex = new Mutex(false, mutexName);

            if (mutex.WaitOne(TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException("Another instance of this application blocked the concurrent execution.");
            }
            
            return mutex;
        }

        private static string GetMD5Hash(string textToHash)
        {
            if (string.IsNullOrEmpty(textToHash))
            {
                return string.Empty;
            }

            var md5 = new MD5CryptoServiceProvider();
            var bytesToHash = Encoding.Default.GetBytes(textToHash);
            var result = md5.ComputeHash(bytesToHash); 

            return BitConverter.ToString(result); 
        } 
    }
}