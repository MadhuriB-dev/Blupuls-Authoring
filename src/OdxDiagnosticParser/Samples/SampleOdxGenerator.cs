using System.IO.Compression;
using System.Text;

namespace OdxDiagnosticParser.Samples;

/// <summary>
/// Utility to generate ISO 22901-1 (ODX) and ISO 22901-2 (PDX) compliant sample ODX XML files and packaged PDX archives.
/// </summary>
public static class SampleOdxGenerator
{
    /// <summary>
    /// Generates a single-layer ECU ODX-D XML string with UDS services, DIDs, DTCs, and DOPs.
    /// </summary>
    public static string CreateSampleOdxXml(string ecuName = "EngineControlUnit", string modelVersion = "2.2.0")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""{modelVersion}"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DIAG-LAYER-CONTAINER ID=""DLC_{ecuName}"">
    <SHORT-NAME>DLC_{ecuName}</SHORT-NAME>
    <ECU-VARIANTS>
      <ECU-VARIANT ID=""EV_{ecuName}"">
        <SHORT-NAME>{ecuName}</SHORT-NAME>
        <LONG-NAME>Engine Management System - Powertrain Control Module</LONG-NAME>
        <DESC>
          <P>Diagnostic specification for 2.0L Turbocharged Gasoline Direct Injection ECU conforming to ISO 14229-1 (UDS).</P>
        </DESC>

        <DIAG-DATA-DICTIONARY-SPEC>
          <DATA-OBJECT-PROPS>
            <DATA-OBJECT-PROP ID=""DOP_VIN"" CATEGORY=""IDENTICAL"">
              <SHORT-NAME>DOP_VIN</SHORT-NAME>
              <LONG-NAME>Vehicle Identification Number</LONG-NAME>
              <COMPU-METHOD>
                <CATEGORY>IDENTICAL</CATEGORY>
              </COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_ASCIISTRING"" BIT-LENGTH=""136"" />
            </DATA-OBJECT-PROP>

            <DATA-OBJECT-PROP ID=""DOP_SW_VERSION"" CATEGORY=""IDENTICAL"">
              <SHORT-NAME>DOP_SW_VERSION</SHORT-NAME>
              <LONG-NAME>Software Version String</LONG-NAME>
              <COMPU-METHOD>
                <CATEGORY>IDENTICAL</CATEGORY>
              </COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_ASCIISTRING"" BIT-LENGTH=""128"" />
            </DATA-OBJECT-PROP>

            <DATA-OBJECT-PROP ID=""DOP_ENGINE_SPEED"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_EngineSpeed</SHORT-NAME>
              <LONG-NAME>Engine Rotational Speed (RPM)</LONG-NAME>
              <COMPU-METHOD>
                <CATEGORY>LINEAR</CATEGORY>
                <COMPU-INTERNAL-TO-PHYS>
                  <COMPU-SCALES>
                    <COMPU-SCALE>
                      <LOWER-LIMIT>0</LOWER-LIMIT>
                      <UPPER-LIMIT>65535</UPPER-LIMIT>
                      <COMPU-RATIONAL-COEFFS>
                        <COMPU-NUMERATOR>
                          <V>0</V>
                          <V>0.25</V>
                        </COMPU-NUMERATOR>
                        <COMPU-DENOMINATOR>
                          <V>1</V>
                        </COMPU-DENOMINATOR>
                      </COMPU-RATIONAL-COEFFS>
                    </COMPU-SCALE>
                  </COMPU-SCALES>
                </COMPU-INTERNAL-TO-PHYS>
              </COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-SPEC ID-REF=""rpm"" />
            </DATA-OBJECT-PROP>

            <DATA-OBJECT-PROP ID=""DOP_COOLANT_TEMP"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_CoolantTemp</SHORT-NAME>
              <LONG-NAME>Engine Coolant Temperature</LONG-NAME>
              <COMPU-METHOD>
                <CATEGORY>LINEAR</CATEGORY>
                <COMPU-INTERNAL-TO-PHYS>
                  <COMPU-SCALES>
                    <COMPU-SCALE>
                      <LOWER-LIMIT>0</LOWER-LIMIT>
                      <UPPER-LIMIT>255</UPPER-LIMIT>
                      <COMPU-RATIONAL-COEFFS>
                        <COMPU-NUMERATOR>
                          <V>-40</V>
                          <V>1</V>
                        </COMPU-NUMERATOR>
                        <COMPU-DENOMINATOR>
                          <V>1</V>
                        </COMPU-DENOMINATOR>
                      </COMPU-RATIONAL-COEFFS>
                    </COMPU-SCALE>
                  </COMPU-SCALES>
                </COMPU-INTERNAL-TO-PHYS>
              </COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""8"" />
              <UNIT-SPEC ID-REF=""degC"" />
            </DATA-OBJECT-PROP>

            <DATA-OBJECT-PROP ID=""DOP_VEHICLE_SPEED"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_VehicleSpeed</SHORT-NAME>
              <LONG-NAME>Vehicle Speed</LONG-NAME>
              <COMPU-METHOD>
                <CATEGORY>LINEAR</CATEGORY>
                <COMPU-INTERNAL-TO-PHYS>
                  <COMPU-SCALES>
                    <COMPU-SCALE>
                      <LOWER-LIMIT>0</LOWER-LIMIT>
                      <UPPER-LIMIT>255</UPPER-LIMIT>
                      <COMPU-RATIONAL-COEFFS>
                        <COMPU-NUMERATOR>
                          <V>0</V>
                          <V>1</V>
                        </COMPU-NUMERATOR>
                      </COMPU-RATIONAL-COEFFS>
                    </COMPU-SCALE>
                  </COMPU-SCALES>
                </COMPU-INTERNAL-TO-PHYS>
              </COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""8"" />
              <UNIT-SPEC ID-REF=""km/h"" />
            </DATA-OBJECT-PROP>

            <DATA-OBJECT-PROP ID=""DOP_BATTERY_VOLTAGE"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_BatteryVoltage</SHORT-NAME>
              <LONG-NAME>ECU Supply Battery Voltage</LONG-NAME>
              <COMPU-METHOD>
                <CATEGORY>LINEAR</CATEGORY>
                <COMPU-INTERNAL-TO-PHYS>
                  <COMPU-SCALES>
                    <COMPU-SCALE>
                      <LOWER-LIMIT>0</LOWER-LIMIT>
                      <UPPER-LIMIT>65535</UPPER-LIMIT>
                      <COMPU-RATIONAL-COEFFS>
                        <COMPU-NUMERATOR>
                          <V>0</V>
                          <V>0.001</V>
                        </COMPU-NUMERATOR>
                      </COMPU-RATIONAL-COEFFS>
                    </COMPU-SCALE>
                  </COMPU-SCALES>
                </COMPU-INTERNAL-TO-PHYS>
              </COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-SPEC ID-REF=""V"" />
            </DATA-OBJECT-PROP>
          </DATA-OBJECT-PROPS>

          <DTC-DOPS>
            <DTC-DOP ID=""DTC_DOP_ECU"">
              <SHORT-NAME>DTC_DOP_ECU</SHORT-NAME>
              <DTCS>
                <DTC ID=""DTC_P010013"">
                  <SHORT-NAME>DTC_P010013</SHORT-NAME>
                  <TROUBLE-CODE>65555</TROUBLE-CODE>
                  <DISPLAY-TROUBLE-CODE>P0100-13</DISPLAY-TROUBLE-CODE>
                  <TEXT>Mass or Volume Air Flow 'A' Circuit - Circuit Open</TEXT>
                  <SEVERITY>Warning</SEVERITY>
                </DTC>

                <DTC ID=""DTC_P011517"">
                  <SHORT-NAME>DTC_P011517</SHORT-NAME>
                  <TROUBLE-CODE>70935</TROUBLE-CODE>
                  <DISPLAY-TROUBLE-CODE>P0115-17</DISPLAY-TROUBLE-CODE>
                  <TEXT>Engine Coolant Temperature Circuit - Circuit Voltage Above Threshold</TEXT>
                  <SEVERITY>Warning</SEVERITY>
                </DTC>

