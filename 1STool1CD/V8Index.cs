using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static _1STool1CD.Constants;
using static _1STool1CD.Utils1CD;

namespace _1STool1CD
{

    public class V8Index
    {

        public V8Index(V8Table _base)
        {
            Tbase = _base;
            Is_primary = false;
            Num_records = 0;
            Records = null;
            Start = 0;
            Rootblock = 0;
            Length = 0;
            Recordsindex_complete = false;
            Pagesize = Tbase.Base_.Pagesize;
            Version = (DBVer)Tbase.Base_.Version;

        }

        private Int32 num_records;
        private String name;
        private bool is_primary;

        public String Getname()
        {
            return Name;
        }

        public bool Get_is_primary()
        {
            return Is_primary;
        }

        public Int32 Get_num_records() // получить количество полей в индексе
        {
            return Num_records;
        }

        public IndexRecord[] Get_records()
        {
            return Records;
        }

        public UInt32 Get_numrecords() // получает количество записей, проиндексированных индексом
        {
            return (UInt32)Num_records;
        }

        public UInt32 Get_numrec(UInt32 num_record) // получает физический индекс записи по порядковому индексу
        {
            if (Start == 0) return 0;
            if (!Recordsindex_complete)
                Create_recordsindex();
            return 
                Recordsindex[(Int32)num_record];
        }

        public void Dump(String _filename)
        {
            FileStream f;
            V8object file_index;
            String s;

            f = new FileStream(_filename, FileMode.Create);

            if (Start == 0)
            {
                f.Dispose();
                return;
            }

            file_index = Tbase.File_index;

            
            
            if (Rootblock == 0)
            {
                //char buf[8];
                byte[] buf = new byte[8];
                file_index.Getdata(buf, Start, 8);

                    //rootblock = *(UInt32*)buf;
                    Rootblock = buf[0]; // скорее всего не правильно

                if (Version >= DBVer.ver8_3_8_0)
                    Rootblock *= Pagesize;

                //length = *(int16_t*)(buf + 4); 
                Length = buf[4]; // скорее всего не правильно, надо проверять
            }

            s = "Index length ";
            s += Length;
            s += "\r\n";
            //f->Write(s.c_str(), s.GetLength());
            f.Write(Encoding.UTF8.GetBytes(s), 0, s.Length);

            Dump_recursive(file_index, f, 0, Rootblock);

            
            f.Dispose();
        }

        public void CalcRecordIndex(byte[] rec, byte[] indexBuf) // вычислить индекс записи rec и поместить в indexBuf. Длина буфера indexBuf должна быть не меньше length
        {
            UInt32 i, j, k;

            j = Length;

            for (i = 0; i < Num_records; i++)
            {
                //k = records[i].field->getSortKey(rec, (unsigned char *)indexBuf, j);
                k = Records[i].Field.GetSortKey(rec, indexBuf, (Int32)j);
                // с этим не очень понятно что делать
                //indexBuf += k;

                j -= k;
            }
            //if(j) memset(indexBuf, 0, j);
            if (j != 0)
                Array.Clear(indexBuf, 0, (Int32)j);

        }

        public UInt32 Get_rootblock()
        {
            if (Start == 0) return 0;

            if (Rootblock == 0)
            {
                //char buf[8];
                byte[] buf = new byte[8];

                Tbase.File_index.Getdata(buf, Start, 8);

                //rootblock = *(uint32_t*)buf;
                Rootblock = buf[0]; // не понятно что с этим делать

                if (Version >= DBVer.ver8_3_8_0)
                    Rootblock *= Pagesize;
                
            }
            return (UInt32)Rootblock;
        }

        public UInt32 Get_length()
        {
            if (Start == 0) return 0;

            if (Rootblock == 0)
            {
                //char buf[8];
                byte[] buf = new byte[8];
                Tbase.File_index.Getdata(buf, Start, 8);

                //length = *(int16_t*)(buf + 4);
                Length = buf[4];
            }

            return Length;

        }
        
