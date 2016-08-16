//
// Copyright (c) 2016 wkmghn.
// 
// Use, modification and distribution is subject to the Boost Software License,
// Version 1.0. (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SimpleArchive;

namespace sarc
{
    class Program
    {
        static int Main(string[] args)
        {
            // 引数をパース
            Options options;
            try
            {
                options = new Options(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("    sarc /Create <Archive> <Files...>");
                Console.WriteLine("    sarc /Append <Archive> <Files...>");
                Console.WriteLine("    sarc /Update <Archive> <Files...>");
                Console.WriteLine("    sarc /Delete <Archive> <Entries...>");
                Console.WriteLine("    sarc /Extract [/Directory DIR] [/OverwriteFiles] <Archive> <Entries...>");
                Console.WriteLine("    sarc /List <Archive>");
                Console.WriteLine();
                Console.WriteLine("Error: " + e.Message);
                return 1;
            }

            // コマンドを実行
            try
            {
                switch (options.Command)
                {
                    case MainCommand.Create:
                        DoCreate(options);
                        break;
                    case MainCommand.Append:
                        DoAppend(options);
                        break;
                    case MainCommand.Update:
                        DoUpdate(options);
                        break;
                    case MainCommand.Delete:
                        DoDelete(options);
                        break;
                    case MainCommand.Extract:
                        DoExtract(options);
                        break;
                    case MainCommand.List:
                        DoList(options);
                        break;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return 1;
            }

            return 0;
        }

        static void DoCreate(Options options)
        {
            ValidateArchivePath(options.ArchivePath, false, true);

            // ファイルが存在するかチェック
            foreach (string path in options.ContentPaths)
            {
                if (Directory.Exists(path))
                {
                    throw new IOException(string.Format("指定されたパス {0} がファイルではなくディレクトリを指しています。", path));
                }

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(string.Format("指定されたファイル {0} が見つかりません。", path), path);
                }
            }

            // ファイル名に重複がないかチェック

            {
                HashSet<string> entryNames = new HashSet<string>();
                foreach (string path in options.ContentPaths)
                {
                    string entryName = GetEntryNameFromFilePath(path);
                    if (!entryNames.Add(entryName))
                    {
                        throw new IOException(string.Format("ファイル名 {0} が複数指定されています。", entryName));
                    }
                }
            }

            bool fileCreationSucceeded = false;
            try
            {
                // アーカイブファイルを作成
                FileStream archiveStream = new FileStream(options.ArchivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                fileCreationSucceeded = true;
                using (archiveStream)
                using (Archive archive = new Archive(archiveStream, ArchiveMode.Create))
                {
                    // ファイルをアーカイブに追加していく
                    foreach (string path in options.ContentPaths)
                    {
                        AddFileToArchive(path, archive);
                    }
                }
            }
            catch(Exception e)
            {
                // 中途半端に作ってしまったアーカイブファイルを削除する
                if (fileCreationSucceeded)
                {
                    File.Delete(options.ArchivePath);
                }

                throw new Exception("アーカイブの作成に失敗しました。", e);
            }
        }

        static void DoAppend(Options options)
        {
            ValidateArchivePath(options.ArchivePath, true, false);

            // ファイルが存在するかチェック
            foreach (string path in options.ContentPaths)
            {
                if (Directory.Exists(path))
                {
                    throw new IOException(string.Format("指定されたパス {0} がファイルではなくディレクトリを指しています。", path));
                }

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(string.Format("指定されたファイル {0} が見つかりません。", path), path);
                }
            }

            // ファイル名に重複がないかチェック
            HashSet<string> entryNames = new HashSet<string>();
            foreach (string path in options.ContentPaths)
            {
                string entryName = GetEntryNameFromFilePath(path);
                if (!entryNames.Add(entryName))
                {
                    throw new IOException(string.Format("ファイル名 {0} が複数指定されています。", entryName));
                }
            }

            using (FileStream archiveStream = new FileStream(options.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (Archive archive = new Archive(archiveStream, ArchiveMode.Update))
            {
                // アーカイブ内の既存のファイルと、追加するファイル間で重複がないかチェック
                foreach (ArchiveEntry entry in archive.Entries)
                {
                    if (!entryNames.Add(entry.Name))
                    {
                        throw new IOException(string.Format("{0} という名前のファイルは既にアーカイブ内に存在します。", entry.Name));
                    }
                }

                // ファイルをアーカイブに追加する
                foreach (string path in options.ContentPaths)
                {
                    AddFileToArchive(path, archive);
                }
            }
        }

        static void DoUpdate(Options options)
        {
            ValidateArchivePath(options.ArchivePath, true, false);

            // ファイルが存在するかチェック
            foreach (string path in options.ContentPaths)
            {
                if (Directory.Exists(path))
                {
                    throw new IOException(string.Format("指定されたパス {0} がファイルではなくディレクトリを指しています。", path));
                }

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(string.Format("指定されたファイル {0} が見つかりません。", path), path);
                }
            }

            // ファイル名に重複がないかチェック
            HashSet<string> entryNames = new HashSet<string>();
            foreach (string path in options.ContentPaths)
            {
                string entryName = GetEntryNameFromFilePath(path);
                if (!entryNames.Add(entryName))
                {
                    throw new IOException(string.Format("ファイル名 {0} が複数指定されています。", entryName));
                }
            }

            using (FileStream archiveStream = new FileStream(options.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (Archive archive = new Archive(archiveStream, ArchiveMode.Update))
            {
                // ファイルを更新or追加する
                foreach (string path in options.ContentPaths)
                {
                    string entryName = GetEntryNameFromFilePath(path);
                    // エントリを取得する。無ければ作る。
                    ArchiveEntry entry = archive.GetEntry(entryName);
                    if (entry == null)
                    {
                        entry = archive.CreateEntry(entryName);
                    }

                    // エントリの内容を更新。
                    using (Stream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    using (Stream entryStream = entry.Open())
                    {
                        entryStream.SetLength(0);
                        entryStream.Position = 0;
                        fileStream.CopyTo(entryStream);
                    }
                }
            }
        }

        static void DoDelete(Options options)
        {
            ValidateArchivePath(options.ArchivePath, true, false);

            if (options.EntryNames.Count == 0)
            {
                throw new IOException("削除するファイルが指定されていません。");
            }

            using (FileStream archiveStream = new FileStream(options.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (Archive archive = new Archive(archiveStream, ArchiveMode.Update))
            {
                foreach (string entryName in options.EntryNames)
                {
                    ArchiveEntry entry = archive.GetEntry(entryName);
                    if (entry == null)
                    {
                        Console.WriteLine("Skip: " + entryName + " (File doesn't exist in the Archive.");
                        continue;
                    }
                    entry.Delete();
                }
            }
        }

        static void DoExtract(Options options)
        {
            ValidateArchivePath(options.ArchivePath, true, false);

            int extractedFiles = 0;

            Action<ArchiveEntry> extractEntry = delegate (ArchiveEntry entry)
            {
                string path = (options.Directory != null) ? Path.Combine(options.Directory, entry.Name) : entry.Name;
                if (Directory.Exists(path))
                {
                    throw new IOException(string.Format("ファイル名 {0} と同じ名前のディレクトリが出力先に存在します。", entry.Name));
                }
                if (File.Exists(path) && !options.OverwriteFiles)
                {
                    Console.WriteLine("Skip: " + entry.Name + " (File exists in output directory.)");
                    return;
                }
                using (Stream entryStream = entry.Open())
                using (Stream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    entryStream.Position = 0;
                    entryStream.CopyTo(fileStream);
                    ++extractedFiles;
                }
            };

            using (FileStream archiveStream = new FileStream(options.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Archive archive = new Archive(archiveStream, ArchiveMode.Read))
            {
                // 出力先ディレクトリがないなら作る
                if (options.Directory != null && !Directory.Exists(options.Directory))
                {
                    Directory.CreateDirectory(options.Directory);
                }

                if (options.EntryNames.Count == 0)
                {
                    // 全ファイルを抽出
                    foreach (ArchiveEntry entry in archive.Entries)
                    {
                        extractEntry(entry);
                    }
                }
                else
                {
                    // 指定のファイルだけを抽出
                    foreach (string entryName in options.EntryNames)
                    {
                        ArchiveEntry entry = archive.GetEntry(entryName);
                        if (entry == null)
                        {
                            Console.WriteLine("Skip: " + entryName + " (File doesn't exist in the Archive.)");
                            continue;
                        }
                        extractEntry(entry);
                    }
                }
            }

            Console.WriteLine("Extracted {0} file(s).", extractedFiles);
        }

        static void DoList(Options options)
        {
            ValidateArchivePath(options.ArchivePath, true, false);

            using (FileStream archiveStream = new FileStream(options.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Archive archive = new Archive(archiveStream, ArchiveMode.Read))
            {
                Console.WriteLine("{0,-10} {1,-5} Name", "Size", "Align");
                foreach (ArchiveEntry entry in archive.Entries)
                {
                    Console.WriteLine("{0,10} {1,5} {2}", entry.Length, entry.Alignment, entry.Name);
                }
                Console.WriteLine("{0} File(s) in {1}.", archive.Entries.Count, options.ArchivePath);
            }
        }

        // アーカイブをチェックする。
        static void ValidateArchivePath(string archivePath, bool needExists = false, bool needNotExists = false)
        {
            if (string.IsNullOrEmpty(archivePath))
            {
                throw new ArgumentException("アーカイブパスが指定されていません。");
            }

            if (needExists)
            {
                if (!File.Exists(archivePath))
                {
                    throw new FileNotFoundException("アーカイブファイルが存在しません。", archivePath);
                }
            }

            if (needNotExists)
            {
                if (File.Exists(archivePath))
                {
                    throw new IOException("アーカイブファイルが既に存在しています。");
                }
                if (Directory.Exists(archivePath))
                {
                    throw new IOException("アーカイブファイルと同名のディレクトリが既に存在しています。");
                }
            }
        }

        static string GetEntryNameFromFilePath(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        // アーカイブにファイルを追加する。同名のファイルがアーカイブ内に無いことを想定している。
        static void AddFileToArchive(string filePath, Archive archive)
        {
            string entryName = GetEntryNameFromFilePath(filePath);
            ArchiveEntry entry = archive.CreateEntry(entryName);
            using (Stream entryStream = entry.Open())
            using (Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(entryStream);
            }
        }
    }
}
