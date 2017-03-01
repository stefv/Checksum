//
// checksum
// Copyright(C) Stéphane VANPOPERYNGHE
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or(at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//

using md5sum;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared
{
    /// <summary>
    /// Abstract class for the checksum checker.
    /// </summary>
    public abstract class AbstractChecker
    {
        #region Fields
        /// <summary>
        /// Name of the algorithm used to encrypt.
        /// </summary>
        private string algorithm = null;

        /// <summary>
        /// True to display the help.
        /// </summary>
        private bool displayHelp = false;

        /// <summary>
        /// Check the files with the sums.
        /// </summary>
        private bool check = false;

        /// <summary>
        /// The files.
        /// </summary>
        private List<string> files = new List<string>();

        /// <summary>
        /// True to create a BSD checksum style.
        /// </summary>
        private bool tag = false;

        /// <summary>
        /// true if the check has errors.
        /// </summary>
        private bool hasCheckingErrors = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Create the checker class.
        /// </summary>
        /// <param name="algorithm">The name of the algorithm to use.</param>
        protected AbstractChecker(string algorithm)
        {
            this.algorithm = algorithm.ToUpper();
        }
        #endregion

        /// <summary>
        /// Perform the checksum.
        /// </summary>
        /// <param name="args"></param>
        public void Run(string[] args)
        {
            // Parse the argument and display help if it's required
            ParseArguments(args);
            if (displayHelp)
            {
                Help();
                Environment.Exit(1);
            }

            // Get the set of files
            ISet<string> filesToCheck = FileHelper.GetFileSet(files);

            foreach (string filename in filesToCheck)
            {
                FileInfo file = null;
                try
                {
                    file = new FileInfo(filename);
                }
                catch (Exception e)
                {
                    ConsoleHelper.LogError(filename + ": " + e.Message);
                    continue;
                }
                if (!check) Generate(file);
                else Check(file);
            }

            // Return the result
            Environment.Exit((check && hasCheckingErrors) ? 1 : 0);
        }

        /// <summary>
        /// Check a list of files from a source file.
        /// </summary>
        /// <param name="file">The source file.</param>
        private void Check(FileInfo file)
        {
            int errors = 0;

            using (StreamReader stream = file.OpenText())
            {
                string line = stream.ReadLine();
                while (line != null)
                {
                    string filename;
                    string originalHash;
                    if (IsTag(line))
                    {
                        Match match = Regex.Match(line, "^" + algorithm + " \\((.+)\\) = ([a-zA-Z0-9]+)$");
                        filename = match.Groups[1].Value;
                        originalHash = match.Groups[2].Value;
                    }
                    else
                    {
                        Match match = Regex.Match(line, "^([a-zA-Z0-9]+)  (.+)$");
                        originalHash = match.Groups[1].Value;
                        filename = match.Groups[2].Value;
                    }

                    FileInfo fileToCheck = new FileInfo(filename);
                    if (fileToCheck.Exists)
                    {
                        try
                        {
                            using (FileStream streamFile = File.OpenRead(filename))
                            {
                                string newHash = ToHex(ComputeHash(streamFile), false);
                                if (newHash == originalHash.ToLower()) Console.WriteLine(filename + ": OK");
                                else
                                {
                                    Console.Error.WriteLine(filename + ": FAILED");
                                    errors++;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            ConsoleHelper.LogError(filename + ": " + e.Message);
                        }

                    }
                    else
                    {
                        ConsoleHelper.LogError(Resources.err_file_dir_not_found, filename);
                    }

                    line = stream.ReadLine();
                }
            }

            if (errors == 1) ConsoleHelper.LogError(errors + " somme de contrôle ne correspond pas.");
            else if (errors > 1) ConsoleHelper.LogError(errors + " sommes de contrôle ne correspondent pas.");
            if (!hasCheckingErrors && errors > 0) hasCheckingErrors = true;
        }

        /// <summary>
        /// Check if the line is using a BSD format or not.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns></returns>
        private bool IsTag(string line)
        {
            if (Regex.Match(line, "^[a-zA-Z0-9]+  ").Success) return false;
            return true;
        }

        /// <summary>
        /// Generate to the standard output the data.
        /// </summary>
        /// <param name="file">The source fill to generate the hash.</param>
        private void Generate(FileInfo file)
        {
            if (!file.Exists)
            {
                ConsoleHelper.LogError(Resources.err_file_dir_not_found, file.FullName);
                return;
            }
            try
            {
                using (FileStream stream = File.OpenRead(file.FullName))
                {
                    byte[] hash = ComputeHash(stream);
                    WriteResult(file, hash);
                }
            }
            catch (Exception e)
            {
                ConsoleHelper.LogError(file.FullName + ": " + e.Message);
            }
        }

        /// <summary>
        /// The method to compute the hash with the desired algorithm.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The result.</returns>
        protected abstract byte[] ComputeHash(FileStream stream);

        /// <summary>
        /// Write the result to the standard output.
        /// </summary>
        /// <param name="file">The filename</param>
        /// <param name="hash">The hash.</param>
        private void WriteResult(FileInfo file, byte[] hash)
        {
            if (tag) Console.WriteLine(algorithm + " (" + file + ") = " + ToHex(hash, false));
            else Console.WriteLine(ToHex(hash, false) + "  " + file);
        }

        /// <summary>
        /// Parse the arguments.
        /// </summary>
        /// <param name="args">The arguments to parse.</param>
        private void ParseArguments(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    if (arg == "--help" || arg == "/?") displayHelp = true;
                    else if (arg == "--check" || arg == "-c") check = true;
                    else if (arg == "--tag") tag = true;
                    else ConsoleHelper.LogError(Resources.err_option, arg);
                }
                else files.Add(arg);
            }
        }


        /// <summary>
        /// Show the help.
        /// </summary>
        private void Help()
        {
            Console.WriteLine(Resources.text_help);
        }

        private static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            }

            return result.ToString();
        }
    }
}