        // распаковывает одну страницу-лист индексов
        // возвращает массив структур unpack_index_record. Количество элементов массива возвращается в number_indexes
        public byte[] Unpack_leafpage(UInt64 page_offset, ref UInt32 number_indexes)
        {
            byte[] buf = new byte[Pagesize]; 
            byte[] ret;

            if (Tbase.File_index == null)
                return null;

            //buf = new char[pagesize];

            Tbase.File_index.Getdata(buf, page_offset, Pagesize);

            ret = Unpack_leafpage(buf, ref number_indexes);

            buf = null;

            return ret;
            
        }

        // распаковывает одну страницу-лист индексов
        // возвращает массив структур unpack_index_record. Количество элементов массива возвращается в number_indexes
        public byte[] Unpack_leafpage(byte[] page, ref UInt32 number_indexes)
        {
            byte[] outbuf;
            byte[] rbuf = new byte[page.Length];
            byte[] ibuf = new byte[page.Length];
            byte[] obuf = new byte[number_indexes * (Length + 4)];
            LeafPageHeader header;

            UInt32 i, j, step;

            if (Length == 0)
            {
                number_indexes = 0;
                return null;
            }

            header = ByteArrayToLeafPageHeader(page);

            if (header.Flags == 0 && indexpage_is_leaf == 0)
            {
                Console.WriteLine($"Попытка распаковки страницы индекса не являющейся листом. Таблица {Tbase.Name} Индекс {Name}");
                number_indexes = 0;
                return null;
            }

            number_indexes = header.Number_indexes;
            if (number_indexes == 0)
            {
                return null;
            }

            UInt32 numrecmask =   header.Numrecmask;
            UInt32 leftmask =   header.Leftmask;
            UInt32 rightmask =  header.Rightmask;
            UInt32 numrecbits = header.Numrecbits;
            UInt32 leftbits =   header.Leftbits;
            UInt32 recbytes =   header.Recbytes;

            step = Length + 4;

            //outbuf = new char[number_indexes * step];
            outbuf = new byte[number_indexes * step];

            //rbuf = page + 30;
            Array.Copy(page, 30, rbuf, 0, page.Length);
            //ibuf = page + pagesize;
            Array.Copy(page, Pagesize, ibuf, 0, page.Length);

            //obuf = outbuf;
            Array.Copy(outbuf, 0, obuf, 0, obuf.Length);

            for (i = 0; i < number_indexes; i++)
            {
                /* пока перевод не очень понятен :(
                int64_t indrec = *(int64_t*)rbuf;
                unsigned numrec = indrec & numrecmask;
                indrec >>= numrecbits;
                unsigned left = indrec & leftmask;
                indrec >>= leftbits;
                unsigned right = indrec & rightmask;
                rbuf += recbytes;
                j = length - left - right;
                ibuf -= j;

                *(uint32_t*)obuf = numrec;
                obuf += 4;

                if (left) memcpy(obuf, obuf - step, left);
                if (j) memcpy(obuf + left, ibuf, j);
                obuf += length;
                if (right) memset(obuf - right, 0, right);
                */
            }

            return outbuf;
        }

        // упаковывает одну страницу-лист индексов.
        // возвращвет истина, если упаковка произведена, и ложь, если упаковка невозможна.
        public bool Pack_leafpage(byte[] unpack_index, UInt32 number_indexes, byte[] page_buf)
        {
            return true;
        }

        private V8Table tbase;
        private DBVer version; // версия базы
        private UInt32 pagesize; // размер одной страницы (до версии 8.2.14 всегда 0x1000 (4K), начиная с версии 8.3.8 от 0x1000 (4K) до 0x10000 (64K))

        private IndexRecord[] records;

        private UInt64 start; // Смещение в файле индексов блока описания индекса
        private UInt64 rootblock; // Смещение в файле индексов корневого блока индекса
        private UInt32 length; // длина в байтах одной распакованной записи индекса
        //std::vector<uint32_t> recordsindex; // динамический массив индексов записей по номеру (только не пустые записи)
        private List<UInt32> recordsindex;
        private bool recordsindex_complete; // признак заполнености recordsindex

        public int Num_records { get { return num_records; } set { num_records = value; } }

        public string Name { get { return name; } set { name = value; } }

