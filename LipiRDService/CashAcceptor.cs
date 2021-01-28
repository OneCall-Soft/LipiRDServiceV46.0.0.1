using CCNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LipiRDService
{
    class CashAcceptor
    {
        CCNET.Iccnet _B2bDevice;
        CNV10 NV10;

        Thread acceptorThread = null;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        public static string strStatus;

        public static bool Note10, Note20, Note50, Note100, Note200, Note500, Note2000;
        public static int amtrec=0;   

        public void InitializeAcceptor()
        {
            try
            {
                Log.WriteLog("**********************Initialize Cash Acceptor Device********************","CashAcceptor");
                Global.bStopNoteThread = false;
                Global.CashAcceptorType = System.Configuration.ConfigurationManager.AppSettings["CashType"].ToString();
                Log.WriteLog("Cash Acceptor Type - " + Global.CashAcceptorType, "CashAcceptor");

                string PortName = System.Configuration.ConfigurationManager.AppSettings["CashPort"].ToString();
                Log.WriteLog("Cash Acceptor Port - " + PortName, "CashAcceptor");

                Note10 = Note20 = Note50 = Note100 = Note200 = Note500 = Note2000 = false;
                Note10 = (System.Configuration.ConfigurationManager.AppSettings["Note10"].ToString() == "1") ? true : false;
                Note20 = (System.Configuration.ConfigurationManager.AppSettings["Note20"].ToString() == "1") ? true : false;
                Note50 = (System.Configuration.ConfigurationManager.AppSettings["Note50"].ToString() == "1") ? true : false;
                Note100 = (System.Configuration.ConfigurationManager.AppSettings["Note100"].ToString() == "1") ? true : false;
                Note200 = (System.Configuration.ConfigurationManager.AppSettings["Note200"].ToString() == "1") ? true : false;
                Note500 = (System.Configuration.ConfigurationManager.AppSettings["Note500"].ToString() == "1") ? true : false;
                Note2000 = (System.Configuration.ConfigurationManager.AppSettings["Note2000"].ToString() == "1") ? true : false;

                if (Global.CashAcceptorType.ToLower() == "nv10")
                {
                    NV10 = new CNV10();
                    NV10.CommandStructure.ComPort = PortName;
                    NV10.CommandStructure.SSPAddress = Byte.Parse("0");

                    // close com port in case it was open
                    NV10.SSPComms.CloseComPort();

                    // turn encryption off for first stage
                    NV10.CommandStructure.EncryptionStatus = false;

                    if (NV10.OpenComPort())
                    {
                        NV10.CommandStructure.EncryptionStatus = true;

                        // if the key negotiation is successful then set the rest up
                        NV10.NegotiateKeys();
                        NV10.CommandStructure.EncryptionStatus = true;
                        byte maxPVersion = FindMaxProtocolVersion();
                        if (maxPVersion >= 6)
                        {
                            NV10.SetProtocolVersion(maxPVersion);
                        }

                        // get info from the validator and store useful vars
                        NV10.ValidatorSetupRequest();

                        // inhibits, this sets which channels can receive notes
                        NV10.SetInhibits();

                        // value reporting, set whether the validator reports channel or coin value in 
                        // subsequent requests
                        NV10.SetValueReportingType(false);

                        // check for notes already in the float on startup
                        NV10.CheckForStoredNotes();
                    }
                    else
                    {
                        Global.CashStatus = "Cash offline";
                    }
                }
                else //CashCode Device
                {   

                    _B2bDevice = new Iccnet(PortName, Device.Bill_Validator);
                    Answer ans = _B2bDevice.RunCommand(CCNETCommand.RESET);
                    if (ans.ReceivedData != null)
                    {                        
                        _B2bDevice.RunCommand(CCNET.CCNETCommand.Poll);
                        _B2bDevice.RunCommand(CCNET.CCNETCommand.GET_STATUS);
                        _B2bDevice.RunCommand(CCNET.CCNETCommand.IDENTIFICATION);
                        _B2bDevice.RunCommand(CCNET.CCNETCommand.SET_SECURITY);
                    }
                    else
                    {
                        Global.CashStatus = "Cash offline";
                    }
                }

                acceptorThread = new Thread(StartNoteAcceptThread);
                acceptorThread.Start();                

                strStatus = "Device accept started";
                Log.WriteLog("Cash Acceptor Initialization success","CashAcceptor");
            }
            catch(Exception ex)
            {
                Log.WriteLog("Excp in InitializeAcceptor - " + ex.StackTrace.ToString(),"CashAcceptor");
            }
        }
        
        public bool StartAccept(int Amount)
        {
            try
            {
                //Lokesh Added[26Set2016]
                // When RD service initalize and cash acceptor is not connected then this function will be called. 
                Global.bStopNoteThread = false;
                if (Global.CashStatus == "Cash offline" || !Global.IsCashAcceptWorking)
                {   
                    Log.WriteLog("Cash Device is offline on StartAccept()", "CashAcceptor");

                    if (Global.CashAcceptorType.ToLower() == "cashcode")
                    {
                        Answer ans = _B2bDevice.RunCommand(CCNET.CCNETCommand.RESET);
                        if (ans.ReceivedData != null)
                        {
                            Log.WriteLog("Init Cash Acceptor successfully", "CashAcceptor");
                            Global.IsCashAcceptWorking = true;
                            Global.CashStatus = "Cash Online";
                        }
                        else
                        {
                            Log.WriteLog("Init Cash Acceptor failed", "CashAcceptor");
                            return false;
                        }
                    }
                    else
                    {
                        NV10.Reset();

                        // close com port in case it was open
                        NV10.SSPComms.CloseComPort();

                        // turn encryption off for first stage
                        NV10.CommandStructure.EncryptionStatus = false;
                         
                        if (NV10.OpenComPort())
                        {
                            // if the key negotiation is successful then set the rest up
                            NV10.NegotiateKeys();

                            NV10.CommandStructure.EncryptionStatus = true;

                            byte maxPVersion = FindMaxProtocolVersion();
                            if (maxPVersion >= 6)
                            {
                                NV10.SetProtocolVersion(maxPVersion);
                            }

                            // get info from the validator and store useful vars
                            NV10.ValidatorSetupRequest();

                            // inhibits, this sets which channels can receive notes
                            NV10.SetInhibits();

                            // value reporting, set whether the validator reports channel or coin value in 
                            // subsequent requests
                            NV10.SetValueReportingType(false);

                            // check for notes already in the float on startup
                            NV10.CheckForStoredNotes();                            
                        }                       
                        else
                        {
                            Log.WriteLog("Init Cash Acceptor failed", "CashAcceptor");
                            return false;
                        }
                    }
                    
                }

                if (Global.CashAcceptorType.ToLower() == "cashcode")
                {
                    // During transaction If power down happen then this function will be called.
                    Answer Ans4 = _B2bDevice.RunCommand(CCNET.CCNETCommand.Poll);
                    if (Ans4.Message == "POWERUP")
                    {
                        Log.WriteLog("Cash device is powerup to reset", "CashAcceptor");
                        _B2bDevice.RunCommand(CCNET.CCNETCommand.RESET);
                    }
                    //End

                    Answer Ans = _B2bDevice.RunCommand(CCNET.CCNETCommand.GET_STATUS); //Lokesh Added[25Sept2018]
                    if (Ans.ReceivedData != null)
                    {
                        Answer Ans1 = _B2bDevice.RunCommand(CCNETCommand.ENABLE_BILL_TYPES, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        amtrec = 0;
                        Log.WriteLog("---Transaction start accepting cash---", "CashAcceptor");
                        Log.WriteLog("Total Amount to receive: Rs." + Amount, "CashAcceptor");
                        Array.Clear(Global.NoteDenomination, 0, 10);
                        Answer Ans2 = _B2bDevice.RunCommand(CCNETCommand.ENABLE_BILL_TYPES, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
                        Global.iTotalAmount = Amount;
                        Global.bStopNoteThread = true;
                        return true;
                    }
                    else
                    {
                        Global.IsCashAcceptWorking = false;
                        Log.WriteLog("StartAccept() - Device is not working", "CashAcceptor");
                        return false;
                    }
                }
                else
                {
                    amtrec = 0;
                    Log.WriteLog("---Transaction start accepting cash---", "CashAcceptor");
                    Log.WriteLog("Total Amount to receive: Rs." + Amount, "CashAcceptor");
                    Global.iTotalAmount = Amount;
                    Global.bStopNoteThread = true;
                    return true;
                }
            }
            catch(Exception ex)
            {
                Log.WriteLog("Excp in StartAccept() - " + ex.Message);
                return false;
            }
        }

        public void StopAccept()
        {
            try
            {
                Log.WriteLog("Stop Accepting Cash","CashAcceptor");

                if (Global.CashAcceptorType.ToLower() == "cashcode")
                    _B2bDevice.RunCommand(CCNETCommand.ENABLE_BILL_TYPES, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                else
                    NV10.DisableValidator();

                Global.bStopNoteThread = false;
            }
            catch(Exception ex)
            {
                Log.WriteLog("Excp in StopAccept() - " + ex.Message,"CashAcceptor");
            }
        }

        public void Hold()
        {
            _B2bDevice.RunCommand(CCNET.CCNETCommand.HOLD);
        }

        public void Stack()
        {
            _B2bDevice.RunCommand(CCNET.CCNETCommand.STACK);
        }

        public void Return()
        {
            _B2bDevice.RunCommand(CCNET.CCNETCommand.RETURN);
        }

        public string DeviceStatus()  //For RMS
        {
            Answer Ans = _B2bDevice.RunCommand(CCNET.CCNETCommand.GET_STATUS); //Lokesh Added[28Nov2018]
            if (Ans.ReceivedData != null)
                return "Connected";
            else
                return "Disconnected";
        }

        public void StartNoteAcceptThread()
        {

            while (true)
            {
                if (Global.bStopNoteThread == true)
                {
                    if (Global.CashAcceptorType.ToLower() == "cashcode")
                    {
                        Answer Ans = _B2bDevice.RunCommand(CCNET.CCNETCommand.Poll);

                        if (Ans.ReceivedData != null)
                        {
                            switch ((Bill_Validator_Sataus)Ans.Additional_Data)
                            //switch (Ans.ReceivedData[3])
                            {
                                case Bill_Validator_Sataus.Escrow_position:   //0x80;
                                    {
                                        switch (Ans.ReceivedData[4])
                                        {
                                            case 1: // 10
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 10 Escrowed", "CashAcceptor");
                                                    if (Note10)
                                                    {
                                                        if (Global.iTotalAmount >= 10)
                                                        {
                                                            Log.WriteLog("Rs 10 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 10 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 10 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                            case 2: // 20
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 20 Escrowed", "CashAcceptor");
                                                    if (Note20)
                                                    {
                                                        if (Global.iTotalAmount >= 20)
                                                        {
                                                            Log.WriteLog("Rs 20 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 20 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 20 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                            case 3: // 50
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 50 Escrowed", "CashAcceptor");
                                                    if (Note50)
                                                    {
                                                        if (Global.iTotalAmount >= 50)
                                                        {
                                                            Log.WriteLog("Rs 50 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 50 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 50 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                            case 4: // 100
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 100 Escrowed", "CashAcceptor");
                                                    if (Note100)
                                                    {
                                                        if (Global.iTotalAmount >= 100)
                                                        {
                                                            Log.WriteLog("Rs 100 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 100 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 100 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                            case 5: // 200
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 200 Escrowed", "CashAcceptor");
                                                    if (Note200)
                                                    {
                                                        if (Global.iTotalAmount >= 200)
                                                        {
                                                            Log.WriteLog("Rs 200 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 200 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 200 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                            case 6: // 500
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 500 Escrowed", "CashAcceptor");
                                                    if (Note500)
                                                    {
                                                        if (Global.iTotalAmount >= 500)
                                                        {
                                                            Log.WriteLog("Rs 500 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 500 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 500 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                            case 7: // 2000
                                                {
                                                    Hold();
                                                    Log.WriteLog("Rs 2000 Escrowed", "CashAcceptor");
                                                    if (Note2000)
                                                    {
                                                        if (Global.iTotalAmount >= 2000)
                                                        {
                                                            Log.WriteLog("Rs 2000 Stack Fire", "CashAcceptor");
                                                            Stack();

                                                        }
                                                        else
                                                        {
                                                            Log.WriteLog("Rs 2000 Return Fire", "CashAcceptor");
                                                            Return();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLog("Rs 2000 Return as not accepted", "CashAcceptor");
                                                        Return();
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case Bill_Validator_Sataus.Bill_stacked:  //0x81;
                                    {
                                        switch (Ans.ReceivedData[4])
                                        {
                                            case 1: // 10
                                                {
                                                    Global.NoteDenomination[0]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 10;
                                                    amtrec += 10;
                                                    Log.WriteLog("Rs 10 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                            case 2: // 20
                                                {
                                                    Global.NoteDenomination[1]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 20;
                                                    amtrec += 20;
                                                    Log.WriteLog("Rs 20 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                            case 3: // 50
                                                {
                                                    Global.NoteDenomination[2]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 50;
                                                    amtrec += 50;
                                                    Log.WriteLog("Rs 50 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                            case 4: // 100
                                                {
                                                    Global.NoteDenomination[3]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 100;
                                                    amtrec += 100;
                                                    Log.WriteLog("Rs 100 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                            case 5: // 200
                                                {
                                                    Global.NoteDenomination[4]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 200;
                                                    amtrec += 200;
                                                    Log.WriteLog("Rs 200 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                            case 6: // 500
                                                {
                                                    Global.NoteDenomination[5]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 500;
                                                    amtrec += 500;
                                                    Log.WriteLog("Rs 500 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                            case 7: // 2000
                                                {
                                                    Global.NoteDenomination[6]++;
                                                    Global.iTotalAmount = Global.iTotalAmount - 2000;
                                                    amtrec += 2000;
                                                    Log.WriteLog("Rs 2000 Stacked Remaining Amount is - ₹" + Global.iTotalAmount, "CashAcceptor");
                                                }
                                                break;
                                        }
                                    }
                                    break;

                                case Bill_Validator_Sataus.Suspected_bill_detected:
                                    Log.WriteLog("Note Suspected bill detected", "CashAcceptor");
                                    //label2.Text = String.Format("BILL #: {0} RETURNED ", Ans.ReceivedData[4].ToString());
                                    break;

                                case Bill_Validator_Sataus.Stacking:
                                    //Log.WriteLog("Note Stacking", "CashAcceptor");
                                    //label2.Text = String.Format("BILL #: {0} RETURNED ", Ans.ReceivedData[4].ToString());
                                    break;

                                case Bill_Validator_Sataus.Bill_returned:
                                    Log.WriteLog("Note Return", "CashAcceptor");
                                    //label2.Text = String.Format("BILL #: {0} RETURNED ", Ans.ReceivedData[4].ToString());
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        NV10.EnableValidator();
                        Log.WriteLog("---Start Cash Accept---");                        
                        NV10.DoPoll();
                    }

                    if (Global.iTotalAmount == 0)
                    {
                        Global.bStopNoteThread = false;
                        Log.WriteLog("Complete Ammount Received","CashAcceptor");
                    }
                    Thread.Sleep(300);
                }
                else
                {
                    Thread.Sleep(1000);
                    if (Global.CashAcceptorType.ToLower() == "cashcode")
                        Return();
                    else
                        NV10.ReturnNote();
                }
            }
        }

        // This function finds the maximum protocol version that a validator supports. To do this
        // it attempts to set a protocol version starting at 6 in this case, and then increments the
        // version until error 0xF8 is returned from the validator which indicates that it has failed
        // to set it. The function then returns the version number one less than the failed version.
        private byte FindMaxProtocolVersion()
        {
            // not dealing with protocol under level 6
            // attempt to set in validator
            byte b = 0x06;
            while (true)
            {
                NV10.SetProtocolVersion(b);
                if (NV10.CommandStructure.ResponseData[0] == CCommands.SSP_RESPONSE_FAIL)
                    return --b;
                b++;
                if (b > 20) return 0x06; // return lowest if p version runs too high
            }
        }
    }    
}
