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
using System.IO;
using System.Diagnostics;

namespace SimpleArchive
{
    /// <summary>
    /// Archive クラスの使用方法を表します。
    /// </summary>
    public enum ArchiveMode
    {
        /// <summary>
        /// アーカイブを新規作成します。
        /// Archive のコンストラクタに渡すストリームは空である必要があります。
        /// Archive がサポートするすべての機能を利用できます。
        /// </summary>
        Create,

        /// <summary>
        /// 既存のアーカイブを読み取り専用として扱います。
        /// Archive のコンストラクタに渡すストリームは既存のアーカイブを含んでいる必要があります。
        /// 書き込みを伴う一部の機能は利用できません。
        /// </summary>
        Read,

        /// <summary>
        /// 既存のアーカイブを変更します。
        /// Archive のコンストラクタに渡すストリームは既存のアーカイブを含んでいる必要があります。
        /// Archive がサポートするすべての機能を利用できます。
        /// </summary>
        Update,
    }

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

        public IReadOnlyCollection<ArchiveEntry> Entries
        {
            get
            {
                if (m_isDisposed) { throw new ObjectDisposedException("Archive"); }
                return m_entries;
            }
        }

        /// <summary>
        /// アーカイブのモードを取得します。
        /// </summary>
        public ArchiveMode Mode { get { return m_mode; } }

        /// <summary>
        /// アーカイブ内のファイルにデフォルトで適用されるアライメントを取得します。
        /// </summary>
        public UInt32 DefaultAlignment { get { return m_defaultAlignment; } }

        /// <summary>
        /// 指定したストリームからアーカイブを初期化します。
        /// <para>
        /// ストリームの CanWrite が true の場合、ArchiveMode は Update に設定されます。
        /// それ以外の場合、ArchiveMode は Read に設定されます。
        /// </para>
        /// </summary>
        /// <param name="stream">
        /// 読み取るアーカイブを格納しているストリーム。
        /// ストリームは少なくともシークと読み取りをサポートしている必要があります。
        /// 指定されたストリームは Archive が破棄される際に閉じられます。
        /// </param>
        public Archive(Stream stream)
            : this(stream, stream.CanWrite ? ArchiveMode.Update : ArchiveMode.Read, false)
        {
        }

        /// <summary>
        /// 指定したストリームとモードを使用してアーカイブを初期化します。
        /// </summary>
        /// <param name="stream">
        /// 読み取るアーカイブを格納しているストリームもしくは新規作成されたアーカイブを出力するストリーム。
        /// ストリームは少なくともシークと読み取りをサポートしている必要があります。
        /// 指定されたストリームは Archive が破棄される際に閉じられます。
        /// </param>
        /// <param name="mode">
        /// アーカイブの使用方法を示す列挙値。
        /// </param>
        public Archive(Stream stream, ArchiveMode mode)
            : this(stream, mode, false)
        {
        }

