using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Konke
{
    public class ScanResult
    {
        string deviceMac;
        string devicePwd;
        string deviceIP;

        public string DeviceMac
        {
            get
            {
                return deviceMac;
            }
        }

        public string DevicePwd
        {
            get
            {
                return devicePwd;
            }
        }

        public string DeviceIP
        {
            get
            {
                return deviceIP;
            }
        }

        public ScanResult(string deviceMac, string devicePwd, string deviceIP)
        {
            this.deviceMac = deviceMac;
            this.devicePwd = devicePwd;
            this.deviceIP = deviceIP;
        }

        public override string ToString()
        {
            return this.deviceIP + "[" + this.deviceMac + "](" + this.deviceMac + ")";
        }
    }
}
