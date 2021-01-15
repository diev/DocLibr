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

using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Tools
{
    public static class FileIO
    {
        private static readonly MD5 algorithm = MD5.Create();

        /// <summary>
        /// Compress a file with GZip
        /// </summary>
        /// <param name="file">Source file</param>
        /// <param name="path">Destination file</param>
        public static async 
        /// <summary>
        /// Compress a file with GZip
        /// </summary>
        /// <param name="file">Source file</param>
        /// <param name="path">Destination file</param>
        Task
CompressFileAsync(FileInfo file, string path)
        {
            using FileStream originalStream = file.OpenRead();
            using FileStream gzipStream = File.Create(path);
            using GZipStream compressionStream = new GZipStream(gzipStream, CompressionMode.Compress);
            await originalStream.CopyToAsync(compressionStream);
        }

        /// <summary>
        /// Decompress a file with GZip
        /// </summary>
        /// <param name="file">Source file</param>
        /// <param name="path">Destination file</param>
        public static async void DecompressFileAsync(FileInfo file, string path)
        {
            using FileStream gzipStream = file.OpenRead();
            using FileStream originalStream = File.Create(path);
            using GZipStream decompressionStream = new GZipStream(gzipStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(originalStream);
        }

        /// <summary>
        /// Calc the hash of a file
        /// </summary>
        /// <param name="filename">Source file</param>
        /// <returns>32 bytes of MD5 hash</returns>
        public static async Task<byte[]> HashFileAsync(string filename)
        {
            using FileStream fileStream = File.OpenRead(filename);
            return await algorithm.ComputeHashAsync(fileStream);
        }

        /// <summary>
        /// Create the store path for a file
        /// </summary>
        /// <param name="basepath">Base path</param>
        /// <param name="hex">Hex hash of a file</param>
        /// <param name="ext">Extension of a new file</param>
        /// <returns>Destination filename without extension</returns>
        public static string CreatePath(string basepath, string hex, string ext)
        {
            const int width = 2; //TODO: Option Number of chars (2) in subdir names: \AB\CD\ABCDEF.ext

            string dir = Path.Combine(basepath, hex.Substring(0, width), hex.Substring(width, width));
            string path = Path.Combine(dir, hex) + ext;
            Directory.CreateDirectory(dir); // and subfolders
            return path;
        }
    }
}
