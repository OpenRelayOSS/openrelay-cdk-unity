﻿/***********************************************************************************************************************
 Copyright (c) 2016, Imagination Technologies Limited and/or its affiliated group companies.
 All rights reserved.

 Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
 following conditions are met:
     1. Redistributions of source code must retain the above copyright notice, this list of conditions and the
        following disclaimer.
     2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the
        following disclaimer in the documentation and/or other materials provided with the distribution.
     3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote
        products derived from this software without specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
 USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
***********************************************************************************************************************/

using DTLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    public class Program
    {
        //public static void Main(string[] args)
        //{
        //    //Server server = new Server(new IPEndPoint(IPAddress.Any, 5684));
        //    Server server = new Server(new IPEndPoint(IPAddress.Any, 7000));
        //    try
        //    {
        //        server.DataReceived += new Server.DataReceivedEventHandler(server_DataReceived);
        //        server.PSKIdentities.AddIdentity(Encoding.UTF8.GetBytes("oFIrQFrW8EWcZ5u7eGfrkw"), HexToBytes("7CCDE14A5CF3B71C0C08C8B7F9E5"));
        //        server.LoadCertificateFromPem("TestServer.pem");
        //        server.RequireClientCertificate = true;
        //        server.Start();
        //        Console.WriteLine("Press any key to stop");
        //        Console.ReadKey(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        Console.ReadKey(true);
        //    }
        //    finally
        //    {
        //        server.Stop();

        //    }
        //}

        static byte[] HexToBytes(string hex)
        {
            byte[] result = new byte[hex.Length / 2];
            int count = 0;
            for (int index = 0; index < hex.Length; index += 2)
            {
                result[count] = Convert.ToByte(hex.Substring(index, 2), 16);
                count++;
            }
            return result;
        }

        static void server_DataReceived(EndPoint endPoint, byte[] data)
        {
            Console.Write(Encoding.UTF8.GetString(data));
        }
    }
}