                <DTC ID=""DTC_P030092"">
                  <SHORT-NAME>DTC_P030092</SHORT-NAME>
                  <TROUBLE-CODE>196754</TROUBLE-CODE>
                  <DISPLAY-TROUBLE-CODE>P0300-92</DISPLAY-TROUBLE-CODE>
                  <TEXT>Random or Multiple Cylinder Misfire Detected - Performance or Incorrect Operation</TEXT>
                  <SEVERITY>Fatal</SEVERITY>
                </DTC>

                <DTC ID=""DTC_U010087"">
                  <SHORT-NAME>DTC_U010087</SHORT-NAME>
                  <TROUBLE-CODE>12648583</TROUBLE-CODE>
                  <DISPLAY-TROUBLE-CODE>U0100-87</DISPLAY-TROUBLE-CODE>
                  <TEXT>Lost Communication With ECM/PCM 'A' - Missing Message</TEXT>
                  <SEVERITY>Fatal</SEVERITY>
                </DTC>

                <DTC ID=""DTC_B100016"">
                  <SHORT-NAME>DTC_B100016</SHORT-NAME>
                  <TROUBLE-CODE>9437206</TROUBLE-CODE>
                  <DISPLAY-TROUBLE-CODE>B1000-16</DISPLAY-TROUBLE-CODE>
                  <TEXT>ECU Internal Electronic Failure - Circuit Voltage Below Threshold</TEXT>
                  <SEVERITY>Fatal</SEVERITY>
                </DTC>
              </DTCS>
            </DTC-DOP>
          </DTC-DOPS>
        </DIAG-DATA-DICTIONARY-SPEC>

        <DIAG-SERVICES>
          <DIAG-SERVICE ID=""SVC_DiagnosticSessionControl"" SEMANTIC=""SESSION"">
            <SHORT-NAME>DiagnosticSessionControl</SHORT-NAME>
            <LONG-NAME>UDS Service 0x10 - Diagnostic Session Control</LONG-NAME>
            <DESC><P>Switches the ECU between default, programming, and extended diagnostic sessions.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_DiagnosticSessionControl</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x10</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" TYPE=""VALUE"">
                  <SHORT-NAME>SessionType</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0x01</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_DiagnosticSessionControl</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x50</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" TYPE=""VALUE"">
                  <SHORT-NAME>SessionEcho</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_SecurityAccess"" SEMANTIC=""SECURITY"">
            <SHORT-NAME>SecurityAccess</SHORT-NAME>
            <LONG-NAME>UDS Service 0x27 - Security Access</LONG-NAME>
            <DESC><P>Requests seed and sends key for ECU diagnostic unlock.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_SecurityAccess</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x27</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" TYPE=""VALUE"">
                  <SHORT-NAME>SecurityLevel</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0x01</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_SecurityAccess</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x67</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_Read_VIN"" SEMANTIC=""DATA-ID"">
            <SHORT-NAME>ReadDataByIdentifier_VIN</SHORT-NAME>
            <LONG-NAME>Read Vehicle Identification Number (DID 0xF190)</LONG-NAME>
            <DESC><P>Retrieves 17-character ISO 3779 VIN from non-volatile memory.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_VIN</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x22</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0xF190</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_Read_VIN</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x62</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID_Echo</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0xF190</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" TYPE=""VALUE"" DOP-REF=""DOP_VIN"">
                  <SHORT-NAME>VIN_Data</SHORT-NAME>
                  <BYTE-POSITION>3</BYTE-POSITION>
                  <BIT-LENGTH>136</BIT-LENGTH>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_Write_VIN"" SEMANTIC=""DATA-ID"">
            <SHORT-NAME>WriteDataByIdentifier_VIN</SHORT-NAME>
            <LONG-NAME>Write Vehicle Identification Number (DID 0xF190)</LONG-NAME>
            <DESC><P>Programs 17-character VIN during End-of-Line manufacturing.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Write_VIN</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x2E</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0xF190</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" TYPE=""VALUE"" DOP-REF=""DOP_VIN"">
                  <SHORT-NAME>VIN_Data</SHORT-NAME>
                  <BYTE-POSITION>3</BYTE-POSITION>
                  <BIT-LENGTH>136</BIT-LENGTH>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_Write_VIN</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x6E</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID_Echo</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0xF190</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_Read_EngineSpeed"" SEMANTIC=""DATA-ID"">
            <SHORT-NAME>ReadDataByIdentifier_EngineSpeed</SHORT-NAME>
            <LONG-NAME>Read Engine Rotational Speed (DID 0x010C)</LONG-NAME>
            <DESC><P>Current engine crankshaft speed in RPM.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_EngineSpeed</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x22</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0x010C</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_Read_EngineSpeed</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x62</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" TYPE=""VALUE"" DOP-REF=""DOP_ENGINE_SPEED"">
                  <SHORT-NAME>EngineSpeed_Data</SHORT-NAME>
                  <BYTE-POSITION>3</BYTE-POSITION>
                  <BIT-LENGTH>16</BIT-LENGTH>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_Read_CoolantTemp"" SEMANTIC=""DATA-ID"">
            <SHORT-NAME>ReadDataByIdentifier_CoolantTemperature</SHORT-NAME>
            <LONG-NAME>Read Engine Coolant Temperature (DID 0x0105)</LONG-NAME>
            <DESC><P>Engine cooling jacket temperature in degrees Celsius.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_CoolantTemp</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x22</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0x0105</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_Read_CoolantTemp</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x62</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" TYPE=""VALUE"" DOP-REF=""DOP_COOLANT_TEMP"">
                  <SHORT-NAME>CoolantTemp_Data</SHORT-NAME>
                  <BYTE-POSITION>3</BYTE-POSITION>
                  <BIT-LENGTH>8</BIT-LENGTH>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_Read_BatteryVoltage"" SEMANTIC=""DATA-ID"">
            <SHORT-NAME>ReadDataByIdentifier_BatteryVoltage</SHORT-NAME>
            <LONG-NAME>Read ECU Battery Voltage (DID 0x0142)</LONG-NAME>
            <DESC><P>Terminal 30 supply voltage in millivolts.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_BatteryVoltage</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x22</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" TYPE=""VALUE"">
                  <SHORT-NAME>DID</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0x0142</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_Read_BatteryVoltage</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x62</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" TYPE=""VALUE"" DOP-REF=""DOP_BATTERY_VOLTAGE"">
                  <SHORT-NAME>BatteryVoltage_Data</SHORT-NAME>
                  <BYTE-POSITION>3</BYTE-POSITION>
                  <BIT-LENGTH>16</BIT-LENGTH>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_ClearDtc"" SEMANTIC=""CLEAR-DTC"">
            <SHORT-NAME>ClearDiagnosticInformation</SHORT-NAME>
            <LONG-NAME>UDS Service 0x14 - Clear Diagnostic Information</LONG-NAME>
            <DESC><P>Clears stored fault codes and freeze frame data.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ClearDtc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x14</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" TYPE=""VALUE"">
                  <SHORT-NAME>GroupOfDTC</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0xFFFFFF</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_ClearDtc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x54</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_ReadDtc"" SEMANTIC=""READ-DTC"">
            <SHORT-NAME>ReadDTCInformation</SHORT-NAME>
            <LONG-NAME>UDS Service 0x19 - Read DTC Information</LONG-NAME>
            <DESC><P>Reports list of active and confirmed diagnostic trouble codes.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadDtc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x19</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" TYPE=""VALUE"">
                  <SHORT-NAME>SubFunction</SHORT-NAME>
                  <BYTE-POSITION>1</BYTE-POSITION>
                  <CODED-VALUE>0x02</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_ReadDtc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x59</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_TesterPresent"" SEMANTIC=""TESTER-PRESENT"">
            <SHORT-NAME>TesterPresent</SHORT-NAME>
            <LONG-NAME>UDS Service 0x3E - Tester Present</LONG-NAME>
            <DESC><P>Keeps non-default diagnostic sessions and security access active.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_TesterPresent</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x3E</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSE>
              <SHORT-NAME>PR_TesterPresent</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" TYPE=""CODED-CONST"">
                  <SHORT-NAME>ResponseSID</SHORT-NAME>
                  <BYTE-POSITION>0</BYTE-POSITION>
                  <CODED-VALUE>0x7E</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </POS-RESPONSE>
          </DIAG-SERVICE>
        </DIAG-SERVICES>

