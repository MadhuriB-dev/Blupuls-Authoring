using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace OdxDiagnosticParser.Samples;

/// <summary>
/// Utility to generate compliant, realistic sample ODX XML files and packaged PDX archives for testing and demonstration.
/// </summary>
public static class SampleOdxGenerator
{
    /// <summary>
    /// Generates a realistic Engine Control Unit (ECU) ODX-D XML string with UDS services, DIDs, DTCs, and DOPs.
    /// </summary>
    public static string CreateSampleOdxXml(string ecuName = "EngineControlUnit", string modelVersion = "2.2.0")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""{modelVersion}"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DIAG-LAYER-CONTAINER>
    <ECU-VARIANTS>
      <ECU-VARIANT ID=""EV_{ecuName}"">
        <SHORT-NAME>{ecuName}</SHORT-NAME>
        <LONG-NAME>Engine Management System - Powertrain Control Module</LONG-NAME>
        <DESC>
          <P>Diagnostic specification for 2.0L Turbocharged Gasoline Direct Injection ECU conforming to ISO 14229-1 (UDS).</P>
        </DESC>

        <DIAG-DATA-DICTIONARY-SPEC>
          <!-- DATA OBJECT PROPERTIES (DOP) -->
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
              <DIAG-CODED-TYPE BASE-DATA-TYPE=""A_ASCIISTRING"" BIT-LENGTH=""32"" />
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

          <!-- DIAGNOSTIC TROUBLE CODES (DTC) -->
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

        <!-- DIAGNOSTIC SERVICES (UDS 0x10, 0x14, 0x19, 0x22, 0x27, 0x2E, 0x3E) -->
        <DIAG-SERVICES>
          <!-- 0x10 DiagnosticSessionControl -->
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

          <!-- 0x27 SecurityAccess -->
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

          <!-- 0x22 ReadDataByIdentifier - VIN (0xF190) -->
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

          <!-- 0x2E WriteDataByIdentifier - VIN (0xF190) -->
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

          <!-- 0x22 ReadDataByIdentifier - Engine Speed (0x010C) -->
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

          <!-- 0x22 ReadDataByIdentifier - Coolant Temperature (0x0105) -->
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

          <!-- 0x22 ReadDataByIdentifier - Battery Voltage (0x0142) -->
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

          <!-- 0x14 ClearDiagnosticInformation -->
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

          <!-- 0x19 ReadDTCInformation -->
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

          <!-- 0x3E TesterPresent -->
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

        <!-- COMMUNICATION PARAMETERS -->
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
    /// Generates a realistic index.xml PDX manifest file.
    /// </summary>
    public static string CreateSamplePdxIndexXml(string ecuName = "EngineControlUnit", string vehicleModel = "BlueBinaries GT-2026")
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CATALOG MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <SHORT-NAME>PACK_{ecuName}_V1</SHORT-NAME>
  <VERSION>1.0.0</VERSION>
  <CREATOR>BlueBinaries Engineering Diagnostic Authoring Suite</CREATOR>
  <DATE>{DateTime.UtcNow:yyyy-MM-dd}</DATE>
  <DESC>Complete packaged diagnostic exchange archive for {vehicleModel} {ecuName}.</DESC>

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

  <CATALOG-ELEMENTS>
    <CATALOG-ELEMENT TYPE=""ODX-D"">
      <SHORT-NAME>{ecuName}_DiagnosticLayer</SHORT-NAME>
      <FILE-PATH>odx-d/{ecuName}.odx-d</FILE-PATH>
      <CHECKSUM>c4ca4238a0b923820dcc509a6f75849b</CHECKSUM>
      <FILE-SIZE>15360</FILE-SIZE>
    </CATALOG-ELEMENT>
    <CATALOG-ELEMENT TYPE=""ODX-C"">
      <SHORT-NAME>Protocols_UDS_CAN</SHORT-NAME>
      <FILE-PATH>odx-c/Protocols_UDS.odx-c</FILE-PATH>
      <CHECKSUM>c81e728d9d4c2f636f067f89cc14862c</CHECKSUM>
      <FILE-SIZE>4096</FILE-SIZE>
    </CATALOG-ELEMENT>
  </CATALOG-ELEMENTS>
</CATALOG>";
    }

    /// <summary>
    /// Generates a sample ODX-C (Diagnostic Communication / Protocol) XML.
    /// </summary>
    public static string CreateSampleOdxCXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""2.2.0"">
  <DIAG-LAYER-CONTAINER>
    <PROTOCOLS>
      <PROTOCOL ID=""PROT_ISO_14229_ON_CAN"">
        <SHORT-NAME>ISO_14229_UDS_ON_CAN</SHORT-NAME>
        <LONG-NAME>Unified Diagnostic Services on ISO 15765-2 CAN</LONG-NAME>
        <DESC><P>Protocol communication layer definition for 500kbps 11-bit CAN bus.</P></DESC>
        <COMPARAM-REFS>
          <COMPARAM-REF ID-REF=""CP_FunctionalRequestCanId"">
            <SHORT-NAME>CP_FunctionalRequestCanId</SHORT-NAME>
            <VALUE>0x7DF</VALUE>
            <UNIT>Hex</UNIT>
          </COMPARAM-REF>
          <COMPARAM-REF ID-REF=""CP_S3Client"">
            <SHORT-NAME>CP_S3Client</SHORT-NAME>
            <VALUE>2000</VALUE>
            <UNIT>ms</UNIT>
          </COMPARAM-REF>
        </COMPARAM-REFS>
      </PROTOCOL>
    </PROTOCOLS>
  </DIAG-LAYER-CONTAINER>
</ODX>";
    }

    /// <summary>
    /// Creates a complete sample .pdx (Zip) archive file at the specified target path.
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
        var odxDXml = CreateSampleOdxXml(ecuName);
        var odxCXml = CreateSampleOdxCXml();

        using (var zipStream = new FileStream(targetPdxFilePath, FileMode.Create, FileAccess.Write))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            // index.xml
            var indexEntry = archive.CreateEntry("index.xml", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(indexEntry.Open(), Encoding.UTF8))
            {
                writer.Write(indexXml);
            }

            // odx-d file
            var odxDEntry = archive.CreateEntry($"odx-d/{ecuName}.odx-d", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(odxDEntry.Open(), Encoding.UTF8))
            {
                writer.Write(odxDXml);
            }

            // odx-c file
            var odxCEntry = archive.CreateEntry("odx-c/Protocols_UDS.odx-c", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(odxCEntry.Open(), Encoding.UTF8))
            {
                writer.Write(odxCXml);
            }
        }

        return targetPdxFilePath;
    }
}
