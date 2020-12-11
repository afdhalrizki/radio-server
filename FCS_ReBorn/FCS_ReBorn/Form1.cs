using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

using System.Net.Sockets;
using System.Net;
using System.Threading;

using System.IO.Ports;

namespace FCS_ReBorn
{
    public partial class Form1 : Form
    {
        int FCSAzimuth;
        int FCSElevation;

        private const Int32 bufferSize = 65535;


        // Server IP for receiving client data
        private IPAddress ipAddressServerBind = null;

        // Ports number for broadcasting to 4 meriam
        // We use 4 ports for each gun ex: { 11111, 11112, 11113, 11114 }
        int[] ports = { 11111, 11112, 11113, 11114 };
        
        // Data Input from Serial
        string dataSerialIn;

        /**
         * 
         * Declaration TcpListener for All Meriams (4 Meriams)
         * 
         * **/
        private TcpListener tcpListener1 = null;
        private TcpListener tcpListener2 = null;
        private TcpListener tcpListener3 = null;
        private TcpListener tcpListener4 = null;

        bool tcpListenerConnected = false;

        
        /**
         * 
         * Declaration TcpClient for All Meriams (4 Meriams)
         * 
         * **/
        private TcpClient tcpClientOnServer1 = null;
        private TcpClient tcpClientOnServer2 = null;
        private TcpClient tcpClientOnServer3 = null;
        private TcpClient tcpClientOnServer4 = null;

        /**
         * 
         * Declaration NetworkStream for All Meriams (4 Meriams)
         * 
         * **/
        private NetworkStream networkStreamConnectedClient1 = null;
        private NetworkStream networkStreamConnectedClient2 = null;
        private NetworkStream networkStreamConnectedClient3 = null;
        private NetworkStream networkStreamConnectedClient4 = null;

        /**
         * 
         * Status connection from server to each client
         * 
         * **/
        private bool serverConnected1 = false;
        private bool serverConnected2 = false;
        private bool serverConnected3 = false;
        private bool serverConnected4 = false;

        private byte[] serverTransmitBuffer = new byte[bufferSize];
        private byte[] serverReceiveBuffer = new byte[bufferSize];

        //Penyimpanan data buffer dari 4 meriam pada timer
        string buffertcpdata1;
        string buffertcpdata2;
        string buffertcpdata3;
        string buffertcpdata4;
        int buffertcpcount;

        //penghubung antara receive procedure dengan buffer
        string bufferhandle1;
        string bufferhandle2;
        string bufferhandle3;
        string bufferhandle4;


        string datatoTCP;
        Queue<char>[] dataNew = new Queue<char>[10];
        Queue<string> dataCOM = new Queue<string>();
        public Form1()
        {
            //timer1.Enabled = false;
            System.Threading.Thread.Sleep(6000);
            InitializeComponent();

            initSerial();

            //Enable Event Handler port 1
           
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

            //Enable Event Handler port 2
            serialPort2.DataReceived += new SerialDataReceivedEventHandler(serialPort2_DataReceived);
            
            /**
             * 
             * Status of each meriam at the beginning
             * 
             * **/
            meriam1Status.Text = "Disconnected.";
            meriam2Status.Text = "Disconnected.";
            meriam3Status.Text = "Disconnected.";
            meriam4Status.Text = "Disconnected.";

            
            // Open all meriam ports
            openServerPorts();

            timer1.Enabled = true;
            timerParse.Enabled = true;
        }
        
        private void Form1_Closed(object sender, EventArgs e)
        {
            // close All Serial Connectinos
            closeAllSerial();

            // Close All Server Ports
            closeServerPorts();
        }

        private void closeAllSerial()
        {
            serialPort1.Close();
            serialPort2.Close();

            closeServerPorts();
        }

