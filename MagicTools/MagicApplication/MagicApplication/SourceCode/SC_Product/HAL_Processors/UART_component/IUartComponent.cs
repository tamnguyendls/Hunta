using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace MagicApplication.SourceCode.SC_Product.HAL_Processors.UART_component
{
    public struct SP_PROPERTIES
    {
        public string              Name;
        public string              DeviceInfo;

        public BaudRate            BaudRate;
        public DataBit             DataBits;
        public Parity              Parity;
        public StopBits            StopBit;
        public Handshake           HandSakeMode;
    }

    public enum BaudRate
    {
        _9600  = 9600,
        _11520 = 11520,
    }

    public enum DataBit
    {
        _7 = 7,
        _8 = 8,
        _9 = 9
    }

    public delegate void DataReceiveUpdate(string sData);
    public delegate void HandsakeStatusUpdate(bool bCts, bool bDsr);

    interface IUartComponent
    {
        bool                Refresh();
        List<SP_PROPERTIES> GetDevicesList();

        bool                Connect(SP_PROPERTIES SPortSelect);
        bool                Disconnect(SP_PROPERTIES SPortSelect);
        
        bool                SetRtsState(bool State);
        bool                SetDtrState(bool State);

        void                RegisterUpdateHandsakeStatus(HandsakeStatusUpdate HandsakeStatusHandler);
        void                RegisterReceiveRealTime(DataReceiveUpdate ReceiveHandler);

        bool                Write(string sData);
        string              Read();
    }
}
