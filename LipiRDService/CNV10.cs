using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using ITLlib;

namespace LipiRDService
{
    public class CNV10
    {
        // ssp library variables
        SSPComms m_eSSP;
        SSP_COMMAND m_cmd;
        SSP_KEYS keys;
        SSP_FULL_KEY sspKey;
        SSP_COMMAND_INFO info;
       
        // The protocol version this validator is using, set in setup request
        int m_ProtocolVersion;

        // Variables to hold the number of notes accepted and dispensed
        int m_TotalNotesAccepted, m_TotalNotesDispensed;

        // The number of channels used in this validator
        int m_NumberOfChannels;

        // The multiplier by which the channel values are multiplied to get their
        // true penny value.
        int m_ValueMultiplier;

        // The type of unit this class represents, set in the setup request
        char m_UnitType;

        // Integer array to hold the value of each note stored in the payout
        int[] m_NotePositionValues;

        //String to hold the currency stored in the note float
        string m_StoredCurrency;

        //Integer to hold total number of Hold messages to be issued before releasing note from escrow
        int m_HoldNumber;
        
        //Integer to hold number of hold messages still to be issued
        int m_HoldCount;

        //Bool to hold flag set to true if a note is being held in escrow
        bool m_NoteHeld;

        // Current poll response and length
        byte[] m_CurrentPollResponse;
        byte m_CurrentPollResponseLength;

        // A list of dataset data, sorted by value. Holds the info on channel number, value, currency,
        // level and whether it is being recycled.
        List<ChannelData> m_UnitDataList;

        // constructor
        public CNV10()
        {
            m_eSSP = new SSPComms();
            m_cmd = new SSP_COMMAND();
            keys = new SSP_KEYS();
            sspKey = new SSP_FULL_KEY();
            info = new SSP_COMMAND_INFO();
           
            m_TotalNotesAccepted = 0;
            m_TotalNotesDispensed = 0;
            m_NumberOfChannels = 0;
            m_ValueMultiplier = 1;
            m_CurrentPollResponse = new byte[256];
            m_CurrentPollResponseLength = 0;
            m_UnitDataList = new List<ChannelData>();
            m_NotePositionValues = new int[30];
            m_StoredCurrency = "";
            m_HoldCount = 0;
            m_HoldNumber = 0;
        }

        /* Variable Access */

        // access to ssp vars
        public SSPComms SSPComms
        {
            get { return m_eSSP; }
            set { m_eSSP = value; }
        }

        public SSP_COMMAND CommandStructure
        {
            get { return m_cmd; }
            set { m_cmd = value; }
        }

        public SSP_COMMAND_INFO InfoStructure
        {
            get { return info; }
            set { info = value; }
        }
      
        // access to notes accepted
        public int NotesAccepted
        {
            get { return m_TotalNotesAccepted; }
            set { m_TotalNotesAccepted = value; }
        }

        // access to notes dispensed
        public int NotesDispensed
        {
            get { return m_TotalNotesDispensed; }
            set { m_TotalNotesDispensed = value; }
        }

        // access to number of channels
        public int NumberOfChannels
        {
            get { return m_NumberOfChannels; }
            set { m_NumberOfChannels = value; }
        }

        // access to value multiplier
        public int Multiplier
        {
            get { return m_ValueMultiplier; }
            set { m_ValueMultiplier = value; }
        }

        // access to the list of data items
        public List<ChannelData> UnitDataList
        {
            get { return m_UnitDataList; }
        }

        // access to currency stored in note float
        public string StoredCurrency
        {
            get { return m_StoredCurrency; }
            set { m_StoredCurrency = value; }
        }
        
        // acccess to hold count
        public int HoldNumber 
        {
            get { return m_HoldNumber; }
            set { m_HoldNumber = value; }
        
        }

        public bool NoteHeld
        {
            get { return m_NoteHeld; }
            //set { m_HoldNumber = value; }

        }


        // get a channel value
        public int GetChannelValue(int channelNum)
        {
            if (channelNum >= 1 && channelNum <= m_NumberOfChannels)
            {
                foreach (ChannelData d in m_UnitDataList)
                {
                    if (d.Channel == channelNum)
                        return d.Value;
                }
            }
            return -1;
        }
        // get a channel currency
        public string GetChannelCurrency(int channelNum)
        {
            if (channelNum >= 1 && channelNum <= m_NumberOfChannels)
            {
                foreach (ChannelData d in m_UnitDataList)
                {
                    if (d.Channel == channelNum)
                        return new string(d.Currency);
                }
            }
            return "";
        }
        /* Command functions */

        // The enable command allows the validator to receive and act on commands sent to it.
        public void EnableValidator()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_ENABLE;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            // check response
            if (CheckGenericResponses())
                Log.WriteLog("Validator enabled", "CashAcceptor");
        }

