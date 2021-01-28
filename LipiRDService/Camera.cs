using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using Lipi.Communication.Scs.Server;
using Lipi.Communication.Scs.Communication.EndPoints.Tcp;
using Lipi.Communication.Scs.Communication.Messages;
using System.Diagnostics;
using System.IO;

namespace LipiRDService
{
    class Camera
    {
        public static string CameraStatus="Camera Disconnected";
        public static string ImgPath;
        public Lipi.Communication.Scs.Server.IScsServer objServerForCamera;
        string base64image;
        public void ConnectWithCameraClient()
        {
            try
            {
                ImgPath = "";
                //create server instance
                if (objServerForCamera == null)
                    objServerForCamera = ScsServerFactory.CreateServer(new ScsTcpEndPoint(8607));

                //if created then map its events
                if (objServerForCamera == null)
                    Log.WriteLog("Could not start port 8607 for listening transaction messages from KPROC", "Camera");
                else
                {
                    objServerForCamera.ClientConnected += Server_ClientConnectedCamera;
                    objServerForCamera.ClientDisconnected += Server_ClientDisconnectedCamera;
                }

                objServerForCamera.Start();
                Log.WriteLog("Port 8607 opened for listening camera transaction messages", "Camera");

                //bool isCameraAppRunning = false;
                //foreach (Process process in Process.GetProcesses())
                //{
                //    if (process.ProcessName.Contains("LipiCamMonitorApp"))
                //    {
                //        isCameraAppRunning = true;
                //        Log.WriteLog("Lipi Camera Monitor Application already running.", "Camera");
                //    }
                //}

                //if (!isCameraAppRunning)
                //{
                //    if (File.Exists(System.Configuration.ConfigurationManager.AppSettings["CamAppPath"].ToString()))
                //    {
                //        Process p = new Process();
                //        p.StartInfo.UseShellExecute = false;
                //        p.StartInfo.FileName = System.Configuration.ConfigurationManager.AppSettings["CamAppPath"].ToString();
                //        p.StartInfo.RedirectStandardOutput = false;
                //        p.StartInfo.CreateNoWindow = false;                      
                //        p.Start();

                //        //Process.Start(System.Configuration.ConfigurationManager.AppSettings["CamAppPath"].ToString());
                //        Log.WriteLog("Lipi Camera Application found and started", "Camera");
                //    }
                //    else
                //    {
                //        Log.WriteLog("Lipi Camera Application not found - " + System.Configuration.ConfigurationManager.AppSettings["CamAppPath"].ToString(), "Camera");
                //    }
                //}

            }
            catch (Exception ex)
            {
                Log.WriteLog("Exception in creating camera client - " + ex.Message, "Camera");
            }
            
        }

        /// <summary
        /// AMK 18Jan2016 :: Added by and on
        /// Camera_Client_Connected:: when KPROC is connected with Camera App.
        /// </summary>           
        public void Server_ClientConnectedCamera(object sender, ServerClientEventArgs e)
        {
            try
            {
                
                e.Client.MessageReceived += KPROCClient_MessageReceived_Camera;
                Log.WriteLog("KPROC Client Connected " + ((Lipi.Communication.Scs.Communication.EndPoints.Tcp.ScsTcpEndPoint)(e.Client.RemoteEndPoint)).IpAddress.ToString(),"Camera");
                SendMessageToCameraClient("STATUS#");//Get Status of Camera Device/Application
                //SendMessageToCameraClient(
                //                            "CapTxn" + "#" +
                //                           "CHQ ACCEPT" + "#" +
                //                           "ABC1234" + "#" +
                //                           "1234567890123" + "#" +
                //                           "123456" + "#" +
                //                           "50000" + "#" +
                //                           "12:12:12");
                //SendMessageToCameraClient("CapOther" + "#" + 
                //       "1" + "#" +
                //       "Go To Maintenance Mode");
                //SendMessageToCameraClient(CameraMsgType.GetStatus, "CapOP#");//Capture Image in Operator Mode

            }
            catch (Exception excp)
            {
                Log.WriteLog("Excp in Server_ClientConnected- " + excp.Message,"Camera");
            }
        }

        /// <summary
        /// AMK 18Jan2016 :: Added by and on
        /// Camera_Client_Disconnected:: when KPROC is disconnected with Camera App.
        /// </summary>           
        public void Server_ClientDisconnectedCamera(object sender, ServerClientEventArgs e)
        {
            objServerForCamera.Stop();
            Log.WriteLog("Camera_Client Disconnected", "Camera");
            Thread.Sleep(500);
            objServerForCamera.Start();
        }


