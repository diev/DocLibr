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

using Model;

using System;
using System.IO;

using Tools;

namespace BulkLoader
{
    public class Program
    {
        #region Init
        //TODO: Option
        static readonly string pathSource = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) 
        static readonly string pathStore = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "DocLibr");
        static readonly int skipLevels = 0; // Skip dateless archive folders like 2021\2021-01\* (2)
        static readonly bool compression = false;

        //Skip possible exceptions with default options (no hidden, no restricted, etc.)
        static readonly EnumerationOptions IOoptions = new EnumerationOptions();

        //static readonly Guid RootId = Guid.Empty; // Start folder's Links.ParentId
        #endregion

        #region Precalc
        static readonly DirectoryInfo dirSource = new DirectoryInfo(pathSource);
        static readonly DirectoryInfo dirStore = new DirectoryInfo(pathStore);

        static int totalLevels = 0;
        static long totalDirs = 0;
        static long totalFiles = 0;
        static long totalDoubles = 0;
        static long totalSize = 0;
        #endregion

        public static void Main(string[] args)
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

            using var db = new ApplicationContext();
#if DEBUG
            db.Database.EnsureDeleted();
#endif
            db.Database.EnsureCreated();

            Item rootItem = new Item
            {
                Id = Guid.Empty,
                Name = dirSource.FullName, //"ROOT",
                Ext = "*",
                No = "б/н"
            };

            db.Items.Add(rootItem);
            db.SaveChanges();

            EachDir(dirSource, rootItem, -skipLevels);

            #region Output
            Console.WriteLine($"\nTotal Dirs: {totalDirs}, Levels: {skipLevels}+{totalLevels}, Files: {totalFiles}+{totalDoubles}, Size: {totalSize} bytes done.\n");
            #endregion

            /// <summary>
            /// Process recursively every folder with files and subfolders
            /// </summary>
            /// <param name="parentDir">Folder to process</param>
            /// <param name="parentItem">Parent directory</param>
            /// <param name="level">Bypass a number of levels</param>
            void EachDir(DirectoryInfo parentDir, Item parentItem, int level = 0)
            {
                if (level > totalLevels)
                {
                    totalLevels = level;
                }
                totalDirs++;

                if (level >= 0)
                {
                    EachFile(parentDir, parentItem);
                }

                var dirs = parentDir.GetDirectories("*", IOoptions);
                foreach (var dir in dirs)
                {
                    if (level >= 0)
                    {
                        var guid = FileIO.GuidPath(dir.FullName);
                        string name = NormalizeName(dir);

                        var dirItem = new Item
                        {
                            Id = guid,
                            Name = name,
                            Registered = dir.CreationTime,
                            Ext = "*",
                            No = level.ToString(),
                            Comments = dir.FullName
                        };
                        dirItem.Parents.Add(parentItem);

                        db.Items.Add(dirItem);
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.InnerException.Message);
                        }

                        Console.WriteLine($"{dir.FullName}");
                        EachDir(dir, dirItem, level + 1);
                    }
                    else // skip
                    {
                        Console.WriteLine($"{dir.FullName} ---");
                        EachDir(dir, parentItem, level + 1);
                    }
                }

                static string NormalizeName(DirectoryInfo dir)
                {
                    string name = dir.Name.Trim();
                    while (name.Contains("  "))
                    {
                        name = name.Replace("  ", " ");
                    }
                    name = name.ToUpper()[0] + name[1..];
                    return name;
                }
            }

            /// <summary>
            /// Process every file in the directory
            /// </summary>
            /// <param name="parentDir">Folder to process</param>
            /// <param name="parentItem">Parent directory</param>
            void EachFile(DirectoryInfo parentDir, Item parentItem)
            {
                var files = parentDir.GetFiles("*", IOoptions);
                foreach (var file in files)
                {
                    totalFiles++;

                    if (file.Length == 0)
                    {
                        Console.WriteLine($"  ERROR: {file.FullName} length 0 - skipped!");
                        continue;
                    }

                    if (file.Name.Equals(file.Extension))
                    {
                        Console.WriteLine($"  ERROR: {file.FullName} hidden - skipped!");
                        continue;
                    }

                    totalSize += file.Length;
                    string ext = NormalizeExtension(file);
                    string ext2 = ext;
                    string temp = Path.GetTempFileName(); // Path.Combine(pathStore, file.Name);

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
                                    FileIO.CompressFile(file, temp);
                                    ext2 = ext + ".gz";
                                    break;
                            }
                        }
                        else
                        {
                            file.CopyTo(temp, true);
                        }

                        var guid = FileIO.GuidFile(temp);
                        string path = FileIO.CreatePath(pathStore, guid, ext2);
                        string name = NormalizeName(file);

                        var fileItem = new Item
                        {
                            Id = guid,
                            Name = name,
                            Ext = ext,
                            Registered = file.LastWriteTime,
                            No = totalFiles.ToString(),
                            Comments = file.FullName
                        };

                        var dbItem = db.Items.Find(guid);
                        if (dbItem == null)
                        {
                            fileItem.Parents.Add(parentItem);
                            db.Items.Add(fileItem);
                            try
                            {
                                db.SaveChanges();
                                if (File.Exists(path))
                                {
                                    Console.WriteLine($"  DOUBLE PATH: {guid} {path}");
                                    File.Delete(temp);
                                }
                                File.Move(temp, path);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                                File.Delete(temp);
                            }
                        }
                        else
                        { 
                            totalDoubles++;
                            Console.WriteLine($"  DOUBLE {guid}");
                            Console.WriteLine($"  1: {dbItem.Comments}");
                            Console.WriteLine($"  2: {fileItem.Comments}");
                            dbItem.Comments += " + " + fileItem.Comments;
                            dbItem.Parents.Add(parentItem);
                            try
                            {
                                db.SaveChanges();
                                if (File.Exists(path))
                                {
                                    //Console.WriteLine($"  DOUBLE PATH: {guid} {path}");
                                    File.Delete(temp);
                                }
                                else
                                {
                                    File.Move(temp, path);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.InnerException.Message);
                                File.Delete(temp);
                            }
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

                static string NormalizeName(FileInfo file)
                {
                    string name = file.Name;
                    if (file.Extension.Length > 0)
                    {
                        name = name.Remove(name.Length - file.Extension.Length).Trim();
                    }
                    while (name.Contains("  "))
                    {
                        name = name.Replace("  ", " ");
                    }
                    name = name.ToUpper()[0] + name[1..];
                    return name;
                }

                static string NormalizeExtension(FileInfo file)
                {
                    string ext = file.Extension.ToLower();
                    switch (ext)
                    {
                        case ".jpeg":
                            ext = ".jpg";
                            break;
                        case ".tiff":
                            ext = ".tif";
                            break;
                    }
                    return ext;
                }
            }
        }
    }
}
