using common_libs.messages;
using Newtonsoft.Json;

namespace common_libs_test;

public class interpreterTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test(Description = "right value of response message of type coil length < 8")]
    public void GoodCoil()
    {
        byte[] buffer = new byte[] { 0x06, 0x01, 0x05, 0x14 };
        Message expectedResult = new Message()
        {
            slaveAddress = 0x06,
            functionCode = FunctionCode.ReadCoils,
            quantity = 0x05,
            values = { 0, 0, 1, 0, 1 }
        };
        Message result = interpreter.ParseMessage(buffer);
        
        if(JsonConvert.SerializeObject(expectedResult) == JsonConvert.SerializeObject(interpreter.ParseMessage(buffer)))
            Assert.Pass();
        else
            Assert.Fail($"unexpected value on result");
    }
    
    [Test(Description = "same of GoodCoil test but the response have to be wrong")]
    public void WrongCoil()
    {
        byte[] buffer = new byte[] { 0x06, 0x01, 0x05, 0x14 };
        Message expectedResult = new Message()
        {
            slaveAddress = 0x06,
            functionCode = FunctionCode.ReadCoils,
            quantity = 0x05,
            values = { 1, 0, 1, 0, 1 } //errato perchè il valore 1 all'inizio non è corretto
        };
        Message result = interpreter.ParseMessage(buffer);

        if (JsonConvert.SerializeObject(expectedResult) ==
            JsonConvert.SerializeObject(interpreter.ParseMessage(buffer)))
            Assert.Fail($"unexpected value on result");
        else
            Assert.Pass();
    }
    
    [Test(Description = "catch error code 02")]
    public void Error02OnCoils()
    {
        byte[] buffer = new byte[] { 0x06, 0x81, 0x02 };
        Message result = interpreter.ParseMessage(buffer);

        if (result is { hasError: true, errorCode: ErrorCode.IllegalDataAddress, functionCode: FunctionCode.ReadCoils }) 
            Assert.Pass();
        else
            Assert.Fail($"invalid error code or uncaught error!");
    }
}