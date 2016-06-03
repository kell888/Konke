using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static string clientId = ConfigurationManager.AppSettings["clientId"];
        static string username = ConfigurationManager.AppSettings["username"];
        static string password = ConfigurationManager.AppSettings["password"];
        static string clientsecret = ConfigurationManager.AppSettings["clientsecret"];
        static string deviceMac = ConfigurationManager.AppSettings["deviceMac"];
        static string wifiPwd = ConfigurationManager.AppSettings["wifiPwd"];
        Konke.Controler control;
        List<Konke.MiniK> ks;

        private void Form1_Load(object sender, EventArgs e)
        {
            ks = new List<Konke.MiniK>();
            label3.Text = username;
            control = new Konke.Controler(clientId, clientsecret, username, password);
            textBox1.Text = control.Authcode;
            textBox2.Text = control.Accesstoken;
        }

        bool openOrClose;
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = control.Authcode;
            textBox2.Text = control.Accesstoken;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string access_token = textBox2.Text;
            string userid = label5.Text;
            if (!string.IsNullOrEmpty(access_token) && !string.IsNullOrEmpty(userid))
            {
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    if (listBox1.SelectedItems.Count > 0)
                    {
                        openOrClose = !openOrClose;
                        bool someOneFalse = false;
                        foreach (object o in listBox1.SelectedItems)
                        {
                            Konke.MiniK k = o as Konke.MiniK;
                            if (k != null)
                            {
                                bool f = control.DoSwitchK(access_token, userid, k.kid, openOrClose ? 1 : 0);
                                if (!f) someOneFalse = true;
                            }
                        }
                        if (someOneFalse)
                        {
                            MessageBox.Show("选定的设备中，至少有一个开关控制失败！");
                        }
                        else
                        {
                            MessageBox.Show("选定的设备全部" + (openOrClose ? "开启" : "关闭") + "成功！");
                        }
                        RefreshDeviceList();
                        button4.Text = button2.Text = openOrClose ? "关闭" : "开启";
                    }
                    else
                    {
                        MessageBox.Show("请先选定要操作的设备！");
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
            else
            {
                MessageBox.Show("请先登录及获取userid！");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string access_token = textBox2.Text;
            MessageBox.Show(control.CheckAccessToken(access_token, username) ? "AccessToken有效！" : "AccessToken失效！");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string access_token = textBox2.Text;
            string userid = control.UserId();
            if (userid != "")
            {
                label5.Text = userid;
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    if (listBox1.SelectedItems.Count > 0)
                    {
                        openOrClose = !openOrClose;
                        bool someOneFalse = false;
                        foreach (object o in listBox1.SelectedItems)
                        {
                            Konke.MiniK k = o as Konke.MiniK;
                            if (k != null)
                            {
                                bool f = control.Turn(k.kid, openOrClose);
                                if (!f) someOneFalse = true;
                            }
                        }
                        if (someOneFalse)
                        {
                            MessageBox.Show("选定的设备中，至少有一个开关控制失败！");
                        }
                        else
                        {
                            MessageBox.Show("选定的设备全部" + (openOrClose ? "开启" : "关闭") + "成功！");
                        }
                        RefreshDeviceList();
                        button2.Text = button4.Text = openOrClose ? "关闭" : "开启";
                    }
                    else
                    {
                        MessageBox.Show("请先选定要操作的设备！");
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
            else
            {
                MessageBox.Show("获取不到userid！");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string t = textBox1.Text.Trim();
            if (t != "")
            {
                string access_token = textBox2.Text;
                string userid = control.UserId(access_token);
                if (userid != "")
                    label5.Text = userid;
                else
                    label5.Text = "获取不到userid！";
            }
            else
            {
                MessageBox.Show("请先登录！");
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            RefreshDeviceList();
        }

        private void RefreshDeviceList()
        {
            string userid = label5.Text.Trim();
            if (userid == "")
                userid = label5.Text = control.UserId();
            this.Cursor = Cursors.WaitCursor;
            try
            {
                listBox1.Items.Clear();
                ks = control.GetKList(control.Accesstoken, userid);
                if (ks.Count > 0)
                {
                    foreach (Konke.MiniK k in ks)
                    {
                        listBox1.Items.Add(k);
                    }
                }
                else
                {
                    MessageBox.Show("当前用户尚未添加任何小K设备，或者APP端尚未上传账户配置！");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                Konke.MiniK k = listBox1.SelectedItem as Konke.MiniK;
                if (k != null)
                {
                    openOrClose = k.state == "开";
                    button2.Text = button4.Text = openOrClose ? "关闭" : "开启";
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string access_token = textBox2.Text;
            string userid = control.UserId();
            if (userid != "")
            {
                label5.Text = userid;
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    if (listBox1.SelectedItems.Count > 0)
                    {
                        openOrClose = !openOrClose;
                        bool someOneFalse = false;
                        foreach (object o in listBox1.SelectedItems)
                        {
                            Konke.MiniK k = o as Konke.MiniK;
                            if (k != null)
                            {
                                bool f = control.Turn(k.kid, openOrClose);
                                if (!f) someOneFalse = true;
                            }
                        }
                        if (someOneFalse)
                        {
                            MessageBox.Show("选定的设备中，至少有一个开关控制失败！");
                        }
                        else
                        {
                            MessageBox.Show("选定的设备全部" + (openOrClose ? "开启" : "关闭") + "成功！");
                        }
                        RefreshDeviceList2();
                        button2.Text = button4.Text = openOrClose ? "关闭" : "开启";
                    }
                    else
                    {
                        MessageBox.Show("请先选定要操作的设备！");
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
            else
            {
                MessageBox.Show("获取不到userid！");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            RefreshDeviceList2();
        }

        private void RefreshDeviceList2()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                listBox2.Items.Clear();
                byte[] dataBuff;
                bool f = Konke.ControlerExtensions.LanScanConfig(4096, out dataBuff);
                if (f)
                {
                    List<Konke.ScanResult> ks = Konke.ControlerExtensions.GetResultFromReplyData(dataBuff);
                    if (ks.Count > 0)
                    {
                        foreach (Konke.ScanResult k in ks)
                        {
                            listBox2.Items.Add(k);
                        }
                    }
                    else
                    {
                        MessageBox.Show("当前用户尚未添加任何小K设备，或者APP端尚未上传账户配置！");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex > -1)
            {
                Konke.MiniK k = listBox2.SelectedItem as Konke.MiniK;
                if (k != null)
                {
                    openOrClose = k.state == "开";
                    button8.Text = openOrClose ? "关闭" : "开启";
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            byte[] dataBuff;
            bool f = Konke.ControlerExtensions.LanInitConfig(deviceMac, wifiPwd, 4096, out dataBuff);
            MessageBox.Show("设备初始化" + (f ? "成功" : "失败"));
        }

        private string GetEnviromentInfo(Konke.PluginInfo pi)
        {
            string kid = pi.kid;
            List<Konke.EnviromentInfo> infos = control.GetEnviromentInfo(kid);
            StringBuilder sb = new StringBuilder();
            foreach (Konke.EnviromentInfo info in infos)
            {
                if (sb.Length == 0)
                {
                    sb.Append(info.hour);
                    sb.Append(Environment.NewLine + info.hour);
                    sb.Append(Environment.NewLine + info.illumination);
                    sb.Append(Environment.NewLine + info.temperature);
                    sb.Append(Environment.NewLine + info.humidity);
                }
                else
                {
                    sb.Append(Environment.NewLine + Environment.NewLine);
                    sb.Append(info.hour);
                    sb.Append(Environment.NewLine + info.hour);
                    sb.Append(Environment.NewLine + info.illumination);
                    sb.Append(Environment.NewLine + info.temperature);
                    sb.Append(Environment.NewLine + info.humidity);
                }
            }
            return sb.ToString();
        }

        private string GetRemoterInfo(Konke.Remoter r)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Konke.Order order in r.orders)
            {
                if (sb.Length == 0)
                {
                    sb.Append(order.action + ":" + order.order);
                }
                else
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(order.action + ":" + order.order);
                }
            }
            return sb.ToString();
        }

        private string GetACRemoterInfo(Konke.ACRemoter r)
        {
            return r.baseOrder + ":" + r.range;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            GetRemoters();
        }

        private void GetRemoters()
        {
            listBox3.Items.Clear();
            List<Konke.Remoter> list = control.GetRemoters(control.UserId());
            foreach (Konke.Remoter r in list)
            {
                listBox3.Items.Add(r);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            GetACRemoters();
        }

        private void GetACRemoters()
        {
            listBox3.Items.Clear();
            List<Konke.ACRemoter> list = control.GetACRemoters(control.UserId());
            foreach (Konke.ACRemoter acr in list)
            {
                listBox3.Items.Add(acr);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                Konke.MiniK device = listBox1.SelectedValue as Konke.MiniK;
                if (device != null)
                {
                    if (device.device_type == "2")//支持插件的设备类型
                    {
                        GetPlugins(device.kid);
                    }
                    else
                    {
                        MessageBox.Show("请先选定一个小K二代的设备！");
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选定一个设备！");
            }
        }

        private void GetPlugins(string kid)
        {
            listBox4.Items.Clear();
            List<Konke.PluginInfo> list = control.GetPlugins(kid);
            foreach (Konke.PluginInfo r in list)
            {
                listBox4.Items.Add(r);
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            ControlRemoter(textBox4.Text.Trim());
        }

        private void ControlRemoter(string order)
        {
            if (order == "")
            {
                MessageBox.Show("请输入控制命令！");
                textBox4.Focus();
                textBox4.SelectAll();
                return;
            }
            if (listBox3.SelectedIndex > -1)
            {
                Konke.Remoter r = listBox3.SelectedValue as Konke.Remoter;
                if (r != null)
                {
                    if (control.Remote(r, order))
                    {
                        MessageBox.Show("控制成功！");
                    }
                    else
                    {
                        MessageBox.Show("控制失败！");
                    }
                }
                else
                {
                    MessageBox.Show("请选择一个普通遥控器！");
                }
            }
            else
            {
                MessageBox.Show("请选择一个遥控器！");
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            ControlACRemoter(textBox5.Text.Trim());
        }

        private void ControlACRemoter(string order)
        {
            if (order == "")
            {
                MessageBox.Show("请输入控制命令！");
                textBox4.Focus();
                textBox4.SelectAll();
                return;
            }
            if (listBox3.SelectedIndex > -1)
            {
                Konke.ACRemoter r = listBox3.SelectedValue as Konke.ACRemoter;
                if (r != null)
                {
                    Konke.ACState state = Konke.ACState.FromString(order);
                    if (control.ACRemote(r, state))
                    {
                        MessageBox.Show("控制成功！");
                    }
                    else
                    {
                        MessageBox.Show("控制失败！");
                    }
                }
                else
                {
                    MessageBox.Show("请选择一个空调遥控器！");
                }
            }
            else
            {
                MessageBox.Show("请选择一个遥控器！");
            }
        }

        private void listBox3_MouseClick(object sender, MouseEventArgs e)
        {
            if (listBox3.SelectedIndex > -1)
            {
                Konke.IRemoter r = listBox3.SelectedValue as Konke.IRemoter;
                if (r != null)
                {
                    if (r is Konke.ACRemoter)
                    {
                        Konke.ACRemoter acr = r as Konke.ACRemoter;
                        textBox5.Text = acr.range.ToString();
                    }
                    else if (r is Konke.Remoter)
                    {
                        Konke.Remoter re = r as Konke.Remoter;
                        if (re.orders.Count > 0)
                        {
                            textBox4.Text = re.orders[0].order;
                        }
                    }
                }
            }
        }

        private void listBox4_MouseClick(object sender, MouseEventArgs e)
        {
            if (listBox4.SelectedIndex > -1)
            {
                Konke.PluginInfo pi = listBox4.SelectedValue as Konke.PluginInfo;
                if (pi != null)
                {
                    textBox3.Text = GetEnviromentInfo(pi);
                }
            }
        }
    }
}
