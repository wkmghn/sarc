using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SimpleArchive
{
    /// <summary>
    /// アーカイブを表します。
    /// </summary>
    public class Archive : IDisposable
    {
        // 's', 'a', 'r', 'c'
        private readonly byte[] MAGIC_NUMBER = new byte[] { 0x73, 0x61, 0x72, 0x63 };
        private const UInt32 FILE_HEADER_SIZE = 4 + 4 + 4;  // MagicNumber + Version + NumFiles
        private const UInt32 MINIMUM_FILE_SIZE = FILE_HEADER_SIZE;
        private const UInt32 CURRENT_VERSION = 1;

        public IReadOnlyCollection<ArchiveEntry> Entries { get { return m_entries; } }

        public Archive(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            if (path == "")
            {
                throw new ArgumentException("path が空です。", "path");
            }
            if (Directory.Exists(path))
            {
                throw new IOException(string.Format("指定されたパス \"{0}\" が既存のディレクトリを指しています。", path));
            }

            m_entries = new List<ArchiveEntry>();

            if (File.Exists(path))
            {
                // 既存のアーカイブファイルを開く
                try
                {
                    m_file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                }
                catch (Exception e)
                {
                    throw new IOException(string.Format("アーカイブファイル \"{0}\"を開けませんでした。", path), e);
                }

                // 既存のアーカイブを読み込む
                try
                {
                    if (m_file.Length < MINIMUM_FILE_SIZE)
                    {
                        throw new InvalidDataException("アーカイブファイルのサイズが小さすぎます。");
                    }

                    using (Reader reader = new Reader(m_file, true))
                    {
                        // マジックナンバーをチェック
                        byte[] magicNumber = reader.ReadBytes(0, 4);
                        if (!magicNumber.SequenceEqual(MAGIC_NUMBER))
                        {
                            throw new InvalidDataException("マジックナンバーが不正です。");
                        }

                        // バージョンをチェック
                        UInt32 version = reader.ReadUInt32(4);
                        if (version != CURRENT_VERSION)
                        {
                            throw new InvalidDataException("バージョンが不正です。");
                        }

                        // アーカイブ内のファイル数を取得
                        UInt32 numFiles = reader.ReadUInt32(8);
                        // すべてのファイルを読み込む
                        for (UInt32 i = 0; i < numFiles; ++i)
                        {
                            ArchiveEntry entry = ReadEntry(reader, (int)i);
                            m_entries.Add(entry);
                        }
                    }
                }
                catch(Exception e)
                {
                    throw new CorruptedArchiveException(string.Format("ファイル \"{0}\" は正常なアーカイブファイルではありません。", path), e);
                }
            }
            else
            {
                // 新しいアーカイブファイルを作成する
                try
                {
                    m_file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                    // とりあえず最低限の情報は書き出しておく
                    using (Writer writer = new Writer(m_file, true))
                    {
                        writer.WriteBytes(MAGIC_NUMBER);
                        writer.WriteUInt32(CURRENT_VERSION);
                        writer.WriteUInt32(0);  // Number of files
                    }
                }
                catch (Exception e)
                {
                    throw new IOException("アーカイブファイルを作成できませんでした。", e);
                }
            }
        }

        /// <summary>
        /// 指定した名前を持つ空のエントリをアーカイブに作成します。
        /// </summary>
        /// <param name="name">作成されるエントリの名前。ASCII 文字だけで構成されている必要があります。</param>
        /// <returns>作成されたエントリ。</returns>
        /// <exception cref="ArgumentException">name が空文字列です。もしくは name に ASCII 以外の文字が含まれています。</exception>
        /// <exception cref="ArgumentNullException">name が null です。</exception>
        /// <exception cref="InvalidOperationException">指定された名前と同名のエントリが既にアーカイブに存在します。</exception>
        public ArchiveEntry CreateEntry(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (name == "")
            {
                throw new ArgumentException("name が空文字列です。", "name");
            }

            // name の文字コードをチェック
            try
            {
                Encoding.ASCII.GetBytes(name);
            }
            catch (EncoderFallbackException e)
            {
                throw new ArgumentException("name に ASCII 以外の文字が含まれています。", "name", e);
            }

            // 同名のエントリが既に無いかを確認
            foreach (var entry in m_entries)
            {
                if (entry.Name == name)
                {
                    throw new InvalidOperationException(string.Format("\"{0}\" という名前のエントリは既にアーカイブに存在します。", name));
                }
            }

            ArchiveEntry newEntry = new ArchiveEntry(this, name, null);
            m_entries.Add(newEntry);
            return newEntry;
        }

        private void Flush()
        {
            using (Writer writer = new Writer(m_file, true))
            {
                writer.Seek(0, SeekOrigin.Begin);
                writer.WriteBytes(MAGIC_NUMBER);
                writer.WriteUInt32(CURRENT_VERSION);
                writer.WriteUInt32((UInt32)m_entries.Count);

                long offsetArraySize = 4 * m_entries.Count;

                // 一つ前のファイル末尾の、アーカイブ先頭からのオフセット
                // 現在のファイルはこのアドレスから書き込みを始める
                long prevFileEndAddress = FILE_HEADER_SIZE + offsetArraySize;
                for (int i = 0; i < m_entries.Count; ++i)
                {
                    ArchiveEntry entry = m_entries[i];
                    long fileHeaderAddress = prevFileEndAddress;

                    // ファイル先頭までオフセットを書き込む
                    {
                        long offsetAddress = FILE_HEADER_SIZE + 4 * i;
                        writer.Seek(offsetAddress, SeekOrigin.Begin);
                        writer.WriteUInt32((UInt32)fileHeaderAddress);
                    }

                    // ファイルヘッダとファイル本体を書き込む
                    {
                        writer.Seek(fileHeaderAddress, SeekOrigin.Begin);
                        // ヘッダの先頭はヘッダからファイル本体までのオフセットだが、一旦ダミー値を書き出す
                        writer.WriteUInt32(0xFFFFFFFF);
                        // ファイルサイズ
                        writer.WriteUInt32((UInt32)entry.Length);
                        // ファイル名
                        writer.WriteCStr(entry.Name);

                        // TODO: アドレスのアライメントを外部から設定可能にする
                        long alignment = 8;

                        // ファイル本体のアーカイブ内での位置をアラインする
                        long fileBodyAddressRaw = writer.Position;
                        long fileBodyAddressAligned = AlignAddress(fileBodyAddressRaw, alignment);
                        // ファイルヘッダ先頭に戻って、ヘッダからファイル本体へのオフセットを書き込む
                        // ファイルサイズがゼロでもそれらしい値が書き込まれる。
                        writer.Seek(fileHeaderAddress, SeekOrigin.Begin);
                        writer.WriteUInt32((UInt32)(fileBodyAddressAligned - fileHeaderAddress));

                        // ヘッダの末尾からアラインされたファイル本体までの隙間をゼロで埋める
                        // ファイルサイズがゼロでもパディングが書き出されるが、とりあえず気にしないでおく。
                        writer.Seek(fileBodyAddressRaw, SeekOrigin.Begin);
                        for (long address = fileBodyAddressRaw; address < fileBodyAddressAligned; ++address)
                        {
                            writer.WriteByte(0);
                        }
                        // 続いてファイル本体を書き出す
                        entry.WriteTo(writer);

                        // 次のファイルのアドレス計算のため、現在のファイルの末尾のアドレスを覚えておく
                        prevFileEndAddress = writer.Position;
                    }
                }
            }
        }

        // stream から index 番目のエントリを読み取る。
        // stream の現在位置が変更される。
        private ArchiveEntry ReadEntry(Reader reader, int index)
        {
            Debug.Assert(reader != null);
            Debug.Assert(0 <= index);

            // オフセット配列へのオフセット
            UInt32 offsetOffset = FILE_HEADER_SIZE + ((UInt32)index * 4);
            // ファイルヘッダへのオフセット
            UInt32 fileHeadOffset = reader.ReadUInt32(offsetOffset);
            // ファイルの本体のバイト列へのオフセット
            UInt32 fileBodyOffset = fileHeadOffset + reader.ReadUInt32(fileHeadOffset);

            // ファイルサイズ
            UInt32 fileSize = reader.ReadUInt32(fileHeadOffset + 4);
            // ファイル名
            string fileName = reader.ReadCStr(fileHeadOffset + 8);
            // ファイル本体
            byte[] data = reader.ReadBytes(fileBodyOffset, (int)fileSize);

            return new ArchiveEntry(this, fileName, data);
        }

        // address を二のべき乗数の倍数に切り上げる。
        // 切り上げ先のべき乗数は alignment で指定する。
        // alignment が二のべき乗数でない場合は例外を投げる。
        // また、address が負の数の場合も例外を投げる。
        private long AlignAddress(long address, long alignment)
        {
            if (address < 0)
            {
                throw new ArgumentException("address が負の数です。", "address");
            }

            if (alignment < 0)
            {
                throw new ArgumentException("alignment が負の数です。", "alignment");
            }

            int numBits = 0;
            for (int i = 0; i < (sizeof(long) * 8); ++i)
            {
                if ((alignment & ((long)1 << i)) != 0)
                {
                    numBits += 1;
                }
            }
            if (numBits != 1)
            {
                throw new ArgumentException("alignment が二のべき乗数ではありません。", "alignment");
            }

            if ((address & alignment) != 0)
            {
                // 切り上げて返す
                address = address & ~(alignment - 1);
                address = address + alignment;
                return address;
            }
            else
            {
                // address は alignment の倍数なので、そのまま返す
                return address;
            }

        }

        private Stream m_file;
        private List<ArchiveEntry> m_entries;

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Flush();
                if (disposing)
                {
                    if (m_file != null)
                    {
                        m_file.Close();
                    }
                }

                m_file = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
