using Microsoft.VisualStudio.TestTools.UnitTesting;
using _1STool1CD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static _1STool1CD.Common;

namespace _1STool1CD.Tests
{
    [TestClass()]
    public class CommonTests
    {
        [TestMethod()]
        public void time1CD_to_FileTimeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void reverse_byte_orderTest()
        {
            // 287454020
            UInt32 test_val = 0x11223344;
            UInt32 test_res = 0;

            // 1144201745
            test_res = reverse_byte_order(test_val);
            bool res = (test_res == 0x44332211);
            Assert.IsTrue(res, 
                String.Format("Исходное число test_val = '{0}': 0x11223344; Преобразованное должно быть 0x44332211, а оно: {1}", 
                               test_val, test_res));

        }

        [TestMethod()]
        public void GUIDas1CTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GUIDasMSTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GUID_to_stringTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void string_to_GUIDTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GUID_to_string_flatTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void string_to_GUID_flatTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void two_hex_digits_to_byteTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void string1C_to_dateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void string_to_dateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void date_to_string1CTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void date_to_stringTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void hexstringTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void hexstringTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void toXMLTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void from_hex_digitTest()
        {
            Assert.Fail();
        }
    }
}