        <COMPARAM-REFS>
          <COMPARAM-REF ID-REF=""CP_CanId"">
            <SHORT-NAME>CP_CanId</SHORT-NAME>
            <VALUE>0x7E0</VALUE>
            <UNIT>Hex</UNIT>
            <DESC><P>ECU Physical Diagnostic Request CAN Identifier.</P></DESC>
          </COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_CanIdRx"">
            <SHORT-NAME>CP_CanIdRx</SHORT-NAME>
            <VALUE>0x7E8</VALUE>
            <UNIT>Hex</UNIT>
            <DESC><P>ECU Physical Diagnostic Response CAN Identifier.</P></DESC>
          </COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_Baudrate"">
            <SHORT-NAME>CP_Baudrate</SHORT-NAME>
            <VALUE>500000</VALUE>
            <UNIT>bps</UNIT>
            <DESC><P>High-Speed CAN Diagnostic Bus Baud Rate.</P></DESC>
          </COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_P2Max"">
            <SHORT-NAME>CP_P2Max</SHORT-NAME>
            <VALUE>50</VALUE>
            <UNIT>ms</UNIT>
            <DESC><P>Maximum server response time for diagnostic request.</P></DESC>
          </COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_P2StarMax"">
            <SHORT-NAME>CP_P2StarMax</SHORT-NAME>
            <VALUE>5000</VALUE>
            <UNIT>ms</UNIT>
            <DESC><P>Enhanced server response time after NRC 0x78.</P></DESC>
          </COMPARAM-REF>
        </COMPARAM-REFS>

      </ECU-VARIANT>
    </ECU-VARIANTS>
  </DIAG-LAYER-CONTAINER>
</ODX>";
    }

    /// <summary>
    /// Generates an ISO 22901-2 compliant index.xml PDX manifest with standard ABLOCKs, COMPANY-DATAS, DOC-REVISIONS,
    /// as well as Vehicle/ECU summary elements.
    /// </summary>
    public static string CreateSamplePdxIndexXml(string ecuName = "EngineControlUnit", string vehicleModel = "BlueBinaries GT-2026")
    {
        var isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CATALOG F-VERSION=""2.2.0"" MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <SHORT-NAME>PACK_{ecuName}_V1</SHORT-NAME>
  <LONG-NAME>ISO 22901-2 PDX Diagnostic Package - {vehicleModel} ({ecuName})</LONG-NAME>
  <VERSION>1.0.0</VERSION>
  <CREATOR>BlueBinaries Engineering Diagnostic Authoring Suite</CREATOR>
  <DATE>{DateTime.UtcNow:yyyy-MM-dd}</DATE>
  <DESC>ISO 22901-1 / ISO 22901-2 compliant packaged diagnostic exchange archive for {vehicleModel} {ecuName}.</DESC>

  <ADMIN-DATA>
    <LANGUAGE>en-US</LANGUAGE>
    <DOC-REVISIONS>
      <DOC-REVISION>
        <REVISION-LABEL>1.0.0</REVISION-LABEL>
        <STATE>Released</STATE>
        <DATE>{isoNow}</DATE>
      </DOC-REVISION>
    </DOC-REVISIONS>
  </ADMIN-DATA>

  <COMPANY-DATAS>
    <COMPANY-DATA ID=""CD_BlueBinaries"">
      <SHORT-NAME>BlueBinaries</SHORT-NAME>
      <LONG-NAME>BlueBinaries Engineering and Solutions Ltd</LONG-NAME>
    </COMPANY-DATA>
  </COMPANY-DATAS>

  <ABLOCKS>
    <ABLOCK>
      <SHORT-NAME>DLC_PR_UDSonCAN</SHORT-NAME>
      <LONG-NAME>ISO 14229-3 / ISO 15765-2 Communication Parameter Spec &amp; Protocol</LONG-NAME>
      <CATEGORY>ODX-COMPARAM-SPEC</CATEGORY>
      <FILES>
        <FILE MIME-TYPE=""application/x-asam.odx.c"" CREATION-DATE=""{isoNow}"">odx-c/Protocols_UDS.odx-c</FILE>
      </FILES>
    </ABLOCK>
    <ABLOCK>
      <SHORT-NAME>DLC_SD_15765_3</SHORT-NAME>
      <LONG-NAME>Shared Standard UDS Diagnostic Services, Units, and DOPs</LONG-NAME>
      <CATEGORY>ODX-ECU-CONFIG</CATEGORY>
      <FILES>
        <FILE MIME-TYPE=""application/x-asam.odx.e"" CREATION-DATE=""{isoNow}"">odx-e/Shared_UDS_Services.odx-e</FILE>
      </FILES>
    </ABLOCK>
    <ABLOCK>
      <SHORT-NAME>DLC_{ecuName}</SHORT-NAME>
      <LONG-NAME>{ecuName} Functional Groups, BaseVariant, and ECU Variants (ODX-D)</LONG-NAME>
      <CATEGORY>ODX-DATA</CATEGORY>
      <FILES>
        <FILE MIME-TYPE=""application/x-asam.odx.d"" CREATION-DATE=""{isoNow}"">odx-d/{ecuName}.odx-d</FILE>
      </FILES>
    </ABLOCK>
    <ABLOCK>
      <SHORT-NAME>VIS_{vehicleModel.Replace(" ", "_").Replace("-", "")}</SHORT-NAME>
      <LONG-NAME>{vehicleModel} Vehicle Topology &amp; Logical Links (ODX-V)</LONG-NAME>
      <CATEGORY>ODX-VEHICLE-INFO</CATEGORY>
      <FILES>
        <FILE MIME-TYPE=""application/x-asam.odx.v"" CREATION-DATE=""{isoNow}"">odx-v/VehicleTopology.odx-v</FILE>
      </FILES>
    </ABLOCK>
  </ABLOCKS>

  <VEHICLES>
    <VEHICLE-INFO>
      <SHORT-NAME>VI_{vehicleModel.Replace(" ", "_")}</SHORT-NAME>
      <MODEL>{vehicleModel}</MODEL>
      <COMPANY>BlueBinaries Engineering</COMPANY>
      <MODEL-YEAR>2026</MODEL-YEAR>
    </VEHICLE-INFO>
  </VEHICLES>

  <ECUS>
    <ECU-INFO>
      <SHORT-NAME>EI_{ecuName}</SHORT-NAME>
      <ECU-NAME>{ecuName}</ECU-NAME>
      <SUPPLIER>Tier-1 Powertrain Systems</SUPPLIER>
      <DIAG-ADDRESS>0x7E0</DIAG-ADDRESS>
    </ECU-INFO>
  </ECUS>
</CATALOG>";
    }

    /// <summary>
    /// Generates an ISO 22901-1 ODX-C Communication Parameters Spec + PROTOCOL layer XML.
    /// </summary>
    public static string CreateSampleOdxCXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <COMPARAM-SPEC ID=""CPS_ISO_15765_UDS"">
    <SHORT-NAME>CPS_ISO_15765_UDS</SHORT-NAME>
    <LONG-NAME>ISO 15765-2 / ISO 14229-3 Communication Parameter Specification</LONG-NAME>
    <DESC><P>Standard CAN diagnostic bus and transport layer timing parameters.</P></DESC>
    <PROT-STACKS>
      <PROT-STACK ID=""PS_UDS_on_CAN"">
        <SHORT-NAME>PS_UDS_on_CAN</SHORT-NAME>
        <LONG-NAME>ISO 14229 UDS on ISO 15765-2 High-Speed CAN</LONG-NAME>
        <PDU-PROTOCOL-TYPE>ISO_14229_3_on_ISO_15765_2</PDU-PROTOCOL-TYPE>
        <PHYSICAL-LINK-TYPE>ISO_11898_2_DWCAN</PHYSICAL-LINK-TYPE>
      </PROT-STACK>
    </PROT-STACKS>
    <COMPARAM-SUBSET ID=""CPSS_CAN_UDS"">
      <SHORT-NAME>CPSS_CAN_UDS</SHORT-NAME>
      <COMPARAMS>
        <COMPARAM ID=""CP_Baudrate"" PARAM-CLASS=""BUS"">
          <SHORT-NAME>CP_Baudrate</SHORT-NAME>
          <PHYSICAL-DEFAULT-VALUE>500000</PHYSICAL-DEFAULT-VALUE>
          <DESC><P>Nominal CAN bus bit rate in bps.</P></DESC>
        </COMPARAM>
        <COMPARAM ID=""CP_P2Max"" PARAM-CLASS=""TIMING"">
          <SHORT-NAME>CP_P2Max</SHORT-NAME>
          <PHYSICAL-DEFAULT-VALUE>50</PHYSICAL-DEFAULT-VALUE>
          <DESC><P>Default P2Max server response timeout (ms).</P></DESC>
        </COMPARAM>
        <COMPARAM ID=""CP_P2StarMax"" PARAM-CLASS=""TIMING"">
          <SHORT-NAME>CP_P2StarMax</SHORT-NAME>
          <PHYSICAL-DEFAULT-VALUE>5000</PHYSICAL-DEFAULT-VALUE>
          <DESC><P>Enhanced P2*Max response pending timeout (ms).</P></DESC>
        </COMPARAM>
        <COMPARAM ID=""CP_S3Client"" PARAM-CLASS=""TIMING"">
          <SHORT-NAME>CP_S3Client</SHORT-NAME>
          <PHYSICAL-DEFAULT-VALUE>2000</PHYSICAL-DEFAULT-VALUE>
          <DESC><P>TesterPresent keep-alive interval S3Client (ms).</P></DESC>
        </COMPARAM>
        <COMPARAM ID=""CP_CanFuncReqId"" PARAM-CLASS=""ADDRESSING"">
          <SHORT-NAME>CP_CanFuncReqId</SHORT-NAME>
          <PHYSICAL-DEFAULT-VALUE>0x7DF</PHYSICAL-DEFAULT-VALUE>
          <DESC><P>Standard 11-bit functional broadcast diagnostic CAN ID.</P></DESC>
        </COMPARAM>
      </COMPARAMS>
    </COMPARAM-SUBSET>
  </COMPARAM-SPEC>

  <DIAG-LAYER-CONTAINER ID=""DLC_PR_UDSonCAN"">
    <SHORT-NAME>DLC_PR_UDSonCAN</SHORT-NAME>
    <LONG-NAME>Protocol Container for ISO 14229 UDS on CAN</LONG-NAME>
    <PROTOCOLS>
      <PROTOCOL ID=""PR_UDSonCAN"">
        <SHORT-NAME>PR_UDSonCAN</SHORT-NAME>
        <LONG-NAME>ISO 14229 Unified Diagnostic Services on ISO 15765-2 CAN</LONG-NAME>
        <DESC><P>Protocol layer defining UDS-on-CAN timings, STmin, BS, and addressing defaults.</P></DESC>
        <COMPARAM-SPEC-REF ID-REF=""CPS_ISO_15765_UDS"" DOCREF=""CPS_ISO_15765_UDS"" DOCTYPE=""COMPARAM-SPEC""/>
        <PROT-STACK-SNREF SHORT-NAME=""PS_UDS_on_CAN""/>
        <COMPARAM-REFS>
          <COMPARAM-REF ID-REF=""CP_FunctionalRequestCanId"">
            <SHORT-NAME>CP_FunctionalRequestCanId</SHORT-NAME>
            <SIMPLE-VALUE>0x7DF</SIMPLE-VALUE>
            <UNIT>Hex</UNIT>
            <DESC><P>Functional broadcast CAN Request ID (OBD/UDS).</P></DESC>
          </COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_STminTx"">
            <SHORT-NAME>CP_STminTx</SHORT-NAME>
            <SIMPLE-VALUE>0</SIMPLE-VALUE>
            <UNIT>ms</UNIT>
            <DESC><P>ISO-TP Minimum Separation Time between Consecutive Frames.</P></DESC>
          </COMPARAM-REF>
        </COMPARAM-REFS>
      </PROTOCOL>
    </PROTOCOLS>
  </DIAG-LAYER-CONTAINER>
