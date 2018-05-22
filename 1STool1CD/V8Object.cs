using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static _1STool1CD.Utils1CD;
using static _1STool1CD.APIcfBase;
using static _1STool1CD.Constants;
using static _1STool1CD.Structures;

namespace _1STool1CD
{
    /// <summary>
    /// структура версии
    /// </summary>
    public struct _version_rec
    {
        private UInt32 version_1; // версия реструктуризации
        private UInt32 version_2; // версия изменения

        //public uint Version_1 { get { return version_1; } set { version_1 = value; } }
        //public uint Version_2 { get { return version_2; } set { version_2 = value; } }
        public uint Version_1 { get; set; }
        public uint Version_2 { get; set; }

    }

    /// <summary>
    /// структура версии
    /// </summary>
    public struct _version
    {
        private UInt32 version_1; // версия реструктуризации
        private UInt32 version_2; // версия изменения
        private UInt32 version_3; // версия изменения 2

        public uint Version_1 { get { return version_1; } set { version_1 = value; } }
        public uint Version_2 { get { return version_2; } set { version_2 = value; } }
        public uint Version_3 { get { return version_3; } set { version_3 = value; } }
        public _version(UInt32 v1, UInt32 v2, UInt32 v3)
        {
            version_1 = v1;
            version_2 = v2;
            version_3 = v3;
        }
    }

    /// <summary>
    /// Структура страницы размещения уровня 1 версий от 8.3.8 
    /// </summary>
    public struct Objtab838
    {
        private UInt32[] blocks; // реальное количество блоков зависит от размера страницы (pagesize)

        public uint[] Blocks { get { return blocks; } set { blocks = value; } }
    }

    /// <summary>
    /// структура заголовочной страницы файла данных или файла свободных страниц 
    /// </summary>
    public struct V8ob
    {
        private char[] sig; // сигнатура SIG_OBJ
        private UInt32 len; // длина файла
        private _version version;
        private UInt32[] blocks;

        public char[] Sig { get { return sig; } set { sig = value; } }
        public uint Len { get { return len; } set { len = value; } }
        public _version Version { get { return version; } set { version = value; } }
        public uint[] Blocks { get { return blocks; } set { blocks = value; } }
    }

    /// <summary>
    /// структура заголовочной страницы файла данных начиная с версии 8.3.8 
    /// </summary>
    public struct V838ob_data
    {
        //public char[] sig;       // сигнатура 0x1C 0xFD (1C File Data?)
        private byte[] sig;
        private Int16 fatlevel;   // уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        private _version version;
        private UInt64 len;       // длина файла
        private UInt32[] blocks;  // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)

        public byte[] Sig { get { return sig; } set { sig = value; } }

        public short Fatlevel { get { return fatlevel; } set { fatlevel = value; } }
        public _version Version { get { return version; } set { version = value; } }
        public ulong Len { get { return len; } set { len = value; } }
        public uint[] Blocks { get { return blocks; } set { blocks = value; } }
    }

    /// <summary>
    /// структура заголовочной страницы файла свободных страниц начиная с версии 8.3.8 
    /// </summary>
    public struct V838ob_free
    {
        //public char[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
        private byte[] sig;     // сигнатура 0x1C 0xFF (1C File Free?)
        private Int16 fatlevel; // 0x0000 пока! но может ... уровень таблицы размещения (0x0000 - в таблице blocks номера страниц с данными, 0x0001 - в таблице blocks номера страниц с таблицами размещения второго уровня, в которых уже, в свою очередь, находятся номера страниц с данными)
        private UInt32 version;        // ??? предположительно...
        private UInt32[] blocks;       // Реальная длина массива зависит от размера страницы и равна pagesize/4-6 (от это 1018 для 4К до 16378 для 64К)

        public byte[] Sig { get { return sig; } set { sig = value; } }
        public short Fatlevel { get { return fatlevel; } set { fatlevel = value; } }
        public uint Version { get { return version; } set { version = value; } }
        public uint[] Blocks { get { return blocks; } set { blocks = value; } }
    }

    /// <summary>
    /// типы внутренних файлов
    /// </summary>
    public enum V8objtype
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
    public class V8object
    {

        private Tools1CD _base;

        private UInt64 len;                 // длина объекта. Для типа таблицы свободных страниц - количество свободных блоков
        private _version version;           // текущая версия объекта
        private _version_rec version_rec;   // текущая версия записи
        private bool new_version_recorded;  // признак, что новая версия объекта записана
        private V8objtype type;             // тип и формат файла
        private Int32 fatlevel;             // Количество промежуточных уровней в таблице размещения
        private UInt64 numblocks;           // кол-во страниц в корневой таблице размещения объекта
        private UInt32 real_numblocks;      // реальное кол-во страниц в корневой таблице (только для файлов свободных страниц, может быть больше numblocks)

        //uint32_t* blocks;        

        // struct {
        //
        //        char lang[N];                Для версии базы «8.0.5.0» эта структура выглядит так: char lang[8];   
        //                                     Для версии базы «8.1.0.0» эта структура выглядит так: char lang[32]; 
        //                                       - код языка базы
        //
        //        int numblocks;                 - содержится количество элементов в массиве tableblocks
        //
        //        int tableblocks[numblocks];    - в массиве же tableblocks содержатся индексы объектов, 
        //                                            содержащих все таблицы данных. Т.е. таблиц в базе ровно numblocks.
        //
        //       }



        // наверное пока так               // номера страниц в массиве                                    
        private UInt32[] blocks;            // таблица страниц корневой таблицы размещения объекта (т.е. уровня 0)

        private UInt32 block;               // номер блока объекта
        private byte[] data;                // данные, представляемые объектом, NULL если не прочитаны или len = 0   //char* data;                      

        private static V8object first;
        private static V8object last;

        private V8object next;
        private V8object prev;

        private UInt32 lastdataget;          // время (Windows time, в миллисекундах) последнего обращения к данным объекта (data)
        private bool lockinmemory;

        public Tools1CD Base { get { return _base; } set { _base = value; } }

        public ulong Len { get { return len; } set { len = value; } }

        public _version Version { get { return version; } set { version = value; } }

        public _version_rec Version_rec { get { return version_rec; } set { version_rec = value; } }

        public bool New_version_recorded { get { return new_version_recorded; } set { new_version_recorded = value; } }

        public V8objtype Type { get { return type; } set { type = value; } }

        public int Fatlevel { get { return fatlevel; } set { fatlevel = value; } }

        public ulong Numblocks { get { return numblocks; } set { numblocks = value; } }

        public uint Real_numblocks { get { return real_numblocks; } set { real_numblocks = value; } }

        public uint[] Blocks { get { return blocks; } set { blocks = value; } }

        public uint Block { get { return block; } set { block = value; } }

        public byte[] Data { get { return data; } set { data = value; } }

        public static V8object First { get { return first; } set { first = value; } }

        public static V8object Last { get { return last; } set { last = value; } }

        public V8object Next { get { return next; } set { next = value; } }

        public V8object Prev { get { return prev; } set { prev = value; } }

        public uint Lastdataget { get { return lastdataget; } set { lastdataget = value; } }