        /// <summary
        /// AMK 18Jan2016 :: Added by and on
        /// Camera_Client_MessageReceived:: when KPROC is received msg from Camera App.
        /// </summary>           
        public void KPROCClient_MessageReceived_Camera(object sender, MessageEventArgs e)
        {
            try
            {
                Log.WriteLog("Camera_Client_MessageReceived","Camera");
                var message = e.Message as ScsRawDataMessage;
                if (message == null)
                {
                    return;
                }

                string strMessageType = Encoding.Default.GetString(message.MessageData);

                if(strMessageType.Split('#')[0] != "D")  //Dont write image data in log file
                    Log.WriteLog("Camera message received- " + strMessageType,"Camera");

                switch (strMessageType.Split('#')[0])
                {
                    case "A"://Camera Application & Device are ok
                        {
                            CameraStatus = "Camera Application and Device are ok";
                            //GlobalMembers.objRMSClient.UpdateHealth(HealthType.Camera, "0", true);
                        }
                        break;
                    case "P"://Camera Application Closed or Video loss occured or Device in Error or Device Disconnected
                        {
                            //GlobalMembers.objRMSClient.UpdateHealth(HealthType.Camera, "1", true);
                            switch (strMessageType.Split('#')[1])
                            {
                                case "1"://Camera Device Disconnected
                                    {
                                        CameraStatus = "Camera Device Disconnected";
                                        Log.WriteLog("Camera Device Disconnected","Camera");
                                    }
                                    break;
                                case "2"://Camera Application Closed
                                    {
                                        CameraStatus = "Camera Application Closed";
                                        Log.WriteLog("Camera Application Closed","Camera");
                                    }
                                    break;
                                case "3"://Video Loss Occured
                                    {
                                        CameraStatus = "Video Loss Occured";
                                        Log.WriteLog("Video Loss Occured","Camera");
                                    }
                                    break;
                                case "4"://Device in error/problematic
                                    {
                                        CameraStatus = "Device in error";
                                        Log.WriteLog("Device in Error","Camera");
                                    }
                                    break;
                                default://UnKnown Error
                                    {
                                        CameraStatus = "Unknown Error";
                                        Log.WriteLog("Unknown Error","Camera");
                                    }
                                    break;
                            }
                        }
                        break;
                    case "C"://mean capture image path in OP mode in Camera Test....  
                        {
                            ImgPath = strMessageType.Split('#')[1];
                        }
                        break;
                    case "D":
                        {
                            Log.WriteLog("Camera message received success" , "Camera");
                            base64image = strMessageType.Split('#')[1];
                            Global.IsCameraTakePicture = true;
                        }
                        break;
                }
            }
            catch (Exception ex1)
            {
                Log.WriteLog("Camera_Client_MessageReceived " + ex1.Message.ToString(),"Camera");
            }
        }

        /// <summary
        /// AMK 18Jan2016 
        /// SendMessageToCameraClient:: send message to Camera Client
        /// 
        /// for TXN-> CapTxn # Event # terminalID # AccNo # ChequeNo # ChkAmt # ChkDate
        /// for Other -> CapOther# for Lipi (1 for lipi, 0 for bank/txn) # Msg
        /// 
        /// 
        /// </summary>        
        public void SendMessageToCameraClient(string strMessage)
        {
            try
            {
                base64image = "";

                List<IScsServerClient> client_list = objServerForCamera.Clients.GetAllItems();

                if (objServerForCamera != null)
                {
                    if (objServerForCamera.Clients.Count > 0)
                    {
                        client_list[0].SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes(strMessage)));

                        Log.WriteLog("Message sent to Camera Application - " + strMessage,"Camera");
                    }
                    else
                    {
                        Log.WriteLog("Message not sent to Camera Application, no Camera Client Connected","Camera");
                        CameraStatus = "Camera disconnected";
                        base64image = "";
                    }
                }
                else
                {
                    Log.WriteLog("Message not sent to Camera Application - " + strMessage,"Camera");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("SendMessageToCameraClient() Exception - " + ex.Message,"Camera");
            }
        }

        public string getStatus()
        {
            return CameraStatus;
        }
        public string Img()
        {
            return base64image;
        }
    }
}
