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

using Shared;
using System.IO;
using System.Security.Cryptography;
using System;

namespace md5sum
{
    /// <summary>
    /// The program.
    /// </summary>
    class Program : AbstractChecker
    {
        #region Constructors
        /// <summary>
        /// Construct the program to use MD5.
        /// </summary>
        public Program() : base("MD5")
        {
        }
        #endregion

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">Argument for the program.</param>
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run(args);
        }

        /// <summary>
        /// Compute the hash using MD5.
        /// </summary>
        /// <param name="stream">The stream of the file.</param>
        /// <returns>The resulting hash.</returns>
        protected override byte[] ComputeHash(FileStream stream)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(stream);
                return hash;
            }
        }

        protected override string GetHelp()
        {
            throw new NotImplementedException();
        }

        protected override string GetMessageFAILED()
        {
            return Resources.text_failed;
        }

        protected override string GetMessageFileOrDirNotFound()
        {
            return Resources.err_file_dir_not_found;
        }

        protected override string GetMessageOK()
        {
            return Resources.text_ok;
        }

        protected override string GetUnknownOption()
        {
            return Resources.err_option;
        }
    }
}
