using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Konke
{
    public static class ControlerExtensions
    {
        [DllImport("KonkeLanApi.dll")]
        extern static int buildConfigData(string macPtr, string wifiPwd, int pwdLen, ref byte[] dataBuff, int buffSize);
        public static bool LanInitConfig(string macAddress, string wifiPassword, int buffSize, out byte[] dataBuff)
        {
            dataBuff = new byte[buffSize];
            int flag = buildConfigData(macAddress, wifiPassword, wifiPassword.Length, ref dataBuff, buffSize);
            if (flag == 0)
                return false;
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 15000);
            client.Send(dataBuff, buffSize, endpoint);
            return true;
        }

        [DllImport("KonkeLanApi.dll")]
        extern static int buildScanData(string timeFormateStr, ref byte[] dataBuff, int buffSize);
        public static bool LanScanConfig(int buffSize, out byte[] dataBuff)
        {
            string timeFormateStr = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            dataBuff = new byte[buffSize];
            int flag = buildScanData(timeFormateStr, ref dataBuff, buffSize);
            if (flag == 0)
                return false;
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 27431);
            client.Send(dataBuff, buffSize, endpoint);
            return true;
        }

        public static List<ScanResult> GetResultFromReplyData(byte[] data)
        {
            List<ScanResult> result = new List<ScanResult>();
            string s = Encoding.UTF8.GetString(data);
            object o = JsonConvert.DeserializeObject(s);
            JToken token = o as JToken;
            if (token != null)
            {
                JToken ds = token.SelectToken("datalist");
                List<JToken> es = GetChildren(ds);
                if (es.Count > 0)
                {
                    foreach (JToken e in es)
                    {
                        string deviceMac = GetJsonValue(e, "device_name");
                        string devicePwd = GetJsonValue(e, "device_mac");
                        string deviceIP = GetJsonValue(e, "device_type");
                        result.Add(new ScanResult(deviceMac, devicePwd, deviceIP));
                    }
                }
            }
            return result;
        }

        private static string GetJsonValue(JToken token, string name)
        {
            if (token != null)
            {
                JToken t = token.SelectToken(name);
                if (t != null)
                    return token.SelectToken(name).ToString();
            }
            return "";
        }

        private static List<JToken> GetChildren(JToken token)
        {
            List<JToken> tokens = new List<JToken>();
            JEnumerable<JToken> ts = token.Children();
            foreach (JToken t in ts)
            {
                tokens.Add(t);
            }
            return tokens;
        }

        [DllImport("KonkeLanApi.dll")]
        extern static int buildOpenRelayCmd(string deviceMac, string devicePwd, int pwdLen, ref byte[] output, int buffSize);
        public static bool LanOpen(ScanResult result, int buffSize, out byte[] dataBuff)
        {
            dataBuff = new byte[buffSize];
            int flag = buildOpenRelayCmd(result.DeviceMac, result.DevicePwd, result.DevicePwd.Length, ref dataBuff, buffSize);
            if (flag == 0)
                return false;
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(result.DeviceIP), 27431);
            client.Send(dataBuff, buffSize, endpoint);
            return true;
        }

        [DllImport("KonkeLanApi.dll")]
        extern static int buildCloseRelayCmd(string deviceMac, string devicePwd, int pwdLen, ref byte[] output, int buffSize);
        public static bool LanClose(ScanResult result, int buffSize, out byte[] dataBuff)
        {
            dataBuff = new byte[buffSize];
            int flag = buildCloseRelayCmd(result.DeviceMac, result.DevicePwd, result.DevicePwd.Length, ref dataBuff, buffSize);
            if (flag == 0)
                return false;
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(result.DeviceIP), 27431);
            client.Send(dataBuff, buffSize, endpoint);
            return true;
        }
    }
}
