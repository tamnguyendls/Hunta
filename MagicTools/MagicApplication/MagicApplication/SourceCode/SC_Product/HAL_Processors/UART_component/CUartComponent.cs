using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MagicApplication.SourceCode.SC_Product.HAL_Processors.UART_component
{
    class CUartComponent : IUartComponent
    {
        // Attributes
        private SP_PROPERTIES SP_DefaultPro = new SP_PROPERTIES()
        {
            Name            = null,
            DeviceInfo      = null,
            BaudRate        = BaudRate._9600,
            DataBits        = DataBit._8,
            Parity          = Parity.None,
            StopBit         = StopBits.One,
            HandSakeMode    = Handshake.None
        };

        private SerialPort                      m_SPCurrent              = null;
        private List<SP_PROPERTIES>             m_SPDetectList           = null;
                                                                         
        private static Thread                   m_SPProcessing           = null;
        private static Queue<string>            m_SPReceiveQueue         = null;
        private SerialDataReceivedEventHandler  m_SPReceiveHandler       = null;
        private static bool                     m_SPFlagNewData          = false;

        private List<DataReceiveUpdate>              m_ClientReadHandlers     = null;
        private List<HandsakeStatusUpdate>      m_ClientHandsakeHandlers = null;

        private bool                            m_bCtsStatus             = false;
        private bool                            m_bDsrStatus             = false;
        private bool                            m_FlagHandsake           = false;

        // Interface Operations
        public CUartComponent()
        {
            m_SPCurrent              = new SerialPort();
            m_SPReceiveHandler       = new SerialDataReceivedEventHandler(DataReceiverHandler);
            m_SPReceiveQueue         = new Queue<string>();
            m_ClientReadHandlers     = new List<DataReceiveUpdate>();
            m_ClientHandsakeHandlers = new List<HandsakeStatusUpdate>();

            // common setting for serial port
            m_SPCurrent.NewLine = "\n";
            m_SPCurrent.DataReceived += m_SPReceiveHandler;
        }

        public bool Refresh()
        {
            bool bRet = false;
            string[] sPortNames = SerialPort.GetPortNames();

            m_SPDetectList = new List<SP_PROPERTIES>();

            foreach (string PortNameElement in sPortNames)
            {
                if (SUltFunc.VerifyString(PortNameElement))
                {
                    SP_PROPERTIES SPortDetect   = SUltFunc.DeepCopy(SP_DefaultPro);
                    SPortDetect.Name            = PortNameElement;
                    SPortDetect.DeviceInfo      = GetSPDeviceInfo(PortNameElement);

                    m_SPDetectList.Add(SPortDetect);
                }
            }

            return bRet;
        }

        public List<SP_PROPERTIES> GetDevicesList()
        {
            return m_SPDetectList;
        }

        public bool Connect(SP_PROPERTIES SPortSelect)
        {
            bool bRet = false;

            if (m_SPCurrent.IsOpen == false)
            {
                m_SPCurrent.PortName      = SPortSelect.Name;
                m_SPCurrent.BaudRate      = (int)SPortSelect.BaudRate;
                m_SPCurrent.DataBits      = (int)SPortSelect.DataBits;
                m_SPCurrent.Parity        = SPortSelect.Parity;
                m_SPCurrent.StopBits      = SPortSelect.StopBit;
                m_SPCurrent.Handshake     = SPortSelect.HandSakeMode;

                try
                {
                    m_SPCurrent.Open();
                    m_SPProcessing = new Thread(new ThreadStart(Processing));
                    m_SPProcessing.Start();
                    bRet = true;
                }
                catch { }
            }

            return bRet;
        }

        public bool Disconnect(SP_PROPERTIES SPortSelect)
        {
            bool bRet = false;

            if (m_SPCurrent.IsOpen == true)
            {
                if (m_SPCurrent.PortName == SPortSelect.Name)
                {
                    try
                    {
                        m_SPCurrent.Close();
                        m_SPProcessing.Abort();
                        bRet = true;
                    }
                    catch { }
                }
            }

            return bRet;
        }

        public bool SetRtsState(bool State)
        {
            bool bRet = false;

            if ((m_SPCurrent.Handshake == Handshake.RequestToSend) ||
                (m_SPCurrent.Handshake == Handshake.RequestToSendXOnXOff))
            {
                if (m_SPCurrent.IsOpen)
                {
                    m_SPCurrent.BaseStream.Flush();
                }

                m_SPCurrent.RtsEnable = State;
                bRet = true;
            }

            return bRet;
        }

        public bool SetDtrState(bool State)
        {
            bool bRet = false;

            if ((m_SPCurrent.Handshake == Handshake.XOnXOff) ||
                (m_SPCurrent.Handshake == Handshake.RequestToSendXOnXOff))
            {
                if (m_SPCurrent.IsOpen)
                {
                    m_SPCurrent.BaseStream.Flush();
                }

                m_SPCurrent.DtrEnable = State;
                bRet = true;
            }

            return bRet;
        }

        public void RegisterUpdateHandsakeStatus(HandsakeStatusUpdate HandsakeStatusHandler)
        {
            m_ClientHandsakeHandlers.Add(HandsakeStatusHandler);
        }

        public void RegisterReceiveRealTime(DataReceiveUpdate Read)
        {
            m_ClientReadHandlers.Add(Read);
        }

        public bool Write(string sData)
        {
            bool bRet = false;

            if (m_SPCurrent.IsOpen == true)
            {
                try
                {
                    m_SPCurrent.Write(sData);
                    bRet = true;
                }
                catch { }
            }

            return bRet;
        }

        public string Read()
        {
            string sRet = null;

            if (m_SPCurrent.IsOpen == true)
            {
                while (m_SPReceiveQueue.Count > 0)
                {
                    sRet += m_SPReceiveQueue.Dequeue();
                }
                m_SPFlagNewData = false;
            }

            return sRet;
        }

        // internal operations
        private string GetSPDeviceInfo(string portName)
        {
            string sResult = null;

            try
            {
                RegistryKey regKey = Registry.LocalMachine;
                regKey = regKey.OpenSubKey("HARDWARE\\DEVICEMAP\\SERIALCOMM");

                foreach (string element in regKey.GetValueNames())
                {
                    if (regKey.GetValue(element).ToString() == portName)
                    {
                        sResult = element;
                    }
                }
            }
            catch { }// solution for handle this one ! 

            return sResult;
        }
        
        private static void DataReceiverHandler(object sender, EventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            m_SPReceiveQueue.Enqueue(sp.ReadExisting());
            m_SPFlagNewData = true;
        }

        private void UpdateDataReceiveForClients()
        {
            if (m_SPFlagNewData == true)
            {
                string sData = Read();
                foreach (DataReceiveUpdate ClientHandler in m_ClientReadHandlers)
                {
                    ClientHandler(sData);
                }
            }
        }

        private bool VerifyHandsakeStatusChange()
        {
            bool bRet = false;

            if ((m_SPCurrent.IsOpen == true) &&
                (m_SPCurrent.Handshake != Handshake.None))
            {
                if (m_SPCurrent.CtsHolding != m_bCtsStatus)
                {
                    bRet = true;
                    m_bCtsStatus = m_SPCurrent.CtsHolding;
                }

                if (m_SPCurrent.DsrHolding != m_bDsrStatus)
                {
                    bRet = true;
                    m_bDsrStatus = m_SPCurrent.DsrHolding;
                }
            }

            return bRet;
        }

        private void UpdateHandsakeStatusForClients()
        {
            if (VerifyHandsakeStatusChange() == true)
            {
                foreach (HandsakeStatusUpdate ClientHandler in m_ClientHandsakeHandlers)
                {
                    ClientHandler(m_bCtsStatus, m_bDsrStatus);
                }
            }
        }
        private void Processing()
        {
            for (;;)
            {
                UpdateDataReceiveForClients();
                UpdateHandsakeStatusForClients();
            }
        }

    }
}
