using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Timers;

namespace LipiRDService
{
    class SerialComm
    {
        /// <summary>
        /// Serial port object used to communicate with device
        /// </summary>
        SerialPort objSP = null;

        /// <summary>
        /// default constructor
        /// </summary>
        public SerialComm()
        {
            //objTimer = new CommTimer();
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="strPortNo">Port number</param>
        /// <param name="strBaudrate">Baud rate</param>
        public SerialComm(string strPortNo, int iBaudrate)
        {
            //objTimer = new CommTimer();

            if (objSP == null)
                objSP = new SerialPort(strPortNo, iBaudrate);
            
            objSP.Handshake = Handshake.None;
            objSP.ReadTimeout = 4000;
            objSP.WriteTimeout = 6000;
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="strPortNo">Port number</param>
        /// <param name="strBaudrate">Baud rate</param>
        /// <param name="strparity">Parity</param>
        /// <param name="strDataBits">Data bits</param>
        /// <param name="strStopBits">Stop bit</param>
        public SerialComm(string strPortNo, int iBaudrate, Parity objParity, int iDataBits, StopBits objStopBits)
        {
            //objTimer = new CommTimer();

            if (objSP == null)
                objSP = new SerialPort(strPortNo, iBaudrate, objParity, iDataBits, objStopBits);

            objSP.Handshake = Handshake.None;
            objSP.ReadTimeout = 4000;
            objSP.WriteTimeout = 6000;
        }

        /// <summary>
        /// It will open the com port for communication
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            try
            {   
                if (objSP.IsOpen == false) //if not open, open the port
                {
                    objSP.Open();
                    return true;
                }
                else
                {
                    if (objSP.IsOpen) //if open, Close the port
                    {
                        objSP.DiscardOutBuffer();
                        objSP.DiscardInBuffer();
                        objSP.Close();
                    }

                    if (objSP.IsOpen == false) //if not open, open the port
                    {
                        objSP.Open();
                        return true;
                    }

                    return false;
                }
                   
            }
            catch (Exception ex)
            {
                Log.WriteLog("Port Open Failed - " + ex.Message, "ReceiptPrinter");
                return false;
            }
        }

        public bool Close()
        {
            try
            {
                if (objSP.IsOpen) //if open, Close the port
                {
                    objSP.DiscardOutBuffer();
                    objSP.DiscardInBuffer();
                    objSP.Close();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Data to send on com port
        /// </summary>
        /// <param name="bData">Data in byte array</param>
        /// <returns>TRUE when send successfully, FALSE otherwise</returns>
        public bool SendData(byte[] bData, int iDelayAfterCommand = 100)
        {
            try
            {
                objSP.DiscardInBuffer();
                objSP.DiscardOutBuffer();

                objSP.Write(bData, 0, bData.Length);

                System.Threading.Thread.Sleep(iDelayAfterCommand);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// It reads the data received on com port
        /// </summary>
        /// <param name="bData">Byte array in which data is received</param>
        /// <returns>TRUE when reads successfully, FALSE otherwsie</returns>
        public bool ReceiveData(ref byte[] bData, out int iBytesRead)
        {
            try
            {
                Array.Clear(bData, 0, bData.Length);
                iBytesRead = 0;

                //read in memory
                iBytesRead = objSP.Read(bData, 0, bData.Length);
                
                return true;
            }
            catch (Exception ex)
            {
                Open();
                bData = null;
                iBytesRead = 0;
                return false;
            }
        }

        public ushort GenerateBlockCheckCharacter(byte[] p, ushort n)
        {
            byte ch;
            ushort i;
            ushort crc = 0x0000;

            for (i = 0; i < n; i++)
            {
                ch = p[i];
                crc = cal_bcc(crc, (ushort)ch);
            }
            return crc;
        }

        ushort cal_bcc(ushort crc, ushort ch)
        {
            ch <<= 8;
            crc = (ushort)(ch ^ crc);
            return crc;
        }
    }    
}