        private void initSerial()
        {

            try
            {

                serialPort1.PortName = "COM1";
                serialPort1.BaudRate = 115200;
                serialPort1.ReadTimeout = 2000;
                serialPort1.Open();

                serialPort2.PortName = "COM2";
                serialPort2.BaudRate = 115200;
                serialPort2.ReadTimeout = 2000;
                serialPort2.Open();

            }
            catch (Exception)
            {
                DialogResult dialogresult = MessageBox.Show("No Serial port detected!", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
            }

        }

        /// <summary>
        /// Prosedur pemrosesan queue
        /// </summary>
        /// <param name="output"></param>
        private void processQueue(string output)
        {
            textBox1.Text = dataCOM.Count.ToString();
            if (dataCOM.Count > 0)
            {
                output = dataCOM.Dequeue();
                textBox4.Text = output;
                datatoTCP = output;
                //Pemilihan metode pemrosesan data antrian sistem
                if (output.Substring(0, 2).CompareTo("S1") == 0)
                {
                    parsingSerial(output.Substring(2, output.Length - 2), 1);
                }
                else if (output.Substring(0, 2).CompareTo("M1") == 0)
                {
                    parsingForStatus(output.Substring(2, output.Length - 2), 1);
                }
                else if (output.Substring(0, 2).CompareTo("M2") == 0)
                {
                    parsingForStatus(output.Substring(2, output.Length - 2), 2);
                }
                else if (output.Substring(0, 2).CompareTo("M3") == 0)
                {
                    parsingForStatus(output.Substring(2, output.Length - 2), 3);
                }
                else if (output.Substring(0, 2).CompareTo("M4") == 0)
                {
                    parsingForStatus(output.Substring(2, output.Length - 2), 4);
                }
                
            }
           
        }
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //int serialNo = 1;
                dataCOM.Enqueue("S1"+serialPort1.ReadExisting());
                //dataSerialIn = serialPort1.ReadExisting();
                //datatoTCP = dataSerialIn;

                //if (dataSerialIn.Length != 0)
                //{

                //    this.Invoke((System.Threading.ThreadStart)delegate
                //    {
                //        parsingSerial(datatoTCP, serialNo);
                //    });
                //}
            }
            catch
            {
                MessageBox.Show("Could not read COM Port");
                return;
            }

        }

        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                dataSerialIn = serialPort2.ReadExisting(); ;
                int serialNo = 2;