</ODX>";
    }

    /// <summary>
    /// Generates an ISO 22901-1 ECU-SHARED-DATA file (Units, DOPs, and standard ISO 14229-1 control/fault services).
    /// </summary>
    public static string CreateSampleOdxSharedDataXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DIAG-LAYER-CONTAINER ID=""DLC_SD_15765_3"">
    <SHORT-NAME>DLC_SD_15765_3</SHORT-NAME>
    <LONG-NAME>Shared Standard Units, DOPs, and ISO 14229 / 15765-3 Services</LONG-NAME>
    <ECU-SHARED-DATAS>
      <ECU-SHARED-DATA ID=""SD_15765_3_Services"">
        <SHORT-NAME>SD_15765_3_Services</SHORT-NAME>
        <LONG-NAME>Standard ISO 15765-3 / UDS Shared Services &amp; Data Dictionary</LONG-NAME>
        <DESC><P>Cross-ECU shared diagnostic layer providing standard session, security, tester present, fault clearing, units, and common DOPs.</P></DESC>

        <DIAG-DATA-DICTIONARY-SPEC>
          <UNIT-SPEC>
            <UNITS>
              <UNIT ID=""UNIT_RPM""><SHORT-NAME>rpm</SHORT-NAME><DISPLAY-NAME>rpm</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_DEGC""><SHORT-NAME>degC</SHORT-NAME><DISPLAY-NAME>°C</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_KMH""><SHORT-NAME>km_h</SHORT-NAME><DISPLAY-NAME>km/h</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_VOLT""><SHORT-NAME>volt</SHORT-NAME><DISPLAY-NAME>V</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_KPA""><SHORT-NAME>kPa</SHORT-NAME><DISPLAY-NAME>kPa</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_PCT""><SHORT-NAME>percent</SHORT-NAME><DISPLAY-NAME>%</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_NM""><SHORT-NAME>Nm</SHORT-NAME><DISPLAY-NAME>Nm</DISPLAY-NAME></UNIT>
              <UNIT ID=""UNIT_KOHM""><SHORT-NAME>kOhm</SHORT-NAME><DISPLAY-NAME>kOhm</DISPLAY-NAME></UNIT>
            </UNITS>
          </UNIT-SPEC>

          <DATA-OBJECT-PROPS>
            <DATA-OBJECT-PROP ID=""DOP_VIN"" CATEGORY=""IDENTICAL"">
              <SHORT-NAME>DOP_VIN</SHORT-NAME>
              <LONG-NAME>Vehicle Identification Number (ISO 3779)</LONG-NAME>
              <COMPU-METHOD><CATEGORY>IDENTICAL</CATEGORY></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_ASCIISTRING"" BIT-LENGTH=""136"" />
            </DATA-OBJECT-PROP>

            <DATA-OBJECT-PROP ID=""DOP_SW_VERSION"" CATEGORY=""IDENTICAL"">
              <SHORT-NAME>DOP_SW_VERSION</SHORT-NAME>
              <LONG-NAME>Application Software Version String</LONG-NAME>
              <COMPU-METHOD><CATEGORY>IDENTICAL</CATEGORY></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_ASCIISTRING"" BIT-LENGTH=""128"" />
            </DATA-OBJECT-PROP>
          </DATA-OBJECT-PROPS>
        </DIAG-DATA-DICTIONARY-SPEC>

        <DIAG-COMMS>
          <DIAG-SERVICE ID=""SVC_SD_SessionControl"" SEMANTIC=""SESSION"" ADDRESSING=""PHYSICAL-AND-FUNCTIONAL"">
            <SHORT-NAME>DiagnosticSessionControl</SHORT-NAME>
            <LONG-NAME>UDS Service 0x10 - Diagnostic Session Control</LONG-NAME>
            <DESC><P>Switches ECU between default (0x01), programming (0x02), and extended (0x03) diagnostic sessions.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_DiagnosticSessionControl</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x10</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" xsi:type=""VALUE"">
                  <SHORT-NAME>SessionType</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x01</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_SD_SessionControl"">
                <SHORT-NAME>PR_DiagnosticSessionControl</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                    <SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x50</CODED-VALUE>
                  </PARAM>
                  <PARAM SEMANTIC=""SUBFUNCTION"" xsi:type=""VALUE"">
                    <SHORT-NAME>SessionEcho</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION>
                  </PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_SD_SecurityAccess"" SEMANTIC=""SECURITY"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>SecurityAccess</SHORT-NAME>
            <LONG-NAME>UDS Service 0x27 - Security Access (Seed &amp; Key)</LONG-NAME>
            <DESC><P>Unlocks protected calibration/programming functions using seed/key challenge-response.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_SecurityAccess</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x27</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" xsi:type=""VALUE"">
                  <SHORT-NAME>SecurityLevel</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x01</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_SD_SecurityAccess"">
                <SHORT-NAME>PR_SecurityAccess</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                    <SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x67</CODED-VALUE>
                  </PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_SD_TesterPresent"" SEMANTIC=""TESTER-PRESENT"" ADDRESSING=""PHYSICAL-AND-FUNCTIONAL"">
            <SHORT-NAME>TesterPresent</SHORT-NAME>
            <LONG-NAME>UDS Service 0x3E - Tester Present</LONG-NAME>
            <DESC><P>Maintains active non-default diagnostic session timer S3Server.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_TesterPresent</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x3E</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" xsi:type=""VALUE"">
                  <SHORT-NAME>SubFunction</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x00</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_SD_TesterPresent"">
                <SHORT-NAME>PR_TesterPresent</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                    <SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x7E</CODED-VALUE>
                  </PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_SD_ClearDtc"" SEMANTIC=""CLEAR-DTC"" ADDRESSING=""PHYSICAL-AND-FUNCTIONAL"">
            <SHORT-NAME>ClearDiagnosticInformation</SHORT-NAME>
            <LONG-NAME>UDS Service 0x14 - Clear Diagnostic Information</LONG-NAME>
            <DESC><P>Erases stored trouble codes and freeze frames from non-volatile DTC memory.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ClearDtc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x14</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"">
                  <SHORT-NAME>GroupOfDTC</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0xFFFFFF</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_SD_ClearDtc"">
                <SHORT-NAME>PR_ClearDtc</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                    <SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x54</CODED-VALUE>
                  </PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_SD_ReadDtc"" SEMANTIC=""READ-DTC"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDTCInformation</SHORT-NAME>
            <LONG-NAME>UDS Service 0x19 - Read DTC Information</LONG-NAME>
            <DESC><P>Reads stored and confirmed fault memory records by status mask.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadDtc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x19</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" xsi:type=""VALUE"">
                  <SHORT-NAME>ReportType</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x02</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_SD_ReadDtc"">
                <SHORT-NAME>PR_ReadDtc</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                    <SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x59</CODED-VALUE>
                  </PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>
        </DIAG-COMMS>
      </ECU-SHARED-DATA>
    </ECU-SHARED-DATAS>
  </DIAG-LAYER-CONTAINER>