        public bool Lockinmemory { get { return lockinmemory; } set { lockinmemory = value; } }


        /// <summary>
        /// Установка новой длины объекта
        /// </summary>
        /// <param name="_len"></param>
        public void Set_len(UInt64 _len) // установка новой длины объекта
        {
            UInt32 num_data_blocks;
            UInt32 num_blocks;
            UInt32 cur_data_blocks;
            UInt32 bl;
            UInt32 i;
            V8ob b;
            V838ob_data bd;
            Objtab838 bb;
            UInt32 offsperpage;
            UInt64 maxlen;
            Int32 newfatlevel;

            if (Len == _len) return;

            if (Type == V8objtype.free80 || Type == V8objtype.free838)
            {
                // Таблица свободных блоков
                Console.WriteLine("Попытка установки длины в файле свободных страниц");
                return;
            }

            Data = null;

            if (Type == V8objtype.data80)
            {

                b = ByteArrayToV8ob(Base.Getblock_for_write(Block, true));
                b.Len = (UInt32)_len;

                num_data_blocks = (UInt32)(_len + 0xfff) >> 12;
                num_blocks = (num_data_blocks + 1022) / 1023;
                cur_data_blocks = (UInt32)(Len + 0xfff) >> 12;

                if (Numblocks != num_blocks)
                {
                    Blocks = null;
                    if (num_blocks != 0)
                        Blocks = new UInt32[num_blocks];
                    else
                        Blocks = null;
                }
                if (num_data_blocks > cur_data_blocks)
                {
                    ObjTab ot = new ObjTab(0,null);
                    // Увеличение длины объекта
                    if (Numblocks != 0)
                    {
                        ot = ByteArrayToObjtab(Base.Getblock_for_write(b.Blocks[Numblocks - 1], true));
                    }
                    for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                    {
                        i = cur_data_blocks % 1023;
                        if (i == 0)
                        {
                            bl = Base.Get_free_block();
                            b.Blocks[Numblocks++] = bl;

                            //ot = (objtab*)base->getblock_for_write(bl, false);
                            ot = ByteArrayToObjtab(Base.Getblock_for_write(bl, false));

                            ot.Numblocks = 0;
                        }
                        bl = Base.Get_free_block();
                        Base.Getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                        //ot.blocks[i] = bl;  // TODO: надо доработать
                        ot.Numblocks = (Int32)i + 1;
                    }
                }
                else if (num_data_blocks < cur_data_blocks)
                {
                    // Уменьшение длины объекта
                    ObjTab ot = ByteArrayToObjtab(Base.Getblock_for_write(b.Blocks[Numblocks - 1], true));

                    for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                    {
                        i = cur_data_blocks % 1023;
                        Base.Set_block_as_free(ot.Blocks[i]);
                        ot.Blocks[i] = 0;
                        ot.Numblocks = (Int32)i;
                        if (i == 0)
                        {
                            Base.Set_block_as_free(b.Blocks[--Numblocks]);
                            b.Blocks[Numblocks] = 0;
                            if (Numblocks != 0)
                                ot = ByteArrayToObjtab(Base.Getblock_for_write(b.Blocks[Numblocks - 1], true));
                        }
                    }

                }
                Len = _len;
                if (Numblocks != 0)
                {
                    //memcpy(blocks, b->blocks, numblocks * 4);
                    Array.Copy(b.Blocks, Blocks, (Int32)Numblocks * 4);
                }

                Write_new_version();

            }
            else if (Type == V8objtype.data838)
            {
                offsperpage = Base.Pagesize / 4;
                maxlen = Base.Pagesize * offsperpage * (offsperpage - 6);
                if (_len > maxlen)
                {
                    Console.WriteLine($"Попытка установки длины файла больше максимальной. Номер страницы файла {Block}. Максимальная длина файла {maxlen}. Запрошенная длина файла {_len}");
                    _len = maxlen;
                }

                //bd = (v838ob_data*)base->getblock_for_write(block, true);
                bd = ByteArrayTov838ob(Base.Getblock_for_write(Block, true));
                bd.Len = _len;

                num_data_blocks = (UInt32)(_len + Base.Pagesize - 1) / Base.Pagesize;
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
                cur_data_blocks = (UInt32)(Len + Base.Pagesize - 1) / Base.Pagesize;

                if (Numblocks != num_blocks)
                {
                    Blocks = null;
                    if (num_blocks != 0)
                        Blocks = new UInt32[num_blocks];
                    else
                        Blocks = null;
                }

                if (num_data_blocks > cur_data_blocks)
                {
                    // Увеличение длины объекта
                    if (Fatlevel == 0 && newfatlevel != 0)
                    {
                        bl = Base.Get_free_block();
                        //bb = (objtab838*)base->getblock_for_write(bl, false);
                        bb = ByteArrayToObjtab838(Base.Getblock_for_write(bl, false));
                        //memcpy(bb->blocks, bd->blocks, numblocks * 4);
                        Array.Copy(bd.Blocks, bb.Blocks, (Int32)Numblocks * 4);
                        Fatlevel = newfatlevel;
                        bd.Fatlevel = (Int16)newfatlevel;
                        bd.Blocks[0] = bl;
                        Numblocks = 1;
                    }
                    else
                    {
                        bb = ByteArrayToObjtab838(Base.Getblock_for_write(bd.Blocks[Numblocks - 1], true));
                    }

                    if (Fatlevel != 0)
                    {
                        for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                        {
                            i = cur_data_blocks % offsperpage;
                            if (i == 0)
                            {
                                bl = Base.Get_free_block();
                                bd.Blocks[Numblocks++] = bl;
                                bb = ByteArrayToObjtab838(Base.Getblock_for_write(bl, false));
                            }
                            bl = Base.Get_free_block();
                            Base.Getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                            bb.Blocks[i] = bl;
                        }
                    }
                    else
                    {
                        for (; cur_data_blocks < num_data_blocks; cur_data_blocks++)
                        {
                            bl = Base.Get_free_block();
                            Base.Getblock_for_write(bl, false); // получаем блок без чтения, на случай, если блок вдруг в конце файла
                            bd.Blocks[cur_data_blocks] = bl;
                        }
                    }
                }
                else if (num_data_blocks < cur_data_blocks)
                {
                    // Уменьшение длины объекта
                    if (Fatlevel != 0)
                    {
                        //bb = (objtab838*)base->getblock_for_write(b->blocks[numblocks - 1], true);
                        //bb = ByteArrayToObjtab838(_base.getblock_for_write(b.blocks[numblocks - 1], true));
                        bb = ByteArrayToObjtab838(Base.Getblock_for_write((UInt32)Numblocks - 1, true)); // TODO: надо доработать
                        for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                        {
                            i = cur_data_blocks % offsperpage;
                            Base.Set_block_as_free(bb.Blocks[i]);
                            bb.Blocks[i] = 0;
                            if (i == 0)
                            {
                                Base.Set_block_as_free(bd.Blocks[--Numblocks]);
                                bd.Blocks[Numblocks] = 0;
                                if (Numblocks != 0)
                                {
                                    //bb = ByteArrayToObjtab838(_base.getblock_for_write(b.blocks[numblocks - 1], true));
                                    bb = ByteArrayToObjtab838(Base.Getblock_for_write((UInt32)Numblocks - 1, true)); // TODO: надо доработать
                                }
                            }
                        }
                    }
                    else
                    {
                        for (cur_data_blocks--; cur_data_blocks >= num_data_blocks; cur_data_blocks--)
                        {
                            Base.Set_block_as_free(bd.Blocks[cur_data_blocks]);
                            bd.Blocks[cur_data_blocks] = 0;
                        }
                        Numblocks = num_data_blocks;
                    }

                    if (Fatlevel != 0 && newfatlevel == 0)
                    {
                        if (Numblocks != 0)
                        {
                            bl = bd.Blocks[0];
                            //memcpy(bd->blocks, bb->blocks, num_data_blocks * 4);
                            //Array.Copy(bb.blocks, bd.blocks, num_data_blocks * 4);  // TODO: надо доработать
                            Base.Set_block_as_free(bl);
                        }
                        Fatlevel = 0;
                        bd.Fatlevel = 0;
                    }

                }

                Len = _len;
                if (Numblocks != 0)
                {
                    //memcpy(blocks, bd->blocks, numblocks * 4);
                    Array.Copy(bd.Blocks, Blocks, (Int32)Numblocks * 4);
                }
                Write_new_version();

            }

        }