        public bool Is_primary { get { return is_primary; } set { is_primary = value; } }

        public V8Table Tbase { get { return tbase; } set { tbase = value; } }

        public DBVer Version { get { return version; } set { version = value; } }

        public uint Pagesize { get { return pagesize; } set { pagesize = value; } }

        public IndexRecord[] Records { get { return records; } set { records = value; } }

        public ulong Start { get { return start; } set { start = value; } }

        public ulong Rootblock { get { return rootblock; } set { rootblock = value; } }

        public uint Length { get { return length; } set { length = value; } }

        public List<uint> Recordsindex { get { return recordsindex; } set { recordsindex = value; } }

        public bool Recordsindex_complete { get { return recordsindex_complete; } set { recordsindex_complete = value; } }

        public void Create_recordsindex()
        {

        }

        public void Dump_recursive(V8object file_index, FileStream f, Int32 level, UInt64 curblock)
        {
        }

        public void Delete_index(byte[] rec, UInt32 phys_numrec) // удаление индекса записи из файла index
        {
            byte[] index_buf = new byte[Length];
            CalcRecordIndex(rec, index_buf);
            Delete_index_record(index_buf, phys_numrec);
            index_buf = null;

        }

        public void Delete_index_record(byte[] index_buf, UInt32 phys_numrec) // удаление одного индекса из файла index
        {
            bool is_last_record = false, page_is_empty = false; // заглушки для вызова рекурсивной функции
            UInt32 new_last_phys_num = 0; // заглушки для вызова рекурсивной функции
            byte[] new_last_index_buf = new byte[Length]; // заглушки для вызова рекурсивной функции
            Delete_index_record(index_buf, phys_numrec, Rootblock, ref is_last_record, ref page_is_empty, new_last_index_buf, ref new_last_phys_num);
            new_last_index_buf = null;

        }

