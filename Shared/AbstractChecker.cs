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

using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        /// True to display the version.
        /// </summary>
        private bool displayVersion = false;

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

        /// <summary>
        /// Don't display the OK lines.
        /// </summary>
        private bool quiet = false;

        /// <summary>
        /// Don't display messages.
        /// </summary>
        private bool status = false;
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

        #region Methods Main loopto generate or check the files
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
            if (displayVersion)
            {
                Version();
                Environment.Exit(1);
            }

            // Get the set of files
            ISet<string> filesToCheck = FileHelper.GetFileSet(files);

            // Generate or check the hashes
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
                                if (newHash == originalHash.ToLower())
                                {
                                    if (!quiet && !ConsoleHelper.Status) Console.WriteLine(filename + ": " + GetMessageOK());
                                }
                                else
                                {
                                    if (!ConsoleHelper.Status) ConsoleHelper.LogError(filename + ": " + GetMessageFAILED());
                                    errors++;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            ConsoleHelper.LogError(filename + ": " + e.Message);
                            errors++;
                        }

                    }
                    else
                    {
                        ConsoleHelper.LogError(GetMessageFileOrDirNotFound(), filename);
                        errors++;
                    }

                    line = stream.ReadLine();
                }
            }

            if (errors == 1) ConsoleHelper.LogError(GetMessageOneFileError(), errors);
            else if (errors > 1) ConsoleHelper.LogError(GetMessageMultiFilesError(), errors);
            if (!hasCheckingErrors && errors > 0) hasCheckingErrors = true;
        }

        /// <summary>
        /// Generate to the standard output the data.
        /// </summary>
        /// <param name="file">The source fill to generate the hash.</param>
        private void Generate(FileInfo file)
        {
            if (!file.Exists)
            {
                ConsoleHelper.LogError(GetMessageFileOrDirNotFound(), file.FullName);
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
        #endregion
        #region Methods for the messages
        /// <summary>
        /// Get the message when a file is OK.
        /// </summary>
        /// <returns>The message.</returns>
        protected abstract string GetMessageOK();

        /// <summary>
        /// Get the message when a file is FAILED.
        /// </summary>
        /// <returns>The message.</returns>
        protected abstract string GetMessageFAILED();

        /// <summary>
        /// Get the message when a file or a directory is not found.
        /// </summary>
        /// <returns>The message.</returns>
        protected abstract string GetMessageFileOrDirNotFound();

        /// <summary>
        /// Get the text for the help.
        /// </summary>
        /// <returns>The text.</returns>
        protected abstract string GetHelp();

        /// <summary>
        /// Get the message when an option if unknown.
        /// </summary>
        /// <returns>The message.</returns>
        protected abstract string GetUnknownOption();

        /// <summary>
        /// Get the message when one error has been found on a sum.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetMessageOneFileError();

        /// <summary>
        /// Get the message when more than one error has been found on sums.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetMessageMultiFilesError();
        #endregion
        #region Method to parse the arguments and display help
        /// <summary>
        /// Parse the arguments.
        /// </summary>
        /// <param name="args">The arguments to parse.</param>
        private void ParseArguments(string[] args)
        {
            // Search if it's to check
            foreach (string arg in args)
            {
                if (arg == "--check" || arg == "-c") check = true;
            }

            // Parse the other arguments
            foreach (string arg in args)
            {
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    if (arg == "--help" || arg == "/?") displayHelp = true;
                    else if (arg == "--version") displayVersion = true;
                    else if (arg == "--tag" && check) tag = true;
                    else if (arg == "--quiet" && check) quiet = true;
                    else if (arg == "--status" && check) ConsoleHelper.Status = true;
                    else if (arg == "--check" || arg == "-c") { }
                    else Console.Error.WriteLine(GetUnknownOption(), arg);
                }
                else files.Add(arg);
            }
        }

        /// <summary>
        /// Show the help.
        /// </summary>
        private void Help()
        {
            Console.WriteLine(GetHelp());
        }
        #endregion
        #region Method to display the version
        /// <summary>
        /// Show the version.
        /// </summary>
        private void Version()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Console.WriteLine("Version " + version);
        }
        #endregion

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
        /// Convert bytes to an hexadecimal value.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <param name="upperCase">true to convert to upper case.</param>
        /// <returns>The hexadecimal value.</returns>
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
