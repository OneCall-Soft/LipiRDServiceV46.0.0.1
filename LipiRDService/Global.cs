using LipiRDLogin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace LipiRDService
{
    static class Global
    {
        public static bool bStopNoteThread;
        public static Int32 iTotalAmount = 100;
        public static Int32 iStan = 0;
        public static Int32 []NoteDenomination;
        public static Int32 iTimeReset = 0;
        public static Int32 iTimeResetHold = 0;
        public static ReceiptPrinter objRP = null;
        public static CashAcceptor objCash = null;
        public static Camera objCamera = null;        
        public static Barcode objBarcode = null;
        public static IniFile objIni = null;

        public static string RPStatus;
        public static string CashStatus;
        public static bool IsCashAcceptWorking = true;
        public static string BarcodeStatus;
        public static bool IsCameraTakePicture = false;
        public static bool bEzetapFound = false;
        public static bool bTouchScreenFound = false;
        public static bool bRFIDFound = false;
        public static bool bStmtPrinterFound = false;
        public static bool bFingerScannerFound = false;        
        public static bool bCameraFound = false;
        public static bool bVCCameraFound = false;
        public static bool bReceiptPrinter = false;
        public static bool bTVFound = false;
        public static bool bBarcodeFound = false;
        public static bool bKeyPadMouseFound = false;
        public static bool bKeyStatmentPrinterFound = false;
        public static bool bDocScannerFound = false;
        public static bool bAppCallRunning = false;
        public static SerialComm objSerial = null;
        public static string PrinterType;
        public static string CashAcceptorType;

        public static string DevName_Barcode;
        public static string DevName_ReceiptPrinter;
        public static string DevName_FingerScanner;
        public static string DevName_Camera;
        public static string DevName_VCCamera;
        public static string DevName_DocScanner;
        public static string DevName_StatementPrinter;
        public static string DevName_RFID;
        public static string DevName_SignageTV;
        public static string DevName_LaserPrinter;
        public static string DevName_CardReader;
        public static string DevName_EzeTap;
        public static string DevName_KeyPadWithMouse;
        public static string DevName_Touch;

        public static string DIndex_CashAcceptor;
        public static string DIndex_Barcode;
        public static string DIndex_ReceiptPrinter;
        public static string DIndex_FingerScanner;
        public static string DIndex_Camera;
        public static string DIndex_RFID;
        public static string DIndex_VCCamera;
        public static string DIndex_DocScanner;
        public static string DIndex_StatementPrinter;
        public static string DIndex_SignageTV;
        public static string DIndex_LaserPrinter;
        public static string DIndex_EzeTap;
        public static string DIndex_KeyPadWithMouse;
        public static string DIndex_Touch;
    }
}
