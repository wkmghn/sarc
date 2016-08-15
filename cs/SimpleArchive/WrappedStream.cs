using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleArchive
{
    // ArchiveEntry.Open() で返されるストリーム。
    internal class WrappedStream : Stream
    {
        public override bool CanRead
        {
            get
            {
                // 常に true であることを想定している。
                return m_baseStream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_baseStream.CanWrite && m_canWrite;
            }
        }

        public override bool CanSeek
        {
            get
            {
                // 常に true であることを想定している。
                return m_baseStream.CanSeek;
            }
        }

        public override long Position
        {
            get
            {
                return m_baseStream.Position;
            }

            set
            {
                m_baseStream.Position = value;
            }
        }

        public override long Length
        {
            get
            {
                return m_baseStream.Length;
            }
        }

        public WrappedStream(Stream baseStream, bool readOnly, Action onClosedCallback)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }

            if (!baseStream.CanSeek)
            {
                throw new ArgumentException("baseStream がシーク操作をサポートしていません。", "baseStream");
            }

            m_baseStream = baseStream;
            m_closeBaseStream = false;  // たぶん true に設定する需要が無い。
            m_canWrite = !readOnly;
            m_onClosed = onClosedCallback;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_baseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_baseStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            m_baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_baseStream.SetLength(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (!m_isDisposed)
            {
                if (disposing)
                {
                    if (m_closeBaseStream && m_baseStream != null)
                    {
                        m_baseStream.Close();
                    }
                }
                m_baseStream = null;

                if (m_onClosed != null)
                {
                    m_onClosed.Invoke();
                }

                m_isDisposed = true;
            }

            base.Dispose(disposing);
        }

        private Stream m_baseStream;
        private bool m_closeBaseStream;  // Dispose の際にベースストリームを閉じるか？
        private bool m_canWrite;
        private Action m_onClosed;
        private bool m_isDisposed = false;
    }
}
