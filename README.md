# sarc
sarc は C++ のためのシンプルなアーカイブライブラリです。

sarc の主な目的は、一つ一つのバイナリファイルに非常に簡素なファイルシステムを内包することです。  
(このようなバイナリファイルは単に "パック" と呼ばれることもあります。)  
内包されるファイルシステムは読み取り専用で、ランタイムのコストができるだけ小さくなるように設計されています。

sarc の C++ ランタイムはアーカイブを読み取る機能しか持っていません。  
このため、アーカイブを作成・編集するためのコマンドラインツールと C# 向けライブラリが別途用意されています。

# C++ ランタイム
前述の通り、sarc の C++ ランタイムライブラリはアーカイブを読み込む機能しか持ちません。  
このためインターフェースはかなり小さなものになっています。

## C++ での使用例
~~~~
const void* binary = /* アーカイブファイルバイナリ */;
const uint32_t size = /* アーカイブファイルサイズ */;

// アーカイブファイルをパース
sarc::Archive arc(binary, size);

// アーカイブ内のファイルを取得
sarc::FileAccessor file = arc.find_file("filename.ext");

// ファイルの内容にアクセス
const uint8_t* data = reinterpret_cast<uint8_t*>(file.data());
for (uint32_t i = 0; i < file.file_size(); ++i) {
    printf("%02x ", data[i]);
}
~~~~

# コマンドラインツール
sarc のアーカイブはコマンドラインツールを使用して作成・変更できます。

## コマンドラインツールの使用例
~~~~
# アーカイブを作成
> sarc /Create arc.bin file1 file2 file3

# アーカイブの内容を表示
> sarc /List arc.bin
Size       Align Name
     15360     8 file1
     34304     8 file2
     15360     8 file3
3 File(s) in arc.bin.

# アーカイブにファイルを追加
> sarc /Append arc.bin file4

# アーカイブからファイルを削除
> sarc /Delete arc.bin file1

# アーカイブ内のファイルを抽出
> sarc /Extract /Directory extracted arc.bin file2
~~~~
