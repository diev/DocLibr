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

namespace BulkLoader
{
    class Program
    {
        //TODO: Option
        readonly static string pathSource = Path.Combine(Environment.ExpandEnvironmentVariables("%USERPROFILE%"), "Documents");
        readonly static string pathStore = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "DocLibr");

        static void Main(string[] args)
        {
            DirectoryInfo dirSource = new DirectoryInfo(pathSource);
            DirectoryInfo dirStore = new DirectoryInfo(pathStore);

#if DEBUG
            if (dirStore.Exists)
            {
                dirStore.Delete(true);
            }
#endif

            if (!dirStore.Exists)
            {
                dirStore.Create();
            }

            EachDir(dirSource);
        }

        /// <summary>
        /// Process recursively every folder with files and subfolders
        /// </summary>
        /// <param name="dir">Folder</param>
        static void EachDir(DirectoryInfo dir)
        {
            Console.WriteLine(dir.FullName);

            //Skip possible exceptions with default options (no hidden, no restricted, etc.)
            EnumerationOptions options = new EnumerationOptions();

            foreach (var fi in dir.GetFiles("*", options))
            {
                EachFile(fi);
            }

            foreach (var di in dir.GetDirectories("*", options))
            {
                EachDir(di);
            }
        }

        /// <summary>
        /// Process every file
        /// </summary>
        /// <param name="file">File</param>
        static void EachFile(FileInfo file)
        {
            const bool compress = true; //TODO: Option

            try
            {
                if (compress && !file.Extension.Equals(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    string temp = Path.GetTempFileName();
                    CompressFile(file, temp);
                    HashFile(temp, out string hex, out string base64, out string path);
                    File.Move(temp, path + file.Extension + ".gz", true);

                    Console.WriteLine($"{hex} {base64} {file.FullName}.gz");
                }
                else
                {
                    HashFile(file.FullName, out string hex, out string base64, out string path);
                    file.CopyTo(path + file.Extension, true);

                    Console.WriteLine($"{hex} {base64} {file.FullName}");
                }
            }
            catch
            {
                Console.WriteLine($"{file.FullName} skipped!");
            }
        }

        /// <summary>
        /// Compress a file with GZip
        /// </summary>
        /// <param name="file">Source file</param>
        /// <param name="path">Destination file</param>
        static void CompressFile(FileInfo file, string path)
        {
            using FileStream fileStream = file.OpenRead();
            using FileStream gzStream = File.Create(path);
            using GZipStream compressionStream = new GZipStream(gzStream, CompressionMode.Compress);
            fileStream.CopyTo(compressionStream);
        }

        /// <summary>
        /// Calc the hash of file and related values
        /// </summary>
        /// <param name="file">Source file</param>
        /// <param name="hex">Hex string in uppercase (length 32 for MD5)</param>
        /// <param name="base64">Base64 string without trail == (length 22 for MD5)</param>
        /// <param name="path">Destination file without any extension</param>
        static void HashFile(string file, out string hex, out string base64, out string path)
        {
            const int width = 2; //TODO: Option Number of chars (2) in subdir names: \AB\CD\ABCDEF.ext

            using FileStream fileStream = File.OpenRead(file);
            using MD5 algorithm = MD5.Create();
            byte[] hash = algorithm.ComputeHash(fileStream);

            hex = Convert.ToHexString(hash); // Length is 32 for MD5
            base64 = Convert.ToBase64String(hash).Substring(0, 22); // Length is 24 for MD5, then trim trailing ==

            path = Path.Combine(pathStore, hex.Substring(0, width), hex.Substring(width, width));
            Directory.CreateDirectory(path);
            path = Path.Combine(path, hex);
        }
    }
}
