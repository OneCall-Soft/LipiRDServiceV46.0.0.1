using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


namespace LipiRDLogin
{
    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key,string val,string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key,string def, StringBuilder retVal,
            int size,string filePath);
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileSectionNamesA")]
        static extern int GetSectionNamesListA(byte[] lpszReturnBuffer, int nSize, string lpFileName);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath)
        {
            path = INIPath;
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section">Section name</PARAM>
        /// <PARAM name="Key">Key Name</PARAM>
        /// <PARAM name="Value">Value Name</PARAM>
        public void IniWriteValue(string Section,string Key,string Value)
        {
            WritePrivateProfileString(Section,Key,Value,this.path);
        }
        
        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section,string Key)
        {
            StringBuilder temp = new StringBuilder(2000);
            int i = GetPrivateProfileString(Section, Key, "", temp, 2000, this.path);
            return temp.ToString();

        }

        public string[] GetSectionsList(string FileName)
        {

            byte[] buff = new byte[1024];
            GetSectionNamesListA(buff, buff.Length, FileName);
            String s = Encoding.Default.GetString(buff);
            String[] names = s.Split('\0');
            if (names.Length > 0)
                return names;
            else
                return null;
        }
       
        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section">Name of the section</PARAM>
        /// <PARAM name="Key">Key to read</PARAM>
        /// <returns>Value in double</returns>
        public double IniReadDoubleValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);
            double dRes;
            Double.TryParse(temp.ToString(), out dRes);
            return dRes;
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section">Name of the section</PARAM>
        /// <PARAM name="Key">Key to read</PARAM>
        /// <returns>Value in double</returns>
        public bool IniReadDateValue(string Section, string Key, out DateTime objDT, out string strExcp)    // Parameter Added [Shubhit 03May13]
        {
            try
            {
                StringBuilder temp = new StringBuilder(25);
                int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);

                objDT = new DateTime(Convert.ToInt32(temp.ToString().Substring(0, 4)), Convert.ToInt32(temp.ToString().Substring(5, 2)), Convert.ToInt32(temp.ToString().Substring(8, 2)), Convert.ToInt32(temp.ToString().Substring(11, 2)), Convert.ToInt32(temp.ToString().Substring(14, 2)), Convert.ToInt32(temp.ToString().Substring(17, 2)));
                strExcp = "";   //Added [Shubhit 03May13]
                return true;
            }
            catch (Exception excp)
            {
                objDT = DateTime.Now;
                strExcp = excp.Message.ToString();  //Added [Shubhit 03May13]
                return false;
            }
        }
    }
}