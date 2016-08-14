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
        internal ArchiveEntry(Archive archive, string name, byte[] data, UInt32 alignment)
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
        }

        public Stream Oepn()
        {
            WrappedStream stream = new WrappedStream(m_stream);
            return stream;
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

        Archive m_archive;
        string m_name;
        UInt32 m_alignment;
        MemoryStream m_stream;
    }
}
