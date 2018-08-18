using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MagicApplication.SourceCode.SC_Product.HAL_Processors.UART_component
{
    class CUartComponent : IUartComponent
    {
        private SerialPort m_SerialPort = null;
        private List<SERIAL_PROPERTIES_T> m_SerialPortDetectedList = null;

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
                    bRet = true;
                }
                catch { }
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
                        bRet = true;
                    }
                    catch { }
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
            return false;
        }

        public string Read()
        {
            string sData = null;
            return sData;
        }

        /// private functions
        /// 

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
