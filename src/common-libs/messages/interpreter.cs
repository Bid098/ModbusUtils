using System.Diagnostics;

namespace common_libs.messages;

/// <summary>
/// here there is all parsing functions 
/// </summary>
public class interpreter
{
    /// <summary>
    /// this function parses input rawMessage and returns a new instance of Message composed of rawMessage data
    /// </summary>
    /// <param name="rawMessage">array of bytes to parse</param>
    /// <returns>result Message instance</returns>
    public static Message ParseMessage(byte[] rawMessage)
    {
        Message result = new Message();

        if (rawMessage.Length < 3)
            throw new Exception("message can't been parsed. rawMessage is too short");

        result.slaveAddress = rawMessage[0];
        
        //catches errors (each function has error function code ex. 0x03 -> err 0x83.
        //using boolean conversion 0x03 = 0000 0011 and 0x83 = 1000 0011.
        //first left bit meaning there is present an error.
        if ((rawMessage[1] & 0x80) == 0x80)
        {
            result.hasError = true;
            result.functionCode = (FunctionCode)(rawMessage[1] & 0x7F);
            result.errorCode = (ErrorCode)rawMessage[2];
            return result;
        }

        result.functionCode = (FunctionCode)(rawMessage[1]);
        switch (result.functionCode)
        {
            case (FunctionCode.Undefined): throw new Exception("caught undefined function type");
            case (FunctionCode.ReadCoils):
            {
                if (rawMessage.Length == 6)
                {
                    //it is request
                    result.startingAddress = BitConverter.ToUInt16(new byte[] { rawMessage[3], rawMessage[2] });
                    result.quantity = BitConverter.ToUInt16(new byte[] { rawMessage[5], rawMessage[4] });
                }
                else
                {
                    //it is answer
                    result.quantity = (ushort)rawMessage[2];
                    for (int i = 0; i < result.quantity; i++)
                    {
                        int powExponent = i % 8;
                        Debug.WriteLine(powExponent);
                        result.values.Add((rawMessage[i / 8 + 3] & (byte)Math.Pow(2, powExponent)) == (byte)Math.Pow(2, powExponent) ? (ushort)1 : (ushort)0);
                    }
                }
                break;
            }
        }
        
        return result;
    }
}

/// <summary>
/// Message composition
/// </summary>
public class Message
{
    /// <summary>
    /// indicates modbus slave address
    /// </summary>
    public byte slaveAddress = 0x00;
    /// <summary>
    /// indicates current function code of message
    /// </summary>
    public FunctionCode functionCode = FunctionCode.Undefined;
    /// <summary>
    /// starting address of request
    /// </summary>
    public ushort startingAddress = 0;
    /// <summary>
    /// quantity of registers to request
    /// </summary>
    public ushort quantity = 0;
    /// <summary>
    /// full message byte count
    /// </summary>
    public byte byteCount = 0;
    /// <summary>
    /// True if there is errors on request, False in other cases
    /// </summary>
    public bool hasError = false;
    /// <summary>
    /// error code applied when "hasError" = True
    /// </summary>
    public ErrorCode errorCode = ErrorCode.NoError;
    /// <summary>
    /// list of values returned from device or in transit to device
    /// </summary>
    public List<ushort> values = new List<ushort>();
}

/// <summary>
/// function codes according to resource /resources/Modbus_Application_Protocol_V1_1b.pdf
/// </summary>
public enum FunctionCode : byte
{
    ///<summary>
    /// default undefined value
    /// </summary>
    Undefined = 0x00,
    /// <summary>
    /// used to read single or multiple coils (0/1 editable values)
    /// </summary>
    ReadCoils = 0x01,
    /// <summary>
    /// used to read single or multiple discrete inputs (0/1 readonly values)
    /// </summary>
    ReadDiscreteInputs = 0x02, 
    /// <summary>
    /// used to read single or multiple holding registers (2 bytes long editable values)
    /// </summary>
    ReadHoldingRegisters = 0x03,
    ///<summary>
    /// used to read single or multiple input registers (2 bytes long readonly values)
    /// </summary>
    ReadInputRegister = 0x04,
    ///<summary>
    /// used to write single coil register (0/1 editable value, it only write 0x0000 or 0xFF00 to set "0" value or "1" value)
    /// </summary>
    WriteSingleCoil = 0x05,
    ///<summary>
    /// used to write single holding register (2 bytes long editable values)
    /// </summary>
    WriteSingleRegister = 0x06,
    SlReadExceptionStatus = 0x07,
    SlDiagnostics = 0x08,
    /// <summary>
    /// used to write multiple coil registers
    /// </summary>
    WriteMultipleCoils = 0x0F,
    /// <summary>
    /// used to write multiple holding registers
    /// </summary>
    WriteMultipleRegisters = 0x10
}

