using CoreScanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;


namespace LipiRDService
{
    class Barcode
    {
 
        CoreScanner.CCoreScannerClass m_pCoreScanner=new CoreScanner.CCoreScannerClass();
        public string DataReaded;
        public string scannerId;      

        public bool Initialize()
        {
            try
            {
                short[] m_arScannerTypes= {1,0,0,0,0,0,0,0,0,0,0};
                int status = 1;
                string inXml = "<inArgs><cmdArgs><arg-int>6</arg-int><arg-int>1,2,4,8,16,32</arg-int></cmdArgs></inArgs>";
                string outXml = "";
                string outXML = "";
                short numOfScanners = 0;
                scannerId = "";
                int[] scannerIdList = new int[255];
                try
                {
                    m_pCoreScanner = new CoreScanner.CCoreScannerClass();
                }
                catch (Exception ex)
                {
                    Log.WriteLog("Exception while initializing BarcodeScanner (CCoreScannerClass ) - " + ex.Message, "Barcode");
                    return false;
                }

                m_pCoreScanner.BarcodeEvent += new CoreScanner._ICoreScannerEvents_BarcodeEventEventHandler(OnBarcodeEvent);
                m_pCoreScanner.Open(0, m_arScannerTypes, 1, out status);
                m_pCoreScanner.ExecCommand(1001, ref inXml, out outXml, out status);
                m_pCoreScanner.GetScanners(out numOfScanners, scannerIdList, out outXML, out status);
                int a = outXML.IndexOf("scannerID")+10;
                int b = outXML.IndexOf("/scannerID")-1;
                scannerId = outXML.Substring(a,b-a);
                inXml = "<inArgs><cmdArgs><arg-int>3</arg-int><arg-int>1,0,2,</arg-int> </cmdArgs></inArgs>";
                m_pCoreScanner.ExecCommand(1005, ref inXml, out outXml, out status);
                Global.BarcodeStatus = "Barcode online";
                return true;
            }
            catch (Exception ex)
            {
                Global.BarcodeStatus = "Barcode offline";
                Log.WriteLog("Exception while initializing BarcodeScanner - " + ex.Message,"Barcode");
                return false;
            }
        }      

        public void OnBarcodeEvent(short eventType, ref string scanData)
        {
            try
            {
                string tmpScanData = scanData;
                ShowBarcodeLabel(tmpScanData);
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception in OnBarcodeEvent - " + ex.Message, "Barcode");
            }
        }

        private void ShowBarcodeLabel(string strXml)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Initial XML" + strXml);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strXml);

                string strData = String.Empty;
                string barcode = xmlDoc.DocumentElement.GetElementsByTagName("datalabel").Item(0).InnerText;
                string symbology = xmlDoc.DocumentElement.GetElementsByTagName("datatype").Item(0).InnerText;
                string[] numbers = barcode.Split(' ');

                foreach (string number in numbers)
                {
                    if (String.IsNullOrEmpty(number))
                    {
                        break;
                    }

                    strData += ((char)Convert.ToInt32(number, 16)).ToString();
                }
                DataReaded = strData;
            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception in ShowBarcodeLabel() - " + ex.Message, "Barcode");
            }

        }

        public int Readbarcode()
        {
            try
            {
                int status = 1;                
                string inXml = "<inArgs><scannerID>" + scannerId + "</scannerID></inArgs>";
                int opCode = 2011;
                string outXml = "";
                m_pCoreScanner.ExecCommand(opCode, ref inXml, out outXml, out status);
                Log.WriteLog("Start Scanning", "Barcode");
                return status;
            }
            catch(Exception ex)
            {
                Log.WriteLog("Exception in Readbarcode() - " + ex.Message, "Barcode");
                return -1;
            }
        }

        public void Stopbarcode()
        {
            try
            {
                string inXml = "<inArgs><scannerID>" + scannerId + "</scannerID></inArgs>";
                int opCode = 2012;
                string outXml = "";
                int status = 1;
                m_pCoreScanner.ExecCommand(opCode, ref inXml, out outXml, out status);
                Log.WriteLog("Stop Scanning", "Barcode");
            }
            catch(Exception ex)
            {
                Log.WriteLog("Exception in Stopbarcode() - " + ex.Message, "Barcode");
                DataReaded = "No Data Found";
            }
        }    

    }
}