</ODX>";
    }

    /// <summary>
    /// Generates an ISO 22901-1 multi-layer ODX-D file with FUNCTIONAL-GROUP, BASE-VARIANT, and concrete ECU-VARIANTs
    /// connected via PARENT-REFS (cross-file and intra-file inheritance) plus ECU-VARIANT-PATTERNS.
    /// </summary>
    public static string CreateSampleIsoLayeredOdxDXml(string ecuName = "EngineControlUnit")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DIAG-LAYER-CONTAINER ID=""DLC_{ecuName}"">
    <SHORT-NAME>DLC_{ecuName}</SHORT-NAME>
    <LONG-NAME>{ecuName} ISO 22901-1 Diagnostic Layer Container</LONG-NAME>

    <!-- 1. FUNCTIONAL GROUP -->
    <FUNCTIONAL-GROUPS>
      <FUNCTIONAL-GROUP ID=""FG_Powertrain"">
        <SHORT-NAME>FG_Powertrain</SHORT-NAME>
        <LONG-NAME>Powertrain Domain Functional Broadcast Services</LONG-NAME>
        <DESC><P>Broadcast diagnostic services addressed via functional CAN ID 0x7DF.</P></DESC>
        <PARENT-REFS>
          <PARENT-REF xsi:type=""PROTOCOL-REF"" ID-REF=""PR_UDSonCAN"" DOCREF=""DLC_PR_UDSonCAN"" DOCTYPE=""CONTAINER""/>
        </PARENT-REFS>
        <DIAG-COMMS>
          <DIAG-SERVICE ID=""SVC_FG_ObdReadiness"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""FUNCTIONAL"">
            <SHORT-NAME>OBD_ReadCurrentPowertrainData</SHORT-NAME>
            <LONG-NAME>SAE J1979 Service 0x01 - Functional Emissions Readiness &amp; Live PIDs</LONG-NAME>
            <DESC><P>Functional broadcast query for powertrain emissions readiness status.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_FG_ObdReadiness</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                  <SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x01</CODED-VALUE>
                </PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""VALUE"">
                  <SHORT-NAME>PID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x01</CODED-VALUE>
                </PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_FG_ObdReadiness"">
                <SHORT-NAME>PR_FG_ObdReadiness</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST"">
                    <SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x41</CODED-VALUE>
                  </PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>
        </DIAG-COMMS>
      </FUNCTIONAL-GROUP>
    </FUNCTIONAL-GROUPS>

    <!-- 2. BASE VARIANT (Common ECU Architecture inheriting Protocol, SharedData, FunctionalGroup) -->
    <BASE-VARIANTS>
      <BASE-VARIANT ID=""BV_{ecuName}"">
        <SHORT-NAME>BV_{ecuName}</SHORT-NAME>
        <LONG-NAME>Base Variant - {ecuName} Architecture</LONG-NAME>
        <DESC><P>Common BaseVariant for all {ecuName} hardware/calibration variants per ISO 22901-1.</P></DESC>
        <PARENT-REFS>
          <PARENT-REF xsi:type=""PROTOCOL-REF"" ID-REF=""PR_UDSonCAN"" DOCREF=""DLC_PR_UDSonCAN"" DOCTYPE=""CONTAINER""/>
          <PARENT-REF xsi:type=""ECU-SHARED-DATA-REF"" ID-REF=""SD_15765_3_Services"" DOCREF=""DLC_SD_15765_3"" DOCTYPE=""CONTAINER""/>
          <PARENT-REF xsi:type=""FUNCTIONAL-GROUP-REF"" ID-REF=""FG_Powertrain"" DOCREF=""DLC_{ecuName}"" DOCTYPE=""CONTAINER""/>
        </PARENT-REFS>

        <DIAG-DATA-DICTIONARY-SPEC>
          <DATA-OBJECT-PROPS>
            <DATA-OBJECT-PROP ID=""DOP_ENGINE_SPEED"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_EngineSpeed</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>0</V><V>0.25</V></COMPU-NUMERATOR><COMPU-DENOMINATOR><V>1</V></COMPU-DENOMINATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-REF ID-REF=""UNIT_RPM"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
            <DATA-OBJECT-PROP ID=""DOP_COOLANT_TEMP"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_CoolantTemp</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>-40</V><V>1</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""8"" />
              <UNIT-REF ID-REF=""UNIT_DEGC"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
            <DATA-OBJECT-PROP ID=""DOP_BATTERY_VOLTAGE"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_BatteryVoltage</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>0</V><V>0.001</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-REF ID-REF=""UNIT_VOLT"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
          </DATA-OBJECT-PROPS>

          <DTC-DOPS>
            <DTC-DOP ID=""DTC_DOP_BV"">
              <SHORT-NAME>DTC_DOP_BV</SHORT-NAME>
              <DTCS>
                <DTC ID=""DTC_P010013""><SHORT-NAME>DTC_P010013</SHORT-NAME><TROUBLE-CODE>65555</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>P0100-13</DISPLAY-TROUBLE-CODE><TEXT>Mass or Volume Air Flow 'A' Circuit - Circuit Open</TEXT><SEVERITY>Warning</SEVERITY></DTC>
                <DTC ID=""DTC_P011517""><SHORT-NAME>DTC_P011517</SHORT-NAME><TROUBLE-CODE>70935</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>P0115-17</DISPLAY-TROUBLE-CODE><TEXT>Engine Coolant Temperature Circuit - Circuit Voltage Above Threshold</TEXT><SEVERITY>Warning</SEVERITY></DTC>
                <DTC ID=""DTC_P030092""><SHORT-NAME>DTC_P030092</SHORT-NAME><TROUBLE-CODE>196754</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>P0300-92</DISPLAY-TROUBLE-CODE><TEXT>Random or Multiple Cylinder Misfire Detected - Performance or Incorrect Operation</TEXT><SEVERITY>Fatal</SEVERITY></DTC>
                <DTC ID=""DTC_U010087""><SHORT-NAME>DTC_U010087</SHORT-NAME><TROUBLE-CODE>12648583</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>U0100-87</DISPLAY-TROUBLE-CODE><TEXT>Lost Communication With ECM/PCM 'A' - Missing Message</TEXT><SEVERITY>Fatal</SEVERITY></DTC>
                <DTC ID=""DTC_B100016""><SHORT-NAME>DTC_B100016</SHORT-NAME><TROUBLE-CODE>9437206</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>B1000-16</DISPLAY-TROUBLE-CODE><TEXT>ECU Internal Electronic Failure - Circuit Voltage Below Threshold</TEXT><SEVERITY>Fatal</SEVERITY></DTC>
              </DTCS>
            </DTC-DOP>
          </DTC-DOPS>
        </DIAG-DATA-DICTIONARY-SPEC>

        <DIAG-COMMS>
          <DIAG-SERVICE ID=""SVC_BV_ReadVIN"" SEMANTIC=""IDENTIFICATION"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_VIN</SHORT-NAME>
            <LONG-NAME>Read Vehicle Identification Number (DID 0xF190)</LONG-NAME>
            <DESC><P>Retrieves 17-character ISO 3779 VIN from EEPROM/NVM.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_VIN</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0xF190</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_BV_ReadVIN"">
                <SHORT-NAME>PR_Read_VIN</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_VIN""><SHORT-NAME>VIN_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>136</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_BV_WriteVIN"" SEMANTIC=""IDENTIFICATION"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>WriteDataByIdentifier_VIN</SHORT-NAME>
            <LONG-NAME>Write Vehicle Identification Number (DID 0xF190)</LONG-NAME>
            <DESC><P>Programs 17-character VIN during End-of-Line manufacturing.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Write_VIN</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x2E</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0xF190</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_VIN""><SHORT-NAME>VIN_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>136</BIT-LENGTH></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_BV_WriteVIN"">
                <SHORT-NAME>PR_Write_VIN</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x6E</CODED-VALUE></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_BV_ReadSwVersion"" SEMANTIC=""IDENTIFICATION"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_SoftwareVersion</SHORT-NAME>
            <LONG-NAME>Read ECU Software Version (DID 0xF189)</LONG-NAME>
            <DESC><P>Reads stored ASCII software version used for variant matching.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_SwVersion</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0xF189</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_BV_ReadSwVersion"">
                <SHORT-NAME>PR_Read_SwVersion</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_SW_VERSION""><SHORT-NAME>SW_Version</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>128</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_BV_ReadEngineSpeed"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_EngineSpeed</SHORT-NAME>
            <LONG-NAME>Read Engine Rotational Speed (DID 0x010C)</LONG-NAME>
            <DESC><P>Current engine crankshaft speed in RPM.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_EngineSpeed</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x010C</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_BV_ReadEngineSpeed"">
                <SHORT-NAME>PR_Read_EngineSpeed</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_ENGINE_SPEED""><SHORT-NAME>EngineSpeed_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>16</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_BV_ReadCoolantTemp"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_CoolantTemperature</SHORT-NAME>
            <LONG-NAME>Read Engine Coolant Temperature (DID 0x0105)</LONG-NAME>
            <DESC><P>Current engine coolant sensor temperature.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_CoolantTemp</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x0105</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_BV_ReadCoolantTemp"">
                <SHORT-NAME>PR_Read_CoolantTemp</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_COOLANT_TEMP""><SHORT-NAME>CoolantTemp_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>8</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_BV_ReadBatteryVoltage"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_BatteryVoltage</SHORT-NAME>
            <LONG-NAME>Read ECU Supply Battery Voltage (DID 0x0142)</LONG-NAME>
            <DESC><P>Live Terminal 30 battery voltage telemetry.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_Read_BatteryVoltage</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x0142</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_BV_ReadBatteryVoltage"">
                <SHORT-NAME>PR_Read_BatteryVoltage</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_BATTERY_VOLTAGE""><SHORT-NAME>BatteryVoltage_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>16</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>
        </DIAG-COMMS>

        <COMPARAM-REFS>
          <COMPARAM-REF ID-REF=""CP_CanId""><SHORT-NAME>CP_CanId</SHORT-NAME><SIMPLE-VALUE>0x7E0</SIMPLE-VALUE><UNIT>Hex</UNIT><DESC><P>Physical TX Request CAN ID</P></DESC></COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_CanIdRx""><SHORT-NAME>CP_CanIdRx</SHORT-NAME><SIMPLE-VALUE>0x7E8</SIMPLE-VALUE><UNIT>Hex</UNIT><DESC><P>Physical RX Response CAN ID</P></DESC></COMPARAM-REF>
        </COMPARAM-REFS>
      </BASE-VARIANT>
    </BASE-VARIANTS>

    <!-- 3. ECU VARIANTS (Inheriting from BV_{ecuName} + adding concrete variant services & identification patterns) -->
    <ECU-VARIANTS>
      <!-- Variant A: Gasoline Turbo GDI -->
      <ECU-VARIANT ID=""EV_{ecuName}_Gasoline"">
        <SHORT-NAME>{ecuName}_Gasoline</SHORT-NAME>
        <LONG-NAME>{ecuName} 2.0L Turbo Gasoline Direct Injection Variant</LONG-NAME>
        <DESC><P>Concrete ECU Variant for Gasoline GDI powertrain calibration.</P></DESC>

        <PARENT-REFS>
          <PARENT-REF xsi:type=""BASE-VARIANT-REF"" ID-REF=""BV_{ecuName}"" DOCREF=""DLC_{ecuName}"" DOCTYPE=""CONTAINER""/>
        </PARENT-REFS>

        <ECU-VARIANT-PATTERNS>
          <ECU-VARIANT-PATTERN>
            <SHORT-NAME>Pat_Gasoline_SW</SHORT-NAME>
            <MATCHING-PARAMETERS>
              <MATCHING-PARAMETER>
                <EXPECTED-VALUE>EMS_GT26_GAS_V2</EXPECTED-VALUE>
                <DIAG-COMM-SNREF SHORT-NAME=""ReadDataByIdentifier_SoftwareVersion""/>
                <OUT-PARAM-IF-SNREF SHORT-NAME=""SW_Version""/>
              </MATCHING-PARAMETER>
            </MATCHING-PARAMETERS>
          </ECU-VARIANT-PATTERN>
        </ECU-VARIANT-PATTERNS>

        <DIAG-DATA-DICTIONARY-SPEC>
          <DATA-OBJECT-PROPS>
            <DATA-OBJECT-PROP ID=""DOP_FUEL_PRESSURE"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_FuelRailPressure</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>0</V><V>10</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-REF ID-REF=""UNIT_KPA"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
            <DATA-OBJECT-PROP ID=""DOP_THROTTLE_POS"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_ThrottlePos</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>0</V><V>0.392</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""8"" />
              <UNIT-REF ID-REF=""UNIT_PCT"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
            <DATA-OBJECT-PROP ID=""DOP_INJECTOR_CODE"" CATEGORY=""IDENTICAL"">
              <SHORT-NAME>DOP_InjectorCoding</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>IDENTICAL</CATEGORY></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_BYTEFIELD"" BIT-LENGTH=""64"" />
            </DATA-OBJECT-PROP>
          </DATA-OBJECT-PROPS>
          <DTC-DOPS>
            <DTC-DOP ID=""DTC_DOP_GAS"">
              <SHORT-NAME>DTC_DOP_GAS</SHORT-NAME>
              <DTCS>
                <DTC ID=""DTC_P008717""><SHORT-NAME>DTC_P008717</SHORT-NAME><TROUBLE-CODE>34583</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>P0087-17</DISPLAY-TROUBLE-CODE><TEXT>Fuel Rail/System Pressure Too Low (GDI High-Pressure Pump)</TEXT><SEVERITY>Fatal</SEVERITY></DTC>
              </DTCS>
            </DTC-DOP>
          </DTC-DOPS>
        </DIAG-DATA-DICTIONARY-SPEC>

        <DIAG-COMMS>
          <DIAG-SERVICE ID=""SVC_EV_ReadFuelPressure"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_FuelRailPressure</SHORT-NAME>
            <LONG-NAME>Read GDI Fuel Rail Pressure (DID 0x0123)</LONG-NAME>
            <DESC><P>Live high-pressure GDI rail pressure telemetry in kPa.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadFuelPressure</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x0123</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_EV_ReadFuelPressure"">
                <SHORT-NAME>PR_ReadFuelPressure</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_FUEL_PRESSURE""><SHORT-NAME>FuelRailPressure_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>16</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_EV_ReadThrottlePos"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_ThrottlePosition</SHORT-NAME>
            <LONG-NAME>Read Electronic Throttle Position (DID 0x0111)</LONG-NAME>
            <DESC><P>Live electronic throttle valve opening percentage.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadThrottlePos</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x0111</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_EV_ReadThrottlePos"">
                <SHORT-NAME>PR_ReadThrottlePos</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_THROTTLE_POS""><SHORT-NAME>ThrottlePosition_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>8</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_EV_WriteInjectorCoding"" SEMANTIC=""CODING"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>WriteDataByIdentifier_InjectorCoding</SHORT-NAME>
            <LONG-NAME>Write GDI Injector IMA Coding (DID 0xF1A2)</LONG-NAME>
            <DESC><P>Programs stored injector flow trim coding into ECU EEPROM.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_WriteInjectorCoding</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x2E</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0xF1A2</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_INJECTOR_CODE""><SHORT-NAME>InjectorIMA</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>64</BIT-LENGTH></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_EV_WriteInjectorCoding"">
                <SHORT-NAME>PR_WriteInjectorCoding</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x6E</CODED-VALUE></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_EV_RoutineCylinderTest"" SEMANTIC=""ROUTINE"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>RoutineControl_CylinderBalanceTest</SHORT-NAME>
            <LONG-NAME>UDS Service 0x31 - GDI Cylinder Contribution Balance Test</LONG-NAME>
            <DESC><P>Executes active diagnostic test routine for cylinder combustion balance.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_RoutineCylinderTest</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x31</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""SUBFUNCTION"" xsi:type=""VALUE""><SHORT-NAME>SubFunction</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x01</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""ID"" xsi:type=""CODED-CONST""><SHORT-NAME>RoutineIdentifier</SHORT-NAME><BYTE-POSITION>2</BYTE-POSITION><CODED-VALUE>0x0201</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_EV_RoutineCylinderTest"">
                <SHORT-NAME>PR_RoutineCylinderTest</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x71</CODED-VALUE></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>
        </DIAG-COMMS>
      </ECU-VARIANT>

      <!-- Variant B: Hybrid PHEV Powertrain -->
      <ECU-VARIANT ID=""EV_{ecuName}_Hybrid"">
        <SHORT-NAME>{ecuName}_Hybrid</SHORT-NAME>
        <LONG-NAME>{ecuName} Plug-In Hybrid (PHEV) Supervisory Variant</LONG-NAME>
        <DESC><P>Concrete ECU Variant for Hybrid / Electrified Powertrain.</P></DESC>

        <PARENT-REFS>
          <PARENT-REF xsi:type=""BASE-VARIANT-REF"" ID-REF=""BV_{ecuName}"" DOCREF=""DLC_{ecuName}"" DOCTYPE=""CONTAINER""/>
        </PARENT-REFS>

        <ECU-VARIANT-PATTERNS>
          <ECU-VARIANT-PATTERN>
            <SHORT-NAME>Pat_Hybrid_SW</SHORT-NAME>
            <MATCHING-PARAMETERS>
              <MATCHING-PARAMETER>
                <EXPECTED-VALUE>EMS_GT26_HEV_V2</EXPECTED-VALUE>
                <DIAG-COMM-SNREF SHORT-NAME=""ReadDataByIdentifier_SoftwareVersion""/>
                <OUT-PARAM-IF-SNREF SHORT-NAME=""SW_Version""/>
              </MATCHING-PARAMETER>
            </MATCHING-PARAMETERS>
          </ECU-VARIANT-PATTERN>
        </ECU-VARIANT-PATTERNS>

        <DIAG-DATA-DICTIONARY-SPEC>
          <DATA-OBJECT-PROPS>
            <DATA-OBJECT-PROP ID=""DOP_HV_SOC"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_HV_SOC</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>0</V><V>0.5</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""8"" />
              <UNIT-REF ID-REF=""UNIT_PCT"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
            <DATA-OBJECT-PROP ID=""DOP_EMOTOR_TORQUE"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_EMotorTorque</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>-500</V><V>0.5</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-REF ID-REF=""UNIT_NM"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
            <DATA-OBJECT-PROP ID=""DOP_HV_ISOLATION"" CATEGORY=""LINEAR"">
              <SHORT-NAME>DOP_HVIsolation</SHORT-NAME>
              <COMPU-METHOD><CATEGORY>LINEAR</CATEGORY><COMPU-INTERNAL-TO-PHYS><COMPU-SCALES><COMPU-SCALE><COMPU-RATIONAL-COEFFS><COMPU-NUMERATOR><V>0</V><V>1</V></COMPU-NUMERATOR></COMPU-RATIONAL-COEFFS></COMPU-SCALE></COMPU-SCALES></COMPU-INTERNAL-TO-PHYS></COMPU-METHOD>
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_UINT32"" BIT-LENGTH=""16"" />
              <UNIT-REF ID-REF=""UNIT_KOHM"" DOCREF=""DLC_SD_15765_3"" />
            </DATA-OBJECT-PROP>
          </DATA-OBJECT-PROPS>
          <DTC-DOPS>
            <DTC-DOP ID=""DTC_DOP_HEV"">
              <SHORT-NAME>DTC_DOP_HEV</SHORT-NAME>
              <DTCS>
                <DTC ID=""DTC_P0A1F00""><SHORT-NAME>DTC_P0A1F00</SHORT-NAME><TROUBLE-CODE>663344</TROUBLE-CODE><DISPLAY-TROUBLE-CODE>P0A1F-00</DISPLAY-TROUBLE-CODE><TEXT>Battery Energy Control Module High-Voltage Interlock Fault</TEXT><SEVERITY>Fatal</SEVERITY></DTC>
              </DTCS>
            </DTC-DOP>
          </DTC-DOPS>
        </DIAG-DATA-DICTIONARY-SPEC>

        <DIAG-COMMS>
          <DIAG-SERVICE ID=""SVC_HEV_ReadHvSoc"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_HV_BatterySOC</SHORT-NAME>
            <LONG-NAME>Read High-Voltage Battery State of Charge (DID 0x2001)</LONG-NAME>
            <DESC><P>Live traction battery State-of-Charge percentage.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadHvSoc</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x2001</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_HEV_ReadHvSoc"">
                <SHORT-NAME>PR_ReadHvSoc</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_HV_SOC""><SHORT-NAME>HV_SOC_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>8</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_HEV_ReadEMotorTorque"" SEMANTIC=""CURRENTDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_EMotorTorque</SHORT-NAME>
            <LONG-NAME>Read Electric Traction Motor Torque (DID 0x2002)</LONG-NAME>
            <DESC><P>Live electric drive motor actual torque in Nm.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadEMotorTorque</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0x2002</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_HEV_ReadEMotorTorque"">
                <SHORT-NAME>PR_ReadEMotorTorque</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_EMOTOR_TORQUE""><SHORT-NAME>EMotorTorque_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>16</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>

          <DIAG-SERVICE ID=""SVC_HEV_ReadIsolation"" SEMANTIC=""STOREDDATA"" ADDRESSING=""PHYSICAL"">
            <SHORT-NAME>ReadDataByIdentifier_HV_IsolationResistance</SHORT-NAME>
            <LONG-NAME>Read Last Recorded HV Isolation Resistance (DID 0xF1B5)</LONG-NAME>
            <DESC><P>Stored high-voltage chassis isolation measurement in kOhm.</P></DESC>
            <REQUEST>
              <SHORT-NAME>RQ_ReadIsolation</SHORT-NAME>
              <PARAMS>
                <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>SID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x22</CODED-VALUE></PARAM>
                <PARAM SEMANTIC=""DATA-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>DID</SHORT-NAME><BYTE-POSITION>1</BYTE-POSITION><CODED-VALUE>0xF1B5</CODED-VALUE></PARAM>
              </PARAMS>
            </REQUEST>
            <POS-RESPONSES>
              <POS-RESPONSE ID=""PR_HEV_ReadIsolation"">
                <SHORT-NAME>PR_ReadIsolation</SHORT-NAME>
                <PARAMS>
                  <PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><SHORT-NAME>ResponseSID</SHORT-NAME><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x62</CODED-VALUE></PARAM>
                  <PARAM SEMANTIC=""DATA"" xsi:type=""VALUE"" DOP-REF=""DOP_HV_ISOLATION""><SHORT-NAME>HV_Isolation_Data</SHORT-NAME><BYTE-POSITION>3</BYTE-POSITION><BIT-LENGTH>16</BIT-LENGTH></PARAM>
                </PARAMS>
              </POS-RESPONSE>
            </POS-RESPONSES>
          </DIAG-SERVICE>
        </DIAG-COMMS>
      </ECU-VARIANT>
    </ECU-VARIANTS>
  </DIAG-LAYER-CONTAINER>
