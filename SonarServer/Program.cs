using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace SonarServer
{
    /*
     * Console application to read bytes from the serial port and
     * send them to web client listening to http://localhost:8088/
     * using SignalR. 
     * Arduino Sonar sents the sonar data in the following format: 
     * [255, ANGLE, DISTANCE] where ANGLE is a value between
     * 0 and 180, DISTANCE is a value between 0 and 100 measured in 
     * cm and 255 is a separator value to distinguish ANGLE,DISTANCE
     * pairs.
     * The SignalR web application sends data as SonarData objects.
     * It runs at: http://localhost:8088/
     * Based on:    https://en.morzel.net/post/OoB-Sonar-with-Arduino-C-JavaScript-and-HTML5
     *              by MiLosz Orzel
     * Author:      Dimitris Pantazopoulos
     * Updated:     2024-05-10
     */
    internal class Program
    {
        // each data block is 3 bytes longs. We are reading
        // 3 blocks so 3x3 bytes total. When the threshold is
        // reached the SerialPort.DataReceived event is fired
        // and the bytes are processed.
        const int bytesThreshold = 9; 

        // this must match the separator value set in the
        // Arduino Sonar sketch.
        const byte dataSeparator = 255;

        // the default url for the SignalR web app
        const string url = "http://localhost:8088/";

        static List<byte> dataBuffer = new List<byte>();

        static void Main(string[] args)
        {
            try
            {   
                using (SerialPort sp = new SerialPort())
                {                    
                    sp.PortName = args[0];  // SerialPort.GetPortNames()[0];

                    // configure serial port
                    sp.BaudRate = 9600;
                    sp.ReceivedBytesThreshold = bytesThreshold;
                    sp.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
                    sp.RtsEnable = true;
                    sp.DtrEnable = true;

                    sp.Open();

                    Console.WriteLine($"Open serial port: {sp.PortName}");

                    // start signalr server
                    using (WebApp.Start<Startup>(url))
                    {
                        Console.WriteLine($"Sonar server running at {url}...");
                        Console.ReadKey();
                        sp.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // error 
                Console.WriteLine($"Error: {Environment.NewLine} {ex}");                
                Console.ReadKey();
            }   
        }

        private static void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // event handler for the SerialPort.DataReceived event. The
            // event is fired when [bytesThreashold] bytes are read
            // from the serial port. A buffer is used to gather data for
            // processing.
            SerialPort sp = (SerialPort)sender;

            int count = sp.BytesToRead;
            byte[] data = new byte[count];
            sp.Read(data, 0, count);

            dataBuffer.AddRange(data);

            Console.WriteLine("Bytes read = " + count);

            ProcessSonarData();
        }

        private static void ProcessSonarData()
        {
            // converts byte data to SonarData objects containing
            // the angle and distance values. The data buffer may
            // contains multiple data blocks of 3 bytes each 
            // separated by a special value (255). Processed data
            // are sent to web client using SignalR.
            if (dataBuffer.Count >= 3)
            {
                int lastUsedIndex = 0;                                
                
                var sonarData = new List<SonarData>();
                
                // find the data separator in the buffer. The
                // separator prefixes each data block.
                int index = dataBuffer.IndexOf(dataSeparator);
                
                while (index != -1 && index < dataBuffer.Count - 2)
                {
                    byte angle = dataBuffer[index + 1];
                    byte distance = dataBuffer[index + 2];

                    if (angle != dataSeparator && distance != dataSeparator)
                    {
                        Console.WriteLine($"Angle={angle}, Distance={distance}");
                        sonarData.Add(new SonarData() { Angle = angle, Distance = distance });
                        lastUsedIndex = index;
                    }

                    // find the next data separator in the buffer. Search starts either from
                    // the end of the processed data block (index+2) or the length of
                    // the data buffer (dataBuffer.Count-1).
                    index = dataBuffer.IndexOf(dataSeparator, Math.Min(index + 2, dataBuffer.Count - 1));
                }

                // clear processed data
                dataBuffer.RemoveRange(0, lastUsedIndex + 3);

                // send any processed data to signalr client(s)
                if (sonarData.Count > 0)
                {
                    SendSonarData(sonarData);
                }
            }
        }

        private static void SendSonarData(List<SonarData> data)
        {
            // send collection of SonarData objects to web client(s)
            // connected to http://localhost:8088/
            var hub = GlobalHost.ConnectionManager.GetHubContext<SonarHub>();
            
            hub.Clients.All.sonarData(data);

            Console.WriteLine("Blocks sent =" + data.Count);
        }
    }
}
