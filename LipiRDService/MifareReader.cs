using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LipiRDService
{
    class MifareReader
    {
        private bool g_isConnected = false;
        private int g_rHandle, g_retCode;
        public static byte g_Sec;
        private byte[] g_pKey = new byte[6];

        public void Connect()
        {
            //=====================================================================
            // This function opens the port(connection) to ACR120 reader
            //=====================================================================

            // Variable declarations
            int ctr = 0;
            byte[] FirmwareVer = new byte[31];
            byte[] FirmwareVer1 = new byte[20];
            byte infolen = 0x00;
            string FirmStr;
            ACR120U.tReaderStatus ReaderStat = new ACR120U.tReaderStatus();

            if (g_isConnected)
            {

                Log.WriteLog("Device is already connected.", "MifareReader");
                return;

            }

            g_rHandle = ACR120U.ACR120_Open(0);
            if (g_rHandle != 0)

                Log.WriteLog("[X] Invalid Handle: " + String.Format("{0}", g_rHandle), "MifareReader");

            else
            {

                Log.WriteLog("Connected to USB" + string.Format("{0}", 1), "MifareReader");
                g_isConnected = true;

                //Get the DLL version the program is using
                g_retCode = ACR120U.ACR120_RequestDLLVersion(ref infolen, ref FirmwareVer[0]);
                if (g_retCode < 0)

                    Log.WriteLog("[X] " + ACR120U.GetErrMsg(g_retCode), "MifareReader");

                else
                {
                    FirmStr = "";
                    for (ctr = 0; ctr < Convert.ToInt16(infolen) - 1; ctr++)
                        FirmStr = FirmStr + char.ToString((char)(FirmwareVer[ctr]));
                    Log.WriteLog("DLL Version : " + FirmStr, "MifareReader");
                }

                //Routine to get the firmware version.
                g_retCode = ACR120U.ACR120_Status(g_rHandle, ref FirmwareVer1[0], ref ReaderStat);
                if (g_retCode < 0)

                    Log.WriteLog("[X] " + ACR120U.GetErrMsg(g_retCode), "MifareReader");

                else
                {
                    FirmStr = "";
                    for (ctr = 0; ctr < Convert.ToInt16(infolen); ctr++)
                        if ((FirmwareVer1[ctr] != 0x00) && (FirmwareVer1[ctr] != 0xFF))
                            FirmStr = FirmStr + char.ToString((char)(FirmwareVer1[ctr]));
                    Log.WriteLog("Firmware Version : " + FirmStr, "MifareReader");
                }

            }
        }

        public void SelectClick()
        {

            //=====================================================================
            // This function selects a single card in range and return the Serial No.
            //=====================================================================

            //Variable Declarations
            byte[] ResultSN = new byte[11];
            byte ResultTag = 0x00;
            byte[] TagType = new byte[51];
            int ctr = 0;
            string SN = "";


            //Select specific card based from serial number	
            g_retCode = ACR120U.ACR120_Select(g_rHandle, ref TagType[0], ref ResultTag, ref ResultSN[0]);
            if (g_retCode < 0)

                Log.WriteLog("[X] " + ACR120U.GetErrMsg(g_retCode), "MifareReader" +
                    "");

            else
            {
                Log.WriteLog("Select Success", "MifareReader");
                //get serial number and convert to hex

                if ((TagType[0] == 4) || (TagType[0] == 5))
                {

                    SN = "";
                    for (ctr = 0; ctr < 7; ctr++)
                    {
                        SN = SN + string.Format("{0:X2} ", ResultSN[ctr]);
                    }

                }
                else
                {

                    SN = "";
                    for (ctr = 0; ctr < ResultTag; ctr++)
                    {
                        SN = SN + string.Format("{0:X2} ", ResultSN[ctr]);
                    }

                }

                //Display Serial Number
                Log.WriteLog("( i ) Card Serial Number: " + SN + " ( " + ACR120U.GetTagType1(TagType[0]) + " )", "MifareReader");
            }

        }


        public void Login()
        {

            //=====================================================================
            // This function is for the authentication to access one sector of a card.
            // Only one sector at a time can be accessed.
            //=====================================================================

            long sto = 0;
            byte vKeyType = 0x00;
            int ctr, tmpInt, PhysicalSector = 0;

            //Login loginForm = new Login();
            //if (loginForm.ShowDialog() == DialogResult.Cancel)
            //return;
            int i = 0;
            switch (i)//loginForm.cbLoginType.SelectedIndex)
            {

                case 0:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_A;
                    break;
                case 1:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_B;
                    break;
                case 2:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_DEFAULT_A;
                    break;
                case 3:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_DEFAULT_B;
                    break;
                case 4:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_DEFAULT_F;
                    break;
                case 5:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_STORED_A;
                    break;
                case 6:
                    vKeyType = ACR120U.ACR120_LOGIN_KEYTYPE_STORED_B;
                    break;

            }
            String s = "FF";
            switch (i)//loginForm.cbLoginType.SelectedIndex)
            {

                case 0:
                case 1:
                    g_pKey[0] = Convert.ToByte(s, 16);
                    g_pKey[1] = Convert.ToByte(s, 16);
                    g_pKey[2] = Convert.ToByte(s, 16);
                    g_pKey[3] = Convert.ToByte(s, 16);
                    g_pKey[4] = Convert.ToByte(s, 16);
                    g_pKey[5] = Convert.ToByte(s, 16);
                    break;
                case 2:
                case 3:
                case 4:
                    for (ctr = 0; ctr < 6; ctr++)
                        g_pKey[ctr] = 0xFF;
                    break;
                case 5:
                case 6:
                    sto = Convert.ToInt32(s);
                    break;

            }

            tmpInt = Convert.ToInt16("0");
            g_Sec = Convert.ToByte(tmpInt);

            //Computation for obtaining the actual Physical Sector.
            if (Convert.ToInt16(g_Sec) > 31)
                PhysicalSector = Convert.ToInt16(g_Sec) + ((Convert.ToInt16(g_Sec) - 32) * 3);
            else
                PhysicalSector = Convert.ToInt16(g_Sec);

            g_retCode = ACR120U.ACR120_Login(g_rHandle, Convert.ToByte(PhysicalSector), Convert.ToInt16(vKeyType),
                Convert.ToByte(sto), ref g_pKey[0]);
            if (g_retCode < 0)

                Log.WriteLog("[X] " + ACR120U.GetErrMsg(g_retCode), "MifareReader");

            else
            {
                Log.WriteLog("Login Success", "MifareReader");
                Log.WriteLog("Log at Logical Sector: " + String.Format("{0}", Convert.ToInt16(g_Sec)), "MifareReader");
                Log.WriteLog("Log at Physical Sector: " + String.Format("{0}", PhysicalSector), "MifareReader");
                //Log.WriteLog("Login Type index: " + string.Format("{0}", 0), "MifareReader");

            }
        }


        public String ReadBlock()
        {

            //Variable Declarations
            byte[] dataRead = new byte[16];
            string dstr = "";
            int ctr, tmpInt = 0;
            byte Blck = 0;

            Blck = Convert.ToByte("0");

            tmpInt = Convert.ToInt16(Blck);
            if (Convert.ToInt16(g_Sec) > 31)
                tmpInt = tmpInt + ((Convert.ToInt16(g_Sec) - 32) * 16) + 128;
            else
                tmpInt = tmpInt + Convert.ToInt16(g_Sec) * 4;
            Blck = Convert.ToByte(tmpInt);

            g_retCode = ACR120U.ACR120_Read(g_rHandle, Blck, ref dataRead[0]);
            if (g_retCode < 0)

                Log.WriteLog("[X] " + ACR120U.GetErrMsg(g_retCode), "MifareReader");

            else
            {
                Log.WriteLog("Read Block Success", "MifareReader");
                // convert bytes read to chosen option (e.g. AS HEX, AS ASCII)
                dstr = "";
                for (ctr = 0; ctr < 16; ctr++)
                {
                    if (true)
                    {
                        dstr = dstr + string.Format("{0:X2} ", dataRead[ctr]);
                    }
                    else
                    {
                        dstr = dstr + char.ToString((char)(dataRead[ctr]));
                    }

                }

                Log.WriteLog("Read Block " + String.Format("{0}", Blck) + ": " + dstr, "MifareReader");

            }
            return dstr;
        }
    }
}
