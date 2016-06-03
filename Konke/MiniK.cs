using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Konke
{
    public class MiniK
    {
        string _device_name;

        public string device_name
        {
            get { return _device_name; }
            set { _device_name = value; }
        }
        string _device_mac;

        public string device_mac
        {
            get { return _device_mac; }
            set { _device_mac = value; }
        }
        string _device_type;

        public string device_type
        {
            get { return _device_type; }
            set { _device_type = value; }
        }
        string _user_id;

        public string user_id
        {
            get { return _user_id; }
            set { _user_id = value; }
        }
        string _kid;

        public string kid
        {
            get { return _kid; }
            set { _kid = value; }
        }
        string _state;

        public string state
        {
            get { return _state; }
            set { _state = value; }
        }
        string _online;

        public string online
        {
            get { return _online; }
            set { _online = value; }
        }

        public override string ToString()
        {
            return device_name + ":" + state + "[type=" + device_type + "]{id=" + kid + "}" + online;
        }
    }

    public class EnviromentInfo
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime hour { get; set; }
        /// <summary>
        /// 光照（光照分为5级，对应1-5）
        /// </summary>
        public byte illumination { get; set; }
        /// <summary>
        /// 温度（单位为摄氏度）
        /// </summary>
        public float temperature { get; set; }
        /// <summary>
        /// 湿度（相对湿度%）
        /// </summary>
        public float humidity { get; set; }
    }

    public class ElectricityInfoMonth
    {
        public int year { get; set; }
        public int month { get; set; }
        public float electricity { get; set; }
    }

    public class ElectricityInfoDay
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public float electricity { get; set; }
    }

    public class ElectricityInfoHour
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public int hour { get; set; }
        public float electricity { get; set; }
    }

    public class PluginInfo
    {
        public string key { get; set; }
        public string kid { get; set; }
        public List<PluginType> module { get; set; }
    }

    public enum RemoteType
    {
        红外 = 1,
        射频 = 2
    }

    public enum PluginType
    {
        /// <summary>
        /// 环境插件
        /// </summary>
        tp_module,
        /// <summary>
        /// 人体感应插件
        /// </summary>
        rt_module,
        /// <summary>
        /// 红外插件
        /// </summary>
        ir_module,
        /// <summary>
        /// 射频插件
        /// </summary>
        rf_module,
        /// <summary>
        /// 烟感插件
        /// </summary>
        yg_module,
        /// <summary>
        /// 门磁插件
        /// </summary>
        vd_module
    }

    public class Order
    {
        public string order;
        public string action;
    }

    public class Remoter : IRemoter
    {
        public RemoteType rt { get; set; }
        public string kname { get; set; }
        public string kid { get; set; }
        public string userid { get; set; }
        public List<Order> orders { get; set; }
        public string GetOrderByAction(string action)
        {
            foreach (Order order in this.orders)
            {
                if (order.action.Equals(action, StringComparison.InvariantCultureIgnoreCase))
                {
                    return order.order;
                }
            }
            return "";
        }

        public override string ToString()
        {
            return "[" + rt.ToString() + "]" + kname;
        }
    }

    public class ACState
    {
        public byte turn;
        public byte mode;
        public byte speed;
        public byte temp;
        public static ACState Empty;

        public static ACState FromString(string str)
        {
            string[] args = str.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 4)
            {
                return new ACState() { turn = Convert.ToByte(args[0]), mode = Convert.ToByte(args[1]), speed = Convert.ToByte(args[2]), temp = Convert.ToByte(args[3]) };
            }
            return ACState.Empty;
        }

        public override string ToString()
        {
            return turn.ToString() + "." + mode.ToString() + "." + speed.ToString() + "." + temp.ToString();
        }
    }

    public class Range
    {
        public ACState from = ACState.Empty;
        public ACState to = ACState.Empty;

        public static Range Empty;

        public override string ToString()
        {
            return from + "-" + to;
        }
    }

    public class ACRemoter : IRemoter
    {
        public RemoteType rt { get; set; }
        public string kname { get; set; }
        public string kid { get; set; }
        public string userid { get; set; }
        public string baseOrder { get; set; }
        public Range range { get; set; }

        public override string ToString()
        {
            return "[" + rt.ToString() + "]" + kname;
        }
    }

    public interface IRemoter
    {
        RemoteType rt { get; set; }
        string kname { get; set; }
        string kid { get; set; }
        string userid { get; set; }
    }
}