        /// <summary>
        /// 指定したストリームとモードを使用してアーカイブを初期化します。
        /// </summary>
        /// <param name="stream">
        /// 読み取るアーカイブを格納しているストリームもしくは新規作成されたアーカイブを出力するストリーム。
        /// ストリームは少なくともシークと読み取りをサポートしている必要があります。
        /// </param>
        /// <param name="mode">
        /// アーカイブの使用方法を示す列挙値。
        /// </param>
        /// <param name="leaveOpen">
        /// アーカイブが破棄されてもストリームを開いたままにする場合は true を指定します。
        /// それ以外の場合は false を指定します。
        /// </param>
        /// <param name="dataAlignment">
        /// アーカイブ内のファイルにデフォルトで適用されるアライメントを取得します。
        /// この値はエントリごとにオーバーライドできます。
        /// 2 のべき乗数を指定する必要があります。
        /// </param>
        public Archive(Stream stream, ArchiveMode mode, bool leaveOpen, UInt32 defaultAlignment = 8)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("読み込みをサポートするストリームが必要です。", "stream");
            }
            if (!stream.CanSeek)
            {
                throw new ArgumentException("シークをサポートするストリームが必要です。", "stream");
            }
            if (mode == ArchiveMode.Create || mode == ArchiveMode.Update)
            {
                if (!stream.CanWrite)
                {
                    throw new ArgumentException("書き込みをサポートするストリームが必要です。", "stream");
                }
            }
            if (!Misc.CheckExp2(defaultAlignment))
            {
                throw new ArgumentException("dataAlignment が二のべき乗数ではありません。", "dataAlignment");
            }

            m_stream = stream;
            m_mode = mode;
            m_closeStream = !leaveOpen;
            m_entries = new List<ArchiveEntry>();
            m_defaultAlignment = defaultAlignment;

            if (m_mode == ArchiveMode.Read || m_mode == ArchiveMode.Update)
            {
                // 既存のアーカイブを読み込む
                try
                {
                    if (m_stream.Length < MINIMUM_FILE_SIZE)
                    {
                        throw new CorruptedArchiveException("ストリームの長さが短すぎます。");
                    }

                    using (Reader reader = new Reader(m_stream, true))
                    {
                        // マジックナンバーをチェック
                        byte[] magicNumber = reader.ReadBytes(0, 4);
                        if (!magicNumber.SequenceEqual(MAGIC_NUMBER))
                        {
                            throw new CorruptedArchiveException("アーカイブのマジックナンバーが不正です。");
                        }

                        // バージョンをチェック
                        UInt32 version = reader.ReadUInt32(4);
                        if (version != CURRENT_VERSION)
                        {
                            throw new CorruptedArchiveException("アーカイブのバージョンが不正です。");
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
                catch (Exception e)
                {
                    throw new CorruptedArchiveException("ストリームから読み込まれたアーカイブが壊れています。", e);
                }
            }
            else
            {
                // Mode == Create の場合。
                // ストリームが空っぽじゃないと困る。
                if (stream.Length != 0)
                {
                    throw new ArgumentException("ストリームが空ではありません。", "stream");
                }
            }
        }

        /// <summary>
        /// アーカイブ内の指定の名前のエントリを取得します。
        /// </summary>
        /// <param name="entryName">取得するエントリの名前。</param>
        /// <returns>アーカイブ内の指定された名前を持つエントリ。エントリがアーカイブ内に存在しない場合は null。</returns>
        /// <exception cref="ArgumentException">entryName が空文字列です。</exception>
        /// <exception cref="ArgumentNullException">entryName が null です。</exception>
        /// <exception cref="ObjectDisposedException">破棄された Archive にアクセスしようとしました。</exception>
        public ArchiveEntry GetEntry(string entryName)
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException("Archive");
            }
            if (entryName == null)
            {
                throw new ArgumentNullException("entryName");
            }
            if (entryName == "")
            {
                throw new ArgumentException("entryName が空文字列です。", "entryName");
            }

            foreach (ArchiveEntry entry in m_entries)
            {
                if (entry.Name == entryName)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 指定した名前を持つ空のエントリをアーカイブに作成します。
        /// </summary>
        /// <param name="entryName">作成されるエントリの名前。ASCII 文字だけで構成されている必要があります。</param>
        /// <returns>作成されたエントリ。</returns>
        /// <exception cref="ArgumentException">entryName が空文字列です。もしくは entryName に ASCII 以外の文字が含まれています。</exception>
        /// <exception cref="ArgumentNullException">entryName が null です。</exception>
        /// <exception cref="InvalidOperationException">
        /// 現在のアーカイブモードはエントリの作成をサポートしていません。
        /// もしくは
        /// 指定された名前と同名のエントリが既にアーカイブに存在します。
        /// </exception>
        /// <exception cref="ObjectDisposedException">破棄された Archive にアクセスしようとしました。</exception>
        public ArchiveEntry CreateEntry(string entryName)
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException("Archive");
            }
            if (m_mode == ArchiveMode.Read)
            {
                throw new InvalidOperationException("現在のモードではエントリを作成できません。");
            }
            if (entryName == null)
            {
                throw new ArgumentNullException("entryName");
            }
            if (entryName == "")
            {
                throw new ArgumentException("entryName が空文字列です。", "entryName");
            }

            // entryName の文字コードをチェック
            try
            {
                Misc.ThrowIfContainsNoneASCIICharacters(entryName);
            }
            catch (EncoderFallbackException e)
            {
                throw new ArgumentException("entryName に ASCII 以外の文字が含まれています。", "entryName", e);
            }

            // 同名のエントリが既に無いかを確認
            foreach (var entry in m_entries)
            {
                if (entry.Name == entryName)
                {
                    throw new InvalidOperationException(string.Format("\"{0}\" という名前のエントリは既にアーカイブに存在します。", entryName));
                }
            }

            ArchiveEntry newEntry = new ArchiveEntry(this, entryName, null, DefaultAlignment, m_mode == ArchiveMode.Read);
            m_entries.Add(newEntry);
            return newEntry;
        }

        // entry をこのアーカイブの管理下から外す。
        // ArchiveEntry.Delete から呼ばれる想定。
        internal void RemoveEntryFromCollection(ArchiveEntry entry)
        {
            Debug.Assert(m_entries.Contains(entry));
            m_entries.Remove(entry);
        }

        // アーカイブの内容をストリームに書き出す。
        // m_mode が Read の場合は何もしない。
        private void Flush()
        {
            if (m_mode == ArchiveMode.Read)
            {
                return;
            }

            long archiveSize = 0;
            using (Writer writer = new Writer(m_stream, true))
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
                        writer.WriteUInt32(entry.Alignment);
                        // ファイル名
                        writer.WriteCStr(entry.Name);

                        // ファイル本体のアーカイブ内での位置をアラインする
                        long fileBodyAddressRaw = writer.Position;
                        long fileBodyAddressAligned = AlignAddress(fileBodyAddressRaw, entry.Alignment);
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

                archiveSize = prevFileEndAddress;
            }

            if (archiveSize < m_stream.Length)
            {
                // アーカイブが以前より小さくなったので、ストリームを切り詰める
                m_stream.SetLength(archiveSize);
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
            // アライメント
            UInt32 alignment = reader.ReadUInt32(fileHeadOffset + 8);
            // ファイル名
            string fileName = reader.ReadCStr(fileHeadOffset + 12);
            // ファイル本体
            byte[] data = reader.ReadBytes(fileBodyOffset, (int)fileSize);

            return new ArchiveEntry(this, fileName, data, alignment, m_mode == ArchiveMode.Read);
        }

        // address を二のべき乗数の倍数に切り上げる。
        // 切り上げ先のべき乗数は alignment で指定する。
        // alignment が二のべき乗数でない場合は例外を投げる。
        // また、address が負の数の場合も例外を投げる。
        private static long AlignAddress(long address, long alignment)
        {
            if (address < 0)
            {
                throw new ArgumentException("address が負の数です。", "address");
            }
            if (alignment < 0)
            {
                throw new ArgumentException("alignment が負の数です。", "alignment");
            }
            if (!Misc.CheckExp2(alignment))
            {
                throw new ArgumentException("alignment が二のべき乗数ではありません。", "alignment");
            }

            if ((address & (alignment - 1)) != 0)
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

        private Stream m_stream;
        private ArchiveMode m_mode;
        private bool m_closeStream;  // Dispose の際に m_stream を Close するか？
        private List<ArchiveEntry> m_entries;
        private UInt32 m_defaultAlignment;

#region IDisposable Support
        private bool m_isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!m_isDisposed)
            {
                Flush();
                if (disposing)
                {
                    if (m_stream != null && m_closeStream)
                    {
                        m_stream.Close();
                    }

                    foreach (ArchiveEntry entry in m_entries)
                    {
                        entry.NotifyArchiveDisposing();
                    }
                }

                m_stream = null;
                m_isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
#endregion
    }
}
