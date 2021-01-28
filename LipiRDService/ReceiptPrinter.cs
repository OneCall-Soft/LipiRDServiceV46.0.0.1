using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LipiRDService
{
    class ReceiptPrinter
    {
        byte[] HeaderBuffer = new byte[100];
        OposPOSPrinter_CCO.OPOSPOSPrinter OPOSPOSPrinter1 = null;       

        public bool InitializeReceipt()
        {
            int ret = -1;
            try
            {
                Log.WriteLog("**************************Called InitializeReceipt()******************************", "ReceiptPrinter");

                Global.PrinterType = System.Configuration.ConfigurationManager.AppSettings["RPType"].ToString();
                Log.WriteLog("Receipt Printer Type - " + Global.PrinterType, "ReceiptPrinter");

                if (Global.PrinterType.ToLower() == "lipi")
                {
                    if (Global.objSerial == null)
                        Global.objSerial = new SerialComm(System.Configuration.ConfigurationManager.AppSettings["RPPort"].ToString(), 115200);

                    if (Global.objSerial.Open())
                    {
                        Global.RPStatus = "Printer Online";
                        Log.WriteLog("Receipt Printer Device Initialize Successfully", "ReceiptPrinter");
                        return true;
                    }
                    else
                    {
                        Global.RPStatus = "Printer Offline";
                        Log.WriteLog("Receipt Printer Initialize Failed", "ReceiptPrinter");
                        return false;
                    }
                }
                else  //BixLon Printer
                {
                    if (OPOSPOSPrinter1 == null)
                        OPOSPOSPrinter1 = new OposPOSPrinter_CCO.OPOSPOSPrinter();

                    if (OPOSPOSPrinter1 != null)
                    {
                        ret = OPOSPOSPrinter1.Open(Global.PrinterType);
                        if (ret == 111 || ret == 106)
                        {
                            OPOSPOSPrinter1.DeviceEnabled = false;
                            OPOSPOSPrinter1.ClearOutput();
                            OPOSPOSPrinter1.Close();
                            ret = OPOSPOSPrinter1.Open(Global.PrinterType);
                        }

                        if (ret == 0)
                        {
                            ret = OPOSPOSPrinter1.ClaimDevice(300);
                            if (ret == 0)
                            {
                                OPOSPOSPrinter1.DeviceEnabled = true;
                                OPOSPOSPrinter1.StatusUpdateEvent += OPOSPOSPrinter1_StatusUpdateEvent;
                                Log.WriteLog("Receipt Printer Device Initialize Successfully", "ReceiptPrinter");
                            }
                            else
                            {
                                Log.WriteLog("Failed to Claim Receipt Printer Device. Error code return: " + ret, "ReceiptPrinter");
                            }
                        }
                        else
                        {
                            Log.WriteLog("Failed to open connection with Receipt Printer. Error code:" + ret, "ReceiptPrinter");
                        }
                    }
                    else
                    {
                        ret = -1;
                        Log.WriteLog("Failed to initialize receipt printer object", "ReceiptPrinter");
                    }

                    if (ret == 0)
                    {
                        GetPrinterStatus();
                        Global.RPStatus = "Printer Online";
                        return true;
                    }
                    //else if(ret==102 || ret==106 || ret== 114)
                    //{
                    //    //zGetPrinterStatus();
                    //    Global.RPStatus = "Paper Out";
                    //    return false;
                    //}
                    else
                    {
                        Global.RPStatus = "Printer Offline";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception occours while initializing printer: " + ex.Message, "ReceiptPrinter");
                return false;
            }
        }

        public string GetPrinterStatus()
        {
            if (Global.PrinterType.ToLower() == "lipi")
            {
                byte[] byStatusCmd = { 0x1D, 0x72, 0x01 };

                Global.objSerial.SendData(byStatusCmd);

                byte[] byReply = new byte[100];
                int iBytesRead = 0;
                if (Global.objSerial.ReceiveData(ref byReply, out iBytesRead) && iBytesRead > 3)
                {
                    switch (Convert.ToInt32(byReply[0]))
                    {
                        case 1:
                        case 9:
                        case 11: Global.RPStatus = "Paper out"; break;
                        case 3:
                        case 2:
                        case 10: Global.RPStatus = "Head up"; break;
                        case 4: Global.RPStatus = "Paper jammed"; break;
                        case 8: Global.RPStatus = "Paper Low"; break;  //Paper Low
                        default: Global.RPStatus = "Printer Online"; break;
                    }
                }
                else
                {
                    Global.RPStatus = "Printer Offline";
                }
            }
            else
            {
                int iRPStatus = OPOSPOSPrinter1.CheckHealth(1);
                if (iRPStatus == 107 || iRPStatus == 101) //Printer Cable Disconnected/Printer Power Off
                    Global.RPStatus = "Printer Offline";
                else if (iRPStatus == 0)
                    Global.RPStatus = "Printer Online";
               // else if (iRPStatus == 102 || iRPStatus == 106 || iRPStatus == 114)
                    //Global.RPStatus = "Paper Out";
            }

            return Global.RPStatus;
        }
        public void StartPrinterStatusMoniter()
        {   
            OPOSPOSPrinter1.StatusUpdateEvent += OPOSPOSPrinter1_StatusUpdateEvent;
        }

        public void StopPrinterStatusMoniter()
        {
            OPOSPOSPrinter1.StatusUpdateEvent -= OPOSPOSPrinter1_StatusUpdateEvent;
        }


        private void OPOSPOSPrinter1_StatusUpdateEvent(int Data)
        {
            //check printer status.
            //const LONG PTR_SUE_COVER_OPEN          = 11;
            //const LONG PTR_SUE_COVER_OK            = 12;
            // const LONG PTR_SUE_REC_EMPTY          = 24;
            // const LONG PTR_SUE_REC_NEAREMPTY      = 25;
            // const LONG PTR_SUE_REC_PAPEROK        = 26;
            // const LONG OPOS_SUE_POWER_ONLINE      = 2001;
            // const LONG OPOS_SUE_POWER_OFFLINE     = 2003;          

            switch (Data)
            {
                case 11:
                    Global.RPStatus = "Cover Open";
                    Log.WriteLog("Cover Open", "ReceiptPrinter");
                    break;
                case 12:
                    //Global.RPStatus = "Printer Online";
                    //Log.WriteLog("Cover state fine", "ReceiptPrinter");
                    break;
                case 24:
                    Global.RPStatus = "Paper Out";
                    Log.WriteLog("Printer out of receipt roll", "ReceiptPrinter");
                    break;
                case 25:
                    Global.RPStatus = "Paper Low";
                    Log.WriteLog("Paper amount low in printer", "ReceiptPrinter");
                    break;
                case 26:
                    Global.RPStatus = "Paper OK";
                    Log.WriteLog("Paper amount enough in printer", "ReceiptPrinter");
                    break;
                case 2001:
                    Global.RPStatus = "Printer Online";
                    Log.WriteLog("Printer went online", "ReceiptPrinter");
                    break;
                case 2003:
                    //Global.RPStatus = "Printer Offline";
                    //Log.WriteLog("Printer went offline", "ReceiptPrinter");
                    break;
                default:
                    //Global.RPStatus = "Unknown Status";
                    //Log.WriteLog("Printer went in Unknown State", "ReceiptPrinter");
                    break;
            }
        }

        public bool PrintReceipt(string PrintData)
        {
            try
            {
                if (PrintData == null || PrintData == "")
                {
                    Log.WriteLog("No data to print", "ReceiptPrinter");
                    return false;
                }
                else
                {
                    Log.WriteLog("Data received to print: " + PrintData, "ReceiptPrinter");

                    if (Global.PrinterType.ToLower() == "lipi")
                    {
                        int iLogoNo = 0;
                        byte[] byLogo = Encoding.Default.GetBytes(PrintData.Substring(0, 3));

                        if (byLogo.Length >= 3 &&
                            byLogo[0] == 27 &&
                            byLogo[1] == 120)
                        {
                            iLogoNo = (byLogo[2] - 48);
                            PrintData = PrintData.Substring(3);
                        }

                        //byte[] byLogoStatusCmd = { 0x1C, 0x70, 0x01, 0x00, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A };
                        //Global.objSerial.SendData(byLogoStatusCmd);

                        //  byte[] byBarCodeCmd = {0x1B,0x4C, 0x1D, 0x48, 0x20, 0x1D, 0x66, 0x20, 0x1D, 0x68, 0x7F, 0x1D, 0x77, 0x04, 0x1D, 0x6B, 0x10, 0x49, 0x0A, 0x7B, 0x42, 0x4E, 0x6F, 0x2E, 0x7B, 0x43, 0x0C, 0x22, 0x38, 0x0C };
                        //  Global.objSerial.SendData(byBarCodeCmd);


                        //Thread.Sleep(500);

                        //byte[] byLogoDownload = File.ReadAllBytes("C:\\Kiosk\\SBI.tlg");
                        //Global.objSerial.SendData(byLogoDownload);

                        //Thread.Sleep(500);

                        byte[] byStatusCmd = ASCIIEncoding.Default.GetBytes(PrintData);
                        Global.objSerial.SendData(byStatusCmd);

                        //Thread.Sleep(500);

                        byte[] byEjectCmd = { 0x1B, 0x69 };
                        Global.objSerial.SendData(byEjectCmd);

                        Log.WriteLog("Data printed successfully", "ReceiptPrinter");
                        return true;
                    }
                    else
                    {
                        try
                        {
                            int iLogoNo = 0;
                            byte[] byLogo = Encoding.Default.GetBytes(PrintData.Substring(0, 3));

                            if (byLogo.Length >= 3 &&
                                byLogo[0] == 27 &&
                                byLogo[1] == 120)
                            {
                                iLogoNo = (byLogo[2] - 48);
                                PrintData = PrintData.Substring(3);
                            }

                            if (iLogoNo > 0)
                            {
                                String LogoPath = "C:\\Kiosk\\Logo_" + iLogoNo.ToString() + ".bmp";   //System.Configuration.ConfigurationManager.AppSettings["LogoPath"].ToString();
                                if (File.Exists(LogoPath))
                                {
                                    PrintBMP(LogoPath);
                                    Log.WriteLog("Logo printed- " + LogoPath, "ReceiptPrinter");
                                }
                                else
                                {
                                    Log.WriteLog("Logopath not found- " + LogoPath, "ReceiptPrinter");
                                }
                            }
                            else
                            {
                                Log.WriteLog("Logo no exist", "ReceiptPrinter");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLog("Excp in logoprint- " + ex.Message, "ReceiptPrinter");
                        }
                        int status = OPOSPOSPrinter1.PrintNormal(2, PrintData);

                        if (status == 0)
                        {
                            Log.WriteLog("Data printed successfully", "ReceiptPrinter");
                            return true;
                        }
                        else
                        {
                            Log.WriteLog("Failed to print data. Error code: " + status, "ReceiptPrinter");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception occours while printing data: " + ex.Message, "ReceiptPrinter");
                return false;
            }
        }

        public bool PrintBMP(string bmpPath)
        {
            int status = -1;
            //const int PTR_BM_LEFT = -1;
            const int PTR_BM_CENTER = -2;
            //const int PTR_BM_RIGHT = -3;

            try
            {
                if (bmpPath == null || bmpPath == "")
                {
                    Log.WriteLog("Path not provided to print BMP", "ReceiptPrinter");
                    return false;
                }
                else
                {
                    Log.WriteLog("BMP Path provided: " + bmpPath, "ReceiptPrinter");

                    status = OPOSPOSPrinter1.PrintBitmap(2, bmpPath, 100, PTR_BM_CENTER);
                    if (status == 0)
                    {
                        Log.WriteLog("BMP image printed successfully", "ReceiptPrinter");
                        return true;
                    }
                    else
                    {
                        Log.WriteLog("Error occurred while printing BMP. Error code: " + status, "ReceiptPrinter");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception occured while printing BMP: " + ex.Message, "ReceiptPrinter");
                return false;
            }
        }
        

        public bool CutRPPaper()
        {
            int status = -1;
            try
            {
                Log.WriteLog("Initiated paper cut process", "ReceiptPrinter");

                if (Global.PrinterType.ToLower() == "lipi")
                {
                    //byte[] byEjectCmd = { 0x1B, 0x69 };
                    //Global.objSerial.SendData(byEjectCmd);
                    return true;
                }
                else
                {
                    status = OPOSPOSPrinter1.CutPaper(0);
                    if (status == 0)
                    {
                        Log.WriteLog("Paper cutting successful", "ReceiptPrinter");
                        return true;
                    }
                    else
                    {
                        Log.WriteLog("Failed to cut the paper. Error code: " + status, "ReceiptPrinter");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception occur while cutting paper:" + ex.Message, "ReceiptPrinter");
                return false;
            }
        }

        public bool DeinitializeReceipt()
        {
            try
            {
                Log.WriteLog("DeinitializeReceipt started", "ReceiptPrinter");
                StopPrinterStatusMoniter();
                OPOSPOSPrinter1.DeviceEnabled = false;
                int ret = OPOSPOSPrinter1.ReleaseDevice();
                ret = OPOSPOSPrinter1.Close();
                OPOSPOSPrinter1 = null;
                if (ret == 0)
                {
                    Log.WriteLog("********************Deinitialization successful**************************", "ReceiptPrinter");
                    return true;
                }
                else
                {
                    Log.WriteLog("Deinitialization failed. Error code:" + ret, "ReceiptPrinter");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception occur while deinitialization" + ex.Message, "ReceiptPrinter");
                return false;
            }
        }
    }
}
