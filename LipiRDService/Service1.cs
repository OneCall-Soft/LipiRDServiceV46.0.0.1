using LipiRDLogin;
using Microsoft.Owin.Hosting;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace LipiRDService
{


    public partial class LipiRDService
#if (!DEBUG)
        : ServiceBase
#endif
    {

        public string baseAddress;
        AutoResetEvent owinwait = new AutoResetEvent(false);
        Thread ThrdOwin;
        Thread DeviceCheck;
        public LipiRDService()
        {
            InitializeComponent();
            Log.strLogPath = ConfigurationManager.AppSettings["LogPath"].ToString();
        }

#if DEBUG
        [STAThread]
        public void OnStart(string[] args)
#else
        protected override void OnStart(string[] args)
#endif
        {
            try
            {

                ThrdOwin = new Thread(startowin);
                ThrdOwin.Start();

                Log.WriteLog("*******RD-Service started " + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion + "*******");

                Log.WriteEJData("VERSION IS: " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);



                if (Global.objIni == null)
                    Global.objIni = new IniFile("C:\\RMS\\KioskClientHealth.ini");



                if (ConfigurationManager.AppSettings["CashAcceptor"].ToString() == "0")
                {
                    Log.WriteLog("Cash Acceptor Device In Demo INIT Successfully");
                }
                else
                {
                    if (Global.objCash == null)
                        Global.objCash = new CashAcceptor();

                    Global.objCash.InitializeAcceptor();

                    Global.NoteDenomination = new Int32[10];
                }

                //if (ConfigurationManager.AppSettings["ReceiptPrinter"].ToString() == "0")
                //{
                //    Log.WriteLog("Receipt Printer Device In Demo INIT Successfully");
                //}
                //else
                //{
                if (Global.objRP == null)
                    Global.objRP = new ReceiptPrinter();

                Global.objRP.InitializeReceipt();
                //  }

                if (ConfigurationManager.AppSettings["BarcodeReader"].ToString() == "0")
                {
                    Log.WriteLog("Barcode Reader Device In Demo INIT Successfully");
                }
                else
                {
                    if (Global.objBarcode == null)
                        Global.objBarcode = new Barcode();

                    Global.objBarcode.Initialize();
                }

                if (ConfigurationManager.AppSettings["Camera"].ToString() == "0")
                {
                    Log.WriteLog("Camera Device In Demo INIT Successfully");
                }
                else
                {
                    if (Global.objCamera == null)
                        Global.objCamera = new Camera();

                    Global.objCamera.ConnectWithCameraClient();
                }
                baseAddress = "http://127.0.0.1:1234/";

                Log.WriteLog("Base Address - " + baseAddress);

                // Devie Health Check
                Global.DevName_Barcode = Global.objIni.IniReadValue("Device_DriverName", "BarcodeReader");
                Global.DevName_StatementPrinter = Global.objIni.IniReadValue("Device_DriverName", "StatementPrinter");
                Global.DevName_RFID = Global.objIni.IniReadValue("Device_DriverName", "RFID");
                Global.DevName_Camera = Global.objIni.IniReadValue("Device_DriverName", "Camera");
                Global.DevName_EzeTap = Global.objIni.IniReadValue("Device_DriverName", "EzeTap");
                Global.DevName_ReceiptPrinter = Global.objIni.IniReadValue("Device_DriverName", "RecieptPrinter");
                Global.DevName_FingerScanner = Global.objIni.IniReadValue("Device_DriverName", "FingerPrintScanner");
                Global.DevName_KeyPadWithMouse = Global.objIni.IniReadValue("Device_DriverName", "Keyboard");
                Global.DevName_Touch = Global.objIni.IniReadValue("Device_DriverName", "TouchScreen");

                Global.DIndex_ReceiptPrinter = Global.objIni.IniReadValue("Device_Detail", "RecieptPrinter");
                Global.DIndex_Barcode = Global.objIni.IniReadValue("Device_Detail", "BarcodeReader");
                Global.DIndex_StatementPrinter = Global.objIni.IniReadValue("Device_Detail", "StatementPrinter");
                Global.DIndex_Camera = Global.objIni.IniReadValue("Device_Detail", "Camera");
                Global.DIndex_RFID = Global.objIni.IniReadValue("Device_Detail", "RFID");
                Global.DIndex_EzeTap = Global.objIni.IniReadValue("Device_Detail", "EzeTap");
                Global.DIndex_FingerScanner = Global.objIni.IniReadValue("Device_Detail", "FingerPrintScanner");
                Global.DIndex_KeyPadWithMouse = Global.objIni.IniReadValue("Device_Detail", "Keyboard");
                Global.DIndex_Touch = Global.objIni.IniReadValue("Device_Detail", "TouchScreen");

                DeviceCheck = new Thread(CheckDeviceHealth);
                DeviceCheck.Start();

                owinwait.Set();
            }
            catch (Exception ex)
            {
                Log.WriteLog("OnStart() Excp  - " + ex.Message + "\r\n" + ex.StackTrace);
            }

        }


#if DEBUG
        public void OnStop()
#else
        protected override void OnStop()
# endif
        {
            try
            {
                if (Global.objRP != null)
                    Global.objRP.DeinitializeReceipt();

                ThrdOwin.Abort();
                Log.WriteLog("ThrdOwin abort");
                Log.WriteLog("Stop Service");
                Log.WriteEJData("SERVICE STOPPED");
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception readed while Stopping service:  " + ex.Message);
                Log.WriteEJData("ERROR IN SERVICE STOP");
            }
            finally
            {

                GC.Collect();    
            }
        }
        public void CheckDeviceHealth()
        {
            while (true)
            {
                try
                {
                    if (Global.bAppCallRunning == false)
                    {
                        ManagementObjectSearcher deviceList = new ManagementObjectSearcher("Select Name, Status, DeviceID from Win32_PnPEntity");

                        // Any results? There should be!
                        if (deviceList != null)
                        {
                            // Enumerate the devices
                            foreach (ManagementObject device in deviceList.Get())    //To Get Status of USB Device 
                            {
                                if (Global.bAppCallRunning)  //In between txn start
                                    break;
                                 
                                if (device.GetPropertyValue("Name") != null && Global.DevName_EzeTap != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_EzeTap.ToLower()) &&
                                    device.GetPropertyValue("Status") != null && device.GetPropertyValue("Status").ToString().ToLower() == "ok")
                                {
                                    Global.bEzetapFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && device.GetPropertyValue("Name").ToString() != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_Touch.ToLower()))
                                {
                                    Global.bTouchScreenFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_ReceiptPrinter != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_ReceiptPrinter.ToLower()))
                                {
                                    Global.bReceiptPrinter = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_FingerScanner != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_FingerScanner.ToLower()))
                                {
                                    Global.bFingerScannerFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_Camera != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_Camera.ToLower()))
                                {
                                    Global.bCameraFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_Barcode != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_Barcode.ToLower()))
                                {
                                    Global.bBarcodeFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_KeyPadWithMouse != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_KeyPadWithMouse.ToLower()))
                                {
                                    Global.bKeyPadMouseFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_StatementPrinter != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_StatementPrinter.ToLower()))
                                {
                                    Global.bKeyStatmentPrinterFound = true;
                                }
                                else if (device.GetPropertyValue("Name") != null && Global.DevName_RFID != "" && device.GetPropertyValue("Name").ToString().ToLower().Contains(Global.DevName_RFID.ToLower()))
                                {
                                    Global.bRFIDFound = true;
                                }

                            }

                            //if (!Global.bAppCallRunning)  //In between txn start
                            //{
                            //    Global.objRP.GetPrinterStatus();
                            //    if (Global.RPStatus.Contains("Offline"))
                            //        Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Disconnected");
                            //    else if (Global.RPStatus.Contains("Online"))
                            //        Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Connected");
                            //    else
                            //        Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, Global.RPStatus);

                            //    // string strCashStatus = Global.objCash.DeviceStatus();
                            //    // Global.objIni.IniWriteValue("Health_Details", Global.DIndex_CashAcceptor, strCashStatus);
                            //}

                            if (!Global.bAppCallRunning) //In between txn start
                            {
                                if (Global.bBarcodeFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_Barcode, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_Barcode, "Disconnected");

                                if (Global.bCameraFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_Camera, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_Camera, "Disconnected");

                                if (Global.bEzetapFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_EzeTap, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_EzeTap, "Disconnected");

                                if (Global.bFingerScannerFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_FingerScanner, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_FingerScanner, "Disconnected");

                                if (Global.bKeyStatmentPrinterFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_StatementPrinter, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_StatementPrinter, "Disconnected");

                                //if (Global.bTVFound)
                                //    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_SignageTV, "Connected");
                                //else
                                //    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_SignageTV, "Disconnected");

                                //if (Global.bVCCameraFound)
                                //    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_VCCamera, "Connected");
                                //else
                                //    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_VCCamera, "Disconnected");

                                if (Global.bKeyPadMouseFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_KeyPadWithMouse, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_KeyPadWithMouse, "Disconnected");

                                Global.objRP.GetPrinterStatus();
                               
                                if (Global.bReceiptPrinter)
                                {
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Connected");
                                    if (Global.RPStatus.ToLower().Contains("open"))
                                    {
                                        Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Cover Open");
                                    }
                                    else if (Global.RPStatus.ToLower().Contains("low"))
                                    {
                                        Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Paper Low");
                                    }   
                                    else if (Global.RPStatus.ToLower().Contains("out"))
                                    {
                                        Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Paper Out");
                                    }
                                       
                                }
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_ReceiptPrinter, "Disconnected");

                                if (Global.bTouchScreenFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_Touch, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_Touch, "Disconnected");

                                if (Global.bRFIDFound)
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_RFID, "Connected");
                                else
                                    Global.objIni.IniWriteValue("Health_Details", Global.DIndex_RFID, "Disconnected");

                                Global.objIni.IniWriteValue("Health_Details", "Changes_DoneHealth", "1");
                            }
                        }
                    }
                }
                catch (Exception excp)
                {
                    Log.WriteLog("Exception in CheckDevieHealthThread:  " + excp.Message);
                }

                Thread.Sleep(1 * 60 * 1000);

                Global.bEzetapFound = false;
                Global.bTouchScreenFound = false;
                Global.bStmtPrinterFound = false;
                Global.bFingerScannerFound = false;
                Global.bCameraFound = false;
                Global.bTVFound = false;
                Global.bDocScannerFound = false;
                Global.bBarcodeFound = false;
                Global.bKeyPadMouseFound = false;
                Global.bVCCameraFound = false;
                Global.bRFIDFound = false;
                Global.bReceiptPrinter = false;

            }
        }


        public void startowin()
        {
            Log.WriteLog("startowin - " + baseAddress);
            owinwait.WaitOne();
            Log.WriteLog("startowin WaitOne - " + baseAddress);
            Thread.Sleep(5000);
            try
            {
                using (WebApp.Start<Startup>(url: baseAddress))
                {
                    //Console.WriteLine("Service Listening at " + baseAddress);
                    Log.WriteLog("Service Is Listening At:-" + baseAddress);
                    Log.WriteEJData("SERVICE STARTED");
                    Thread.Sleep(-1);
                }
            }
            catch (Exception e)
            {
                Log.WriteLog("Exception in WebApp:-" + e.StackTrace + "Message:-" + e.Message);
                Log.WriteEJData("SERVICE NOT STARTED");
            }
            finally
            {
                //this.Dispose();
            }
        }

    }

    /// <summary>
    /// Global static class for logging
    /// </summary>
    public static class Log
    {
        // member variables
        public static string strLogPath;

        /// <summary>
        /// Static function to write log
        /// </summary>
        /// <param name="inText">String to log in the file</param>
        public static void WriteLog(string plainText, string strInFolderName = "LipiRDService")
        {
            try
            {
                // Create directory if not exist
                if (!Directory.Exists(strLogPath + "\\" + strInFolderName))
                    Directory.CreateDirectory(strLogPath + "\\" + strInFolderName);

                //Added to encrypt log
                //string enString = SSTCryptographer.Encrypt(DateTime.Now.ToString("HH:mm:ss") + " " + plainText);

                string enString = DateTime.Now.ToString("HH:mm:ss") + " " + plainText;

                StreamWriter swFile = new StreamWriter(strLogPath + "\\" + strInFolderName + "\\" + strInFolderName + "_" + DateTime.Today.ToString("dd_MM_yyyy") + ".txt", true);

                if (swFile != null)
                {
                    swFile.WriteLine(enString);
                }

                if (swFile != null)
                {
                    swFile.Close();
                }
            }
            catch (Exception excp)
            { }
        }

        public static void WriteEJData(string EJString)
        {
            try
            {
                string strDir = ConfigurationManager.AppSettings["LogPath"].ToString();

                // Create directory if not exist
                if (!Directory.Exists(strDir + "\\EJDATA"))
                    Directory.CreateDirectory(strDir + "\\EJDATA");

                if (!Directory.Exists("D:\\EJDATA_BK"))
                    Directory.CreateDirectory("D:\\EJDATA_BK");

                //Added to encrypt log
                //string enString = SSTCryptographer.Encrypt(DateTime.Now.ToString("HH:mm:ss") + " " + plainText);

                string enString = DateTime.Now.ToString("HH:mm:ss") + " " + EJString;

                StreamWriter swFile = new StreamWriter(strDir + "\\EJDATA\\EJDATA" + "_" + DateTime.Today.ToString("dd_MM_yyyy") + ".txt", true);

                if (swFile != null)
                {
                    swFile.WriteLine(enString);
                    swFile.Close();
                }

                StreamWriter swFileBK = new StreamWriter("D:\\EJDATA_BK\\EJDATA" + "_" + DateTime.Today.ToString("dd_MM_yyyy") + ".txt", true);

                if (swFileBK != null)
                {
                    swFileBK.WriteLine(enString);
                    swFileBK.Close();
                }

            }
            catch (Exception excp)
            { }
        }


        public static void WriteLog(byte[] plainText, string strInFolderName = "LipiRDService")
        {
            try
            {
                FileStream objFS = null;

                // Create directory if not exist
                if (!Directory.Exists(strLogPath + "\\" + strInFolderName))
                    Directory.CreateDirectory(strLogPath + "\\" + strInFolderName);

                //Added to encrypt log
                //string enString = SSTCryptographer.Encrypt(DateTime.Now.ToString("HH:mm:ss") + " " + plainText);

                //string enString = DateTime.Now.ToString("HH:mm:ss") + " " + plainText;

                objFS = new FileStream(strLogPath + "\\" + strInFolderName + "\\" + strInFolderName + "_" + DateTime.Today.ToString("dd_MM_yyyy") + ".txt", FileMode.Append);
                objFS.Write(Encoding.Default.GetBytes(DateTime.Now.ToString("HH:mm:ss") + "\t"), 0, Encoding.Default.GetBytes(DateTime.Now.ToString("HH:mm:ss") + "\t").Length);
                objFS.Write(plainText, 0, plainText.Length);
                objFS.Write(Encoding.Default.GetBytes("\n"), 0, Encoding.Default.GetBytes("\n").Length);
                objFS.Close();
            }
            catch (Exception excp)
            { }
        }

    }

    /// <summary>
    /// Global static class for encryption
    /// </summary>
    public class SSTCryptographer
    {
        private static string _key;

        public SSTCryptographer()
        {
        }


        public static string Key
        {
            set
            {
                _key = value;
            }
        }

        /// <summary>
        /// Encrypt the given string using the default key.
        /// </summary>
        /// <param name="strToEncrypt">The string to be encrypted.</param>
        /// <returns>The encrypted string.</returns>
        public static string Encrypt(string strToEncrypt)
        {
            try
            {
                return Encrypt(strToEncrypt, "lipi");
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        /// <summary>
        /// Decrypt the given string using the default key.
        /// </summary>
        /// <param name="strEncrypted">The string to be decrypted.</param>
        /// <returns>The decrypted string.</returns>
        public static string Decrypt(string strEncrypted)
        {
            try
            {
                return Decrypt(strEncrypted, _key);
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        /// <summary>
        /// Encrypt the given string using the specified key.
        /// </summary>
        /// <param name="strToEncrypt">The string to be encrypted.</param>
        /// <param name="strKey">The encryption key.</param>
        /// <returns>The encrypted string.</returns>
        public static string Encrypt(string strToEncrypt, string strKey)
        {
            try
            {
                TripleDESCryptoServiceProvider objDESCrypto = new TripleDESCryptoServiceProvider();
                MD5CryptoServiceProvider objHashMD5 = new MD5CryptoServiceProvider();

                byte[] byteHash, byteBuff;
                string strTempKey = strKey;

                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strTempKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB; //CBC, CFB

                byteBuff = ASCIIEncoding.ASCII.GetBytes(strToEncrypt);
                return Convert.ToBase64String(objDESCrypto.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }


        /// <summary>
        /// Decrypt the given string using the specified key.
        /// </summary>
        /// <param name="strEncrypted">The string to be decrypted.</param>
        /// <param name="strKey">The decryption key.</param>
        /// <returns>The decrypted string.</returns>
        public static string Decrypt(string strEncrypted, string strKey)
        {
            try
            {
                TripleDESCryptoServiceProvider objDESCrypto = new TripleDESCryptoServiceProvider();
                MD5CryptoServiceProvider objHashMD5 = new MD5CryptoServiceProvider();

                byte[] byteHash, byteBuff;
                string strTempKey = strKey;

                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strTempKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB; //CBC, CFB

                byteBuff = Convert.FromBase64String(strEncrypted);
                string strDecrypted = ASCIIEncoding.ASCII.GetString(objDESCrypto.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
                objDESCrypto = null;

                return strDecrypted;
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }
    }
}
