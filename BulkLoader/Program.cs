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
using Tools;

namespace BulkLoader
{
    class Program
    {
        //TODO: Option
        readonly static string pathSource = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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

            foreach (var fi in dir.EnumerateFiles("*", options))
            {
                EachFile(fi);
            }

            foreach (var di in dir.EnumerateDirectories("*", options))
            {
                EachDir(di);
            }
        }

        /// <summary>
        /// Process every file
        /// </summary>
        /// <param name="file">File</param>
        static async void EachFile(FileInfo file)
        {
            const bool compress = true; //TODO: Option

            try
            {
                if (compress && !file.Extension.Equals(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    string temp = Path.GetTempFileName();
                    await FileIO.CompressFileAsync(file, temp);
                    byte[] hash = await FileIO.HashFileAsync(temp);

                    string hex = Convert.ToHexString(hash); // Length is 32 for MD5
                    string base64 = Convert.ToBase64String(hash).Substring(0, 22); // Length is 24 for MD5, then trim trailing ==

                    string path = FileIO.CreatePath(pathStore, hex, file.Extension + ".gz");

                    if (File.Exists(path))
                    {
                        File.Delete(temp);
                        Console.WriteLine($"{file.FullName}.gz exists!");
                    }
                    else
                    {
                        File.Move(temp, path);
                        Console.WriteLine($"{hex} {base64} {file.FullName}.gz");
                    }
                }
                else // compress == false
                {
                    byte[] hash = await FileIO.HashFileAsync(file.FullName);

                    string hex = Convert.ToHexString(hash); // Length is 32 for MD5
                    string base64 = Convert.ToBase64String(hash).Substring(0, 22); // Length is 24 for MD5, then trim trailing ==

                    string path = FileIO.CreatePath(pathStore, hex, file.Extension);

                    if (File.Exists(path))
                    {
                        Console.WriteLine($"{file.FullName} exists!");
                    }
                    else
                    {
                        file.CopyTo(path);
                        Console.WriteLine($"{hex} {base64} {file.FullName}");
                    }
                }
            }
            catch
            {
                Console.WriteLine($"{file.FullName} skipped!");
            }
        }
    }
}