</ODX>";
    }

    /// <summary>
    /// Generates an ISO 22901-1 ODX-V Vehicle Topology file connecting Vehicle Bus Links -> Logical Links -> BaseVariant &amp; Protocol.
    /// </summary>
    public static string CreateSampleOdxVXml(string ecuName = "EngineControlUnit", string vehicleModel = "BlueBinaries GT-2026")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <VEHICLE-INFO-SPEC ID=""VIS_GT2026"">
    <SHORT-NAME>VIS_{vehicleModel.Replace(" ", "_").Replace("-", "")}</SHORT-NAME>
    <LONG-NAME>Vehicle Information Specification for {vehicleModel}</LONG-NAME>
    <VEHICLE-INFORMATIONS>
      <VEHICLE-INFORMATION ID=""VI_GT2026"">
        <SHORT-NAME>VI_{vehicleModel.Replace(" ", "_").Replace("-", "")}</SHORT-NAME>
        <LONG-NAME>{vehicleModel}</LONG-NAME>
        <MODEL>{vehicleModel}</MODEL>
        <COMPANY>BlueBinaries Engineering</COMPANY>
        <MODEL-YEAR>2026</MODEL-YEAR>

        <PHYSICAL-VEHICLE-LINKS>
          <PHYSICAL-VEHICLE-LINK ID=""PVL_PT_CAN_500k"" TYPE=""ISO_11898_CAN_HS"">
            <SHORT-NAME>PVL_PT_CAN_500k</SHORT-NAME>
            <LONG-NAME>Powertrain High-Speed CAN Bus (500 kbps)</LONG-NAME>
            <DESC><P>Primary powertrain diagnostic CAN channel (ISO 15765-2).</P></DESC>
          </PHYSICAL-VEHICLE-LINK>
        </PHYSICAL-VEHICLE-LINKS>

        <LOGICAL-LINKS>
          <GATEWAY-LOGICAL-LINK ID=""LL_{ecuName}"">
            <SHORT-NAME>LL_{ecuName}</SHORT-NAME>
            <LONG-NAME>{ecuName} Logical Diagnostic Link</LONG-NAME>
            <PHYSICAL-VEHICLE-LINK-REF ID-REF=""PVL_PT_CAN_500k""/>
            <PROTOCOL-REF ID-REF=""PR_UDSonCAN"" DOCREF=""DLC_PR_UDSonCAN"" DOCTYPE=""CONTAINER""/>
            <BASE-VARIANT-REF ID-REF=""BV_{ecuName}"" DOCREF=""DLC_{ecuName}"" DOCTYPE=""CONTAINER""/>
            <FUNCTIONAL-GROUP-REF ID-REF=""FG_Powertrain"" DOCREF=""DLC_{ecuName}"" DOCTYPE=""CONTAINER""/>
            <LINK-COMPARAM-REFS>
              <LINK-COMPARAM-REF ID-REF=""CP_CanPhysReqId"">
                <SHORT-NAME>CP_CanPhysReqId</SHORT-NAME>
                <SIMPLE-VALUE>0x7E0</SIMPLE-VALUE>
              </LINK-COMPARAM-REF>
              <LINK-COMPARAM-REF ID-REF=""CP_CanPhysRespId"">
                <SHORT-NAME>CP_CanPhysRespId</SHORT-NAME>
                <SIMPLE-VALUE>0x7E8</SIMPLE-VALUE>
              </LINK-COMPARAM-REF>
              <LINK-COMPARAM-REF ID-REF=""CP_CanFuncReqId"">
                <SHORT-NAME>CP_CanFuncReqId</SHORT-NAME>
                <SIMPLE-VALUE>0x7DF</SIMPLE-VALUE>
              </LINK-COMPARAM-REF>
            </LINK-COMPARAM-REFS>
          </GATEWAY-LOGICAL-LINK>
        </LOGICAL-LINKS>
      </VEHICLE-INFORMATION>
    </VEHICLE-INFORMATIONS>
  </VEHICLE-INFO-SPEC>
