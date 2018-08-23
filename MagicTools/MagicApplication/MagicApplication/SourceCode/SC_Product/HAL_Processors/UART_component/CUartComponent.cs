using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Forms;
using System.Threading;
//using System.Threading.Tasks;

namespace MagicApplication.SourceCode.SC_Product.HAL_Processors.UART_component
{
    class CUartComponent : IUartComponent
    {
        private SerialPort                      m_SerialPort                    = null;
        private bool                            m_SerialPortFlagNewData         = false;
        private SerialDataReceivedEventHandler  m_SerialDataReceivedHandler     = null;
        private List<SERIAL_PROPERTIES_T>       m_SerialPortDetectedList        = null;
        private List<DataReceivedUpdate>        m_ClientReceivedDataHandlers    = null;
        private static Queue<string>            m_SerialReceivedQueue           = null;
        private static Thread                   m_SerialPortProcessing          = null;

        private SERIAL_PROPERTIES_T SerialPortDefaultProp = new SERIAL_PROPERTIES_T()
        {
            portName = null,
            deviceInfo = null,
            baudRate = BaudRate._9600,
            stopBits = StopBits.One,
            dataBits = DataBits._8b,
            parity = Parity.None,
            handShaking = Handshake.None
        };

        public CUartComponent()
        {
            m_SerialPort = new SerialPort();
            m_SerialDataReceivedHandler = new SerialDataReceivedEventHandler(DataReceiverHandler);
            m_SerialReceivedQueue = new Queue<string>();

            m_SerialPort.DataReceived += m_SerialDataReceivedHandler;
        }

        public bool Refresh()
        {
            bool bRet = false;
            m_SerialPortDetectedList = new List<SERIAL_PROPERTIES_T>();
            string[] portNames = SerialPort.GetPortNames();

            foreach (var portNameElement in portNames)
            {
                if (VerifyString(portNameElement))
                {
                    SERIAL_PROPERTIES_T portDetect;

                    portDetect = DeepCopy(SerialPortDefaultProp);
                    portDetect.portName = portNameElement;
                    portDetect.deviceInfo = GetSerialPortDeviceInfo(portNameElement);
                    m_SerialPortDetectedList.Add(portDetect);
                }
            }
            return bRet;
        }

        public bool Connect(SERIAL_PROPERTIES_T serialPortConfig)
        {
            bool bRet = false;

            if (!m_SerialPort.IsOpen)
            {
                m_SerialPort.PortName = serialPortConfig.portName;
                m_SerialPort.BaudRate = (int)serialPortConfig.baudRate;
                m_SerialPort.DataBits = (int)serialPortConfig.dataBits;
                m_SerialPort.StopBits = serialPortConfig.stopBits;
                m_SerialPort.Handshake = serialPortConfig.handShaking;

                try
                {
                    m_SerialPort.Open();
                    m_SerialPortProcessing = new Thread(new ThreadStart(Processing));
                    m_SerialPortProcessing.Start();
                    bRet = true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return bRet;
        }

        public bool Disconnect(SERIAL_PROPERTIES_T serialPortConfig)
        {
            bool bRet = false;

            if (m_SerialPort.IsOpen)
            {
                if (serialPortConfig.portName == m_SerialPort.PortName)
                {
                    try
                    {
                        m_SerialPort.Close();
                        m_SerialPortProcessing.Start();
                        bRet = true;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            return bRet;
        }

        public List<SERIAL_PROPERTIES_T> GetDeviceList()
        {
            return m_SerialPortDetectedList;
        }

        public bool Write(string sData)
        {
            bool bRet = false;

            if (m_SerialPort.IsOpen == true)
            {
                try
                {
                    m_SerialPort.Write(sData);
                    bRet = true;
                }
                catch { }
            }

            return bRet;
        }

        public string Read()
        {
            string sData = null;

            if (m_SerialPort.IsOpen == true)
            {
                while(m_SerialReceivedQueue.Count > 0)
                {
                    sData += m_SerialReceivedQueue.Dequeue();
                }
                m_SerialPortFlagNewData = false;
            }

            return sData;
        }

        public void RegisterDataReceivedUpdate(DataReceivedUpdate ReceiveHandler)
        {
            m_ClientReceivedDataHandlers.Add(ReceiveHandler);
        }

        // private functions =========================================================================
        /// 

        private void Processing()
        {
            for (;;)
            {
                Console.WriteLine("UpdateReceivedDataToClients");
                UpdateReceivedDataToClients();
            }
        }

        private void UpdateReceivedDataToClients()
        {
            if (m_SerialPortFlagNewData == true)
            {
                string sData = Read();
                foreach (DataReceivedUpdate ClientHandler in m_ClientReceivedDataHandlers)
                {
                    ClientHandler(sData);
                }
            }
        }

        private void DataReceiverHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = new SerialPort();
            m_SerialReceivedQueue.Enqueue(sp.ReadExisting());
            m_SerialPortFlagNewData = true;
        }

        private string GetSerialPortDeviceInfo(string serialPortName)
        {
            return null;
        }

        private bool VerifyString(string sData)
        {
            bool bRet = false;

            if ((sData != null) && (sData != string.Empty))
            {
                bRet = true;
            }

            return bRet;
        }

        static private T DeepCopy<T>(T obj)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                memoryStream.Position = 0;
                T t = (T)binaryFormatter.Deserialize(memoryStream);

                return t;
            }
        }
    }
}
