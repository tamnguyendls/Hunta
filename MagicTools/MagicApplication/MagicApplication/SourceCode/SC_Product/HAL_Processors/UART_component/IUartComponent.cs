using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace MagicApplication.SourceCode.SC_Product.HAL_Processors.UART_component
{
    public enum BaudRate
    {
        _9600 = 9600,
        _115200 = 115200,
    }

    public enum DataBits
    {
        _7b = 7,
        _8b = 8,
        _9b = 9,
    }

    public struct SERIAL_PROPERTIES_T
    {
        public string portName;
        public string deviceInfo;
        public BaudRate baudRate;
        public Parity parity;
        public DataBits dataBits;
        public StopBits stopBits;
        public Handshake handShaking;
    }

    interface IUartComponent
    {
        bool Refresh();
        bool Connect(SERIAL_PROPERTIES_T serialConfig);
        bool Disconnect(SERIAL_PROPERTIES_T serialConfig);
        List<SERIAL_PROPERTIES_T> GetDeviceList();
        bool Write(string sData);
        string Read();
    }
}
