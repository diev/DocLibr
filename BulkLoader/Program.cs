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
using System.Linq;
using System.Threading.Tasks;

using Model;
using Tools;

namespace BulkLoader
{
    public class Program
    {
        #region Init
        //TODO: Option
        static readonly string pathSource = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static readonly string pathStore = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "DocLibr");
        static readonly int startLevel = 0; // Skip dateless archive folders like 2021\2021-01\* (2)

        //Skip possible exceptions with default options (no hidden, no restricted, etc.)
        static readonly EnumerationOptions IOoptions = new EnumerationOptions();

        static readonly Guid RootId = Guid.Empty; // Start folder's Links.PrevId
        #endregion

        #region Precalc
        static readonly DirectoryInfo dirSource = new DirectoryInfo(pathSource);
        static readonly DirectoryInfo dirStore = new DirectoryInfo(pathStore);

        static readonly string RootName = dirSource.Parent.Name;
        //static readonly int pathCut = dirSource.Parent.FullName.Length + 1;
        static readonly int pathCut = dirSource.FullName.Length + 1;

        static int totalLevels = 0;
        static long totalDirs = 0;
        static long totalFiles = 0;
        static long totalDoubles = 0;
        static long totalSize = 0;
        #endregion

        public static async Task Main(string[] args)
        {
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

            using (var db = new ApplicationContext())
            {
#if DEBUG
                await db.Database.EnsureDeletedAsync();
#endif
                await db.Database.EnsureCreatedAsync();
            }

            await EachDirAsync(dirSource, RootId, 0);

            #region Output
#if DEBUG
            Console.WriteLine($"\nLinks in {RootName} ({RootId}):\n");
            using (var db = new ApplicationContext())
            {
                var links = db.Links.ToList();
                foreach (var link in links)
                {
                    Console.WriteLine($"{link.PrevId} {link.NextId} {link.Path}");
                }
            }
#endif
            Console.WriteLine($"\nTotal Dirs: {totalDirs}, Levels: {totalLevels}-{startLevel}, Files: {totalFiles}+{totalDoubles}, Size: {totalSize} bytes done.\n");
            #endregion
        }

        /// <summary>
        /// Process recursively every folder with files and subfolders
        /// </summary>
        /// <param name="dir">Folder to process</param>
        public static async Task EachDirAsync(DirectoryInfo parentDir, Guid parentId, int level)
        {
            if (level > totalLevels)
            {
                totalLevels = level;
            }
            totalDirs++;


            if (level < startLevel) //skip
            {
                foreach (var dir in parentDir.GetDirectories("*", IOoptions))
                {
                    await EachDirAsync(dir, parentId, level + 1);
                }
            }
            else if (level == startLevel) //start
            {
                foreach (var file in parentDir.GetFiles("*", IOoptions))
                {
                    await EachFileAsync(file, parentId, true);
                }

                foreach (var dir in parentDir.GetDirectories("*", IOoptions))
                {
                    await EachDirAsync(dir, parentId, level + 1);
                }
            }
            else //deeper
            {
                var guid = FileIO.GuidPath(parentDir.FullName);

                // Normalize Name
                string name = parentDir.Name.Trim();
                while (name.Contains("  "))
                {
                    name = name.Replace("  ", " ");
                }
                name = name.ToUpper()[0] + name[1..];

                var item = new Item { Id = guid, Name = name, Registered = parentDir.CreationTime };
                var link = new Link { PrevId = parentId, NextId = guid, Path = parentDir.FullName[pathCut..] }; //TODO: pathCut+startLevel

                await AddDataAsync(item, link);

                Console.WriteLine($"{parentDir.FullName}");

                foreach (var file in parentDir.GetFiles("*", IOoptions))
                {
                    await EachFileAsync(file, guid, true);
                }

                foreach (var dir in parentDir.GetDirectories("*", IOoptions))
                {
                    await EachDirAsync(dir, guid, level + 1);
                }
            }
        }

        /// <summary>
        /// Process every file
        /// </summary>
        /// <param name="file">File to process</param>
        /// <param name="parentId">Id of parent directory</param>
        /// <param name="compression">Compress streams with GZip</param>
        public static async Task EachFileAsync(FileInfo file, Guid parentId, bool compression = false)
        {
            totalFiles++;
            totalSize += file.Length;
            string ext = file.Extension.ToLower();
            string ext2 = ext;
            string temp = Path.GetTempFileName(); // Path.Combine(pathStore, file.Name);

            // Normalize Extension
            switch (ext)
            {
                case ".jpeg":
                    ext = ".jpg";
                    break;
                case ".tiff":
                    ext = ".tif";
                    break;
            }

            try
            {
                if (compression)
                {
                    switch (ext)
                    {
                        case ".gz":
                        case ".zip":
                        case ".7z":
                        case ".arj":
                        case ".rar":
                        case ".avi":
                        case ".mp4":
                            //case ".jpg":
                            //case ".png":
                            file.CopyTo(temp, true);
                            break;

                        default:
                            await FileIO.CompressFileAsync(file, temp);
                            ext2 = ext + ".gz";
                            break;
                    }
                }
                else
                {
                    file.CopyTo(temp, true);
                }

                var guid = await FileIO.GuidFileAsync(temp);
                string path = FileIO.CreatePath(pathStore, guid, ext2);

                // Normalize Name
                string name = file.Name;
                name = name.Remove(name.Length - file.Extension.Length).Trim();
                while (name.Contains("  "))
                {
                    name = name.Replace("  ", " ");
                }
                name = name.ToUpper()[0] + name[1..];

                var item = new Item { Id = guid, Name = name, Ext = ext, Registered = file.LastWriteTime };
                var link = new Link { PrevId = parentId, NextId = guid, Path = file.FullName[pathCut..] };

                if (File.Exists(path) || !await AddDataAsync(item, link))
                {
                    totalDoubles++;
                    await AddDataAsync(link);

                    File.Delete(temp);
                }
                else // Unique file
                {
                    File.Move(temp, path);
                }
#if !DEBUG
                file.Delete();
#endif
            }
            catch
            {
                Console.WriteLine($"  ERROR: {file.FullName} skipped!");
            }
        }

        private static async Task<bool> AddDataAsync(Link link)
        {
            using var db = new ApplicationContext();
            db.Links.Add(link);

            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> AddDataAsync(Item item, Link link)
        {
            using var db = new ApplicationContext();
            db.Items.Add(item);
            db.Links.Add(link);

            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
