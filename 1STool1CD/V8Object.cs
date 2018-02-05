using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static _1STool1CD.Utils1CD;

namespace _1STool1CD
{
    /// <summary>
    /// структура версии
    /// </summary>
    public struct _version_rec
    {
        public UInt32 version_1; // версия реструктуризации
        public UInt32 version_2; // версия изменения
    }

    /// <summary>
    /// структура версии
    /// </summary>
    public struct _version
    {
        public UInt32 version_1; // версия реструктуризации
        public UInt32 version_2; // версия изменения
        public UInt32 version_3; // версия изменения 2
    }

    /// <summary>
    /// Структура страницы размещения уровня 1 версий от 8.3.8 
    /// </summary>
    public struct objtab838
    {
        public UInt32[] blocks; // реальное количество блоков зависит от размера страницы (pagesize)
    }

    /// <summary>
    /// структура заголовочной страницы файла данных или файла свободных страниц 
    /// </summary>
    public struct v8ob
    {
        public char[] sig; // сигнатура SIG_OBJ
        public UInt32 len; // длина файла
        public _version version;
        public UInt32[] blocks;
    }

    /// <summary>
    /// структура заголовочной страницы файла данных начиная с версии 8.3.8 
    /// </summary>
    struct v838ob_data
    {
        public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)
        public Int16 fatlevel;   // уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        public _version version;
        public UInt64 len;       // длина файла
        public UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)
    }

    /// <summary>
    /// структура заголовочной страницы файла свободных страниц начиная с версии 8.3.8 
    /// </summary>
    public struct v838ob_free
    {
        public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
        public Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        public UInt32 version;        // ??? предположительно...
        public UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)
    }

    /// <summary>
    /// типы внутренних файлов
    /// </summary>
    public enum v8objtype
    {
	    unknown = 0, // тип неизвестен
	    data80  = 1, // файл данных формата 8.0 (до 8.2.14 включительно)
	    free80  = 2, // файл свободных страниц формата 8.0 (до 8.2.14 включительно)
	    data838 = 3, // файл данных формата 8.3.8
	    free838 = 4  // файл свободных страниц формата 8.3.8
    }

    /// <summary>
    /// Класс v8object
    /// </summary>
    public class v8object
    {
        /// <summary>
        /// Конструктор существующего объекта
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        public v8object(T_1CD _base, UInt32 blockNum) 
        {
            init(_base, (int)blockNum);
        }

        /// <summary>
        /// Конструктор нового (еще не существующего) объекта
        /// </summary>
        /// <param name="_base"></param>
        public v8object(T_1CD _base) 
        {
            UInt32 blockNum;
            Byte[] b = new Byte[_base.pagesize];

            blockNum = _base.get_free_block();
            b = _base.getblock_for_write(blockNum, false);

            //memset(b, 0, _base->pagesize);

            if (_base.version < db_ver.ver8_3_8_0)
            {
                //memcpy(((v8ob*)b)->sig, SIG_OBJ, 8);
                //v8ob
            }
            else
            {
                b[0] = 0x1c;
                b[1] = 0xfd;
            }
            init(_base, (int)blockNum);
        }

        #region Public

        public char getdata() { return '0'; } // чтение всего объекта целиком, поддерживает кеширование объектов. Буфер принадлежит объекту
        public char getdata(byte[] buf, UInt64 _start, UInt64 _length) { return '0'; } // чтение кусочка объекта, поддерживает кеширование блоков. Буфер не принадлежит объекту
        public bool setdata(byte[] buf, UInt64 _start, UInt64 _length) { return true; } // запись кусочка объекта, поддерживает кеширование блоков.
        public bool setdata(byte[] buf, UInt64 _length) { return true; } // запись объекта целиком, поддерживает кеширование блоков.
        public bool setdata(Stream stream) { return true; } // записывает поток целиком в объект, поддерживает кеширование блоков.
        public bool setdata(Stream stream, UInt64 _start, UInt64 _length) { return true; } // запись части потока в объект, поддерживает кеширование блоков.
        public UInt64 getlen() { return 0; }
        public void savetofile(String filename) { }
        public void set_lockinmemory(bool _lock) { }
        public static void garbage() { }
        public UInt64 get_fileoffset(UInt64 offset) { return 0; } // получить физическое смещение в файле по смещению в объекте
        public void set_block_as_free(UInt32 block_number) { } // пометить блок как свободный
        public UInt32 get_free_block() { return 0; } // получить номер свободного блока (и пометить как занятый)
        public void get_version_rec_and_increase(_version ver) {  } // получает версию очередной записи и увеличивает сохраненную версию объекта
        public void get_version(_version ver) {  } // получает сохраненную версию объекта
        public void write_new_version() { } // записывает новую версию объекта
        public static v8object get_first() { return (v8object)null; }
        public static v8object get_last() { return (v8object)null; }
        public v8object get_next() { return (v8object)null; }
        public UInt32 get_block_number() { return 0; }
        public Stream readBlob(Stream _str, UInt32 _startblock, UInt32 _length = UInt32.MaxValue, bool rewrite = true) { return (Stream)null; }

        #endregion

        #region Private

        private T_1CD base_;

    	private UInt64 len;                 // длина объекта. Для типа таблицы свободных страниц - количество свободных блоков
        private _version version;           // текущая версия объекта
        private _version_rec version_rec;   // текущая версия записи
        private bool new_version_recorded;  // признак, что новая версия объекта записана
        private v8objtype type;             // тип и формат файла
        private Int32 fatlevel;             // Количество промежуточных уровней в таблице размещения
        private UInt64 numblocks;           // кол-во страниц в корневой таблице размещения объекта
        private UInt32 real_numblocks;      // реальное кол-во страниц в корневой таблице (только для файлов свободных страниц, может быть больше numblocks)
        private UInt32[] blocks;            // таблица страниц корневой таблицы размещения объекта (т.е. уровня 0)
        private UInt32 block;               // номер блока объекта
        private char[] data;                // данные, представляемые объектом, NULL если не прочитаны или len = 0
        
        private static v8object first;
        private static v8object last;
        private v8object next;
        private v8object prev;
        private UInt32 lastdataget;         // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)
        private bool lockinmemory;

        /// <summary>
        /// Установка новой длины объекта
        /// </summary>
        /// <param name="_len"></param>
        private void set_len(UInt64 _len)
        {
            UInt32 num_data_blocks;
            UInt32 num_blocks;
            UInt32 cur_data_blocks;
            UInt32 bl;
            UInt32 i;
            v8ob b = new v8ob();
            v838ob_data bd = new v838ob_data();
            objtab838 bb = new objtab838();
            UInt32 offsperpage;
            UInt64 maxlen;
            UInt32 newfatlevel;

            if (len == _len) return;

            if (type == v8objtype.free80 || type == v8objtype.free838)
            {
                // Таблица свободных блоков
                //msreg_g.AddError("Попытка установки длины в файле свободных страниц");
                return;
            }

            
            data = null;

            if (type == v8objtype.data80)
            {
                //b = (v8ob*)base->getblock_for_write(block, true); //TODO: надо разбираться
                b.len = (uint)_len;

                num_data_blocks = (uint)(_len + 0xfff) >> 12;
                num_blocks = (num_data_blocks + 1022) / 1023;
                cur_data_blocks = (uint)(len + 0xfff) >> 12;

                if (numblocks != num_blocks)
                {
                    
                    if (num_blocks != 0 )
                        blocks = new UInt32[num_blocks];
                    else
                        blocks = null;
                }

                if (num_data_blocks > cur_data_blocks)
                {
                    objtab ot;
                    // Увеличение длины объекта
                    if (numblocks != 0 )
                        //ot = (objtab)base_.getblock_for_write(b.blocks[numblocks - 1], true); // TODO: надо перерабатывать
                    for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                    {
                        i = cur_data_blocks % 1023;
                        if (i == 0)
                        {
                            bl = base_.get_free_block();
                            b.blocks[numblocks++] = bl;
                            //ot = (objtab)base_.getblock_for_write(bl, false); // TODO: надо перерабатывать
                            ot.numblocks = 0;
                        }
                        bl = base_.get_free_block();
                        base_.getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                        // ot.blocks[i] = bl; // TODO: надо перерабатывать
                        ot.numblocks = i + 1;
                    }
                }
                else if (num_data_blocks < cur_data_blocks)
                {
                    // Уменьшение длины объекта
                    //objtab ot = (objtab)base_.getblock_for_write(b.blocks[numblocks - 1], true);
                    objtab ot = new objtab(); // TODO: надо перерабатывать
                    for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                    {
                        i = cur_data_blocks % 1023;
                        base_.set_block_as_free(ot.blocks[i]);
                        ot.blocks[i] = 0;
                        ot.numblocks = i;
                        if (i == 0)
                        {
                            base_.set_block_as_free(b.blocks[--numblocks]);
                            b.blocks[numblocks] = 0;
                            if (numblocks != 0)
                            {
                                //ot = (objtab)base_.getblock_for_write(b.blocks[numblocks - 1], true);// TODO: надо перерабатывать
                            }
                        }
                    }

                }

                len = _len;
                if (numblocks != 0)
                {
                    //memcpy(blocks, b->blocks, numblocks * 4);
                    Array.Copy(b.blocks, blocks, (int)numblocks * 4);
                }



                write_new_version();
            }
            else if (type == v8objtype.data838)
            {
                offsperpage = base_.pagesize / 4;
                maxlen = base_.pagesize * offsperpage * (offsperpage - 6);
                if (_len > maxlen)
                {
                    _len = maxlen;
                }

                //bd = (v838ob_data*)base->getblock_for_write(block, true);
                bd = new v838ob_data(); // TODO: надо перерабатывать

                bd.len = _len;

                num_data_blocks = (_len + base_.pagesize - 1) / base->pagesize;
                if (num_data_blocks > offsperpage - 6)
                {
                    num_blocks = (num_data_blocks + offsperpage - 1) / offsperpage;
                    newfatlevel = 1;
                }
                else
                {
                    num_blocks = num_data_blocks;
                    newfatlevel = 0;
                }
                cur_data_blocks = (len + base->pagesize - 1) / base->pagesize;

                if (numblocks != num_blocks)
                {
                    delete[] blocks;
                    if (num_blocks) blocks = new uint32_t[num_blocks];
                    else blocks = nullptr;
                }

                if (num_data_blocks > cur_data_blocks)
                {
                    // Увеличение длины объекта
                    if (fatlevel == 0 && newfatlevel)
                    {
                        bl = base->get_free_block();
                        bb = (objtab838*)base->getblock_for_write(bl, false);
                        memcpy(bb->blocks, bd->blocks, numblocks * 4);
                        fatlevel = newfatlevel;
                        bd->fatlevel = newfatlevel;
                        bd->blocks[0] = bl;
                        numblocks = 1;
                    }
                    else bb = (objtab838*)base->getblock_for_write(bd->blocks[numblocks - 1], true);

                    if (fatlevel)
                    {
                        for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                        {
                            i = cur_data_blocks % offsperpage;
                            if (i == 0)
                            {
                                bl = base->get_free_block();
                                bd->blocks[numblocks++] = bl;
                                bb = (objtab838*)base->getblock_for_write(bl, false);
                            }
                            bl = base->get_free_block();
                            base->getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                            bb->blocks[i] = bl;
                        }
                    }
                    else
                    {
                        for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                        {
                            bl = base->get_free_block();
                            base->getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                            bd->blocks[cur_data_blocks] = bl;
                        }
                    }
                }
                else if (num_data_blocks < cur_data_blocks)
                {
                    // Уменьшение длины объекта
                    if (fatlevel)
                    {
                        bb = (objtab838*)base->getblock_for_write(b->blocks[numblocks - 1], true);
                        for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                        {
                            i = cur_data_blocks % offsperpage;
                            base->set_block_as_free(bb->blocks[i]);
                            bb->blocks[i] = 0;
                            if (i == 0)
                            {
                                base->set_block_as_free(bd->blocks[--numblocks]);
                                bd->blocks[numblocks] = 0;
                                if (numblocks) bb = (objtab838*)base->getblock_for_write(b->blocks[numblocks - 1], true);
                            }
                        }
                    }
                    else
                    {
                        for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                        {
                            base->set_block_as_free(bd->blocks[cur_data_blocks]);
                            bd->blocks[cur_data_blocks] = 0;
                        }
                        numblocks = num_data_blocks;
                    }

                    if (fatlevel && newfatlevel == 0)
                    {
                        if (numblocks)
                        {
                            bl = bd->blocks[0];
                            memcpy(bd->blocks, bb->blocks, num_data_blocks * 4);
                            base->set_block_as_free(bl);
                        }
                        fatlevel = 0;
                        bd->fatlevel = 0;
                    }

                }

                len = _len;
                if (numblocks) memcpy(blocks, bd->blocks, numblocks * 4);

                write_new_version();
            }

        } // установка новой длины объекта

        /// <summary>
        /// Инициализация 1
        /// </summary>
        private void init()
        {
            len = 0;
            version.version_1 = 0;
            version.version_2 = 0;
            version_rec.version_1 = 0;
            version_rec.version_2 = 0;
            new_version_recorded = false;
            numblocks = 0;
            real_numblocks = 0;
            blocks = null;
            block = 100500;
            data = null;
            lockinmemory = false;
            type = v8objtype.unknown;
            fatlevel = 0;

        }

        /// <summary>
        /// Инициализация 2
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        private void init(T_1CD _base, Int32 blockNum)
        {
            base_ = _base;
            lockinmemory = false;

            prev = last;
            next = null;

            if (last != null)
                last.next = this;
            else
                first = this;

            last = this;

            if (blockNum == 1)
            {
                type = (base_.version < db_ver.ver8_3_8_0) ? v8objtype.free80 : v8objtype.free838;
            }
            else
            {
                type = (base_.version < db_ver.ver8_3_8_0) ? v8objtype.data80 : v8objtype.data838;
            }

            if (type == v8objtype.data80 || type == v8objtype.free80)
            {
                fatlevel = 1;
                v8ob t = new v8ob();

                /* TODO: надо перерабатывать
                base_.getblock(t, blockNum);

                if (memcmp(&(t->sig), SIG_OBJ, 8) != 0)
                {
                    init();
                    return;
                }
                */

                len = t.len;
                version.version_1 = t.version.version_1;
                version.version_2 = t.version.version_2;
                version.version_3 = t.version.version_3;
                version_rec.version_1 = version.version_1 + 1;
                version_rec.version_2 = 0;
                new_version_recorded = false;
                block = (uint)blockNum;
                real_numblocks = 0;
                data = null;

                if (type == v8objtype.free80)
                {
                    numblocks = (len != 0) ? numblocks = (len - 1) / 0x400 + 1 : 0;

                    // в таблице свободных блоков в разделе blocks может быть больше блоков, чем numblocks
                    // numblocks - кол-во блоков с реальными данными
                    // оставшиеся real_numblocks - numblocks блоки принадлежат объекту, но не содержат данных
                    while (t.blocks[real_numblocks] != 0)
                        real_numblocks++;

                    if (real_numblocks != 0)
                    {
                        blocks = new UInt32[real_numblocks];
                        //memcpy(blocks, t->blocks, real_numblocks * sizeof(*blocks));
                        Array.Copy(t.blocks, blocks, real_numblocks * sizeof(UInt32)); // TODO: нуждается в проверке

                    }
                    else
                        blocks = null;
                }
                else
                {
                    if (len != 0)
                        numblocks = (len - 1) / 0x3ff000 + 1;
                    else
                        numblocks = 0;

                    if (numblocks != 0)
                    {
                        blocks = new UInt32[numblocks];

                        //memcpy(blocks, t->blocks, numblocks * sizeof(*blocks));
                        Array.Copy(t.blocks, blocks, (int)numblocks * sizeof(UInt32)); // TODO: нуждается в проверке
                    }
                    else
                        blocks = null;
                }
            }
            else if (type == v8objtype.data838)
            {
                char[] b = new char[base_.pagesize];

                // v838ob_data t = (v838ob_data*)b; //TODO: пока ХЗ
                v838ob_data t = new v838ob_data();

                //base_.getblock(t, blockNum);

                if (t.sig[0] != 0x1c || t.sig[1] != 0xfd)
                {
                    b = null;
                    init();
                    return;
                }

                len = t.len;
                fatlevel = t.fatlevel;
                if (fatlevel == 0 && len > ((base_.pagesize / 4 - 6) * base_.pagesize))
                {
                    b = null;
                    init();
                    return;
                }
                version.version_1 = t.version.version_1;
                version.version_2 = t.version.version_2;
                version.version_3 = t.version.version_3;
                version_rec.version_1 = version.version_1 + 1;
                version_rec.version_2 = 0;
                new_version_recorded = false;
                block = (uint)blockNum;
                real_numblocks = 0;
                data = null;

                if (len != 0)
                {
                    if (fatlevel == 0)
                    {
                        numblocks = (len - 1) / base_.pagesize + 1;
                    }
                    else
                    {
                        numblocks = (len - 1) / (base_.pagesize / 4 * base_.pagesize) + 1;
                    }
                }
                else numblocks = 0;
                if (numblocks != 0)
                {
                    blocks = new UInt32[numblocks];
                    //memcpy(blocks, t->blocks, numblocks * sizeof(*blocks));
                    Array.Copy(t.blocks, blocks, (int)numblocks * sizeof(UInt32)); // TODO: нуждается в проверке
                }
                else
                    blocks = null;

                b = null;
            }
            else
            {
                char[] b = new char[base_.pagesize];
                //v838ob_free t = (v838ob_free*)b;
                v838ob_free t = new v838ob_free();

                //base_.getblock(t, blockNum); // TODO: необходима доработка

                if (t.sig[0] != 0x1c || t.sig[1] != 0xff)
                {
                    b = null;
                    init();
                    return;
                }

                len = 0; // ВРЕМЕННО! Пока не понятна структура файла свободных страниц

                version.version_1 = t.version;
                version_rec.version_1 = version.version_1 + 1;
                version_rec.version_2 = 0;
                new_version_recorded = false;
                block = (uint)blockNum;
                real_numblocks = 0;
                data = null;

                if (len != 0)
                    numblocks = (len - 1) / 0x400 + 1;
                else
                    numblocks = 0;

                // в таблице свободных блоков в разделе blocks может быть больше блоков, чем numblocks
                // numblocks - кол-во блоков с реальными данными
                // оставшиеся real_numblocks - numblocks блоки принадлежат объекту, но не содержат данных
                while (t.blocks[real_numblocks] != 0)
                    real_numblocks++;
                if (real_numblocks != 0)
                {
                    blocks = new UInt32[real_numblocks];

                    //memcpy(blocks, t->blocks, real_numblocks * sizeof(*blocks));
                    Array.Copy(t.blocks, blocks, real_numblocks * sizeof(UInt32)); // TODO: нуждается в проверке
                }
                else blocks = null;

            }



        }

        #endregion

    }
}
