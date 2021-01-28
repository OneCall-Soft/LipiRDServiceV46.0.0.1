using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LipiRDService
{
    class PipeClient
    {
        const int buffersize = 1024;       
        byte[] ReadBuffer = new byte[buffersize];

        public string Send(string SendStr, string PipeName, int TimeOut = 1000)
        {
            try
            {
                NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                // The connect function will indefinitely wait for the pipe to become available
                // If that is not acceptable specify a maximum waiting time (in ms)
                pipeStream.Connect(1000 * 60);
                Debug.WriteLine("[Client] Pipe connection established");

                byte[] _buffer = Encoding.UTF8.GetBytes(SendStr);

                pipeStream.Write(_buffer, 0, _buffer.Length);

                Array.Clear(ReadBuffer, 0, ReadBuffer.Length);

                int iReadBytes = 0;           
                do
                {
                    iReadBytes = pipeStream.Read(ReadBuffer, 0, ReadBuffer.Length); 

                } while (iReadBytes > 0);

                pipeStream.Close();
                //pipeStream.Flush();
                pipeStream.Dispose();

                string str = ASCIIEncoding.Default.GetString(ReadBuffer);
                str = str.Replace("\0", "");
                return str;
            }
            catch (TimeoutException ex)
            {
                return "Device Disconnected";
            }
        }

        private void AsyncSend(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeClientStream pipeStream = (NamedPipeClientStream)iar.AsyncState;

                // End the write
                pipeStream.EndWrite(iar);

                pipeStream.Flush();
                pipeStream.Close();
                pipeStream.Dispose();

            }
            catch (Exception oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }      

    }
}
