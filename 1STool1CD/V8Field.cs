using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Constants;
using static _1STool1CD.Structures;

namespace _1STool1CD
{
    public class V8Field
    {
        #region Конструктора
        public V8Field(V8Table _parent)
        {
            if (!Null_index_initialized)
            {
                Array.Clear(Null_index, 0, 0x1000);
                Null_index_initialized = true;
            }

            Parent = _parent;
            Len = 0;
            Offset = 0;
            Name = "";

        }

        static V8Field()
        {
            Null_index_initialized = false;
            Null_index = new byte[PAGE4K];
        }
        #endregion

        /// <summary>
        /// возвращает длину поля в байтах
        /// </summary>
        /// <returns></returns>
        public Int32 GetLen() 
        {
            return (Null_exists ? 1 : 0) + Type_manager.Getlen();
        }

        public String GetName()
        {
            return Name;
        }

        public String Get_presentation(byte[] rec, bool EmptyNull = false, char Delimiter = '0', bool ignore_showGUID = false, bool detailed = false)
        {
            return "";
        }

        public String Get_XML_presentation(byte[] rec, bool ignore_showGUID = false)
        {
            //const char* fr = rec + offset;
            byte[] fr = new byte[rec.Length];
            Array.Copy(rec, Offset, fr, 0,rec.Length);
            if (Null_exists)
            {
                if (fr[0] == 0)
                {
                    return "";
                }
                //fr++; // пока не очень понятно что с этим делать
            }
            byte[] fr_char = new byte[fr.Length];
            return Type_manager.Get_XML_presentation(fr_char, Parent, ignore_showGUID);
        }

        public bool Get_binary_value(byte[] binary_value, bool NULL, String value)
        {
            //memset(binary_value, 0, len);
            Array.Clear(binary_value, 0, Len);

            if (Null_exists)
            {
                if (NULL)
                {
                    return true;
                }
                //*binary_value = 1;
                binary_value[0] = 1;
                //binary_value++; // пока не очень понятно что с этим делать
            }
            return Type_manager.Get_binary_value(binary_value, value);

            
        }

        public TypeFields Gettype()
        {
            return Type_manager.Gettype();
        }
        
        public V8Table Getparent()
        {
            return Parent;
        }

        public bool Getnull_exists()
        {
            return Null_exists;
        }

        public Int32 Getlength()
        {
            return Type_manager.Getlength();
        }

        public Int32 Getprecision()
        {
            return Type_manager.Getprecision();
        }

        public bool Getcase_sensitive()
        {
            return Type_manager.Getcase_sensitive();
        }

        public Int32 Getoffset()
        {
            return Offset;
        }

        public String Get_presentation_type()
        {
            return Type_manager.Get_presentation_type();
        }