        /// <summary>
        /// Конструктор нового (еще не существующего) объекта
        /// </summary>
        /// <param name="_base"></param>
        public V8object(Tools1CD _base)
        {
            UInt32 blockNum;
            byte[] b;

            blockNum = _base.Get_free_block();
            b = _base.Getblock_for_write(blockNum, false);

            //memset(b, 0, _base->pagesize);

            if (_base.Version < DBVer.ver8_3_8_0)
            {
                //memcpy(((v8ob*)b)->sig, SIG_OBJ, 8);

                
                Array.Copy(SIG_OBJ.ToCharArray(), ByteArrayToV8ob(b).Sig, 8);
            }
            else
            {
                b[0] = 0x1c;
                b[1] = 0xfd;
            }
            Init(_base, (Int32)blockNum);

        }

        /// <summary>
        /// Конструктор существующего объекта
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        public V8object(Tools1CD _base, Int32 blockNum)
        {
            Init(_base, blockNum);
        }

        /// <summary>
        /// Инициализация с параметрами
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="blockNum"></param>
        public void Init(Tools1CD _base, Int32 blockNum)
        {
            this.Base = _base;
            Prev = Last;
            Next = null;

            if (Last != null)
                Last.Next = this;
            else
                First = this;
            Last = this;
            if (blockNum == 1)
            {
                if ((int)this.Base.Version < (int)DBVer.ver8_3_8_0)
                    Type = V8objtype.free80;
                else
                    Type = V8objtype.free838;

            }
            else
            {
                if ((int)this.Base.Version < (int)DBVer.ver8_3_8_0)
                    Type = V8objtype.data80;
                else
                    Type = V8objtype.data838;
            }

            if (Type == V8objtype.data80 || Type == V8objtype.free80)
            {
                Fatlevel = 1;
                V8ob t = new V8ob();
                Byte[] buf = new Byte[0x1000];

                //this._base.getblock(t, (UInt32)blockNum);
                this.Base.Getblock(ref buf, (UInt32)blockNum);

                t = ByteArrayToV8ob(buf);
                if (!t.Sig.SequenceEqual(SIG_OBJ))
                {
                    t = new V8ob();
                    Init();
                    Console.WriteLine($"Ошибка получения объекта из блока. Блок не является объектом. Блок {blockNum}");
                }

                Len = t.Len;

                _version VV = new _version();
                VV.Version_1 = t.Version.Version_1;
                VV.Version_2 = t.Version.Version_2;
                VV.Version_3 = t.Version.Version_3;

                /*
                Version.Version_1 = t.Version.Version_1;
                Version.Version_2 = t.Version.Version_2;
                Version.Version_3 = t.Version.Version_3;
                */
                Version = VV;


                _version_rec VR = new _version_rec();

                /*
                Version_rec.Version_1 = Version.Version_1 + 1;
                Version_rec.Version_2 = 0;
                */

                VR.Version_1 = Version.Version_1 + 1;
                VR.Version_2 = 0;
                Version_rec = VR;


                New_version_recorded = false;
                Block = (UInt32)blockNum;
                Real_numblocks = 0;
                Data = null;

                if (Type == V8objtype.free80)
                {
                    if (Len != 0)
                        Numblocks = (Len - 1) / 0x400 + 1;
                    else
                        Numblocks = 0;

                    // в таблице свободных блоков в разделе blocks может быть больше блоков, чем numblocks
                    // numblocks - кол-во блоков с реальными данными
                    // оставшиеся real_numblocks - numblocks блоки принадлежат объекту, но не содержат данных
                    while (t.Blocks[Real_numblocks] != 0)
                        Real_numblocks++;
                    if (Real_numblocks != 0)
                    {
                        Blocks = new UInt32[Real_numblocks];
                        //memcpy(blocks, t->blocks, real_numblocks * sizeof(*blocks));
                        Array.Copy(t.Blocks, 0, Blocks, 0, Real_numblocks * 4);
                    }
                    else
                        Blocks = null;

                }
                else
                {
                    if (Len != 0)
                        Numblocks = (Len - 1) / 0x3ff000 + 1;
                    else
                        Numblocks = 0;
                    if (Numblocks != 0)
                    {
                        Blocks = new UInt32[Numblocks];
                        //memcpy(blocks, t->blocks, numblocks * sizeof(*blocks));
                        Array.Copy(t.Blocks, 0, Blocks, 0, Real_numblocks * 4);
                    }
                    else
                        Blocks = null;
                }

            }
            else if (Type == V8objtype.data838)
            {
                byte[] b = new byte[this.Base.Pagesize];
                this.Base.Getblock(ref b, (UInt32)blockNum);
                V838ob_data t = ByteArrayTov838ob(b);
                if (t.Sig[0] != 0x1c || t.Sig[1] != 0xfd)
                {
                    b = null;
                    Init();
                    Console.WriteLine($"Ошибка получения файла из страницы. Страница не является заголовочной страницей файла данных. Блок {blockNum}");
                    return;
                }
                Len = t.Len;
                Fatlevel = t.Fatlevel;
                if (Fatlevel == 0 && Len > ((this.Base.Pagesize / 4 - 6) * this.Base.Pagesize))
                {
                    b = null;
                    Init();
                    Console.WriteLine($"Ошибка получения файла из страницы. Длина файла больше допустимой при одноуровневой таблице размещения. Блок {blockNum}. Длина файла {Len}");
                    return;
                }

                _version VV = new _version();

                VV.Version_1 = t.Version.Version_1;
                VV.Version_2 = t.Version.Version_2;
                VV.Version_3 = t.Version.Version_3;
                
                /*                
                Version.Version_1 = t.Version.Version_1;
                Version.Version_2 = t.Version.Version_2;
                Version.Version_3 = t.Version.Version_3;
                */

                Version = VV;

                _version_rec VR = new _version_rec();

                VR.Version_2 = Version.Version_1 + 1;
                VR.Version_2 = 0;
                
                /*
                Version_rec.Version_1 = Version.Version_1 + 1;
                Version_rec.Version_2 = 0;
                */

                Version_rec = VR;

                New_version_recorded = false;
                Block = (UInt32)blockNum;
                Real_numblocks = 0;
                Data = null;

                if (Len != 0)
                {
                    if (Fatlevel == 0)
                    {
                        Numblocks = (Len - 1) / this.Base.Pagesize + 1;
                    }
                    else
                    {
                        Numblocks = (Len - 1) / (this.Base.Pagesize / 4 * this.Base.Pagesize) + 1;
                    }
                }
                else
                    Numblocks = 0;

                if (Numblocks != 0)
                {
                    Blocks = new UInt32[Numblocks];

                    //memcpy(blocks, t->blocks, numblocks * sizeof(*blocks));
                    //Array.Copy(t.blocks, 0, blocks, 0, (int)numblocks);
                    Array.Copy(t.Blocks, 0, Blocks, 0, t.Blocks.Length);
                }
                else
                    Blocks = null;

                b = null;
            }
            else
            {
                byte[] b = new byte[this.Base.Pagesize];

                this.Base.Getblock(ref b, (UInt32)blockNum);

                V838ob_free t = ByteArrayTov838ob_free(b);

                if (t.Sig[0] != 0x1c || t.Sig[1] != 0xff)
                {
                    b = null;
                    Init();
                    Console.WriteLine($"Ошибка получения файла из страницы. Страница не является заголовочной страницей файла свободных блоков. Блок {blockNum}");
                    return;
                }

                Len = 0; // ВРЕМЕННО! Пока не понятна структура файла свободных страниц


                _version VV = new _version();

                VV.Version_1 = t.Version;

                //Version.Version_1 = t.Version;

                Version = VV;

                /*
                Version_rec.Version_1 = Version.Version_1 + 1;
                Version_rec.Version_2 = 0;
                */

                _version_rec VR = new _version_rec();

                VR.Version_1 = Version.Version_1 + 1;
                VR.Version_2 = 0;

                Version_rec = VR;

                New_version_recorded = false;
                Block = (UInt32)blockNum;
                Real_numblocks = 0;
                Data = null;

                if (Len != 0)
                    Numblocks = (Len - 1) / 0x400 + 1;
                else
                    Numblocks = 0;

                // в таблице свободных блоков в разделе blocks может быть больше блоков, чем numblocks
                // numblocks - кол-во блоков с реальными данными
                // оставшиеся real_numblocks - numblocks блоки принадлежат объекту, но не содержат данных
                while (t.Blocks[Real_numblocks] != 0)
                    Real_numblocks++;
                if (Real_numblocks != 0)
                {
                    Blocks = new UInt32[Real_numblocks];
                    //memcpy(blocks, t->blocks, real_numblocks * sizeof(*blocks));
                    Array.Copy(t.Blocks, 0, Blocks, 0, Real_numblocks * 4);
                }
                else
                    Blocks = null;

                b = null;

            }

        }

