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
					var combiner = new Combiner();
					combiner.Combine(args[0], args[1]);
				}

				// TODO: Add help output.

				stopwatch.Stop();
				Console.WriteLine("Combine time: {0}", stopwatch.Elapsed);

				if (Debugger.IsAttached)
				{
					Console.ReadLine();
				}

				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return 1;
			}
        }
    }
}