        public bool Save_blob_to_file(byte[] rec, String filename, bool unpack)
        {
            /*
	        TStream* blob_stream;
	        TStream* _s;
	        TStream* _s2;
	        TStream* _sx;
	        TStream* _sx2;
	        uint32_t k, l;
	        bool usetemporaryfiles = false;

	        char *orec = rec;
	        rec += offset;
	        if (getnull_exists()) {
		        if (*rec == 0) {
			        return false;
		        }
		        rec++;
	        }

	        if (*(uint32_t*)rec == 0 || *(uint32_t*)(rec + 4) == 0) {
		        return false;
	        }

	        if(!unpack) {
		        TFileStream temp_stream(_filename, fmCreate);
		        parent->readBlob(&temp_stream, *(uint32_t*)rec, *(uint32_t*)(rec + 4));
		        return true;
	        }

	        usetemporaryfiles = *(uint32_t*)(rec + 4) > 10 * 1024 * 1024;
	        if(usetemporaryfiles) blob_stream = new TTempStream;
	        else blob_stream = new TMemoryStream;
	        parent->readBlob(blob_stream, *(uint32_t*)rec, *(uint32_t*)(rec + 4));
	        if(blob_stream->GetSize() == 0)
	        {
		        delete blob_stream;
		        return false;
	        }

	        Table *tab = parent;
	        if(usetemporaryfiles) _s = new TTempStream;
	        else _s = new TMemoryStream;

	        if(tab->get_issystem())
	        {

		        // спецобработка для users.usr
		        String tabname = tab->getname();
		        bool is_users_usr = false;
		        if(tabname.CompareIC("PARAMS") == 0)
		        {
			        Field *_f = tab->getfield(0);
			        if(_f->get_presentation(orec).CompareIC("users.usr") == 0) is_users_usr = true;
		        }
		        else if(tabname.CompareIC("V8USERS") == 0) is_users_usr = true;

		        bool maybezipped_twice = true;
		        if(tabname.CompareIC("CONFIG") == 0 || tabname.CompareIC("CONFIGSAVE") == 0)
		        {
			        Field *_f = tab->getfield(0);
			        maybezipped_twice = _f->get_presentation(orec).GetLength() > 72;
		        }

		        if(is_users_usr)
		        {

			        size_t stream_size = blob_stream->GetSize();
			        char *_bb = new char[stream_size];
			        blob_stream->Seek(0, soFromBeginning);
			        blob_stream->Read(_bb, stream_size);

			        size_t xor_mask_size = _bb[0];
			        char *_xor_mask = &_bb[1];
			        char *_xor_buf = &_xor_mask[xor_mask_size];
			        size_t data_size = stream_size - xor_mask_size - 1;
			        for(size_t i = 0, k = 0; i < data_size; i++, k++)
			        {
				        if (k >= xor_mask_size) {
					        k = 0;
				        }
				        _xor_buf[i] ^= _xor_mask[k];
			        }
			        TFileStream temp_stream(_filename, fmCreate);
			        temp_stream.SetSize(0);
			        temp_stream.WriteBuffer(_xor_buf, data_size);
			        delete[] _bb;
		        }
		        else
		        {
			        bool zippedContainer = false;
			        bool zipped = false;
			        try
			        {
				        blob_stream->Seek(0, soFromBeginning);
				        ZInflateStream(blob_stream, _s);
				        zipped = true;
				        if(maybezipped_twice) _sx = _s;
				        else _sx2 = _s;
				        _s = nullptr;
				        delete blob_stream;
				        blob_stream = nullptr;
			        }
			        catch (...)
			        {
				        _sx2 = blob_stream;
				        delete _s;
				        _s = nullptr;
				        blob_stream = nullptr;
				        zipped = false;
			        }

			        if(zipped && maybezipped_twice)
			        {
				        if(usetemporaryfiles) _s2 = new TTempStream;
				        else _s2 = new TMemoryStream;
				        try
				        {
					        _sx->Seek(0, soFromBeginning);
					        ZInflateStream(_sx, _s2);
					        zippedContainer = true;
					        _sx2 = _s2;
					        _s2 = nullptr;
					        delete _sx;
					        _sx = nullptr;
				        }
				        catch (...)
				        {
					        _sx2 = _sx;
					        _sx = nullptr;
					        delete _s2;
					        _s2 = nullptr;
				        }
			        }

			        v8catalog *cat = new v8catalog(_sx2, zippedContainer, true);
			        if(!cat->GetFirst())
			        {
				        TFileStream temp_stream(_filename, fmCreate);
				        temp_stream.CopyFrom(_sx2, 0);
			        }
			        else cat->SaveToDir(_filename);
			        delete cat;
			        delete _sx2;

		        }
	        }
	        else 
                    {
                        char _buf[16];
                        _s->CopyFrom(blob_stream, 0);
                        blob_stream->Seek(0, soFromBeginning);
                        if (blob_stream->Read(_buf, 2) >= 2) if ((_buf[0] == 1 || _buf[0] == 2) && _buf[1] == 1)
                            {
                                if (usetemporaryfiles) _s2 = new TTempStream;
                                else _s2 = new TMemoryStream;
                                bool isOK = true;
                                if (_buf[0] == 1) // неупакованное хранилище
                                {
                                    _s2->CopyFrom(blob_stream, blob_stream->GetSize() - 2);
                                }
                                else
                                {
                                    if (blob_stream->Read(_buf, 16) < 16) isOK = false;
                                    else
                                    {
                                        if (memcmp(_buf, SIG_ZIP, 16) != 0) isOK = false;
                                        else
                                        {
                                            try
                                            {
                                                ZInflateStream(blob_stream, _s2);
                                            }
                                            catch (...)
						        {
                                                isOK = false;
                                            }
                                            }
                                        }
                                    }
                                    if (isOK)
                                    {
                                        _s2->Seek(0, soFromBeginning);
                                        if (_s2->Read(_buf, 8) < 8) isOK = false;
                                        else
                                        {
                                            _s->SetSize(0);
                                            _s->CopyFrom(_s2, _s2->GetSize() - 8);
                                        }

                                    }

                                    if (isOK)
                                    {
                                        int64_t len1C = *(int64_t*)_buf;
                                        if (_s->GetSize() > len1C)
                                        {
                                            _s->Seek(len1C, (TSeekOrigin)soFromBeginning);
                                            _s2->SetSize(0);
                                            _s2->CopyFrom(_s, _s->GetSize() - len1C);
                                            _s2->Seek(0, soFromBeginning);
                                            if (_s2->Read(_buf, 12) >= 12)
                                            {
                                                len1C = *(int64_t*)&_buf[4];
                                                if (len1C <= _s2->GetSize() - 12)
                                                {
                                                    _s->SetSize(0);
                                                    _s->CopyFrom(_s2, len1C);
                                                }
                                            }
                                        }
                                    }
                                    delete _s2;
                                }

                                TFileStream temp_stream(_filename, fmCreate);
                                temp_stream.CopyFrom(_s, 0);
                            }

                        delete _s;
                        delete blob_stream;

                */
            return true;
        }

