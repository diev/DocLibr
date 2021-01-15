#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Source https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public static class FileIO
    {
        private static readonly MD5 md5 = MD5.Create();

        /// <summary>
        /// Compress a file with GZip
        /// </summary>
        /// <param name="file">Source file</param>
        /// <param name="path">Destination file</param>
        public static async Task CompressFileAsync(FileInfo file, string path)
        {
            using FileStream originalStream = file.OpenRead();
            using FileStream gzipStream = File.Create(path);
            using GZipStream compressionStream = new GZipStream(gzipStream, CompressionMode.Compress);
            await originalStream.CopyToAsync(compressionStream);
        }

        /// <summary>
        /// Decompress a file with GZip
        /// </summary>
        /// <param name="filename">Source file</param>
        /// <param name="path">Destination file</param>
        public static async Task DecompressFileAsync(string filename, string path)
        {
            using FileStream gzipStream = File.OpenRead(filename);
            using FileStream originalStream = File.Create(path);
            using GZipStream decompressionStream = new GZipStream(gzipStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(originalStream);
        }

        /// <summary>
        /// Get the guid from the hash of a path
        /// </summary>
        /// <param name="path">Source path</param>
        /// <returns>Guid of path aka MD5 hash</returns>
        public static Guid GuidPath(string path)
        {
            byte[] data = Encoding.UTF8.GetBytes(path);
            byte[] bytes = md5.ComputeHash(data); // MD5 produces 16 bytes like GUID
            Guid hash = new Guid(bytes);
            return hash;
        }

        /// <summary>
        /// Get the guid from the hash of a file
        /// </summary>
        /// <param name="filename">Source file</param>
        /// <returns>Guid of file aka MD5 hash</returns>
        public static async Task<Guid> GuidFileAsync(string filename)
        {
            using FileStream fileStream = File.OpenRead(filename);
            byte[] bytes = await md5.ComputeHashAsync(fileStream); // MD5 produces 16 bytes like GUID
            Guid hash = new Guid(bytes);
            return hash;
        }

        /// <summary>
        /// Create the store path for a file
        /// </summary>
        /// <param name="basepath">Base path</param>
        /// <param name="hex">Hex hash of a file</param>
        /// <param name="ext">Extension of a new file</param>
        /// <returns>Destination filename without extension</returns>
        public static string CreatePath(string basepath, Guid guid, string ext)
        {
            const int width = 2; //TODO: Option Number of chars (2) in subdir names: \AB\CD\ABCDEF.ext

            string hex = guid.ToString();
            string dir = Path.Combine(basepath, hex.Substring(0, width), hex.Substring(width, width));
            string path = Path.Combine(dir, hex) + ext;
            Directory.CreateDirectory(dir); // and subfolders
            return path;
        }
    }
}
