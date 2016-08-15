//
// Copyright (c) 2016 wkmghn.
// 
// Use, modification and distribution is subject to the Boost Software License,
// Version 1.0. (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.Diagnostics;
using System.IO;

namespace SimpleArchive
{
    /// <summary>
    /// アーカイブ内のファイルを表します。
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class ArchiveEntry
    {
        /// <summary>
        /// ファイルの名前を取得します。
        /// </summary>
        public string Name { get { return m_name; } }

        /// <summary>
        /// ファイル本体のサイズを取得します。
        /// </summary>
        public long Length { get { return m_stream.Length; } }

        /// <summary>
        /// ファイル本体のアーカイブ内でのアライメントを取得または設定します。
        /// </summary>
        public UInt32 Alignment
        {
            get { return m_alignment; }
            set
            {
                if (!Misc.CheckExp2(value))
                {
                    throw new ArgumentException("Alignment の値が二のべき乗数ではありません。");
                }
                m_alignment = value;
            }
        }

        // data は null でもよい。
        internal ArchiveEntry(Archive archive, string name, byte[] data, UInt32 alignment, bool readOnly)
        {
            Debug.Assert(archive != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_archive = archive;
            m_name = name;
            m_alignment = alignment;
            m_stream = new MemoryStream();
            if (data != null && data.Length != 0)
            {
                m_stream.Write(data, 0, data.Length);
            }

            m_readOnly = readOnly;
        }

        /// <summary>
        /// ファイル内容をストリームとして開きます。
        /// <para>
        /// 一つのエントリに対して同時にオープンできるストリームは一つだけです。
        /// 以前の Open で返されたストリームを閉じる前に次の Open を呼び出すと IOException がスローされます。
        /// </para>
        /// </summary>
        /// <returns>ファイルの内容を表すストリーム。ストリームの現在位置はファイルの末尾に設定されます。</returns>
        /// <exception cref="IOException">以前の Open で返されたストリームが閉じられる前に次の Open が呼び出されました。</exception>
        /// <exception cref="ObjectDisposedException">このエントリを含んだアーカイブが既に破棄されています。</exception>
        public Stream Open()
        {
            if (m_archive == null)
            {
                throw new InvalidOperationException("削除されたエントリを操作できません。");
            }
            if (m_archiveDisposed)
            {
                throw new ObjectDisposedException("Archive", "このエントリを含んだアーカイブが既に破棄されています。");
            }
            if (m_openedStream != null)
            {
                throw new IOException("このエントリは既に開かれています。以前の Open で返されたストリームが閉じられるまで、次の Open を呼び出すことはできません。");
            }

            m_stream.Seek(0, SeekOrigin.End);
            m_openedStream = new WrappedStream(m_stream, m_readOnly, OnStreamClosed);
            return m_openedStream;
        }

        /// <summary>
        /// ファイルをアーカイブから削除します。
        /// </summary>
        public void Delete()
        {
            if (m_archive == null)
            {
                throw new InvalidOperationException("削除されたエントリを操作できません。");
            }
            if (m_archiveDisposed)
            {
                throw new ObjectDisposedException("Archive", "このエントリを含んだアーカイブが既に破棄されています。");
            }
            if (m_openedStream != null)
            {
                throw new IOException("エントリのストリームがオープンされているため、エントリを削除できません。");
            }
            if (m_archive.Mode == ArchiveMode.Read)
            {
                throw new NotSupportedException("アーカイブが Read モードで開かれています。");
            }

            m_archive.RemoveEntryFromCollection(this);
            m_archive = null;
            if (m_openedStream != null)
            {
                m_openedStream.Close();
                // 注意: コールバックから m_opendStream が null に設定される。
            }
            m_stream.Close();
        }

        // 現在の全内容を writer に書き出す。
        // 内部ストリームの位置は変更されない。
        internal void WriteTo(Writer writer)
        {
            long pos = m_stream.Position;

            m_stream.Seek(0, SeekOrigin.Begin);
            writer.Write(m_stream);

            m_stream.Seek(pos, SeekOrigin.Begin);
        }

        // 持ち主の Archive が破棄されている最中に Archive から呼ばれる。
        internal void NotifyArchiveDisposing()
        {
            if (m_openedStream != null)
            {
                m_openedStream.Close();
                // 注意: コールバックから m_opendStream が null に設定される。
            }
            m_stream.Close();
            m_archiveDisposed = true;
        }

        // m_opendStream が閉じられたときに呼ばれるコールバック。
        private void OnStreamClosed()
        {
            Debug.Assert(m_openedStream != null);
            m_openedStream = null;
        }

        Archive m_archive;  // エントリがアーカイブから削除された場合は null
        string m_name;
        UInt32 m_alignment;
        MemoryStream m_stream;  // Archive が Dispose されるときに閉じる
        bool m_readOnly;

        // Open で返したストリーム。
        // Close されたら null に戻る。
        // 同時に Open 状態にできるのは一つのストリームだけ。
        WrappedStream m_openedStream;

        bool m_archiveDisposed = false;
    }
}
