using System;
using System.Text;
using System.IO;

namespace SimpleArchive
{
    class Reader : IDisposable
    {
        public Reader(Stream stream, bool leaveOpen)
        {
            m_stream = stream;
            m_leaveOpen = leaveOpen;
        }

        // count 未満しか読めなかった場合は例外を投げる。
        public byte[] ReadBytes(long pos, int count)
        {
            m_stream.Seek(pos, SeekOrigin.Begin);

            byte[] bytes = new byte[count];
            if (m_stream.Read(bytes, 0, count) != count)
            {
                throw new IOException("ファイルサイズが足りません。");
            }

            return bytes;
        }

        public UInt32 ReadUInt32(long pos)
        {
            m_stream.Seek(pos, SeekOrigin.Begin);

            byte[] bytes = new byte[4];
            if (m_stream.Read(bytes, 0, 4) != 4)
            {
                throw new IOException("ファイルサイズが足りません。");
            }

            return (UInt32)bytes[0] << 24 | (UInt32)bytes[1] << 16 | (UInt32)bytes[2] << 8 | (UInt32)bytes[3];
        }

        // ASCII 文字列以外を検出した場合は例外を投げる。
        public string ReadCStr(long pos)
        {
            m_stream.Seek(pos, SeekOrigin.Begin);

            Decoder decoder = Encoding.ASCII.GetDecoder();
            StringBuilder s = new StringBuilder();
            byte[] bytes = new byte[1];
            char[] chars = new char[1];
            int c = m_stream.ReadByte();
            while (0 < c)
            {
                bytes[0] = (byte)c;
                if (decoder.GetChars(bytes, 0, 1, chars, 0, true) != 1)
                {
                    throw new InvalidDataException("ストリーム内の文字列に ASCII 文字以外の文字が使用されています。");
                }
                s.Append(chars[0]);
                c = m_stream.ReadByte();
            }

            if (c == 0)
            {
                return s.ToString();
            }

            throw new EndOfStreamException("C 言語スタイルの文字列の読み取り中にストリームの末尾に到達しました。");
        }

        private Stream m_stream;
        private bool m_leaveOpen;

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (m_stream != null && !m_leaveOpen)
                    {
                        m_stream.Close();
                    }
                }

                m_stream = null;
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
