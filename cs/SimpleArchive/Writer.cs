using System;
using System.IO;
using System.Text;

namespace SimpleArchive
{
    class Writer : IDisposable
    {
        public long Position { get { return m_stream.Position; } }

        public Writer(Stream stream, bool leaveOpen)
        {
            m_stream = stream;
            m_leaveOpen = leaveOpen;
        }

        public void WriteByte(byte value)
        {
            m_stream.WriteByte(value);
        }

        public void WriteBytes(byte[] bytes)
        {
            m_stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteUInt32(UInt32 value)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)((value >> 24) & 0xFF);
            bytes[1] = (byte)((value >> 16) & 0xFF);
            bytes[2] = (byte)((value >> 8) & 0xFF);
            bytes[3] = (byte)((value >> 0) & 0xFF);
            WriteBytes(bytes);
        }

        public void WriteCStr(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            byte[] bytes = Encoding.ASCII.GetBytes(value);
            WriteBytes(bytes);
            WriteByte(0);
        }

        public void Write(MemoryStream stream)
        {
            stream.WriteTo(m_stream);
        }

        public void Flush()
        {
            m_stream.Flush();
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            m_stream.Seek(offset, origin);
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
