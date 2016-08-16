//
// Copyright (c) 2016 wkmghn.
// 
// Use, modification and distribution is subject to the Boost Software License,
// Version 1.0. (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sarc
{
    enum MainCommand
    {
        Help,
        Create,
        Append,
        Update,
        Delete,
        Extract,
        List,
        //Sort,
    }

    class Options
    {
        // ◆コマンド
        // /Create [Options] ARCHIVE[ FILE1[ FILE2[ ...]]]
        //     アーカイブを新規に作成する。
        //     既に ARCHIVE が存在する場合は失敗を報告する。(ARCHIVE に変更は加えない)
        //     ファイルの列挙中に同名のファイルを見つけた場合は処理に失敗する。
        //     /C でも同じ。
        // /Append [Options] ARCHIVE FILE1[ FILE2[ ...]]
        //     ARCHIVE の末尾に FILE を追加する。
        //     同名のファイルが ARCHIVE 内に既にある場合は処理に失敗する。
        //     /A でも同じ。
        // /Update [Options] ARCHIVE FILE1[ FILE2[ ...]]
        //     ARCHIVE に同名のファイルが既に存在する場合、その内容を指定されたファイルの内容で置き換える。
        //     ARCHIVE に同名のファイルが存在しない場合は Append と同様に動作する。
        //     /U でも同じ。
        // /Delete [Options] ARCHIVE ENTRY1[ ENTRY2[ ...]]
        //     ARCHIVE から ENTRY を削除する。
        //     ENTRY が ARCHIVE に存在しない場合は単に無視する。
        //     /D でも同じ。
        // /Extract [Options] ARCHIVE[ ENTRY1[ ENTRY2[ ...]]]
        //     ARCHIVE の内容を抽出する。
        //     /Directory を指定しなかった場合はカレントディレクトリに抽出する。
        //     ENTRY を指定しなかった場合はすべてのファイルを抽出する。
        //     ENTRY が ARCHIVE に存在しない場合は単に無視する。
        //     /X でも同じ。
        // /List ARCHIVE
        //     ARCHIVE に含まれるファイルの一覧を表示する。
        //     /L でも同じ。
        // /Sort CRITERIA ORDER ARCHIVE
        //     ARCHIVE の内容を CRITERIA ORDER に従ってソートする。
        //     /Sort コマンドはほかのコマンドのオプションとしても利用できる。

        // ◆オプション
        // /OverwriteFiles
        //     /Extract のオプション。
        //     抽出したファイルと同名のファイルが既に存在する場合、既存のファイルを上書きします。
        // /Directory DIR
        //     /Extract のオプション。
        //     抽出先のディレクトリを指定する。
        //     DIR が存在しない場合は作成する。
        //     DIR が既存のファイルを指す場合は処理に失敗する。
        // /Sort CRITERIA ORDER
        //     /Create /Append /Update /Delete のオプション。
        //     アーカイブ内のファイルをソートする。
        //     CRITERIA: FileName
        //     ORDER   : Ascending, Descending
        //     指定しなかった場合はソートされない。
        //     /Sort は単体のコマンドとしても利用できる。

        public MainCommand Command { get { return m_command; } }
        MainCommand m_command;

        // Extract: 抽出したファイルと同名のファイルが既に存在する場合、既存のファイルを上書きするかどうか。
        public bool OverwriteFiles { get { return m_optOverwriteFiles.Value; } }
        WithDefault<bool> m_optOverwriteFiles = new WithDefault<bool>(false);

        // Extract: 抽出先のディレクトリ。指定がない場合は null。
        public string Directory { get { return m_optDirectory.Value; } }
        WithDefault<string> m_optDirectory = new WithDefault<string>(null);

        // Create: 新しいアーカイブに含めるファイルのリスト。空の場合もある。その場合は要素数がゼロのアーカイブを作成する。
        // Append: 既存のアーカイブに追加するファイルのリスト。
        // Update: 既存のアーカイブに追加、もしくは更新するファイルのリスト。
        public IReadOnlyList<string> ContentPaths { get { return m_contentPaths; } }
        List<string> m_contentPaths = new List<string>();

        // Delete: アーカイブから削除するファイル名のリスト。
        // Extract: アーカイブから抽出するファイル名のリスト。空の場合もある。
        public IReadOnlyList<string> EntryNames { get { return m_entryNames; } }
        List<string> m_entryNames = new List<string>();

        // Create: 新しく作成するアーカイブのパス
        // Append, Update, Delete, Extract, List: 既存のアーカイブのパス
        public string ArchivePath { get { return m_archivePath; } }
        string m_archivePath = null;

        // 引数のパースに失敗した場合は例外を投げる。
        public Options(string[] args)
        {
            Debug.Assert(args != null);
            if (args.Length == 0)
            {
                throw new ArgumentException("引数を指定してください。");
            }

            Context context = new Context(args);

            // コマンドをパース
            m_command = ParseMainCommand(context.CurrentArg);
            context.Next();

            // 共通のオプションをパース
            ParseOptions(context, m_command);

            // コマンドごとに処理を分ける
            switch (m_command)
            {
                case MainCommand.Help:
                    break;
                case MainCommand.Create:
                    ParseToCreate(context);
                    break;
                case MainCommand.Append:
                    ParseToAppend(context);
                    break;
                case MainCommand.Update:
                    ParseToUpdate(context);
                    break;
                case MainCommand.Delete:
                    ParseToDelete(context);
                    break;
                case MainCommand.Extract:
                    ParseToExtract(context);
                    break;
                case MainCommand.List:
                    ParseToList(context);
                    break;
            }
        }

        private static MainCommand ParseMainCommand(string s)
        {
            switch (s)
            {
                case "/Help":
                case "/?":
                    return MainCommand.Help;
                case "/Create":
                case "/C":
                    return MainCommand.Create;
                case "/Append":
                case "/A":
                    return MainCommand.Append;
                case "/Update":
                case "/U":
                    return MainCommand.Update;
                case "/Delete":
                case "/D":
                    return MainCommand.Delete;
                case "/Extract":
                case "/X":
                    return MainCommand.Extract;
                case "/List":
                case "/L":
                    return MainCommand.List;
            }

            throw new ArgumentException("文字列からコマンドを特定できません。");
        }

        // コマンド直後に指定されるオプション類をパースする
        private void ParseOptions(Context context, MainCommand currentCommand)
        {
            if (currentCommand == MainCommand.Help)
            {
                return;
            }

            while (context.CurrentArg.StartsWith("/"))
            {
                switch (context.CurrentArg)
                {
                    case "/OverwriteFiles":
                        m_optOverwriteFiles.Set(true);
                        context.Next();
                        break;
                    case "/Directory":
                        context.Next();
                        m_optDirectory.Set(context.CurrentArg);
                        context.Next();
                        break;
                    default:
                        throw new ArgumentException(string.Format("不明なオプション {0} が指定されました。", context.CurrentArg));
                }
            }
        }

        // 残りの引数をすべてファイルへのパスと解釈して、ContentPaths に追加する。
        private void ParseContentPaths(Context context)
        {
            while (0 < context.RestArgsCount)
            {
                m_contentPaths.Add(context.CurrentArg);
                context.Next();
            }
        }

        // 残りの引数をすべてアーカイブ内のファイル名と解釈して、EntryNames に追加する。
        private void ParseEntryNames(Context context)
        {
            while (0 < context.RestArgsCount)
            {
                m_entryNames.Add(context.CurrentArg);
                context.Next();
            }
        }

        private void ParseToCreate(Context context)
        {
            m_archivePath = context.CurrentArg;
            context.Next();

            ParseContentPaths(context);
        }

        private void ParseToAppend(Context context)
        {
            m_archivePath = context.CurrentArg;
            context.Next();

            ParseContentPaths(context);
        }

        private void ParseToUpdate(Context context)
        {
            m_archivePath = context.CurrentArg;
            context.Next();

            ParseContentPaths(context);
        }

        private void ParseToDelete(Context context)
        {
            m_archivePath = context.CurrentArg;
            context.Next();

            ParseEntryNames(context);
        }

        private void ParseToExtract(Context context)
        {
            m_archivePath = context.CurrentArg;
            context.Next();

            ParseEntryNames(context);
        }

        private void ParseToList(Context context)
        {
            m_archivePath = context.CurrentArg;
            context.Next();

            if (context.RestArgsCount != 0)
            {
                throw new ArgumentException("引数が多すぎます。");
            }
        }

        class Context
        {
            public string[] AllArgs { get; private set; }
            public int AllArgsCount { get { return AllArgs.Length; } }
            // 現在の引数を含めた、残りの引数の数
            public int RestArgsCount { get { return AllArgsCount - CurrentArgIndex; } }
            public int CurrentArgIndex { get; private set; }

            public string CurrentArg
            {
                get
                {
                    if (AllArgs.Length <= CurrentArgIndex)
                    {
                        throw new ArgumentException("引数が足りません。");
                    }
                    return AllArgs[CurrentArgIndex];
                }
            }

            public Context(string[] args)
            {
                Debug.Assert(args != null);
                AllArgs = args;
                CurrentArgIndex = 0;
            }

            public void Next()
            {
                CurrentArgIndex = CurrentArgIndex + 1;
            }
        }

        class WithDefault<T>
        {
            public bool IsSpecified { get { return m_valueSpecified; } }
            public T Value { get { return m_valueSpecified ? m_specifiedValue: m_defaultValue; } }

            public WithDefault(T defaultValue)
            {
                m_defaultValue = defaultValue;
                m_specifiedValue = defaultValue;
                m_valueSpecified = false;
            }

            public void Set(T value)
            {
                m_specifiedValue = value;
                m_valueSpecified = true;
            }

            T m_defaultValue;
            T m_specifiedValue;
            bool m_valueSpecified;
        }
    }
}