        /// <summary>
        /// Инициализация без параметров
        /// </summary>
        public void Init()
        {
            Len = 0;

            _version VV = new _version();

            VV.Version_1 = 0;
            VV.Version_2 = 0;

            Version = VV;
            /*
            Version.Version_1 = 0;
            Version.Version_2 = 0;
            

            Version_rec.Version_1 = 0;
            Version_rec.Version_2 = 0;
            */
            _version_rec VR = new _version_rec();
            VR.Version_1 = 0;
            VR.Version_2 = 0;
            Version_rec = VR;



            New_version_recorded = false;
            Numblocks = 0;
            Real_numblocks = 0;
            Blocks = null;
            Block = 999999;
            Data = null;
            Lockinmemory = false;
            Type = V8objtype.unknown;
            Fatlevel = 0;
        }

        /// <summary>
        /// чтение всего объекта целиком, поддерживает кеширование объектов. Буфер принадлежит объекту
        /// </summary>
        /// <returns></returns>
        public byte[] Getdata()
        {

            byte[] tt;
            ObjTab b;
            Objtab838 bb;
            UInt32 i, l;
            Int32 j, pagesize, blocksperpage;
            UInt64 ll;
            UInt32 curlen = 0;

            //lastdataget = GetTickCount();
            if (Len == 0)
                return null;

            if (Data != null)
                return Data;

            if (Type == V8objtype.free80)
            {
                l = (UInt32)Len * 4;
                Data = new byte[l];
                tt = Data;
                i = 0;
                while (l > PAGE4K)
                {
                    Base.Getblock(ref tt, Blocks[i++]);
                    // tt += PAGE4K; TODO: Надо понять что с этим сделать
                    l -= (UInt32)PAGE4K;
                }
                Base.Getblock(ref tt, Blocks[i], (Int32)l);
            }
            else if (Type == V8objtype.data80)
            {
                l = (UInt32)Len;
                Data = new byte[l];
                tt = Data;
                for (i = 0; i < Numblocks; i++)
                {
                    //b = (objtab*)base->getblock(blocks[i]);
                    b = ByteArrayToObjtab(Base.Getblock(Blocks[i]));


                    for (j = 0; j < b.Numblocks; j++)
                    {
                        //curlen = std::min(DEFAULT_PAGE_SIZE, l);
                        curlen = (UInt32)Math.Min(PAGE4K, l);
                        Base.Getblock(ref tt, b.Blocks[j], (Int32)curlen);
                        if (l <= curlen)
                            break;
                        l -= curlen;
                        // tt += PAGE4K; TODO: Надо понять что с этим сделать
                    }
                    if (l <= curlen) break;
                }
            }
            else if (Type == V8objtype.data838)
            {
                pagesize = (Int32)Base.Pagesize;
                blocksperpage = pagesize / 4;
                ll = Len;
                Data = new byte[ll];
                tt = Data;
                if (Fatlevel != 0)
                {
                    for (i = 0; i < Numblocks; i++)
                    {
                        //bb = (objtab838*)base->getblock(blocks[i]);
                        bb = ByteArrayToObjtab838(Base.Getblock(Blocks[i]));
                        for (j = 0; j < blocksperpage; j++)
                        {
                            curlen = ll > (UInt32)pagesize ? (UInt32)pagesize : (UInt32)ll;
                            Base.Getblock(ref tt, bb.Blocks[j], (Int32)curlen);
                            if (ll <= curlen) break;
                            ll -= curlen;

                            // tt += pagesize; TODO: Надо понять что с этим сделать
                        }
                        if (ll <= curlen) break;
                    }
                }
                else
                {
                    for (i = 0; i < Numblocks; i++)
                    {
                        //curlen = ll > pagesize ? pagesize : ll;
                        curlen = ll > (UInt32)pagesize ? (UInt32)pagesize : (UInt32)ll;
                        Base.Getblock(ref tt, Blocks[i], (Int32)curlen);
                        if (ll <= curlen)
                            break;
                        ll -= curlen;
                        // tt += pagesize; TODO: Надо понять что с этим сделать
                    }
                }
            }
            else if (Type == V8objtype.free838)
            {
                // TODO: реализовать v8object::getdata() для файла свободных страниц формата 8.3.8
            }
            return Data;

            //return new byte[100];

        }

