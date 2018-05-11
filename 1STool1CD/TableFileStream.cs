using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace _1STool1CD
{
    /// <summary>
    /// Класс таблицы контейнера файлов (CONFIG, CONFIGSAVE, PARAMS, FILES, CONFICAS, CONFICASSAVE)
    /// </summary>
    public class TableFileStream : FileStream
    {
        #region public

        public TableFileStream(Table_file tf) : base(" ", FileMode.Open) { }

        public virtual Int64 Read(byte[] Buffer, Int64 Count) { return 0; }
	    public override Int32 Read(byte[] Buffer, Int32 Offset, Int32 Count) { return 0; }
        public virtual Int64 Write(byte[] Buffer, Int64 Count)  { throw new Exception("Write read-only stream"); }
        public override void Write(byte[] Buffer, Int32 Offset, Int32 Count) { throw new Exception("Write read-only stream"); }
        public virtual Int32 Seek(Int32 Offset, UInt16 Origin) { return 0; }
        public override Int64 Seek(Int64 Offset, SeekOrigin Origin) { return 0; }

        #endregion

        #region private
        private Int64 curoffset;
        private Table_file tablefile;
        private Stream streams;
        #endregion

    }
}