        // Disable command stops the validator from acting on commands.
        public void DisableValidator() 
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_DISABLE;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            // check response
            if (CheckGenericResponses())
                Log.WriteLog("Validator disabled", "CashAcceptor");
        }

        // Enable payout allows the validator to payout and store notes.
        public void EnablePayout()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_ENABLE_PAYOUT_DEVICE;
            m_cmd.CommandData[1] = 0x01; // second byte to enable note value to be sent with stored event
            m_cmd.CommandDataLength = 2;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
                Log.WriteLog("Payout enabled", "CashAcceptor");
        }

        // Disable payout stops the validator being able to store/payout notes.
        public void DisablePayout()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_DISABLE_PAYOUT_DEVICE;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
                Log.WriteLog("Payout disabled", "CashAcceptor");
        }

        // Empty payout device takes all the notes stored and moves them to the cashbox.
        public void EmptyPayoutDevice()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_EMPTY_ALL;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
                Log.WriteLog("Emptying payout device", "CashAcceptor");
            }
        }

        // Payout last note command takes the last note paid in and dispenses it first (LIFO system). 
        public void PayoutNextNote()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_PAYOUT_NOTE;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {
                Log.WriteLog("Paying out next note", "CashAcceptor");
            }
        }

        // Return Note command returns note held in escrow to bezel. 
        public void ReturnNote()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_REJECT_BANKNOTE;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {
                Log.WriteLog("Returning note", "CashAcceptor");
                m_HoldCount = 0;
            }
        }

        // Smart Empty command places all stored notes into cashbox and returns interim and total values
        // of notes transferred.
        public void SmartEmpty()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SMART_EMPTY;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {
               Log.WriteLog("Smart Empty In Progress", "CashAcceptor");
            }
        }

        // Set value reporting type changes the validator to return either the value of the note, or the channel it is stored on
        // depending on what byte is sent after the command (0x01 is channel, 0x00 is 4 bit value).
        public void SetValueReportingType(bool byChannel)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_VALUE_REPORTING_TYPE;
            if (byChannel)
                m_cmd.CommandData[1] = 0x01; // report by channel number
            else
                m_cmd.CommandData[1] = 0x00; // report by 4 bit value
            m_cmd.CommandDataLength = 2;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
                Log.WriteLog("Value reporting type changed", "CashAcceptor");
        }

        // The set routing command changes the way the validator deals with a note, either it can send the note straight to the cashbox
        // or it can store the note for payout. This is specified in the second byte (0x00 to store for payout, 0x01 for cashbox). The 
        // bytes after this represent the 4 bit value of the note, or the channel (see SetValueReportingType()).
        // This function allows the note to be specified as an int in the param note, the stack bool is true for cashbox, false for storage.
        public void ChangeNoteRoute(int note, char[] currency, bool stack)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_ROUTE;

            // if this note is being changed to stack (cashbox)
            if (stack)
                m_cmd.CommandData[1] = 0x01;
            // note being stored (payout)
            else
                m_cmd.CommandData[1] = 0x00;

            // get the note as a byte array
            byte[] b = BitConverter.GetBytes(note);
            m_cmd.CommandData[2] = b[0];
            m_cmd.CommandData[3] = b[1];
            m_cmd.CommandData[4] = b[2];
            m_cmd.CommandData[5] = b[3];

            // send country code
            m_cmd.CommandData[6] = (byte)currency[0];
            m_cmd.CommandData[7] = (byte)currency[1];
            m_cmd.CommandData[8] = (byte)currency[2];

            m_cmd.CommandDataLength = 9;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
                string s;
                string cs = new string(currency);
                if (stack) s = " to cashbox) ";
                else s = " to storage) ";
                Log.WriteLog("Note routing successful (" + CHelpers.FormatToCurrency(note) + cs + s, "CashAcceptor");
            }
        }

        // This function sends the command LAST REJECT CODE which gives info about why a note has been rejected. It then
        // outputs the info to a passed across textbox.
        public void QueryRejection()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_LAST_REJECT_CODE;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {               
                switch (m_cmd.ResponseData[1])
                {
                    case 0x00: Log.WriteLog("Note accepted", "CashAcceptor"); break;
                    case 0x01: Log.WriteLog("Note length incorrect", "CashAcceptor"); break;
                    case 0x02: Log.WriteLog("Invalid note 0x2", "CashAcceptor"); break;
                    case 0x03: Log.WriteLog("Invalid note 0x03", "CashAcceptor"); break;
                    case 0x04: Log.WriteLog("Invalid note 0x04", "CashAcceptor"); break;
                    case 0x05: Log.WriteLog("Invalid note 0x05", "CashAcceptor"); break;
                    case 0x06: Log.WriteLog("Channel inhibited", "CashAcceptor"); break;
                    case 0x07: Log.WriteLog("Second note inserted during read", "CashAcceptor"); break;
                    case 0x08: Log.WriteLog("Host rejected note", "CashAcceptor"); break;
                    case 0x09: Log.WriteLog("Invalid note 0x09", "CashAcceptor"); break;
                    case 0x0A: Log.WriteLog("Invalid note read 0x0A", "CashAcceptor"); break;
                    case 0x0B: Log.WriteLog("Note too long", "CashAcceptor"); break;
                    case 0x0C: Log.WriteLog("Validator disabled", "CashAcceptor"); break;
                    case 0x0D: Log.WriteLog("Mechanism slow/stalled", "CashAcceptor"); break;
                    case 0x0E: Log.WriteLog("Strim attempt", "CashAcceptor"); break;
                    case 0x0F: Log.WriteLog("Fraud channel reject", "CashAcceptor"); break;
                    case 0x10: Log.WriteLog("No notes inserted", "CashAcceptor"); break;
                    case 0x11: Log.WriteLog("Peak detect fail", "CashAcceptor"); break;
                    case 0x12: Log.WriteLog("Twisted note detected", "CashAcceptor"); break;
                    case 0x13: Log.WriteLog("Escrow time-out", "CashAcceptor"); break;
                    case 0x14: Log.WriteLog("Bar code scan fail", "CashAcceptor"); break;
                    case 0x15: Log.WriteLog("Rear sensor fail 2", "CashAcceptor"); break;
                    case 0x16: Log.WriteLog("Slot fail 1", "CashAcceptor"); break;
                    case 0x17: Log.WriteLog("Slot fail 2", "CashAcceptor"); break;
                    case 0x18: Log.WriteLog("Lens over-sample", "CashAcceptor"); break;
                    case 0x19: Log.WriteLog("Incorrect note width", "CashAcceptor"); break;
                    case 0x1A: Log.WriteLog("Note too short", "CashAcceptor"); break;
                }
            }
        }

        // The get note positions command instructs the validator to return in the second byte the number of
        // notes stored and then in the following bytes, the values/channel (see SetValueReportingType()) of the stored
        // notes. The length of the response will vary based on the number of stored notes.
        public void CheckForStoredNotes()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_GET_NOTE_POSITIONS;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
                int counter = 0;
                Array.Clear(m_NotePositionValues, 0, m_NotePositionValues.Length);
                // Work backwards for a more accurate display (LIFO)
                for (int i = (m_cmd.ResponseData[1] * 4) + 1; i >= 2; i -= 4, counter++)
                    m_NotePositionValues[counter] = CHelpers.ConvertBytesToInt32(m_cmd.ResponseData, i-3);
            }
        }

        // The stack last note command is similar to the payout last note command (PayoutNextNote()) except it
        // moves the stored notes to the cashbox instead of dispensing.
        public void StackNextNote()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_STACK_NOTE;
            m_cmd.CommandDataLength = 1;
            if (!SendCommand()) return;

            if (CheckGenericResponses())
            {
                Log.WriteLog("Moved note from storage to cashbox", "CashAcceptor");
            }
        }

        // The reset command instructs the validator to restart (same effect as switching on and off)
        public void Reset()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_RESET;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            CheckGenericResponses();
        }

        // This function sets the protocol version in the validator to the version passed across. Whoever calls
        // this needs to check the response to make sure the version is supported.
        public void SetProtocolVersion(byte pVersion)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_HOST_PROTOCOL_VERSION;
            m_cmd.CommandData[1] = pVersion;
            m_cmd.CommandDataLength = 2;
            if (!SendCommand()) return;
        }
        

        // This function performs a number of commands in order to setup the encryption between the host and the validator.
        public bool NegotiateKeys()
        {
            // make sure encryption is off
            m_cmd.EncryptionStatus = false;

            // send sync
            Log.WriteLog("Syncing...", "CashAcceptor");
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SYNC;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return false;
            Log.WriteLog("Success", "CashAcceptor");

            m_eSSP.InitiateSSPHostKeys(keys, m_cmd);

            // send generator
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_GENERATOR;
            m_cmd.CommandDataLength = 9;
            Log.WriteLog("Setting generator...", "CashAcceptor");

            // Convert generator to bytes and add to command data.
            BitConverter.GetBytes(keys.Generator).CopyTo(m_cmd.CommandData, 1);

            if (!SendCommand()) return false;
            Log.WriteLog("Success");

            // send modulus
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_MODULUS;
            m_cmd.CommandDataLength = 9;
            Log.WriteLog("Sending modulus...", "CashAcceptor");

            // Convert modulus to bytes and add to command data.
            BitConverter.GetBytes(keys.Modulus).CopyTo(m_cmd.CommandData, 1);

            if (!SendCommand()) return false;
            Log.WriteLog("Success ", "CashAcceptor");

            // send key exchange
            m_cmd.CommandData[0] = CCommands.SSP_CMD_REQUEST_KEY_EXCHANGE;
            m_cmd.CommandDataLength = 9;
            Log.WriteLog("Exchanging keys...", "CashAcceptor");

            // Convert host intermediate key to bytes and add to command data.
            BitConverter.GetBytes(keys.HostInter).CopyTo(m_cmd.CommandData, 1);


            if (!SendCommand()) return false;
            Log.WriteLog("Success", "CashAcceptor");

            // Read slave intermediate key.
            keys.SlaveInterKey = BitConverter.ToUInt64(m_cmd.ResponseData, 1);

            m_eSSP.CreateSSPHostEncryptionKey(keys);

            // get full encryption key
            m_cmd.Key.FixedKey = 0x0123456701234567;
            m_cmd.Key.VariableKey = keys.KeyHost;

            Log.WriteLog("Keys successfully negotiated", "CashAcceptor");

            return true;
        }

        // This function uses the setup request command to get all the information about the validator.
        public void ValidatorSetupRequest()
        {
            StringBuilder sbDisplay = new StringBuilder(1000);

            // send setup request
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SETUP_REQUEST;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;

            // display setup request

            // unit type
            int index = 1;
            sbDisplay.Append("Unit Type:");
            m_UnitType = (char)m_cmd.ResponseData[index++];
            switch (m_UnitType)
            {
                case (char)0x00: sbDisplay.Append("Validator"); break;
                case (char)0x03: sbDisplay.Append("SMART Hopper"); break;
                case (char)0x06: sbDisplay.Append("SMART Payout"); break;
                case (char)0x07: sbDisplay.Append("NV11"); break;
                case (char)0x0D: sbDisplay.Append("TEBS"); break;
                default: sbDisplay.Append("Unknown Type"); break;
            }

            // firmware
            sbDisplay.AppendLine();
            sbDisplay.Append("Firmware: ");
            while (index <= 5)
            {
                sbDisplay.Append((char)m_cmd.ResponseData[index++]);
                if (index == 4)
                    sbDisplay.Append(".");
            }
            sbDisplay.AppendLine();
            // country code.
            // legacy code so skip it.
            index += 3;

            // value multiplier.
            // legacy code so skip it.
            index += 3;

            // Number of channels
            sbDisplay.AppendLine();
            sbDisplay.Append("Number of Channels: ");
            m_NumberOfChannels = m_cmd.ResponseData[index++];
            sbDisplay.Append(m_NumberOfChannels);
            sbDisplay.AppendLine();

            // channel values.
            // legacy code so skip it.
            index += m_NumberOfChannels; // Skip channel values

            // channel security
            // legacy code so skip it.
            index += m_NumberOfChannels;

            // real value multiplier
            // (big endian)
            sbDisplay.Append("Real Value Multiplier: ");
            m_ValueMultiplier = m_cmd.ResponseData[index + 2];
            m_ValueMultiplier += m_cmd.ResponseData[index + 1] << 8;
            m_ValueMultiplier += m_cmd.ResponseData[index] << 16;
            sbDisplay.Append(m_ValueMultiplier);
            sbDisplay.AppendLine();
            index += 3;


            // protocol version
            sbDisplay.Append("Protocol Version: ");
            m_ProtocolVersion = m_cmd.ResponseData[index++];
            sbDisplay.Append(m_ProtocolVersion);
            sbDisplay.AppendLine();

            // Add channel data to list then display.
            // Clear list.
            m_UnitDataList.Clear();
            // Loop through all channels.

            for (byte i = 0; i < m_NumberOfChannels; i++)
            {
                ChannelData loopChannelData = new ChannelData();
                // Channel number.
                loopChannelData.Channel = (byte)(i + 1);

                // Channel value.
                loopChannelData.Value = BitConverter.ToInt32(m_cmd.ResponseData, index + (m_NumberOfChannels * 3) + (i * 4)) * m_ValueMultiplier;

                // Channel Currency
                loopChannelData.Currency[0] = (char)m_cmd.ResponseData[index + (i * 3)];
                loopChannelData.Currency[1] = (char)m_cmd.ResponseData[(index + 1) + (i * 3)];
                loopChannelData.Currency[2] = (char)m_cmd.ResponseData[(index + 2) + (i * 3)];

                // Channel level.
                loopChannelData.Level = 0;

                // Channel recycling
                IsNoteRecycling(loopChannelData.Value, loopChannelData.Currency, ref loopChannelData.Recycling);

                // Add data to list.
                m_UnitDataList.Add(loopChannelData);

                //Display data
                sbDisplay.Append("Channel ");
                sbDisplay.Append(loopChannelData.Channel);
                sbDisplay.Append(": ");
                sbDisplay.Append(loopChannelData.Value / m_ValueMultiplier);
                sbDisplay.Append(" ");
                sbDisplay.Append(loopChannelData.Currency);
                sbDisplay.AppendLine();
            }

            // Sort the list by .Value.
            m_UnitDataList.Sort((d1, d2) => d1.Value.CompareTo(d2.Value));

            Log.WriteLog(sbDisplay.ToString(), "CashAcceptor");
        }

        // This function sends the set inhibits command to set the inhibits on the validator. An additional two
        // bytes are sent along with the command byte to indicate the status of the inhibits on the channels.
        // For example 0xFF and 0xFF in binary is 11111111 11111111. This indicates all 16 channels supported by
        // the validator are uninhibited. If a user wants to inhibit channels 8-16, they would send 0x00 and 0xFF.
        public void SetInhibits(TextBox log = null)
        {
            // set inhibits
            m_cmd.CommandData[0] = CCommands.SSP_CMD_SET_CHANNEL_INHIBITS;
            m_cmd.CommandData[1] = 0xFF;
            m_cmd.CommandData[2] = 0xFF;
            m_cmd.CommandDataLength = 3;

            if (!SendCommand()) return;
            if (CheckGenericResponses() && log != null)
            {
                Log.WriteLog("Inhibits set", "CashAcceptor");
            }
        }

        // This function uses the GET ROUTING command to determine whether a particular note
        // is recycling.
        void IsNoteRecycling(int note, char[] currency, ref bool b, TextBox log = null)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_GET_DENOMINATION_ROUTE;
            byte[] byteArr = CHelpers.ConvertIntToBytes(note);
            m_cmd.CommandData[1] = byteArr[0];
            m_cmd.CommandData[2] = byteArr[1];
            m_cmd.CommandData[3] = byteArr[2];
            m_cmd.CommandData[4] = byteArr[3];
            m_cmd.CommandData[5] = (byte)currency[0];
            m_cmd.CommandData[6] = (byte)currency[1];
            m_cmd.CommandData[7] = (byte)currency[2];
            m_cmd.CommandDataLength = 8;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
                if (m_cmd.ResponseData[1] == 0x00)
                    b = true;
                else
                    b = false;
            }
        }
        // This function gets the serial number of the device.  An optional Device parameter can be used
        // for TEBS systems to specify which device's serial number should be returned.
        // 0x00 = NV200
        // 0x01 = SMART Payout
        // 0x02 = Tamper Evident Cash Box.
        public void GetSerialNumber(byte Device)
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_GET_SERIAL_NUMBER;
            m_cmd.CommandData[1] = Device;
            m_cmd.CommandDataLength = 2;


            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
                // Response data is big endian, so reverse bytes 1 to 4.
                Array.Reverse(m_cmd.ResponseData, 1, 4);
                Log.WriteLog("Serial Number Device " + Device + ": ", "CashAcceptor");
                Log.WriteLog(BitConverter.ToUInt32(m_cmd.ResponseData, 1).ToString(), "CashAcceptor");               
            }
        }

        public void GetSerialNumber()
        {
            m_cmd.CommandData[0] = CCommands.SSP_CMD_GET_SERIAL_NUMBER;
            m_cmd.CommandDataLength = 1;

            if (!SendCommand()) return;
            if (CheckGenericResponses())
            {
                // Response data is big endian, so reverse bytes 1 to 4.
                Array.Reverse(m_cmd.ResponseData, 1, 4);
                Log.WriteLog("Serial Number : " + BitConverter.ToUInt32(m_cmd.ResponseData, 1).ToString(), "CashAcceptor");
            }
        }
        // The poll function is called repeatedly to poll to validator for information, it returns as
        // a response in the command structure what events are currently happening.
        public bool DoPoll()
        {
            byte i;        
            m_cmd.CommandData[0] = CCommands.SSP_CMD_POLL;
            m_cmd.CommandDataLength = 10;

            if (!SendCommand()) return false;

            // Store poll response to avoid corruption if the cmd structure is accessed whilst polling
            m_cmd.ResponseData.CopyTo(m_CurrentPollResponse, 0);
            m_CurrentPollResponseLength = m_cmd.ResponseDataLength;

            //parse poll m_CurrentPollResponse
            int noteVal = 0;
            for (i = 1; i < m_CurrentPollResponseLength; i++)
            {
                switch (m_CurrentPollResponse[i])
                {
                    // This m_CurrentPollResponse indicates that the unit was reset and this is the first time a poll
                    // has been called since the reset.
                    case CCommands.SSP_POLL_SLAVE_RESET:
                        Log.WriteLog("Unit reset ");
                        UpdateData();
                        break;
                    // A note is currently being read by the validator sensors. The second byte of this response
                    // is zero until the note's type has been determined, it then changes to the channel of the 
                    // scanned note.
                    case CCommands.SSP_POLL_READ_NOTE:
                        if (m_CurrentPollResponse[i + 1] > 0)
                        {
                            noteVal = GetChannelValue(m_CurrentPollResponse[i + 1]);
                            Log.WriteLog("Note in escrow, amount: " + CHelpers.FormatToCurrency(noteVal) + " " + GetChannelCurrency(m_CurrentPollResponse[i + 1]) + " ", "CashAcceptor");
                            noteVal = noteVal / 100;
                            if (Global.iTotalAmount < noteVal)
                            {
                                Log.WriteLog("Rs " + noteVal + " Return Fire", "CashAcceptor");
                                ReturnNote();
                            }

                            m_HoldCount = m_HoldNumber;
                        }
                        else
                            Log.WriteLog("Reading note ", "CashAcceptor");
                        i++;
                        break;
                    // A credit event has been detected, this is when the validator has accepted a note as legal currency.
                    case CCommands.SSP_POLL_CREDIT_NOTE:
                        noteVal = GetChannelValue(m_CurrentPollResponse[i + 1]);
                        Log.WriteLog("Credit " + CHelpers.FormatToCurrency(noteVal) + " " + GetChannelCurrency(m_CurrentPollResponse[i + 1]) + " ", "CashAcceptor");
                        Global.iTotalAmount = Global.iTotalAmount - (noteVal / 100);
                        NotesAccepted++;
                        UpdateData();
                        i++;
                        break;
                    // A note is being rejected from the validator. This will carry on polling while the note is in transit.
                    case CCommands.SSP_POLL_NOTE_REJECTING:
                        break;
                    // A note has been rejected from the validator. This response only appears once.
                    case CCommands.SSP_POLL_NOTE_REJECTED:
                        Log.WriteLog("Note rejected ", "CashAcceptor");
                        QueryRejection();
                        UpdateData();
                        EnableValidator();
                        break;
                    // A note is in transit to the cashbox.
                    case CCommands.SSP_POLL_NOTE_STACKING:
                        Log.WriteLog("Stacking note ", "CashAcceptor");
                        break;
                    // A note has reached the cashbox.
                    case CCommands.SSP_POLL_NOTE_STACKED:
                        Log.WriteLog("Note stacked ", "CashAcceptor");
                        break;
                    // A safe jam has been detected. This is where the user has inserted a note and the note
                    // is jammed somewhere that the user cannot reach.
                    case CCommands.SSP_POLL_SAFE_NOTE_JAM:
                        Log.WriteLog("Safe jam ", "CashAcceptor");
                        break;
                    // An unsafe jam has been detected. This is where a user has inserted a note and the note
                    // is jammed somewhere that the user can potentially recover the note from.
                    case CCommands.SSP_POLL_UNSAFE_NOTE_JAM:
                        Log.WriteLog("Unsafe jam ", "CashAcceptor");
                        break;
                    // The validator is disabled, it will not execute any commands or do any actions until enabled.
                    case CCommands.SSP_POLL_DISABLED:
                        Log.WriteLog("Unit disabled... ", "CashAcceptor");
                        break;
                    // A fraud attempt has been detected. The second byte indicates the channel of the note that a fraud
                    // has been attempted on.
                    case CCommands.SSP_POLL_FRAUD_ATTEMPT:
                        Log.WriteLog("Fraud attempt, note type: " + GetChannelValue(m_CurrentPollResponse[i + 1]) + " ", "CashAcceptor");
                        i++;
                        break;
                    // The stacker (cashbox) is full.
                    case CCommands.SSP_POLL_STACKER_FULL:
                        Log.WriteLog("Stacker full ", "CashAcceptor");
                        break;
                    // A note was detected somewhere inside the validator on startup and was rejected from the front of the
                    // unit.
                    case CCommands.SSP_POLL_NOTE_CLEARED_FROM_FRONT:
                        Log.WriteLog(GetChannelValue(m_CurrentPollResponse[i + 1]) + " " + GetChannelCurrency(m_CurrentPollResponse[i + 1]) + " note cleared from front at reset." + " ", "CashAcceptor");
                        i++;
                        break;
                    // A note was detected somewhere inside the validator on startup and was cleared into the cashbox.
                    case CCommands.SSP_POLL_NOTE_CLEARED_TO_CASHBOX:
                        Log.WriteLog(GetChannelValue(m_CurrentPollResponse[i + 1]) + " " + GetChannelCurrency(m_CurrentPollResponse[i + 1]) + " note cleared to stacker at reset." + " ", "CashAcceptor");
                        i++;
                        break;
                    // The cashbox has been removed from the unit. This will continue to poll until the cashbox is replaced.
                    case CCommands.SSP_POLL_CASHBOX_REMOVED:
                        Log.WriteLog("Cashbox removed ", "CashAcceptor");
                        break;
                    // The cashbox has been replaced, this will only display on a poll once.
                    case CCommands.SSP_POLL_CASHBOX_REPLACED:
                        Log.WriteLog("Cashbox replaced ", "CashAcceptor");
                        break;
                    // A note has been stored in the payout device to be paid out instead of going into the cashbox.
                    case CCommands.SSP_POLL_NOTE_STORED_IN_PAYOUT:
                        Log.WriteLog("Note stored ", "CashAcceptor");
                        i += (byte)((m_CurrentPollResponse[i + 1] * 7) + 1);
                        UpdateData();
                        break;
                    // The validator is in the process of paying out a note, this will continue to poll until the note has 
                    // been fully dispensed and removed from the front of the validator by the user.
                    case CCommands.SSP_POLL_DISPENSING:
                        Log.WriteLog("Dispensing note ", "CashAcceptor");
                        i += (byte)((m_CurrentPollResponse[i + 1] * 7) + 1);
                        break;
                    // The note has been dispensed and removed from the bezel by the user.
                    case CCommands.SSP_POLL_DISPENSED:
                        for (int j = 0; j < m_CurrentPollResponse[i + 1]; j += 7)
                        {
                            Log.WriteLog("Dispensed " + (CHelpers.ConvertBytesToInt32(m_CurrentPollResponse, j + i + 2) / 100).ToString() +
                                " " + (char)m_CurrentPollResponse[j + i + 6] + (char)m_CurrentPollResponse[j + i + 7] +
                                (char)m_CurrentPollResponse[j + i + 8] + " ", "CashAcceptor");
                        }
                        i += (byte)((m_CurrentPollResponse[i + 1] * 7) + 1);
                        NotesDispensed++;
                        UpdateData();
                        EnableValidator();
                        break;
                    // A note has been transferred from the payout storage to the cashbox
                    case CCommands.SSP_POLL_NOTE_TRANSFERED_TO_STACKER:
                        Log.WriteLog("Note stacked ", "CashAcceptor");
                        UpdateData();
                        EnableValidator();
                        break;
                    // This single poll response indicates that the payout device has finished emptying.
                    case CCommands.SSP_POLL_EMPTIED:
                        Log.WriteLog("Device emptied ", "CashAcceptor");
                        UpdateData();
                        EnableValidator();
                        break;
                    // This response indicates a note is being dispensed and is resting in the bezel waiting to be removed
                    // before the validator can continue
                    case CCommands.SSP_POLL_NOTE_HELD_IN_BEZEL:
                        for (int j = 0; j < m_CurrentPollResponse[i + 1]; j += 7)
                        {
                            Log.WriteLog((CHelpers.ConvertBytesToInt32(m_CurrentPollResponse, j + i + 2) / 100).ToString() +
                                " " + (char)m_CurrentPollResponse[j + i + 6] + (char)m_CurrentPollResponse[j + i + 7] +
                                (char)m_CurrentPollResponse[j + i + 8] + " held in bezel... ", "CashAcceptor");
                        }
                        i += (byte)((m_CurrentPollResponse[i + 1] * 7) + 1);
                        break;

                    case CCommands.SSP_POLL_SMART_EMPTYING:
                        Log.WriteLog((CHelpers.ConvertBytesToInt32(m_CurrentPollResponse, i + 2) / 100).ToString() +
                                " " + (char)m_CurrentPollResponse[i + 6] + (char)m_CurrentPollResponse[i + 7] +
                                (char)m_CurrentPollResponse[i + 8] + " emptied so far... ", "CashAcceptor");
                        i += 9;
                        break;

                    case CCommands.SSP_POLL_SMART_EMPTIED:
                        Log.WriteLog((CHelpers.ConvertBytesToInt32(m_CurrentPollResponse, i + 2) / 100).ToString() +
                                " " + (char)m_CurrentPollResponse[i + 6] + (char)m_CurrentPollResponse[i + 7] +
                                (char)m_CurrentPollResponse[i + 8] + " emptied in total. ", "CashAcceptor");
                        i += 9;
                        EnableValidator();
                        break;

                    //Default condition: do nothing
                    default:
                        break;
                }
            }

            return true;
        }

        /* Non-Command functions */

        // This function uses the set routing command to send all notes to the cashbox, it just calls the
        // ChangeNoteRoute() function for each channel.
        public void RouteAllToStack()
        {
            // all notes from channels need setting
            foreach (ChannelData d in m_UnitDataList)
            {
                ChangeNoteRoute(d.Value, d.Currency, true);
            }
        }

        // This function returns a formatted string of what notes are available in the NV11 storage device.
        public string GetStorageInfo()
        {
           StringBuilder sb = new StringBuilder("" , 100);

            sb.Append(m_StoredCurrency);
            sb.Append(" ");
            for (int i = 0; i < m_NotePositionValues.Length; i++)
            {
                if (m_NotePositionValues[i] > 0)
                {
                    sb.Append("Position ");
                    sb.Append(i);
                    sb.Append(": ");
                    sb.Append(m_NotePositionValues[i] / 100);
                    sb.Append(".00 ");
                }

            }
           
            return sb.ToString();
        }

        // This function updates the internal structures to check they are in sync with the validator.
        void UpdateData()
        {
            foreach (ChannelData d in m_UnitDataList)
            {
                IsNoteRecycling(d.Value, d.Currency, ref d.Recycling);
            }
            CheckForStoredNotes();
        }

        public void ShowAllRouting()
        {
            UpdateData();
            string Route;
            m_StoredCurrency = "";
            foreach (ChannelData d in m_UnitDataList)
            {

                if (d.Recycling)
                {
                    Route = " storage";
                    m_StoredCurrency = GetChannelCurrency(d.Channel);
                }
                else
                {
                    Route = " cashbox";
                }

                Log.WriteLog(CHelpers.FormatToCurrency(d.Value) + " " + GetChannelCurrency(d.Channel) + Route + " ", "CashAcceptor");
             }

        }

        // This function calls the open com port function of the SSP library.
        public bool OpenComPort()
        {   
            if (!m_eSSP.OpenSSPComPort(m_cmd))
            {   
                return false;
            }
            return true;
        }
        
        /* Exception and Error Handling */

        // This is used for generic response error catching, it outputs the info in a
        // meaningful way.
        private bool CheckGenericResponses()
        {
            if (m_cmd.ResponseData[0] == CCommands.SSP_RESPONSE_OK)
                return true;
            else
            {

                switch (m_cmd.ResponseData[0])
                {
                    case CCommands.SSP_RESPONSE_COMMAND_CANNOT_BE_PROCESSED:
                        if (m_cmd.ResponseData[1] == 0x03)
                        {
                            Log.WriteLog("Validator has responded with \"Busy\", command cannot be processed at this time ", "CashAcceptor");
                        }
                        else
                        {
                            Log.WriteLog("Command response is CANNOT PROCESS COMMAND, error code - 0x"
                            + BitConverter.ToString(m_cmd.ResponseData, 1, 1) + " ", "CashAcceptor");
                        }

                        return false;
                    case CCommands.SSP_RESPONSE_FAIL:
                        Log.WriteLog("Command response is FAIL ", "CashAcceptor");
                        return false;
                    case CCommands.SSP_RESPONSE_KEY_NOT_SET:
                        Log.WriteLog("Command response is KEY NOT SET, renegotiate keys ", "CashAcceptor");
                        return false;
                    case CCommands.SSP_RESPONSE_PARAMETER_OUT_OF_RANGE:
                        Log.WriteLog("Command response is PARAM OUT OF RANGE ", "CashAcceptor");
                        return false;
                    case CCommands.SSP_RESPONSE_SOFTWARE_ERROR:
                        Log.WriteLog("Command response is SOFTWARE ERROR ", "CashAcceptor");
                        return false;
                    case CCommands.SSP_RESPONSE_COMMAND_NOT_KNOWN:
                        Log.WriteLog("Command response is UNKNOWN ", "CashAcceptor");
                        return false;
                    case CCommands.SSP_RESPONSE_WRONG_NO_PARAMETERS:
                        Log.WriteLog("Command response is WRONG PARAMETERS ", "CashAcceptor");
                        return false;
                    default:
                        return false;
                }
            }
        }

        public bool SendCommand()
        {
            // Backup data and length in case we need to retry
            byte[] backup = new byte[255];
            m_cmd.CommandData.CopyTo(backup, 0);
            byte length = m_cmd.CommandDataLength;

            // attempt to send the command
            if (m_eSSP.SSPSendCommand(m_cmd, info) == false)
            {
                m_eSSP.CloseComPort();
                Log.WriteLog("Sending command failed Port status: " + m_cmd.ResponseStatus.ToString() + " ", "CashAcceptor");
                return false;
            }
            return true;
        }      
    };
}