        /// <summary>
        /// чтение кусочка объекта, поддерживает кеширование блоков. Буфер не принадлежит объекту
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public byte[] Getdata(byte[] buf, UInt64 _start, UInt64 _length)
        {
            UInt32 curblock;
            UInt32 curoffblock;
            //byte[] _buf = new byte[0x1000];
            byte[] _buf = new byte[PAGE8K];
            byte[] _bu;
            UInt32 curlen = 0;
            UInt32 destIndex = 0;
            UInt32 offsperpage = 0;

            ObjTab b;
            Objtab838 bb;
            UInt32 curobjblock = 0;
            UInt32 curoffobjblock = 0;

            if (Data != null)
            {
                //memcpy(buf, data + _start, _length);
                Array.Copy(Data, (int)_start, buf, 0, (int)_length);
            }
            else
            {
                if (Type == V8objtype.free80)
                {
                    if (_start + _length > Len * 4)
                    {
                        Console.WriteLine($"Попытка чтения данных за пределами объекта. Номер блока объекта, {Block}. Длина объекта, {Len * 4}. Начало читаемых данных, {_start}. Длина читаемых данных {_length}");
                        return null;
                    }
                    curblock = (UInt32)_start >> 12;
                    //_buf = (char*)buf;
                    Array.Copy(buf, _buf, buf.Length);
                    curoffblock = (UInt32)_start - (curblock << 12);
                    // curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE - curoffblock), _length);
                    curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);

                    while (_length != 0)
                    {
                        _bu = Base.Getblock(Blocks[curblock++]);
                        if (_bu == null)
                            return null;

                        Array.Copy(_bu, curoffblock, _buf, destIndex, curlen);
                        // _buf += curlen; пока не понятно что с этим делать
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);
                    }

                }
                else if (Type == V8objtype.data80)
                {
                    if (_start + _length > Len)
                    {
                        Console.WriteLine($"Попытка чтения данных за пределами объекта. Номер блока объекта, {Block}. Длина объекта, {Len * 4}. Начало читаемых данных, {_start}. Длина читаемых данных {_length}");
                        return null;
                    }

                    curblock = (UInt32)_start >> 12;
                    Array.Copy(_buf, buf, buf.Length);
                    curoffblock = (UInt32)_start - (curblock << 12);
                    curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);

