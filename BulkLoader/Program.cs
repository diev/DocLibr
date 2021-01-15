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
using System.Threading.Tasks;
using Tools;

namespace BulkLoader
{
    public class Program
    {
        //TODO: Option
        readonly static string pathSource = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        readonly static string pathStore = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "DocLibr");

        public static async Task Main(string[] args)
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

            await EachDirAsync(dirSource);
        }

        /// <summary>
        /// Process recursively every folder with files and subfolders
        /// </summary>
        /// <param name="dir">Folder to process</param>
        public static async Task EachDirAsync(DirectoryInfo dir)
        {
            Guid guid = FileIO.GuidPath(dir.FullName);

            Console.WriteLine($@"{guid} {dir.FullName}\");

            //Skip possible exceptions with default options (no hidden, no restricted, etc.)
            EnumerationOptions options = new EnumerationOptions();

            foreach (var fi in dir.GetFiles("*", options))
            {
                await EachFileAsync(fi);
            }

            foreach (var di in dir.GetDirectories("*", options))
            {
                await EachDirAsync(di);
            }
        }

        /// <summary>
        /// Process every file
        /// </summary>
        /// <param name="file">File to process</param>
        public static async Task EachFileAsync(FileInfo file)
        {
            string ext = file.Extension.ToLower();
            string temp = Path.Combine(pathStore, file.Name);

            try
            {
                switch (ext)
                {
                    case ".gz":
                    case ".zip":
                        file.CopyTo(temp, true);
                        break;

                    default:
                        await FileIO.CompressFileAsync(file, temp);
                        ext += ".gz";
                        break;
                }

                Guid guid = await FileIO.GuidFileAsync(temp);
                string path = FileIO.CreatePath(pathStore, guid, ext);

                if (File.Exists(path)) // Add same file from another folder
                {
                    File.Delete(temp);
                    Console.WriteLine($"{file.FullName} exists!");
                }
                else // Add unique file
                {
                    File.Move(temp, path);
                    Console.WriteLine($"{guid} {file.FullName}");
                }
#if !DEBUG
                file.Delete();
#endif
            }
            catch
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"{file.FullName} skipped!");
                Console.ResetColor();
            }
        }
    }
}
