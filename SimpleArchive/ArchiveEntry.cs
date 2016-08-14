using System;
using System.Diagnostics;
using System.IO;

namespace SimpleArchive
{
    /// <summary>
    /// アーカイブ内のファイルを表します。
    /// </summary>
    public class ArchiveEntry
    {
        public string Name { get { return m_name; } }
        public long Length { get { return m_stream.Length; } }

        // data は null でもよい。
        internal ArchiveEntry(Archive archive, string name, byte[] data)
        {
            Debug.Assert(archive != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_archive = archive;
            m_name = name;
            m_stream = new MemoryStream();
            if (data != null && data.Length != 0)
            {
                m_stream.Write(data, 0, data.Length);
            }
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
        MemoryStream m_stream;
    }
}