</ODX>";
    }

    /// <summary>
    /// Creates a complete ISO 22901-1 &amp; ISO 22901-2 compliant .pdx archive package with index.xml (ABLOCKS),
    /// ODX-C (Protocols &amp; ComParamSpecs), ODX-E (Shared Services &amp; Units), ODX-D (FunctionalGroup, BaseVariant, ECU Variants),
    /// and ODX-V (Vehicle Topology &amp; Logical Links).
    /// </summary>
    public static string CreateSamplePdxFile(string targetPdxFilePath, string ecuName = "EngineControlUnit")
    {
        var dir = Path.GetDirectoryName(targetPdxFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(targetPdxFilePath))
        {
            File.Delete(targetPdxFilePath);
        }

        var indexXml = CreateSamplePdxIndexXml(ecuName);
        var odxCXml = CreateSampleOdxCXml();
        var odxEXml = CreateSampleOdxSharedDataXml();
        var odxDXml = CreateSampleIsoLayeredOdxDXml(ecuName);
        var odxVXml = CreateSampleOdxVXml(ecuName);

        using (var zipStream = new FileStream(targetPdxFilePath, FileMode.Create, FileAccess.Write))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            WriteZipEntry(archive, "index.xml", indexXml);
            WriteZipEntry(archive, "odx-c/Protocols_UDS.odx-c", odxCXml);
            WriteZipEntry(archive, "odx-e/Shared_UDS_Services.odx-e", odxEXml);
            WriteZipEntry(archive, $"odx-d/{ecuName}.odx-d", odxDXml);
            WriteZipEntry(archive, "odx-v/VehicleTopology.odx-v", odxVXml);
        }

        return targetPdxFilePath;
    }

    private static void WriteZipEntry(ZipArchive archive, string entryPath, string content)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