        public UInt32 GetSortKey(byte[] rec, byte[] SortKey, Int32 maxlen)
        {
            //const char* fr = rec + offset;
            byte[] fr = new byte[rec.Length];
            Array.Copy(rec, Offset, fr, 0, rec.Length);

            if (Null_exists)
            {
                if (fr[0] == 0)
                {
                    //*(SortKey++) = 0;
                    //SortKey[1] = 0;
                    //memcpy(SortKey, (void*)null_index, len);
                    Array.Copy(Null_index, 0, SortKey, 0, Len);
                    return 0;
                }
                //*(SortKey++) = 1;
                SortKey[1] = 1; // Пока не очень понятно чем заменить

                //fr++;
                // пока не очень понятно чем заменить
            }

            try
            {
                return Type_manager.GetSortKey(fr, SortKey, maxlen);
            }
            catch
            {
                throw new Exception($"Таблица {Parent.Name}, поле {Name}");
            }
            
        }

        public static V8Field Field_from_tree(Tree field_tree, ref bool has_version, V8Table parent)
        {
            V8Field fld = new V8Field(parent);

            if (field_tree.Get_type() != Node_type.nd_string)
            {
                throw new Exception("Ошибка получения имени поля таблицы. Узел не является строкой.");
            }
            fld.Name = field_tree.Get_value();

            field_tree = field_tree.Get_next();

            Field_type_declaration type_declaration;
            try
            {

                type_declaration = Field_type_declaration.Parse_tree(field_tree);

            }
            catch
            {
                throw new Exception($"Поле {fld.Name}");
            }

            fld.Type = type_declaration.Type;
            fld.Null_exists = type_declaration.Null_exists;
            fld.Type_manager = FieldType.Create_type_manager(type_declaration);

            if (fld.Type == TypeFields.tf_version)
            {
                has_version = true;
            }
            return fld;
            }

        private String name;

        private TypeFields type;
        private bool null_exists = false;
        private FieldType type_manager;

        private V8Table parent;
        private Int32 len; // длина поля в байтах
        private Int32 offset; // смещение поля в записи
        private static char[] buf;
        //public static char[] null_index;
        private static byte[] null_index;
        private static bool null_index_initialized;

        public string Name { get { return name; } set { name = value; } }

        public TypeFields Type { get { return type; } set { type = value; } }

        public bool Null_exists { get { return null_exists; } set { null_exists = value; } }

        public FieldType Type_manager { get { return type_manager; } set { type_manager = value; } }

        public V8Table Parent { get { return parent; } set { parent = value; } }

        public int Len { get { return len; } set { len = value; } }
        public int Offset { get { return offset; } set { offset = value; } }
        public static char[] Buf { get { return buf; } set { buf = value; } }
        public static byte[] Null_index { get { return null_index; } set { null_index = value; } }
        public static bool Null_index_initialized { get { return null_index_initialized; } set { null_index_initialized = value; } }
    }
}