                if (dataSerialIn.Length != 0)
                {
                    this.Invoke((System.Threading.ThreadStart)delegate
                    {
                        parsingSerial(dataSerialIn, serialNo);
                    });
                }
            }
            catch
            {
                MessageBox.Show("Could not read COM Port");
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            // Rebuild tcpListener (opening ports) if connection failed
            if (tcpListenerConnected == false) 
            {
                //closeServerPorts();

                openServerPorts();
            }
                //KODE MASALAH JADI PADA GABISA CONNECT, MUNGKIN KARENA TIMER KECEPETAN
            //else if ((serverConnected1 == false) && (serverConnected2 == false) && (serverConnected3 == false) && (serverConnected4 == false)) 
            //{
            //    closeServerPorts();
            //    openServerPorts();
            //}
            else
            {
                // Check all meriam clients connection
                //this.Invoke((System.Threading.ThreadStart)delegate
                //{
                    checkClientsConnection();
                //});

                //checkClientsConnection();
                //if (datatoTCP == null)
                //{
                    string tempData = "*1,1@1$1&456(789#\n";
                    int serial_no = 1;
                    parsingSerial(tempData, serial_no);
                //}

            }
            
            if (buffertcpcount < 1)
            {
                buffertcpcount++;
                //buffertcp1
                buffertcpdata1 += bufferhandle1;
                //buffertcp2
                buffertcpdata2 += bufferhandle2;
                //buffertcp3
                buffertcpdata3 += bufferhandle3;
                //buffertcp4
                buffertcpdata4 += bufferhandle4;


            }
            else
            {
                /*
                 * Ditunda dulu karena bikin error teyuzs
                 */

                //buffertcpcount = 0;
                //this.Invoke((System.Threading.ThreadStart)delegate
                //{
                //    textBox21.Text = buffertcpdata1;
                //    parsingForStatus(buffertcpdata1,1);
                //    buffertcpdata1 = null;
                //});
                //this.Invoke((System.Threading.ThreadStart)delegate
                //{
                //    textBox22.Text = buffertcpdata2;
                //    parsingForStatus(buffertcpdata2,2);
                //    buffertcpdata2 = null;
                //});
                //this.Invoke((System.Threading.ThreadStart)delegate
                //{
                //    textBox23.Text = buffertcpdata3;
                //    parsingForStatus(buffertcpdata3,3);
                //    buffertcpdata3 = null;
                //});
                //this.Invoke((System.Threading.ThreadStart)delegate
                //{
                //    textBox34.Text = buffertcpdata4;
                //    parsingForStatus(buffertcpdata4,4);
                //    buffertcpdata4 = null;
                //});
                //serialPort1.DiscardInBuffer();
            }

            
        }
        private void parsingForStatus(string bufferdata, int meriam)
        {
            this.Invoke((System.Threading.ThreadStart)delegate //Kalo ga make thread bakal blocking algoritma lainnya
            {
                bool thereisdata = false;
                if (bufferdata.Length > 0)
                {
                    int idx = 0;
                    while ((idx < bufferdata.Length) || !(thereisdata))
                    {
                        if (bufferdata[idx] == '*')
                        {
                            thereisdata = true;
                            idx++;
                            break;
                        }
                        else
                        {
                            idx++;
                        }
                    }
                    if (thereisdata)
                    {
                        ///INI YANG BISA MANTAP LAH
                        if (bufferdata.Substring(idx).Length >= 5)
                        {
                            if (bufferdata.Substring(idx, 5).CompareTo("CP,01") == 0)
                            {
                                string status = "ON";
                                gunStatus(meriam, status);
                            }
                            else if (bufferdata.Substring(idx, 5).CompareTo("CP,02") == 0)
                            {
                                string status = "LOCAL";
                                gunStatus(meriam, status);
                            }
                            else if (bufferdata.Substring(idx, 5).CompareTo("CP,03") == 0)
                            {
                                string status = "REMOTE";
                                gunStatus(meriam, status);
                            }
                            else if (bufferdata.Substring(idx, 5).CompareTo("CP,09") == 0)
                            {
                                string status = "UNDEFINED";
                                gunStatus(meriam, status);
                            }

                            else if (bufferdata.Substring(idx, 5).CompareTo("GY1") == 0)
                            {
                                string azimuth = bufferdata.Substring(idx + 5, 3);
                                //Ya ini pokoknya isi lah data giroskopnya
                                //Isi status GY1
                            }
                        }
                        //else
                        //{
                        //    string status = "gajelas";
                        //    gunStatus(meriam, status);
                        //}
                    }
                }
            });
        }

        private void showGunAzimuth(int gun, string azimuth)
        {
            if (gun == 017)
            {
                textBox24.Text = azimuth;
            }
            else if (gun == 013)
            {
                textBox25.Text = azimuth;
            }
            else if (gun == 011)
            {
                textBox26.Text = azimuth;
            }
            else
            {
                textBox27.Text = azimuth;
            }
        }

        private void showGunElevation(int gun, string elevation)
        {
            if (gun == 017)
            {
                textBox28.Text = elevation;
            }
            else if (gun == 013)
            {
                textBox29.Text = elevation;
            }
            else if (gun == 011)
            {
                textBox30.Text = elevation;
            }
            else
            {
                textBox31.Text = elevation;
            }
        }

        private void remotingGuns(string azimuth, string elevation) {
            // data azimuth & elevasi dari FCS TURRET
            // string dataAE = azimuth.ToString() + ' ' + elevation.ToString();
            //string dataAE = azimuth + " " + elevation;
            string dataAE = elevation;
            //string dataAE = dataCOM.Dequeue();
            streamingDataToMeriam(dataAE);
            //this.Invoke((System.Threading.ThreadStart)delegate
            //{
            //    streamingDataToMeriam(dataAE);
            //});
            
        }

        private void gunStatus(int gun, string status)
        {
            string gunStatusText = status;
            if (gun == 1)
            {
                textBox13.Text = status;
            }
            else if (gun == 2)
            {
                textBox14.Text = status;
            }
            else if (gun == 3)
            {
                textBox15.Text = status;
            }
            else if (gun == 4)
            {
                textBox16.Text = status;
            }

        }

        private void parsingSerial(string data, int serialNo)
        {
            /** Parsing serial can be one of this input:
             * BCC, LRF, RADAR, CO-PROC, OPTDIR
             * 
             * **/

            // Input Serial received
            if (serialNo == 1)
            {
                textBox37.Text = data;
            }
            else
            {
                textBox39.Text = data;
            }

            textBox20.Text = data;

            // Output Azimuth & Elevation in FCS Screen Computer Drawer
            textBox32.Text = "data azimuth turret FCS";
            textBox33.Text = "data elevasi turret FCS";

            // Output OptDir in FCS Screen Computer Drawer
            textBox35.Text = "data azimuth OptDir";
            textBox36.Text = "data elevasi OptDir";

            // Untuk Teting aja
            remotingGuns(data, data);

            //if (data == "data azimuth & elevasi")
            //{
            //    // remotingGuns(FCSAzimuth, FCSElevation);
            //    remotingGuns(data, data);
            //}
            //else 
            //{
            //    {}
            //}

        }

        private void openServerPorts()
        {
            /** Program for Sending/Broadcast Azimuth & Elevation to All Meriam
             * 
             * IPAddress is the FCS IPAddress (Radiolink of FCS)
             * 
             * **/
            
            //ipAddressServerBind = IPAddress.Any; ///< Any available IP.
            ipAddressServerBind = IPAddress.Parse("192.168.1.60"); ///< IP Address of FCS

            /**
             * listening from tcplistener1 (Meriam 1)
             * 
             * **/
            try
            {
                tcpListener1 = new TcpListener(ipAddressServerBind, ports[0]);
                tcpListener1.Start();                        ///< Starts server.

                tcpListenerConnected = true;
            }
            catch (SocketException exception)
            {
                meriam1Status.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                tcpListenerConnected = false;
            }
            catch (ArgumentOutOfRangeException)
            {
                meriam1Status.Text = "Failed. Unable to bind on this port.";
                tcpListenerConnected = false;
            }
            catch
            {
                meriam1Status.Text = "Failed. Unable to bind.";
                tcpListenerConnected = false;
            }


            /**
             * listening from tcplistener2 (Meriam 2)
             * 
             * **/
            try
            {
                tcpListener2 = new TcpListener(ipAddressServerBind, ports[1]);
                tcpListener2.Start();                        ///< Starts server.
                tcpListenerConnected = true;
            }
            catch (SocketException exception)
            {
                meriam2Status.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                tcpListenerConnected = false;
            }
            catch (ArgumentOutOfRangeException)
            {
                meriam2Status.Text = "Failed. Unable to bind on this port.";
                tcpListenerConnected = false;
            }
            catch
            {
                meriam2Status.Text = "Failed. Unable to bind.";
                tcpListenerConnected = false;
            }


            /**
             * listening from tcplistener3 (Meriam 3)
             * 
             * **/
            try
            {
                tcpListener3 = new TcpListener(ipAddressServerBind, ports[2]);
                tcpListener3.Start();                        ///< Starts server.
                tcpListenerConnected = true;
            }
            catch (SocketException exception)
            {
                meriam3Status.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                tcpListenerConnected = false;
            }
            catch (ArgumentOutOfRangeException)
            {
                meriam3Status.Text = "Failed. Unable to bind on this port.";
                tcpListenerConnected = false;
            }
            catch
            {
                meriam3Status.Text = "Failed. Unable to bind.";
                tcpListenerConnected = false;
            }


            /**
             * listening from tcplistener4
             * 
             * **/
            try
            {
                tcpListener4 = new TcpListener(ipAddressServerBind, ports[3]);
                tcpListener4.Start();                        ///< Starts server.
                tcpListenerConnected = true;
            }
            catch (SocketException exception)
            {
                meriam4Status.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                tcpListenerConnected = false;
            }
            catch (ArgumentOutOfRangeException)
            {
                meriam4Status.Text = "Failed. Unable to bind on this port.";
                tcpListenerConnected = false;
            }
            catch
            {
                meriam4Status.Text = "Failed. Unable to bind.";
                tcpListenerConnected = false;
            }

            if (tcpListenerConnected == false)
            {
                tcpListener1 = null;
                tcpListener2 = null;
                tcpListener3 = null;
                tcpListener4 = null;

                return;
            }
            
            string hostName = null;
            int addressCount = 0;

            hostName = Dns.GetHostName();
            /// Get IPv4 address server is bound to.
            for (addressCount = 0; addressCount <= System.Net.Dns.GetHostEntry(hostName).AddressList.Length - 1; addressCount++)
            {
                if (System.Net.Dns.GetHostEntry(hostName).AddressList[addressCount].AddressFamily == AddressFamily.InterNetwork)
                {

                    meriam1ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[0].ToString() + Environment.NewLine + "Listening for a connection...";

                    meriam2ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[1].ToString() + Environment.NewLine + "Listening for a connection...";

                    meriam3ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[2].ToString() + Environment.NewLine + "Listening for a connection...";

                    meriam4ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[3].ToString() + Environment.NewLine + "Listening for a connection...";

                    break;


                }
            }


            timer1.Enabled = true;
        }

        /**
         * 
         * Check all clients connection of meriam
         * 
         * **/
        private void checkClientsConnection()
        {
            IPEndPoint clientConnected1 = null;
            IPEndPoint clientConnected2 = null;
            IPEndPoint clientConnected3 = null;
            IPEndPoint clientConnected4 = null;

            /**
             * 
             * Checking client connection to Meriam 1
             * 
             * **/


            ///Accept client 1
            ///
            if (tcpListener1.Pending() && serverConnected1 == false)
            {
                tcpClientOnServer1 = new TcpClient();
                tcpClientOnServer1 = tcpListener1.AcceptTcpClient();
                clientConnected1 = (IPEndPoint)tcpClientOnServer1.Client.RemoteEndPoint;

                meriam1Status.Text = "Connected to " + IPAddress.Parse(clientConnected1.Address.ToString());
                meriam1ConnectionStatus.Text = "Connected to " + IPAddress.Parse(clientConnected1.Address.ToString());
                try
                {
                    networkStreamConnectedClient1 = tcpClientOnServer1.GetStream();       ///< Gets network stream for Read/Write.
                }
                catch
                {
                    meriam1Status.Text = Environment.NewLine +
                                                    IPAddress.Parse(clientConnected1.Address.ToString()) +
                                                    "Send/Receive failure.";
                }

                serverConnected1 = true;
            }

            /**
             * 
             * Checking client connection to Meriam 2
             * 
             * **/

            ///Accept Client 2
            if (tcpListener2.Pending() && serverConnected2 == false)
            {
                tcpClientOnServer2 = new TcpClient();
                tcpClientOnServer2 = tcpListener2.AcceptTcpClient();
                clientConnected2 = (IPEndPoint)tcpClientOnServer2.Client.RemoteEndPoint;

                meriam2Status.Text = "Connected to " + IPAddress.Parse(clientConnected2.Address.ToString());
                meriam2ConnectionStatus.Text = "Connected to " + IPAddress.Parse(clientConnected2.Address.ToString());
                try
                {
                    networkStreamConnectedClient2 = tcpClientOnServer2.GetStream();       ///< Gets network stream for Read/Write.
                }
                catch
                {
                    meriam2Status.Text = Environment.NewLine +
                                                    IPAddress.Parse(clientConnected2.Address.ToString()) +
                                                    "Send/Receive failure.";
                }

                serverConnected2 = true;
            }

            /**
             * 
             * Checking client connection to Meriam 3
             * 
             * **/

            //Accept client 3
            if (tcpListener3.Pending() && serverConnected3 == false)
            {
                tcpClientOnServer3 = new TcpClient();
                tcpClientOnServer3 = tcpListener3.AcceptTcpClient();
                clientConnected3 = (IPEndPoint)tcpClientOnServer3.Client.RemoteEndPoint;

                meriam3Status.Text = "Connected to " + IPAddress.Parse(clientConnected3.Address.ToString());
                meriam3ConnectionStatus.Text = "Connected to " + IPAddress.Parse(clientConnected3.Address.ToString());
                try
                {
                    networkStreamConnectedClient3 = tcpClientOnServer3.GetStream();       ///< Gets network stream for Read/Write.
                }
                catch
                {
                    meriam3Status.Text = Environment.NewLine +
                                                    IPAddress.Parse(clientConnected3.Address.ToString()) +
                                                    "Send/Receive failure.";
                }

                serverConnected3 = true;
            }

            /**
             * 
             * Checking client connection to Meriam 4
             * 
             * **/

            ///Accept client 4
            if (tcpListener4.Pending() && serverConnected4 == false)
            {
                tcpClientOnServer4 = new TcpClient();
                tcpClientOnServer4 = tcpListener4.AcceptTcpClient();
                clientConnected4 = (IPEndPoint)tcpClientOnServer4.Client.RemoteEndPoint;

                meriam4Status.Text = "Connected to " + IPAddress.Parse(clientConnected4.Address.ToString());
                meriam4ConnectionStatus.Text = "Connected to " + IPAddress.Parse(clientConnected4.Address.ToString());

                try
                {
                    networkStreamConnectedClient4 = tcpClientOnServer4.GetStream();       ///< Gets network stream for Read/Write.
                }
                catch
                {
                    meriam4Status.Text = Environment.NewLine +
                                                    IPAddress.Parse(clientConnected4.Address.ToString()) +
                                                    "Send/Receive failure.";
                }

                serverConnected4 = true;
            }

            Int32 totalBytesReceived = 0;

            /// Checks if connection is active and scans for data from client side.  
            /// 
            ///Scan data dari client 1
            if (serverConnected1 == true)
            {
                /*
                if (tcpClientOnServer1.Client.Poll(100,SelectMode.SelectError))
                {
                    tcpListenerConnected = false;
                    serverConnected1 = false;
                    closeServerPorts();
                }
                 * */
                if (tcpClientOnServer1.Client.Poll(100, SelectMode.SelectRead))
                {
                    if (networkStreamConnectedClient1.CanRead)
                    {
                        if (networkStreamConnectedClient1.DataAvailable)
                        {
                            try
                            {
                                /// Reads data from connected client, if available.
                                totalBytesReceived = networkStreamConnectedClient1.Read(serverReceiveBuffer, 0, bufferSize);
                            }
                            catch (SystemException)
                            {
                                // serverReceivedBytes.Text = "Fail";
                                {}
                            }
                            string receivedBytes = totalBytesReceived.ToString();
                            //serverReceivedBytes.Text = totalBytesReceived.ToString();
                            string gun1Data = Encoding.UTF8.GetString(serverReceiveBuffer, 0, totalBytesReceived);
                            if (gun1Data != null)
                            {
                                dataCOM.Enqueue("M1" + gun1Data);
                            }
                            //bufferhandle1 = gun1Data;


                            meriam1Data.Text = gun1Data;
                            meriam1Data.SelectionStart = gun1Data.Length;
                            meriam1Data.ScrollToCaret();
                            int gunNo = 1;

                            if (gun1Data == "azimuth data or elevation data")
                            { 
                                string azimuthData = "data azimuth";
                                string elevationData = "data elevation";         
                                showGunAzimuth(gunNo, azimuthData);
                                showGunElevation(gunNo, elevationData);
                            } 
                            else if (gun1Data == "data remote or local")
                            {
                                gunStatus(gunNo, gun1Data);
                            }
                            else
                            {
                                // Do nothing
                                {}
                            }
                        }
                        else
                        {
                            /// Sends signal to serverConnectButton to disconnect the socket as
                            /// it is no more active.
                            // serverOpenPort_Click(sender, e);
                            resetPortCon(1);
                        }
                    }
                }

            }

            if (serverConnected2 == true)
            {
                //if (tcpClientOnServer2.Client.Poll(100, SelectMode.SelectError))
                //{
                //    tcpListenerConnected = false;
                //    serverConnected2 = false;
                //}
                if (tcpClientOnServer2.Client.Poll(100, SelectMode.SelectRead))
                {
                    if (networkStreamConnectedClient2.CanRead)
                    {
                        if (networkStreamConnectedClient2.DataAvailable)
                        {
                            try
                            {
                                /// Reads data from connected client, if available.
                                totalBytesReceived = networkStreamConnectedClient2.Read(serverReceiveBuffer, 0, bufferSize);
                            }
                            catch (SystemException)
                            {
                                //serverReceivedBytes.Text = "Fail";
                                //return;
                            }
                            string receivedBytes = totalBytesReceived.ToString();
                            //serverReceivedBytes.Text = totalBytesReceived.ToString();
                            string gun2Data = Encoding.UTF8.GetString(serverReceiveBuffer, 0, totalBytesReceived);
                            dataCOM.Enqueue("M2" + gun2Data);
                            bufferhandle2 = gun2Data;

                            meriam2Data.Text = gun2Data;
                            meriam2Data.SelectionStart = gun2Data.Length;
                            meriam2Data.ScrollToCaret();
                            int gunNo = 2;

                            if (gun2Data == "azimuth data or elevation data")
                            {
                                string azimuthData = "data azimuth";
                                string elevationData = "data elevation";
                                showGunAzimuth(gunNo, azimuthData);
                                showGunElevation(gunNo, elevationData);
                            }
                            else if (gun2Data == "data remote or local")
                            {
                                gunStatus(gunNo, gun2Data);
                            }
                            else
                            {
                                // Do nothing
                                { }
                            }
                        }
                        else
                        {
                            /// Sends signal to serverConnectButton to disconnect the socket as
                            /// it is no more active.
                            // serverOpenPort_Click(sender, e);
                            resetPortCon(2);
                        }
                    }
                }
            }

            if (serverConnected3 == true)
            {
                //if (tcpClientOnServer3.Client.Poll(100, SelectMode.SelectError))
                //{
                //    tcpListenerConnected = false;
                //    serverConnected3 = false;
                //}
                if (tcpClientOnServer3.Client.Poll(100, SelectMode.SelectRead))
                {
                    if (networkStreamConnectedClient3.CanRead)
                    {
                        if (networkStreamConnectedClient3.DataAvailable)
                        {
                            try
                            {
                                /// Reads data from connected client, if available.
                                totalBytesReceived = networkStreamConnectedClient3.Read(serverReceiveBuffer, 0, bufferSize);
                            }
                            catch (SystemException)
                            {
                                //serverReceivedBytes.Text = "Fail";
                                //return;
                            }
                            string receivedBytes = totalBytesReceived.ToString();
                            //serverReceivedBytes.Text = totalBytesReceived.ToString();
                            string gun3Data = Encoding.UTF8.GetString(serverReceiveBuffer, 0, totalBytesReceived);
                            dataCOM.Enqueue("M3" + gun3Data);
                            bufferhandle3 = gun3Data;

                            meriam3Data.Text = gun3Data;
                            meriam3Data.SelectionStart = gun3Data.Length;
                            meriam3Data.ScrollToCaret();
                            int gunNo = 3;

                            if (gun3Data == "azimuth data or elevation data")
                            {
                                string azimuthData = "data azimuth";
                                string elevationData = "data elevation";
                                showGunAzimuth(gunNo, azimuthData);
                                showGunElevation(gunNo, elevationData);
                            }
                            else if (gun3Data == "data remote or local")
                            {
                                gunStatus(gunNo, gun3Data);
                            }
                            else
                            {
                                // Do nothing
                                { }
                            }
                        }
                        else
                        {
                            resetPortCon(3);

                        }
                    }
                }
            }

            if (serverConnected4 == true)
            {
                //if (tcpClientOnServer4.Client.Poll(100, SelectMode.SelectError))
                //{
                //    tcpListenerConnected = false;
                //    serverConnected4 = false;
                //}
                if (tcpClientOnServer4.Client.Poll(100, SelectMode.SelectRead))
                {
                    if (networkStreamConnectedClient4.CanRead)
                    {
                        if (networkStreamConnectedClient4.DataAvailable)
                        {
                            try
                            {
                                /// Reads data from connected client, if available.
                                totalBytesReceived = networkStreamConnectedClient4.Read(serverReceiveBuffer, 0, bufferSize);
                            }
                            catch (SystemException)
                            {
                                //serverReceivedBytes.Text = "Fail";
                                //return;
                            }
                            string receivedBytes = totalBytesReceived.ToString();
                            //serverReceivedBytes.Text = totalBytesReceived.ToString();
                            string gun4Data = Encoding.UTF8.GetString(serverReceiveBuffer, 0, totalBytesReceived);
                            dataCOM.Enqueue("M4" + gun4Data);
                            bufferhandle4 = gun4Data;

                            meriam4Data.Text = gun4Data;
                            meriam4Data.SelectionStart = gun4Data.Length;
                            meriam4Data.ScrollToCaret();
                            int gunNo = 4;

                            if (gun4Data == "azimuth data or elevation data")
                            {
                                string azimuthData = "data azimuth";
                                string elevationData = "data elevation";
                                showGunAzimuth(gunNo, azimuthData);
                                showGunElevation(gunNo, elevationData);
                            }
                            else if (gun4Data == "data remote or local")
                            {
                                gunStatus(gunNo, gun4Data);
                            }
                            else
                            {
                                // Do nothing
                                { }
                            }
                        }
                        else
                        {
                            /// Sends signal to serverConnectButton to disconnect the socket as
                            /// it is no more active.
                            // serverOpenPort_Click(sender, e);
                            resetPortCon(4);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Bikin prosedur reset port biar lebih rapih dikit
        /// </summary>
        /// <param name="portNum"></param>
        private void resetPortCon(int portNum)
        {
            if (portNum == 1)
            {
                ///TUTUP CLIENT 1
                networkStreamConnectedClient1.Close();
                tcpClientOnServer1.Close();
                tcpListener1.Stop();
                networkStreamConnectedClient1 = null;
                tcpClientOnServer1 = null;
                tcpListener1 = null;
                serverConnected1 = false;
                meriam1Status.Text = "Disconnected.";
                meriam1ConnectionStatus.Text = "Disconnected.";

                /**
                 * listening from tcplistener1
                 * 
                 * BUKA ULANG CLIENT 1
                 * **/
                try
                {
                    tcpListener1 = new TcpListener(ipAddressServerBind, ports[0]);
                    tcpListener1.Start();                        ///< Starts server.
                }
                catch (SocketException exception)
                {
                    meriam1ConnectionStatus.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                    tcpListenerConnected = false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    meriam1ConnectionStatus.Text = "Failed. Unable to bind on this port.";
                    tcpListenerConnected = false;
                }
                catch
                {
                    meriam1ConnectionStatus.Text = "Failed. Unable to bind.";
                    tcpListenerConnected = false;
                }
            }
            else if (portNum == 2)
            {
                networkStreamConnectedClient2.Close();
                tcpClientOnServer2.Close();
                tcpListener2.Stop();
                networkStreamConnectedClient2 = null;
                tcpClientOnServer2 = null;
                tcpListener2 = null;
                serverConnected2 = false;
                meriam2Status.Text = "Disconnected.";
                meriam2ConnectionStatus.Text = "Disconnected.";

                /**
                 * listening from tcplistener2
                 * RESET BUKA PORT 2
                 * **/
                try
                {
                    tcpListener2 = new TcpListener(ipAddressServerBind, ports[1]);
                    tcpListener2.Start();                        ///< Starts server.
                    tcpListenerConnected = true;
                    //serverConnected2 = true;///
                }
                catch (SocketException exception)
                {
                    meriam2ConnectionStatus.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                    tcpListenerConnected = false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    meriam2ConnectionStatus.Text = "Failed. Unable to bind on this port.";
                    tcpListenerConnected = false;
                }
                catch
                {
                    meriam2ConnectionStatus.Text = "Failed. Unable to bind.";
                    tcpListenerConnected = false;
                }

            }
            else if (portNum == 3)
            {
                /// Sends signal to serverConnectButton to disconnect the socket as
                /// it is no more active.
                // serverOpenPort_Click(sender, e);
                networkStreamConnectedClient3.Close();
                tcpClientOnServer3.Close();
                tcpListener3.Stop();
                networkStreamConnectedClient3= null;
                tcpClientOnServer3 = null;
                tcpListener3 = null;
                serverConnected3 = false;
                meriam3Status.Text = "Disconnected.";
                meriam3ConnectionStatus.Text = "Disconnected.";

                /**
                 * listening from tcplistener3
                 * RESET BUKA PORT 3
                 * **/
                try
                {
                    tcpListener3 = new TcpListener(ipAddressServerBind, ports[2]);
                    tcpListener3.Start();                        ///< Starts server.
                    tcpListenerConnected = true;
                    //serverConnected2 = true;///
                }
                catch (SocketException exception)
                {
                    meriam3ConnectionStatus.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                    tcpListenerConnected = false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    meriam3ConnectionStatus.Text = "Failed. Unable to bind on this port.";
                    tcpListenerConnected = false;
                }
                catch
                {
                    meriam3ConnectionStatus.Text = "Failed. Unable to bind.";
                    tcpListenerConnected = false;
                }

            }
            else if (portNum == 4)
            {
                networkStreamConnectedClient4.Close();
                tcpClientOnServer4.Close();
                tcpListener4.Stop();
                networkStreamConnectedClient4 = null;
                tcpClientOnServer4 = null;
                tcpListener4 = null;
                serverConnected4 = false;
                meriam4Status.Text = "Disconnected.";
                meriam4ConnectionStatus.Text = "Disconnected.";

                /**
                 * listening from tcplistener1
                 * 
                 * **/
                try
                {
                    tcpListener4 = new TcpListener(ipAddressServerBind, ports[3]);
                    tcpListener4.Start();
                    //serverConnected4 = true;///< Starts server.
                }
                catch (SocketException exception)
                {
                    meriam4ConnectionStatus.Text = "Failed. Socket error Code = " + (exception.ErrorCode).ToString();
                    tcpListenerConnected = false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    meriam4ConnectionStatus.Text = "Failed. Unable to bind on this port.";
                    tcpListenerConnected = false;
                }
                catch
                {
                    meriam4ConnectionStatus.Text = "Failed. Unable to bind.";
                    tcpListenerConnected = false;
                }
            }
            string hostName = null;
            int addressCount = 0;

            hostName = Dns.GetHostName();
            /// Get IPv4 address server is bound to.
            for (addressCount = 0; addressCount <= System.Net.Dns.GetHostEntry(hostName).AddressList.Length - 1; addressCount++)
            {
                if (System.Net.Dns.GetHostEntry(hostName).AddressList[addressCount].AddressFamily == AddressFamily.InterNetwork)
                {
                    if (serverConnected1 == false)
                    {
                        meriam1ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[0].ToString() + Environment.NewLine + "Listening for a connection...";
                    }

                    if (serverConnected2 == false)
                    {
                        meriam2ConnectionStatus.Text = "Bound to " +
                                         Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[1].ToString() + Environment.NewLine + "Listening for a connection...";
                    }

                    if (serverConnected3 == false)
                    {
                        meriam3ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[2].ToString() + Environment.NewLine + "Listening for a connection...";

                    }

                    if (serverConnected4 == false)
                    {
                        meriam4ConnectionStatus.Text = "Bound to " +
                                        Dns.GetHostEntry(hostName).AddressList[addressCount].ToString() + ":" +
                                        ports[3].ToString() + Environment.NewLine + "Listening for a connection...";
                    }

                    break;
                }
            }
        }
        
        private void streamingDataToMeriam(string data) {
            /** 
             * 
             * Streaming Data from Serial to All Meriam (All ports opened)
             * 
             * **/

            Int32 totalNumberOfBytes = 0;
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            totalNumberOfBytes = data.Length;
            encoding.GetBytes(data, 0, totalNumberOfBytes, serverTransmitBuffer, 0);
            
            /**
             * Streaming to Meriam 1
             * 
             * **/
            try
            {
                networkStreamConnectedClient1.Write(serverTransmitBuffer, 0, totalNumberOfBytes);        ///< Sends data.                                
            }
            catch (SystemException)
            {
                serverConnected1 = false;//return;
            }


            /**
             * Streaming to Meriam 2
             * 
             * **/
            try
            {
                networkStreamConnectedClient2.Write(serverTransmitBuffer, 0, totalNumberOfBytes);        ///< Sends data.
            }
            catch (SystemException)
            {
                serverConnected2 = false;//return;
            }

            /**
             * Streaming to Meriam 3
             * 
             * **/
            try
            {
                networkStreamConnectedClient3.Write(serverTransmitBuffer, 0, totalNumberOfBytes);        ///< Sends data.
            }
            catch (SystemException)
            {
                serverConnected3 = false;//return;
            }

            /**
             * Streaming to Meriam 4
             * 
             * **/
            try
            {
                networkStreamConnectedClient4.Write(serverTransmitBuffer, 0, totalNumberOfBytes);        ///< Sends data.
            }
            catch (SystemException)
            {
                serverConnected4 = false;//return;
            }
        }

        private void closeServerPorts()
        {
            //timer1.Enabled = false;

            if (serverConnected1 == true)
            {
                networkStreamConnectedClient1.Close();       ///< Closes client network stream.
                tcpClientOnServer1.Close();                  ///< Closes client socket.
            }

            if (serverConnected2 == true)
            {
                networkStreamConnectedClient2.Close();
                tcpClientOnServer2.Close();
            }

            if (serverConnected3 == true)
            {
                networkStreamConnectedClient3.Close();
                tcpClientOnServer3.Close();
            }

            if (serverConnected4 == true)
            {
                networkStreamConnectedClient4.Close();
                tcpClientOnServer4.Close();
            }
            
            tcpListener1.Stop();                             ///< Stops server.
            tcpListener2.Stop();                             ///< Stops server.
            tcpListener3.Stop();                             ///< Stops server.
            tcpListener4.Stop();                             ///< Stops server.

            networkStreamConnectedClient1 = null;
            networkStreamConnectedClient2 = null;
            networkStreamConnectedClient3 = null;
            networkStreamConnectedClient4 = null;

            tcpClientOnServer1 = null;
            tcpClientOnServer2 = null;
            tcpClientOnServer3 = null;
            tcpClientOnServer4 = null;

            tcpListener1 = null;
            tcpListener2 = null;
            tcpListener3 = null;
            tcpListener4 = null;

            //ipAddressServerBind = null;
        }
        private void timerParse_Tick(object sender, EventArgs e)
        {
            processQueue(datatoTCP); 
        }
    }
}