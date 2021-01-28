using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Threading;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.IO;

namespace LipiRDService
{  
    public class Website
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class WebsitesController : ApiController
    {
        private PipeClient _pipeClient;
        public static string Buffer = "";
        public string RDresult = "";
        Website[] websites = new Website[]
        {
            new Website { Id = 1, Name = "BirdMMO.com", Description = "Bird Flapping Game For Everybody"},
            new Website { Id = 2, Name = "SpiderMMO.com", Description = "Spider Versus Spider Death Match" },
            new Website { Id = 3, Name = "LiveAutoWheel.com", Description = "Random Number Generator"},
            new Website { Id = 4, Name = "SeanWasEre.com", Description = "A Blog of Trivial Things"},
            new Website { Id = 5, Name = "SeanWasEre.com", Description = "A Blog of Trivial Things"},
        };

        // GET api/Websites 
        public IEnumerable Get()
        {
            return websites;
        }

        // GET api/Websites/5 
        public string Get(int id, int amt, string printdata = " ")
        {
            try
            {
                Global.bAppCallRunning = true;
                switch (id)
                {
                    case 1://Init Cash
                        {
                            Log.WriteEJData("CASH ACCEPT STATE CALL");
                            if (amt == 0)
                            {
                                Log.WriteEJData("APP PASS AMOUNT IS ZERO");
                                RDresult = "Please enter valid amount.";
                            }
                            else if ((amt % 10) == 0)
                            {
                                Log.WriteEJData("CASH ACCEPTANCE STARTED");
                                if (Global.objCash.StartAccept(amt))
                                {
                                    RDresult = "Place notes one after another.";//Cash Accepting Mode 
                                }
                                else
                                {
                                    Log.WriteEJData("CASH ACCEPT DEVICE DISCONNECTED");
                                    RDresult = "Device disconnected"; //Lokesh Added[25Sept2018]
                                }
                            }
                            else
                            {
                                Log.WriteEJData("AMOUNT IS NOT IN MULTIPLE OF Rupee 10");
                                RDresult = "Amount should be multiple of 10";
                            }
                        }
                        break;
                    case 2://Receipt Print
                        {
                            if (amt != 1)
                            {
                                Log.WriteEJData("PRINT RECEIPT METHOD CALL");
                                if (Global.RPStatus == "Printer Offline")
                                {
                                    Log.WriteEJData("RECEIPT PRINTER OFFLINE");
                                    Log.WriteLog("On Start Print Printer is Offline", "PrintReceipt");
                                    Log.WriteEJData("TRY FOR OFFLINE RECOVERY");
                                    if (!Global.objRP.InitializeReceipt())
                                    {
                                        Buffer = "";
                                        Global.RPStatus = "Printer Offline";
                                        Log.WriteEJData("RECEIPT PRINTER OFFLINE AFTER RECOVERY");
                                        RDresult = "Printer Offline";
                                    }
                                    else
                                    {
                                        Log.WriteEJData("RECEIPT PRINTER ONLINE AFTER RECOVERY");
                                    }
                                }

                                string iRPStatus = Global.objRP.GetPrinterStatus();

                                if (iRPStatus == "Printer Online" || iRPStatus == "Paper Low")
                                {
                                    printdata = Buffer + printdata;
                                    if (printdata == "")
                                    {
                                        Log.WriteEJData("RECEIPT DATA NOT RECEIVED");
                                        Log.WriteLog("No Data Received to print", "PrintReceipt");
                                        RDresult = "Print Data Not Found";
                                    }
                                    else
                                    {
                                        string[] data = printdata.Split('@');
                                        string datatoprint = "";    //\x1b|1B\r\n";
                                        char c;
                                        for (int i = 0; i < data.Length; i++)
                                        {
                                            if (data[i].Contains("~~~"))
                                            {
                                                c = (char)(Convert.ToInt32(data[i].Substring(data[i].IndexOf("~~~") + 3)));
                                                data[i] = "" + c;
                                            }
                                            datatoprint += data[i];
                                        }
                                        if (Global.objRP.PrintReceipt(datatoprint))        //amt is logo number to print with receipt
                                        {
                                            Log.WriteEJData("RECEIPT PRINTED SUCCESSFULLY");
                                            Buffer = "";
                                            if (Global.objRP.CutRPPaper())
                                                RDresult = "Print OK";
                                            else
                                                RDresult = "Print Successful but failed to cut";
                                        }
                                        else
                                        {
                                            Log.WriteEJData("RECEIPT PRINT FAILURE");
                                            Buffer = "";
                                            return "Printing failed";
                                        }
                                    }
                                }
                                else
                                {
                                    Log.WriteEJData("RECEIPT PRINTER ERROR STATUS: " + Global.RPStatus);
                                    Buffer = "";
                                    RDresult = Global.RPStatus;
                                }
                            }
                            else
                            {
                                if (printdata == "")
                                {
                                    Log.WriteEJData("PRINT DATA NOT PROVIDED AND Amt == 1");
                                    Log.WriteLog("No Data Received to print", "PrintReceipt");
                                    RDresult = "Print Data Not Found";
                                }
                                else
                                {
                                   // Global.objRP.PrintReceipt(printdata);
                                    Buffer += printdata;
                                    RDresult = "MORE";
                                }

                            }
                        }
                        break;
                    case 3: //Get RP Status
                        {
                            Log.WriteEJData("GET RECEIPT PRINTER STATUS METHOD CALL");
                            if (Global.RPStatus == "Printer Offline")
                            {
                                Log.WriteEJData("RECEIPT PRINTER OFFLINE");
                                Log.WriteLog("On Start Printer Statue is Offline", "PrintReceipt");
                                Log.WriteEJData("RECEIPT PRINTER OFFLINE TRY FOR RECOVERY");
                                if (!Global.objRP.InitializeReceipt())
                                {
                                    Global.RPStatus = "Printer Offline";
                                    Log.WriteEJData("RECEIPT PRINTER STILL OFFLINE AFTER RECOVERY");
                                    RDresult = "Printer Offline";
                                }
                                else
                                {
                                    Global.RPStatus = Global.objRP.GetPrinterStatus();
                                    Log.WriteEJData("RECEIPT PRINTER STATUS IS: " + Global.RPStatus);
                                    RDresult = Global.RPStatus;
                                }
                            }
                            else
                            {
                                Global.RPStatus = Global.objRP.GetPrinterStatus();
                                Log.WriteEJData("RECEIPT PRINTER STATUS IS: " + Global.RPStatus);
                                RDresult = Global.RPStatus;
                            }
                        }
                        break;
                    case 4: //Get Note Details
                        {
                            Log.WriteEJData("GET NOTE DETAIL METHOD CALL");
                            string RetCount = Global.NoteDenomination[0].ToString() + "#";
                            RetCount += Global.NoteDenomination[1].ToString() + "#";
                            RetCount += Global.NoteDenomination[2].ToString() + "#";
                            RetCount += Global.NoteDenomination[3].ToString() + "#";
                            RetCount += Global.NoteDenomination[4].ToString() + "#";
                            RetCount += Global.NoteDenomination[5].ToString() + "#";
                            RetCount += Global.NoteDenomination[6].ToString();
                            Log.WriteEJData("ACCEPTED NOTE DETAILS ARE : " + RetCount);
                            RDresult = RetCount;
                        }
                        break;
                    case 5: //Stop Note Accept
                        {
                            Log.WriteEJData("STOP NOTE ACCEPT METHOD CALL");
                            Global.objCash.StopAccept();
                            string RetCount = Global.NoteDenomination[0].ToString() + "#";
                            RetCount += Global.NoteDenomination[1].ToString() + "#";
                            RetCount += Global.NoteDenomination[2].ToString() + "#";
                            RetCount += Global.NoteDenomination[3].ToString() + "#";
                            RetCount += Global.NoteDenomination[4].ToString() + "#";
                            RetCount += Global.NoteDenomination[5].ToString() + "#";
                            RetCount += Global.NoteDenomination[6].ToString();
                            Log.WriteEJData("ACCEPTED NOTE DETAILS ARE: " + RetCount);
                            RDresult = RetCount;
                        }
                        break;
                    case 6:
                        {
                            Log.WriteEJData("GET TRANSACTION CAMERA STATUS METHOD CALL");
                            Global.objCamera.SendMessageToCameraClient("Status");
                            Thread.Sleep(1000);
                            Log.WriteEJData("TRANSACTION CAMERA STATUS IS: " + Global.objCamera.getStatus());
                            RDresult = Global.objCamera.getStatus();
                        }
                        break;
                    case 7:
                        {
                            Log.WriteEJData("TAKE PICTURE METHOD CALL");
                            Global.objCamera.SendMessageToCameraClient("0#Lipi#" + printdata);
                            Global.IsCameraTakePicture = false;
                            Log.WriteEJData("STAMP DATA IS: " + printdata);
                            Int32 iCount = 0;
                            do
                            {
                                iCount++;
                                if (Global.IsCameraTakePicture)
                                    break;
                                else
                                    Thread.Sleep(1000);

                            } while (iCount <= 5);
                            Log.WriteEJData("PICTURE CAPTURED WITH STAMP DATA");
                            string strBase64 = Global.objCamera.Img();
                            RDresult = strBase64;
                        }
                        break;
                    case 8:
                        {
                            if (_pipeClient == null)
                                _pipeClient = new PipeClient();

                            Log.WriteEJData("SCAN DOCUMENT METHOD CALL");

                            if (!Directory.Exists("C:\\DocImages\\"))
                                Directory.CreateDirectory("C:\\DocImages\\");

                            string strDocPath = "C:\\DocImages\\" + printdata + ".jpg";

                            string strReturn = _pipeClient.Send("2" + "#" + strDocPath, "DocScannerRequestResponse", 1000);
                            Log.WriteLog("Start Scan - " + strReturn, "Scanner");
                            Log.WriteEJData("DOCUMENT SCANNING STARTED");
                            if (strReturn == "success")
                            {
                                string base64String = null;
                                using (System.Drawing.Image image = System.Drawing.Image.FromFile(strDocPath))
                                {
                                    using (MemoryStream m = new MemoryStream())
                                    {
                                        image.Save(m, image.RawFormat);
                                        byte[] imageBytes = m.ToArray();
                                        base64String = Convert.ToBase64String(imageBytes);
                                        Log.WriteLog("Image Path - " + strDocPath, "Scanner");
                                        Log.WriteEJData("DOCUMENT SCAN SUCCESSFULLY");
                                        RDresult = base64String;
                                    }
                                }
                            }
                            else
                            {
                                Log.WriteEJData("DOCUMENT SCAN FAILURE");
                                strReturn = "Scan Failed";
                                RDresult = "Scan Failed";
                            }

                        }
                        break;
                    case 9:
                        {
                            Log.WriteEJData("SCANNER START PREVIEW METHOD CALL");
                            if (_pipeClient == null)
                                _pipeClient = new PipeClient();

                            string strData = _pipeClient.Send("1", "DocScannerRequestResponse", 1000);
                            Log.WriteLog("Start Preview - " + strData, "Scanner");
                            Log.WriteEJData("DOCUMENT SCANNER PREVIEW STARTED");
                            RDresult = strData;
                        }
                        break;
                    case 10:
                        {
                            Log.WriteEJData("SCANNER STOP PREVIEW METHOD CALL");
                            if (_pipeClient == null)
                                _pipeClient = new PipeClient();

                            string strData = _pipeClient.Send("3", "DocScannerRequestResponse", 1000);
                            Log.WriteLog("Stop Preview - " + strData, "Scanner");
                            Log.WriteEJData("DOCUMENT SCANNER PREVIEW STOPPED");
                            RDresult = strData;
                        }
                        break;
                    case 11:
                        {
                            Log.WriteEJData("READ BARCODE METHOD CALL");
                            Global.objBarcode.DataReaded = "No Data Found";
                            int status = 0;
                            if (Global.BarcodeStatus == "Barcode offline")
                            {
                                if (!Global.objBarcode.Initialize())
                                {
                                    RDresult = "Device disconnected";
                                    status = 1;
                                }
                            }
                            if (status == 0)
                            {
                                int iTimeOut = 0;
                                while (true)
                                {
                                    iTimeOut++;

                                    if (Global.objBarcode.Readbarcode() != 0)
                                    {
                                        Log.WriteEJData("BARCODE DEVICE DISCONNECTED");
                                        Global.objBarcode.DataReaded = "Device disconnected";
                                        Global.BarcodeStatus = "Barcode offline";
                                        break;
                                    }

                                    if (iTimeOut == 20)
                                        break;

                                    Thread.Sleep(1000);

                                    if (Global.objBarcode.DataReaded != "No Data Found")
                                        break;
                                }

                                Global.objBarcode.Stopbarcode();
                                Log.WriteEJData("BARCODE READED SUCCESSFULLY and DATA IS: " + Global.objBarcode.DataReaded);
                                RDresult = Global.objBarcode.DataReaded;
                            }
                        }
                        break;
                    case 12:
                        {
                            Log.WriteEJData("GET KIOSK ID METHOD CALL");
                            string mac = "";
                            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                            {

                                if (nic.OperationalStatus == OperationalStatus.Up && (!nic.Description.Contains("Virtual") && !nic.Description.Contains("Pseudo")))
                                {
                                    if (nic.GetPhysicalAddress().ToString() != "")
                                    {
                                        mac = nic.GetPhysicalAddress().ToString();
                                    }
                                }
                            }
                            Log.WriteEJData("KIOSK ID IS: " + mac);
                            RDresult = mac;

                        }
                        break;
                    case 13: //MifareReader
                        {

                            MifareReader obj = new MifareReader();
                            obj.Connect();
                            obj.SelectClick();
                            obj.Login();

                            RDresult = obj.ReadBlock();
                            if (RDresult == "")
                                RDresult = "No Data Found";
                        }
                        break;
                    case 14:
                        return "success";
                        break;
                }
                Global.bAppCallRunning = false;
                return RDresult;
            }
            catch (Exception e)
            {
                Buffer = "";
                return e.Message;
            }
        }


        // POST api/values 
        public void Post([FromBody]string value)
        {
            Console.WriteLine("Post method called with value = " + value);
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value)
        {
            Console.WriteLine("Put method called with value = " + value);
        }

        // DELETE api/values/5 
        public void Delete(int id)
        {
            Console.WriteLine("Delete method called with id = " + id);
        }
    }
}