        public void Delete_index_record(byte[] index_buf, UInt32 phys_numrec, UInt64 block, ref bool is_last_record, ref bool page_is_empty, byte[] new_last_index_buf, ref UInt32 new_last_phys_num) // рекурсивное удаление одного индекса из блока файла index
        {
            /*
            byte[] page = new byte[pagesize]; 
            branch_page_header bph;
            leaf_page_header lph;

            bool _is_last_record, _page_is_empty;
            UInt32 _new_last_phys_num;
            UInt32 number_indexes;
            byte[] unpack_indexes_buf;
            Int16 flags;
            UInt64 _block_;

            byte[] cur_index;
            Int32 i, j, k, delta;

            
            tbase.file_index.getdata(page, block, pagesize);

            is_last_record = false;
            page_is_empty = false;

            if (page != null && indexpage_is_leaf != 0)
            {
                // страница-лист
                //lph = (leaf_page_header*)page;
                lph = BytearrayToLeafPageHeader(page);
                flags = lph.flags;
                unpack_indexes_buf = unpack_leafpage(page, number_indexes);
                cur_index = unpack_indexes_buf;
                delta = length + 4;
                for (i = 0; i < lph->number_indexes; i++, cur_index += delta)
                {
                    j = memcmp(index_buf, cur_index + 4, length);
                    if (j == 0 && *(uint32_t*)cur_index == phys_numrec)
                    {
                        if (i == lph->number_indexes - 1) is_last_record = true;

                        lph->number_indexes--;
                        for (k = i; k < lph->number_indexes; k++) memcpy(unpack_indexes_buf + k * delta, unpack_indexes_buf + (k + 1) * delta, delta);

                        if (lph->number_indexes == 0)
                        {
                            page_is_empty = true;
                            if (lph->prev_page != LAST_PAGE)
                            {
                                tbase->file_index->setdata(&(lph->next_page), (version < db_ver::ver8_3_8_0 ? lph->prev_page : lph->prev_page * pagesize) + 8, 4);
                            }
                            if (lph->next_page != LAST_PAGE)
                            {
                                tbase->file_index->setdata(&(lph->prev_page), (version < db_ver::ver8_3_8_0 ? lph->next_page : lph->next_page * pagesize) + 4, 4);
                            }
                            // TODO проверить, надо ли номера свободных страниц преобразовывать в смещения для версий от 8.0 до 8.2.14
                            tbase->file_index->getdata(&k, 0, 4);
                            memset(page, 0, pagesize);
                            *(uint32_t*)page = k;
                            k = block / pagesize;
                            tbase->file_index->setdata(&k, 0, 4);
                        }
                        else
                        {
                            if (is_last_record)
                            {
                                cur_index = unpack_indexes_buf + (lph->number_indexes - 1) * delta;
                                memcpy(new_last_index_buf, cur_index + 4, length);
                                new_last_phys_num = *(uint32_t*)cur_index;
                            }
                            pack_leafpage(unpack_indexes_buf, lph->number_indexes, page);
                            lph->flags = flags;
                        }
                        tbase->file_index->setdata(page, block, pagesize);

                        break;
                    }
                }
                unpack_indexes_buf = null;
            }
            else
            {
                // страница-ветка
                bph = (branch_page_header*)page;

                cur_index = page + 12; // 12 = size_of(branch_page_header)
                delta = length + 8;
                for (i = 0; i < bph->number_indexes; i++, cur_index += delta)
                {
                    j = memcmp(index_buf, cur_index, length);
                    if (j <= 0)
                    {
                        if (i == bph->number_indexes - 1 && j == 0) is_last_record = true;

                        _block_ = reverse_byte_order(*(uint32_t*)(cur_index + length + 4));
                        if (version >= db_ver::ver8_3_8_0) _block_ *= pagesize;
                        delete_index_record(index_buf, phys_numrec, _block_, _is_last_record, _page_is_empty, new_last_index_buf, _new_last_phys_num);

                        if (_page_is_empty)
                        {
                            bph->number_indexes--;
                            if (bph->number_indexes > i) memcpy(cur_index, cur_index + delta, (bph->number_indexes - i) * delta);
                            memset(page + 12 + bph->number_indexes * delta, 0, delta);
                        }
                        else if (_is_last_record)
                        {
                            memcpy(cur_index, new_last_index_buf, length);
                            *(uint32_t*)(cur_index + length) = reverse_byte_order(_new_last_phys_num);
                        }

                        if (bph->number_indexes == 0)
                        {
                            page_is_empty = true;
                            if (bph->prev_page != LAST_PAGE)
                            {
                                tbase->file_index->setdata(&(bph->next_page), (version < db_ver::ver8_3_8_0 ? bph->prev_page : bph->prev_page * pagesize) + 8, 4);
                            }
                            if (bph->next_page != LAST_PAGE)
                            {
                                tbase->file_index->setdata(&(bph->prev_page), (version < db_ver::ver8_3_8_0 ? bph->next_page : bph->next_page * pagesize) + 4, 4);
                            }
                            tbase->file_index->getdata(&k, 0, 4);
                            memset(page, 0, pagesize);
                            *(uint32_t*)page = k;
                            k = block / pagesize;
                            tbase->file_index->setdata(&k, 0, 4);
                        }
                        else
                        {
                            if (is_last_record)
                            {
                                cur_index = page + 12 + (bph->number_indexes - 1) * delta;
                                memcpy(new_last_index_buf, cur_index, length);
                                new_last_phys_num = reverse_byte_order(*(uint32_t*)(cur_index + length));
                            }
                        }
                        if (_page_is_empty || _is_last_record || page_is_empty || is_last_record) tbase->file_index->setdata(page, block, pagesize);

                        break;
                    }
                }
            }

            //page = null;
            */
        }

        public void Write_index(UInt32 phys_numrecord, byte[] rec) // запись индекса записи
        {
        }

        public void Write_index_record(UInt32 phys_numrecord, byte[] index_buf) // запись индекса
        {
        }

        public void Write_index_record(UInt32 phys_numrecord, byte[] index_buf, UInt64 block, ref Int32 result, byte[] new_last_index_buf, ref UInt32 new_last_phys_num, byte[] new_last_index_buf2, ref UInt32 new_last_phys_num2, ref UInt64 new_last_block2) // рекурсивная запись индекса
        {
        }


    }
}