/// <summary>
/// error codes according to table at page 49 of /resources/Modbus_Application_Protocol_V1_1b.pdf
/// </summary>
public enum ErrorCode : byte
{
    /// <summary>
    /// no error has been caught
    /// </summary>
    NoError = 0x00,
    /// <summary>
    /// The function code received in the query is not an allowable action for the server (or slave). This may be because the function code is only applicable to newer devices, and was not implemented in the unit selected. It could also indicate that the server (or slave) is in the wrong state to process a request of this type, for example because it is unconfigured and is being asked to return register values.
    /// </summary>
    IllegalFunction = 0x01,
    /// <summary>
    /// The data address received in the query is not an allowable address for the server (or slave). More specifically, the combination of reference number and transfer length is  invalid. For a controller with 100 registers, the PDU addresses the first register as 0, and the last one as 99. If a request is submitted with a starting register address of  6 and a quantity of registers of 4, then this request will successfully operate (address-wise at least) on registers 96, 97, 98, 99. If a request is submitted with a starting  register address of 96 and a quantity of registers of 5, then this request will fail with Exception Code 0x02 “Illegal Data Address” since it attempts to operate on registers 96, 97, 98, 99 and 100, and there is no register with address 100.
    /// </summary>
    IllegalDataAddress = 0x02,
    /// <summary>
    /// A value contained in the query data field is not an allowable value for server (or slave). This indicates a fault in the structure of the remainder of a complex request, such as that the implied length is incorrect. It specifically does NOT mean that a data item submitted for storage in a register has a value outside the expectation of the application program, since the MODBUS protocol is unaware of the significance of any particular value of any particular register.
    /// </summary>
    IllegalDataValue = 0x03,
    /// <summary>
    /// An unrecoverable error occurred while the server (or slave) was attempting to perform the requested action.
    /// </summary>
    SlaveDeviceFailure = 0x04,
    /// <summary>
    /// Specialized use in conjunction with programming commands. The server (or slave) has accepted the request and is processing it, but a long duration of time will be required to do so. This response is returned to prevent a timeout error from occurring in the client (or master). The client (or master) can next issue a Poll Program Complete message to determine if processing is completed.
    /// </summary>
    Acknowledge = 0x05,
    /// <summary>
    /// Specialized use in conjunction with programming commands. The server (or slave) is engaged in processing a long–duration program command. The client (or master) should retransmit the message later when  the server (or slave) is free.
    /// </summary>
    SlaveDeviceBusy = 0x06,
    /// <summary>
    /// Specialized use in conjunction with function codes 20 and 21 and reference type 6, to indicate that the extended file area failed to pass a consistency check. The server (or slave) attempted to read record file, but detected a parity error in the memory. The client (or master) can retry the request, but service may be required on the server (or slave) device.
    /// </summary>
    MemoryPartitionError = 0x08,
    /// <summary>
    /// Specialized use in conjunction with gateways, indicates that the gateway was unable to allocate an internal communication path from the input port to the output port for processing the request. Usually means that the gateway is misconfigured or overloaded.
    /// </summary>
    GatewayPathUnavailable = 0x0A,
    /// <summary>
    /// Specialized use in conjunction with gateways, indicates that no response was obtained from the target device. Usually means that the device is not present on the network.
    /// </summary>
    GatewayTargetDeviceFailedToRespond = 0x0B
}