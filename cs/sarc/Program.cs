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
        static void Main(string[] args)
        {
            // ◆コマンド
            // /Create [Options] <Archive> <Files... and/or Directories...>
            //     アーカイブを新規に作成する。
            //     ディレクトリを指定した場合は、その中の全ファイルをアーカイブに含める。
            //     既に Archive が存在する場合は失敗を報告する。(Archive に変更は加えない)
            //     ファイルの列挙中に同名のファイルを見つけた場合は処理に失敗する。
            //     /C でも同じ。
            // /Append [Options] <Archive> <File>
            //     Archive の末尾に File を追加する。
            //     同名のファイルが Archive 内に既にある場合、アーカイブから既存のファイルを削除したあとに File を Archive の末尾に追加する。
            //     /A でも同じ。
            // /Delete [Options] <Archive> <File>
            //     Archive から File を削除する。
            //     File が Archive に存在しない場合は失敗を報告する。
            //     /D でも同じ。
            // /Extract [Options] <Archive> [Files...]
            //     Archive の内容を OutputDirectory に抽出する。
            //     /Directory を指定しなかった場合はカレントディレクトリに抽出する。
            //     Files を指定しなかった場合はすべてのファイルを抽出する。
            //     /X でも同じ。
            // /List <Archive>
            //     アーカイブに含まれるファイルの一覧を表示する。
            //     /L でも同じ。
            // /Sort CRITERIA ORDER <Archive>
            //     Archive の内容を CRITERIA ORDER に従ってソートする。
            //     /Sort コマンドはほかのコマンドのオプションとしても利用できる。

            // ◆オプション
            // /KeepOldFiles
            //     /Extract のオプション。
            //     抽出したファイルと同名のファイルが既に存在する場合、既存のファイルを上書きせずにそのまま残す。
            // /Directory DIR
            //     /Extract のオプション。
            //     抽出先のディレクトリを指定する。
            //     DIR が存在しない場合は作成する。
            //     DIR が既存のファイルを指す場合は処理に失敗する。
            // /Sort CRITERIA ORDER
            //     /Create /Append /Delete のオプション。
            //     アーカイブ内のファイルをソートする。
            //     CRITERIA: FileName
            //     ORDER   : Ascending, Descending
            //     指定しなかった場合はソートされない。
            //     /Sort は単体のコマンドとしても利用できる。

            // 更新
            //using (Stream stream = new FileStream("test.arc", FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            //using (Archive arc = new Archive(stream, ArchiveMode.Update))
            // 新規作成
            using (Stream stream = new FileStream("test.arc", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (Archive arc = new Archive(stream, ArchiveMode.Create))
            {
#if false
                //arc.CreateEntry("test1");
                //arc.CreateEntry("test2");
                ArchiveEntry entry = arc.GetEntry("test3");
                using (Stream s = entry.Open())
                {
                    s.SetLength(0);
                    s.Position = 0;
                }
                using (TextWriter writer = new StreamWriter(entry.Open(), Encoding.ASCII))
                {
                    writer.WriteLine("abcde");
                }
#endif
            }
        }
    }
}
