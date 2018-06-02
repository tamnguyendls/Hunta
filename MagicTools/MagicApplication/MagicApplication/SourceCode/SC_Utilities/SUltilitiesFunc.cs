using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MagicApplication
{
    static class SUltFunc
    {
        public static T DeepCopy<T>(T SourceObject)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, SourceObject);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }

        public static bool VerifyString(string sData)
        {
            bool bRet = false;

            if (sData != null && sData != string.Empty)
            {
                bRet = true;
            }

            return bRet;
        }
    }
}