                    curobjblock = curblock / 1023;
                    curoffobjblock = curblock - curobjblock * 1023;
                    b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock++]));
                    /*
                    if (!b)
                    {
                        return nullptr;
                    }
                    */
                    while (_length != 0)
                    {
                        _bu = Base.Getblock(b.Blocks[curoffobjblock++]);
                        if (_bu == null)
                        {
                            return null;
                        }
                        //memcpy(_buf, _bu + curoffblock, curlen);
                        Array.Copy(_bu, curoffblock, buf, destIndex, curlen);

                        //_buf += curlen; пока не понятно что делать
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = Math.Min((UInt32)(0x1000 - curoffblock), (UInt32)_length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= 1023)
                            {
                                curoffobjblock = 0;
                                b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else if (Type == V8objtype.data838)
                {
                    if (_start + _length > Len)
                    {
                        Console.WriteLine($"Попытка чтения данных за пределами объекта. Номер блока объекта, {Block}. Длина объекта, {Len * 4}. Начало читаемых данных, {_start}. Длина читаемых данных {_length}");
                        return null;
                    }

                    curblock = (UInt32)_start / Base.Pagesize;

                    //_buf = (char*)buf;
                    Array.Copy(_buf, buf, buf.Length);
                    offsperpage = Base.Pagesize / 4;
                    curoffblock = (UInt32)_start - (curblock * Base.Pagesize);
                    //curlen = std::min(static_cast<uint64_t>(base->pagesize - curoffblock), _length);
                    curlen = Math.Min((UInt32)(Base.Pagesize - curoffblock), (UInt32)_length);
                    if (Fatlevel != 0)
                    {
                        curobjblock = curblock / offsperpage;
                        curoffobjblock = curblock - curobjblock * offsperpage;

                        bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                        /*
                        if (!bb)
                        {
                            return nullptr;
                        }
                        */
                        while (_length != 0)
                        {
                            _bu = Base.Getblock(bb.Blocks[curoffobjblock++]);
                            if (_bu == null)
                            {
                                return null;
                            }
                            //memcpy(_buf, _bu + curoffblock, curlen);
                            Array.Copy(_bu, curoffblock, buf, destIndex, curlen);
                            //_buf += curlen;
                            destIndex += curlen;
                            _length -= curlen;
                            curoffblock = 0;
                            //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                            curlen = Math.Min((UInt32)(Base.Pagesize - curoffblock), (UInt32)_length);
                            if (_length > 0)
                            {
                                if (curoffobjblock >= offsperpage)
                                {
                                    curoffobjblock = 0;
                                    bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                                }
                            }
                        }
                    }
                    else
                    {
                        destIndex = 0;
                        while (_length != 0)
                        {
                            _bu = Base.Getblock(Blocks[curblock++]);
                            if (_bu == null)
                            {
                                return null;
                            }
                            //memcpy(_buf, _bu + curoffblock, curlen);
                            Array.Copy(_bu, curoffblock, buf, destIndex, curlen);
                            //_buf += curlen;
                            destIndex += curlen;
                            _length -= curlen;
                            curoffblock = 0;
                            //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                            curlen = Math.Min((UInt32)(Base.Pagesize - curoffblock), (UInt32)_length);
                        }

                    }

                }
                else if (Type == V8objtype.free838)
                {
                    // TODO: реализовать V8Object::getdata для файла свободных страниц формата 8.3.8
                }
            }
            return buf;
        }

        /// <summary>
        /// запись кусочка объекта, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public bool Setdata(byte[] buf, UInt64 _start, UInt64 _length)
        {
            UInt32 curblock;
            UInt32 curoffblock;
            //char* _buf;
            byte[] _buf;
            UInt32 curlen;

            UInt32 curobjblock;
            UInt32 curoffobjblock;
            UInt32 offsperpage;
            UInt32 destIndex = 0;

            if (Base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {Block}");
                return false;
            }

            if (Type == V8objtype.free80 || Type == V8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {Block}");
                return false;
            }

            //lastdataget = GetTickCount();


            Data = null;
            if (_start + _length > Len)
            {
                Set_len(_start + _length);
            }

            if (Type == V8objtype.data80)
            {
                curblock = (UInt32)_start >> 12;

                _buf = buf;

                curoffblock = (UInt32)_start - (curblock << 12);

                //curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE - curoffblock), _length);
                curlen = (UInt32)Math.Min((UInt64)(PAGE4K - curoffblock), _length);

                curobjblock = curblock / 1023;
                curoffobjblock = curblock - curobjblock * 1023;

                //objtab* b = (objtab*)base->getblock(blocks[curobjblock++]);
                ObjTab b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock++]));
                while (_length != 0)
                {
                    //memcpy((char*)(base->getblock_for_write(b->blocks[curoffobjblock++], curlen != DEFAULT_PAGE_SIZE)) + curoffblock, _buf, curlen);
                    Array.Copy(_buf, destIndex, Base.Getblock_for_write(b.Blocks[curoffobjblock++], curlen != PAGE4K), curoffblock, curlen);

                    //_buf += curlen; TODO : Надо что-то с этим делать
                    destIndex += curlen;
                    _length -= curlen;
                    curoffblock = 0;

                    //curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE), _length);
                    curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);

                    if (_length > 0)
                    {
                        if (curoffobjblock >= 1023)
                        {
                            curoffobjblock = 0;
                            //b = (objtab*)base->getblock(blocks[curobjblock++]);
                            b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock++]));
                        }
                    }
                }

                Write_new_version();
                return true;
            }
            else if (Type == V8objtype.data838)
            {
                curblock = (UInt32)_start / Base.Pagesize;

                //_buf = (char*)buf;
                _buf = buf;

                curoffblock = (UInt32)_start - (curblock * Base.Pagesize);

                //curlen = std::min(static_cast<uint64_t>(base->pagesize - curoffblock), _length);
                curlen = (UInt32)Math.Min((UInt64)Base.Pagesize - curoffblock, _length);

                if (Fatlevel != 0)
                {
                    offsperpage = Base.Pagesize / 4;
                    curobjblock = curblock / offsperpage;
                    curoffobjblock = curblock - curobjblock * offsperpage;

                    //objtab838* bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                    Objtab838 bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                    destIndex = 0;
                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(bb->blocks[curoffobjblock++], curlen != base->pagesize)) + curoffblock, _buf, curlen);

                        Array.Copy(_buf, destIndex, Base.Getblock_for_write(bb.Blocks[curoffobjblock++], curlen != Base.Pagesize), curoffblock, curlen);
                        // _buf += curlen; TODO : Надо что-то с этим делать
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;

                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                //bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                                bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    destIndex = 0;
                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(blocks[curblock++], curlen != base->pagesize)) + curoffblock, _buf, curlen);
                        Array.Copy(_buf, destIndex, Base.Getblock_for_write(Blocks[curblock++], curlen != Base.Pagesize), curoffblock, curlen);
                        //_buf += curlen;
                        destIndex += curlen;
                        _length -= curlen;
                        curoffblock = 0;
                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);
                    }
                }

                Write_new_version();
                return true;
            }

            return false;
        }

        /// <summary>
        /// запись объекта целиком, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public bool Setdata(byte[] buf, UInt64 _length)
        {
            byte[] _buf;

            UInt32 curlen = 0;
            UInt32 offsperpage;
            UInt32 curblock;
            UInt32 curobjblock;
            UInt32 curoffobjblock;

            UInt32 srcIndex = 0;

            if (Base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {Block}");
                return false;
            }

            if (Type == V8objtype.free80 || Type == V8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {Block}");
                return false;
            }

            Data = null;
            Set_len(_length);

            _buf = buf;

            if (Type == V8objtype.data80)
            {
                for (UInt32 i = 0; i < Numblocks; i++)
                {
                    //objtab* b = (objtab*)base->getblock(blocks[i]);
                    ObjTab b = ByteArrayToObjtab(Base.Getblock(Blocks[i]));

                    for (UInt32 j = 0; j < b.Numblocks; j++)
                    {

                        //curlen = std::min(static_cast<uint64_t>(DEFAULT_PAGE_SIZE), _length);
                        curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);

                        //char* tt = base->getblock_for_write(b->blocks[j], false);
                        byte[] tt = Base.Getblock_for_write(b.Blocks[j], false);

                        //memcpy(tt, buf, curlen);
                        Array.Copy(_buf, srcIndex, tt, 0, curlen);

                        srcIndex += (UInt32)PAGE4K;

                        if (_length <= curlen)
                        {
                            break;
                        }

                        _length -= curlen;

                    }
                    if (_length <= curlen)
                    {
                        break;
                    }
                }

                Write_new_version();
                return true;
            }
            else if (Type == V8objtype.data838)
            {
                curblock = 0;
                srcIndex = 0;
                //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);

                if (Fatlevel != 0)
                {
                    offsperpage = Base.Pagesize / 4;
                    curobjblock = 0;
                    curoffobjblock = 0;

                    //objtab838* bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                    Objtab838 bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));

                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(bb->blocks[curoffobjblock++], false)), buf, curlen);
                        Array.Copy(_buf, srcIndex, Base.Getblock_for_write(bb.Blocks[curoffobjblock++], false), 0, curlen);
                        srcIndex += curlen;
                        //buf += curlen;
                        _length -= curlen;
                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                //bb = (objtab838*)base->getblock(blocks[curobjblock++]);
                                bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    srcIndex = 0;
                    while (_length != 0)
                    {
                        //memcpy((char*)(base->getblock_for_write(blocks[curblock++], false)), buf, curlen);
                        Array.Copy(_buf, srcIndex, Base.Getblock_for_write(Blocks[curblock++], false), 0, curlen);
                        srcIndex += curlen;
                        _length -= curlen;
                        //curlen = std::min(static_cast<uint64_t>(base->pagesize), _length);
                        curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);
                    }
                }

                Write_new_version();
                return true;
            }

            return false;
        }

        /// <summary>
        /// записывает поток целиком в объект, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool Setdata(Stream stream)
        {

            UInt32 curlen = 0;
            UInt32 offsperpage;
            UInt32 curblock;
            UInt32 curobjblock;
            UInt32 curoffobjblock;

            UInt64 _length;

            if (Base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {Block}");
                return false;
            }

            if (Type == V8objtype.free80 || Type == V8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {Block}");
                return false;
            }

            Data = null;

            //_length = stream->GetSize();
            _length = (UInt64)stream.Length;

            Set_len(_length);

            //stream->Seek(0, soFromBeginning);
            stream.Seek(0, SeekOrigin.Begin);

            if (Type == V8objtype.data80)
            {
                for (UInt32 i = 0; i < Numblocks; i++)
                {
                    ObjTab b = ByteArrayToObjtab(Base.Getblock(Blocks[i]));

                    for (UInt32 j = 0; j < b.Numblocks; j++)
                    {
                        curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);

                        //char* tt = base->getblock_for_write(b->blocks[j], false);
                        byte[] tt = Base.Getblock_for_write(b.Blocks[j], false);
                        stream.Read(tt, 0, (Int32)curlen);
                        if (_length <= curlen)
                        {
                            break;
                        }
                        _length -= curlen;
                    }
                    if (_length <= (UInt64)curlen)
                    {
                        break;
                    }
                }

                Write_new_version();
                return true;
            }
            else if (Type == V8objtype.data838)
            {
                curblock = 0;

                curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);

                if (Fatlevel != 0)
                {
                    offsperpage = Base.Pagesize / 4;
                    curobjblock = 0;
                    curoffobjblock = 0;

                    Objtab838 bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));

                    while (_length != 0)
                    {
                        stream.Read(Base.Getblock_for_write(bb.Blocks[curoffobjblock++], false), 0, (Int32)curlen);

                        _length -= curlen;
                        curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    while (_length != 0)
                    {
                        stream.Read(Base.Getblock_for_write(Blocks[curblock++], false), 0, (Int32)curlen);
                        _length -= curlen;
                        curlen = (UInt32)Math.Min((UInt64)Base.Pagesize, _length);
                    }
                }

                Write_new_version();
                return true;
            }

            return false;
        }

        /// <summary>
        /// запись части потока в объект, поддерживает кеширование блоков.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        /// <returns></returns>
        public bool Setdata(Stream stream, UInt64 _start, UInt64 _length)
        {
            UInt32 curblock;
            UInt32 curoffblock;
            UInt32 curlen;

            UInt32 curobjblock;
            UInt32 curoffobjblock;
            UInt32 offsperpage;

            if (Base.ReadOnly)
            {
                Console.WriteLine($"Попытка записи в файл в режиме \"Только чтение\". Номер страницы файла {Block}");
                return false;
            }

            if (Type == V8objtype.free80 || Type == V8objtype.free838)
            {
                Console.WriteLine($"Попытка прямой записи в файл свободных страниц. Номер страницы файла {Block}");
                return false;
            }




            Data = null;

            if (_start + _length > Len)
                Set_len(_start + _length);

            if (Type == V8objtype.data80)
            {
                curblock = (UInt32)_start >> 12;
                curoffblock = (UInt32)_start - (curblock << 12);
                curlen = (UInt32)Math.Min((UInt64)(PAGE4K - curoffblock), _length);

                curobjblock = curblock / 1023;
                curoffobjblock = curblock - curobjblock * 1023;

                //objtab* b = (objtab*)base->getblock(blocks[curobjblock++]);
                ObjTab b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock++]));

                while (_length != 0)
                {
                    stream.Read(Base.Getblock_for_write(b.Blocks[curoffobjblock++], curlen != PAGE4K), (Int32)curoffblock, (Int32)curlen);
                    _length -= curlen;
                    curoffblock = 0;
                    curlen = (UInt32)Math.Min((UInt64)PAGE4K, _length);
                    if (_length > 0)
                    {
                        if (curoffobjblock >= 1023)
                        {
                            curoffobjblock = 0;
                            b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock++]));
                        }
                    }
                }

                Write_new_version();
                return true;
            }
            else if (Type == V8objtype.data838)
            {
                curblock = (UInt32)_start / Base.Pagesize;
                curoffblock = (UInt32)_start - (curblock * Base.Pagesize);
                curlen = (UInt32)Math.Min((UInt64)(Base.Pagesize - curoffblock), _length);

                if (Fatlevel != 0)
                {
                    offsperpage = Base.Pagesize / 4;
                    curobjblock = curblock / offsperpage;
                    curoffobjblock = curblock - curobjblock * offsperpage;

                    Objtab838 bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));

                    while (_length != 0)
                    {
                        stream.Read(Base.Getblock_for_write(bb.Blocks[curoffobjblock++], curlen != Base.Pagesize), (Int32)curoffblock, (Int32)curlen);
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = (UInt32)Math.Min((UInt64)(Base.Pagesize), _length);
                        if (_length > 0)
                        {
                            if (curoffobjblock >= offsperpage)
                            {
                                curoffobjblock = 0;
                                bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock++]));
                            }
                        }
                    }
                }
                else
                {
                    while (_length != 0)
                    {
                        stream.Read(Base.Getblock_for_write(Blocks[curblock++], curlen != Base.Pagesize), (Int32)curoffblock, (Int32)curlen);
                        _length -= curlen;
                        curoffblock = 0;
                        curlen = (UInt32)Math.Min((UInt64)(Base.Pagesize), _length);
                    }
                }

                Write_new_version();
                return true;
            }

            return false;

        }

        /// <summary>
        /// Возвращает длину объекта
        /// </summary>
        /// <returns></returns>
        public UInt64 Getlen()
        {
            return (Type == V8objtype.free80) ? (Len * 4) : Len;
        }

        /// <summary>
        /// Сохранение в файл
        /// </summary>
        /// <param name="_filename"></param>
        public void SaveToFile(String _filename)
        {
            UInt64 pagesize = Base.Pagesize;
            FileStream fs = new FileStream(_filename, FileMode.CreateNew);

            //char* buf = new char[pagesize];
            byte[] buf = new byte[pagesize];

            UInt64 total_size = Getlen();
            UInt64 remain_size = total_size;
            for (UInt64 offset = 0; offset < total_size; offset += pagesize)
            {
                UInt32 size_of_block = Math.Min((UInt32)remain_size, (UInt32)pagesize);

                Getdata(buf, offset, size_of_block);

                fs.Write(buf, 0, (Int32)size_of_block);
                remain_size -= pagesize;
            }
            buf = null;
            fs.Dispose();
        }

        #region Не используемые пока
        public void Set_lockinmemory(bool _lock)
        {
        }

        public static void Garbage()
        {
        }
        #endregion

        /// <summary>
        /// получить физическое смещение в файле по смещению в объекте
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public UInt64 Get_fileoffset(UInt64 offset)
        {
            UInt32 _start = (UInt32)offset;
            ObjTab b;
            Objtab838 bb;
            UInt32 curblock;
            UInt32 curoffblock;
            UInt32 curobjblock;
            UInt32 curoffobjblock;
            UInt32 offsperpage;

            if (Type == V8objtype.free80)
            {
                curblock = _start >> 12;
                curoffblock = _start - (curblock << 12);
                return (((UInt64)(Blocks[curblock])) << 12) + curoffblock;
            }
            else if (Type == V8objtype.data80)
            {
                curblock = _start >> 12;
                curoffblock = _start - (curblock << 12);

                curobjblock = curblock / 1023;
                curoffobjblock = curblock - curobjblock * 1023;

                //b = (objtab*)base->getblock(blocks[curobjblock]);
                b = ByteArrayToObjtab(Base.Getblock(Blocks[curobjblock]));

                return (((UInt64)(b.Blocks[curoffobjblock])) << 12) + curoffblock;
            }
            else if (Type == V8objtype.data838)
            {
                curblock = _start / Base.Pagesize;
                curoffblock = _start - (curblock * Base.Pagesize);
                if (Fatlevel != 0)
                {
                    offsperpage = Base.Pagesize / 4;
                    curobjblock = curblock / offsperpage;
                    curoffobjblock = curblock - curobjblock * offsperpage;
                    //bb = (objtab838*)base->getblock(blocks[curobjblock]);
                    bb = ByteArrayToObjtab838(Base.Getblock(Blocks[curobjblock]));
                    return (((UInt64)(bb.Blocks[curoffobjblock])) * Base.Pagesize) + curoffblock;
                }
                else
                {
                    return (((UInt64)(Blocks[curblock])) * Base.Pagesize) + curoffblock;
                }
            }

            else if (Type == V8objtype.free838)
            {
                // TODO: реализовать v8object::get_fileoffset для файла свободных страниц формата 8.3.8
                return 0;
            }

            return 0;
        }

        /// <summary>
        /// пометить блок как свободный
        /// </summary>
        /// <param name="block_number"></param>
        public void Set_block_as_free(UInt32 block_number)
        {
            if (Block != 1)
            {
                // Таблица свободных блоков
                Console.WriteLine($"Попытка установки свободного блока в объекте, не являющимся таблицей свободных блоков. Блок объекта {Block}");
                return;
            }

            Int32 j = (Int32)Len >> 10;   // length / 1024
            Int32 i = (Int32)Len & 0x3ff; // length % 1024

            //v8ob ob = (v8ob*)base->getblock_for_write(block, true);
            V8ob ob = ByteArrayToV8ob(Base.Getblock_for_write(Block, true));

            if (Real_numblocks > j)
            {
                Len++;
                ob.Len = (UInt32)Len;
                //uint32_t* b = (uint32_t*)base->getblock_for_write(blocks[j], true);
                byte[] b = Base.Getblock_for_write(Blocks[j], true);
                b[i] = (byte)block_number;
                if (Numblocks <= (UInt32)j)
                    Numblocks = (UInt32)j + 1;
            }
            else
            {
                ob.Blocks[Real_numblocks] = block_number;
                Blocks = null;
                Real_numblocks++;
                Blocks = new UInt32[Real_numblocks];
                //memcpy(blocks, ob->blocks, real_numblocks * 4);
                Array.Copy(ob.Blocks, Blocks, Real_numblocks * 4);
            }

        }

        /// <summary>
        /// получить номер свободного блока (и пометить как занятый)
        /// </summary>
        /// <returns></returns>
        public UInt32 Get_free_block()
        {
            if (Block != 1)
            {
                // Таблица свободных блоков
                Console.WriteLine($"Попытка получения свободного блока в объекте, не являющимся таблицей свободных блоков. Блок объекта {Block}");
                return 0;
            }

            if (Len != 0)
            {
                Len--;
                UInt32 j = (UInt32)Len >> 10;    // length / 1024
                UInt32 i = (UInt32)Len & 0x3ff;  // length % 1024
                byte[] b = Base.Getblock_for_write(Blocks[j], true);

                UInt32 k = b[i];
                b[i] = 0;

                //v8ob* ob = (v8ob*)base->getblock_for_write(block, true);
                V8ob ob = ByteArrayToV8ob(Base.Getblock_for_write(Block, true));

                ob.Len = (UInt32)Len;

                return k;
            }
            else
            {
                //unsigned i = MemBlock::get_numblocks();
                UInt32 i = 10000000; // TODO: надо доработать
                Base.Getblock_for_write(i, false);

                return i;
            }

        }

        /// <summary>
        /// получает версию очередной записи и увеличивает сохраненную версию объекта
        /// </summary>
        /// <param name="ver"></param>
        public void Get_version_rec_and_increase(_version ver)
        {
            ver.Version_1 = Version_rec.Version_1;
            ver.Version_2 = Version_rec.Version_2;

            _version_rec VR = new _version_rec();

            VR.Version_1 = Version_rec.Version_1;
            VR.Version_2++;

            Version_rec = VR;

            //Version_rec.Version_2++;

        }

        /// <summary>
        /// получает сохраненную версию объекта
        /// </summary>
        /// <param name="ver"></param>
        public void Get_version(_version ver)
        {
            ver.Version_1 = Version.Version_1;
            ver.Version_2 = Version.Version_2;
        }

        /// <summary>
        /// записывает новую версию объекта
        /// </summary>
        public void Write_new_version()
        {
            _version new_ver = new _version(0,0,0);
            if (New_version_recorded) return;
            Int32 veroffset = Type == V8objtype.data80 || Type == V8objtype.free80 ? 12 : 4;

            new_ver.Version_1 = Version.Version_1 + 1;
            new_ver.Version_2 = Version.Version_2;
            //memcpy(base->getblock_for_write(block, true) + veroffset, &new_ver, 8);
            // TODO: надо доработать
            //Array.Copy();
            New_version_recorded = true;
        }

        /// <summary>
        /// Возвращает "первый" объект
        /// </summary>
        /// <returns></returns>
        public static V8object Get_first()
        {
            return First;
        }

        /// <summary>
        /// Возвращает "последний" объект
        /// </summary>
        /// <returns></returns>
        public static V8object Get_last()
        {
            return Last;
        }

        /// <summary>
        /// Возвращает "следующий" объект
        /// </summary>
        /// <returns></returns>
        public V8object Get_next()
        {
            return Next;
        }

        /// <summary>
        /// Возвращает номер блока
        /// </summary>
        /// <returns></returns>
        public UInt32 Get_block_number()
        {
            return Block;
        }

        /// <summary>
        /// rewrite - перезаписывать поток _str. Истина - перезаписывать (по умолчанию), Ложь - дописывать
        /// </summary>
        /// <param name="_str"></param>
        /// <param name="_startblock"></param>
        /// <param name="_length"></param>
        /// <param name="rewrite"></param>
        /// <returns></returns>
        public Stream ReadBlob(Stream _str, UInt32 _startblock, UInt32 _length = UInt32.MaxValue, bool rewrite = true)
        {

            UInt32 _curblock = 0;
            byte[] _curb;

            UInt32 _numblock = 0;
            UInt32 startlen = 0;

            if (rewrite)
                _str.SetLength(0);

            startlen = (UInt32)_str.Position;
            if (_startblock == 0 && _length != 0)
            {
                Console.WriteLine($"Попытка чтения нулевого блока файла Blob. Номер страницы файла {Block}");
                return _str;
            }

            _numblock = (UInt32)Len >> 8;
            if (_numblock << 8 != Len)
            {
                Console.WriteLine($"Длина файла Blob не кратна 0x100. Номер страницы файла {Block}. Длина файла {Len}");
            }

            _curb = new byte[0x100];
            _curblock = _startblock;

            while (_curblock != 0)
            {
                if (_curblock >= _numblock)
                {
                    Console.WriteLine($"Попытка чтения блока файла Blob за пределами файла. Номер страницы файла {Block}. Всего блоков {_numblock}. Читаемый блок {_curblock}");
                    return _str;
                }
                Getdata(_curb, _curblock << 8, 0x100);

                _curblock = (UInt32)_curb[0];

                UInt16 _curlen = _curb[4];

                if (_curlen > 0xfa)
                {
                    Console.WriteLine($"Попытка чтения из блока файла Blob более 0xfa байт. Номер страницы файла {Block}. Индекс блока {_curblock}. Читаемый байт {_curlen}");
                    return _str;
                }
                _str.Write(_curb, 6, _curlen);

                if (_str.Length - startlen > _length)
                    break; // аварийный выход из возможного ошибочного зацикливания

            }

            _curb = null;

            if (_length != UInt32.MaxValue)
                if (_str.Length - startlen != _length)
                {
                    Console.WriteLine($"Несовпадение длины Blob-поля, указанного в записи, с длиной практически прочитанных данных. Номер страницы файла {Block}. Длина поля {_length}. Прочитано {_str.Length - startlen}");
                }

            return _str;
        }

    }
}
