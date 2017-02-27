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

using System.Collections.Generic;
using System.IO;

namespace Shared.Helpers
{
    /// <summary>
    /// File helper class.
    /// </summary>
    public sealed class FileHelper
    {
        #region Constructors
        /// <summary>
        /// Static calls only.
        /// </summary>
        private FileHelper()
        {
        }
        #endregion

        /// <summary>
        /// Retrieve the set of files from filenames and wildcards.
        /// </summary>
        /// <param name="files">The filenames with wildcards.</param>
        /// <returns>The files found.</returns>
        public static ISet<string> GetFileSet(List<string> files)
        {
            ISet<string> result = new HashSet<string>();

            foreach (string filename in files)
            {
                if (filename.Contains("*") || filename.Contains("?"))
                {
                    string[] filesFound = Directory.GetFiles(".", filename);
                    foreach (string file in filesFound)
                    {
                        if (file.StartsWith(@".\")) result.Add(file.Substring(2));
                        else result.Add(file);
                    }
                }
                else
                {
                    result.Add(filename);
                }
            }

            return result;
        }
    }
}
