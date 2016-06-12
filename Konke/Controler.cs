using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Windows.Forms;
using mshtml;
using Newtonsoft.Json.Linq;
using System.Security.Permissions;

namespace Konke
{
    /// <summary>
    /// 控客智能开关控制类
    /// </summary>
    //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]   
    //[System.Runtime.InteropServices.ComVisible(true)]
    public class Controler
    {
        string _authcode;

        public string Authcode
        {
            get { return _authcode; }
        }
        string _accesstoken;

        public string Accesstoken
        {
            get { return _accesstoken; }
        }
        string _username;
        string _password;
        string _clientId;
        string _clientSecret;
        bool auth;
        string callbackurl = "http://www.baidu.com";
        WebBrowser browser;
        volatile bool get;
        public delegate void NavigateHandler(string url);
        List<Remoter> remoters;
        List<ACRemoter> acremoters;

        public bool Get
        {
            get { return get; }
        }

        public void AuthCode(string clientId, string clientSecret, string username, string password)
        {
            _username = username;
            _password = password;
            _clientId = clientId;
            _clientSecret = clientSecret;
            //string url = "https://kk.bigk2.com:8443/KOAuthDemeter/Alley/authorize?client_id=" + clientId + "&response_type=code&user=" + username + "&pwd=" + password;
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/authorize?client_id=" + clientId + "&response_type=code&redirect_uri=" + callbackurl;
            auth = true;
            NavigateUrl(url);
            //Thread th = new Thread(new ParameterizedThreadStart(NavigateUrl));
            //th.SetApartmentState(ApartmentState.STA);//属性设置成单线程
            //th.IsBackground = true;
            //th.Start(url);
            //return th;
        }

        private void NavigateUrl(object o)
        {
            string url = o.ToString();
            browser = new WebBrowser();
            browser.ScriptErrorsSuppressed = true;
            browser.Navigated += new WebBrowserNavigatedEventHandler(browser_Navigated);
            browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(browser_DocumentCompleted);
            if (this.browser.InvokeRequired)
            {
                NavigateHandler handler = new NavigateHandler(NavigateUrl);
                this.browser.Invoke(handler, url);
            }
            else
            {
                this.browser.Navigate(url);
            }
        }

        void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser browser = sender as WebBrowser;
            if (browser != null && auth)
            {
                IHTMLDocument2 doc = browser.Document.DomDocument as IHTMLDocument2;
                IHTMLElement user = doc.all.item("username", 0);
                IHTMLElement pwd = doc.all.item("password", 0);
                if (user != null && pwd != null)
                {
                    get = false;
                    user.setAttribute("value", _username);
                    pwd.setAttribute("value", _password);
                    auth = false;
                    IHTMLElementCollection fs = doc.forms;
                    foreach (IHTMLFormElement f in fs)
                    {
                        f.submit();
                    }
                    //MessageBox.Show("登录成功！");
                }
                else
                {
                    //MessageBox.Show("登录失败，请重新登录！");
                }
            }
        }

        void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            WebBrowser browser = sender as WebBrowser;
            if (browser != null)
            {
                string[] ss = browser.Url.Query.Split('=');
                if (ss.Length == 2 && ss[0].Equals("?code", StringComparison.InvariantCultureIgnoreCase))
                {
                    _authcode = ss[1];
                    _accesstoken = AccessToken();
                    get = true;
                }
            }
        }

        public string AccessToken(string auth_code = null)
        {
            if (!string.IsNullOrEmpty(auth_code) || !string.IsNullOrEmpty(_authcode))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/accessToken";
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("grant_type", "authorization_code");
                postParams.Add("client_id", _clientId);
                postParams.Add("client_secret", _clientSecret);
                postParams.Add("redirect_uri", callbackurl);
                if (auth_code != null)
                    postParams.Add("code", auth_code);
                else
                    postParams.Add("code", _authcode);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/x-www-form-urlencoded", postParams);
                object o = JsonConvert.DeserializeObject(s);
                JToken t = o as JToken;
                if (t != null)
                {
                    return t.SelectToken("access_token").ToString();
                }
                return "";
            }
            else
            {
                AuthCode(_clientId, _clientSecret, _username, _password);
                //Thread th = AuthCode(_clientId, _clientSecret, _username, _password, this.auth_code);
                //th.Join();
            }
            return "";
        }

        public string AccessToken2(string refreshtoken)
        {
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/token";
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            postParams.Add("grant_type", "authorization_code");
            postParams.Add("client_id", _clientId);
            postParams.Add("client_secret", _clientSecret);
            postParams.Add("redirect_uri", callbackurl);
            postParams.Add("refresh_token", refreshtoken);
            string s = RequestUrl(url, Encoding.UTF8, "POST", "application/x-www-form-urlencoded", postParams);
            object o = JsonConvert.DeserializeObject(s);
            JToken t = o as JToken;
            if (t != null)
            {
                return t.SelectToken("access_token").ToString();
            }
            return "";
        }
        string userid;
        public string UserID
        {
            get
            {
                if (userid == null)
                    UserId();
                return userid;
            }
        }

        public string UserId(string accesstoken = null)
        {
            if (!string.IsNullOrEmpty(accesstoken) || !string.IsNullOrEmpty(_accesstoken))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/queryUserId";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(accesstoken))
                    headers.Add("Authorization", "Bearer " + accesstoken);
                else
                    headers.Add("Authorization", "Bearer " + _accesstoken);

                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("username", _username);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object u = JsonConvert.DeserializeObject(s);
                JToken user = u as JToken;
                if (user != null)
                {
                    userid = user.SelectToken("userid").ToString();
                    return userid;
                }
            }
            else
            {
                _accesstoken = AccessToken();
            }
            return "";
        }

        public bool CheckAccessToken(string accesstoken, string username)
        {
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(username))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/verificateAccessToken";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("username", username);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object c = JsonConvert.DeserializeObject(s);
                JToken check = c as JToken;
                if (check != null)
                {
                    string result = check.SelectToken("result").ToString();
                    return result == "0";
                }
            }
            return false;
        }

        public List<MiniK> GetKList(string accesstoken, string userid)
        {
            List<MiniK> ks = new List<MiniK>();
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(userid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/getKList";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                //string ss = GetJsonValue(s, "datalist");
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        JToken ds = token.SelectToken("datalist");
                        List<JToken> es = GetChildren(ds);
                        if (es.Count > 0)
                        {
                            foreach (JToken e in es)
                            {
                                string _device_name = GetJsonValue(e, "device_name");
                                string _device_mac = GetJsonValue(e, "device_mac");
                                string _device_type = GetJsonValue(e, "device_type");
                                string _user_id = GetJsonValue(e, "user_id");
                                string _kid = GetJsonValue(e, "kid");
                                string status = "关";
                                string _state;
                                if (GetKState(accesstoken, userid, _kid, out _state))
                                    status = _state == "open" ? "开" : "关";
                                string onLine = "在线";
                                string _online;
                                if (GetKOnlineState(accesstoken, userid, _kid, out _online))
                                    onLine = _online == "online" ? "在线" : "离线";
                                string t = "代";
                                if (_device_type == "1")
                                {
                                    t = "1代";
                                }
                                else if (_device_type == "2")
                                {
                                    t = "2代";
                                }
                                else if (_device_type == "3")
                                {
                                    t = "mini";
                                }
                                else if (_device_type == "4")
                                {
                                    t = "minPro";
                                }
                                ks.Add(new MiniK() { device_name = _device_name, device_mac = _device_mac, device_type = t, user_id = _user_id, kid = _kid, state = status, online = onLine });
                            }
                        }
                    }
                }
            }
            return ks;
        }

        public bool DoSwitchK(string accesstoken, string userid, string kid, int openOrClose)
        {
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KControl/doSwitchK";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                postParams.Add("key", openOrClose == 1 ? "open" : "close");
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    return result == "0";
                }
            }
            return false;
        }

        public bool GetKState(string accesstoken, string userid, string kid, out string state)
        {
            state = "close";
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getKState";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        state = token.SelectToken("data").ToString();
                        return true;
                    }
                }
            }
            return false;
        }

        public bool GetKOnlineState(string accesstoken, string userid, string kid, out string state)
        {
            state = "offline";
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getKOnlineStatus";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        state = token.SelectToken("data").ToString();
                        return true;
                    }
                }
            }
            return false;
        }

        private static List<JToken> GetChildren(JToken token)
        {
            List<JToken> tokens = new List<JToken>();
            if (token != null)
            {
                JEnumerable<JToken> ts = token.Children();
                foreach (JToken t in ts)
                {
                    tokens.Add(t);
                }
            }
            return tokens;
        }

        private static string GetJsonValue(JToken token, string name)
        {
            if (token != null)
            {
                JToken t = token.SelectToken(name);
                if (t != null)
                    return t.ToString();
            }
            return "";
        }

        #region 弃用部分
        [Obsolete("不再建议使用这个老api", true)]
        private static List<string> GetJsonValues(string json)
        {
            json = json.Trim();
            json = json.Trim(Environment.NewLine.ToCharArray());
            List<string> s= new List<string>();
            if (json.StartsWith("[") && json.EndsWith("]"))
            {
                json = json.Remove(0, 1);
                json = json.Remove(json.Length - 1, 1);
                json = json.Trim();
                json = json.Trim(Environment.NewLine.ToCharArray());
                string[] ss = json.Split("{".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string e in ss)
                {
                    string ee = e.TrimEnd(',');
                    ee = ee.TrimEnd('}');
                    s.Add(ee);
                }
            }
            return s;
        }

        [Obsolete("不再建议使用这个老api", true)]
        private static string GetJsonValue(string json, string fieldname)
        {
            string[] ss = json.Split(',');
            if (ss.Length > 0)
            {
                foreach (string s in ss)
                {
                    string[] se = s.Split(':');
                    if (se.Length == 2)
                    {
                        if (se[0] == "\"" + fieldname + "\"")
                        {
                            string val = se[1].TrimEnd('}');
                            if (val.StartsWith("\"") && val.EndsWith("\""))
                                val = val.Substring(1, val.Length - 2);
                            return val;
                        }
                    }
                }
            }
            return "";
        }
        #endregion
        private static string RequestUrl(string url, Encoding encoding, string method = "POST", string contentType = "application/json", Dictionary<string, string> postParams = null, Dictionary<string, string> headers = null)
        {
            string content = string.Empty;
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            if (!string.IsNullOrEmpty(method))
                myRequest.Method = method;
            else
                myRequest.Method = "POST";
            if (headers != null && headers.Count > 0)
            {
                foreach (string key in headers.Keys)
                {
                    myRequest.Headers.Add(key, headers[key]);
                }
            }
            if (!string.IsNullOrEmpty(contentType))
                myRequest.ContentType = contentType;
            else
                myRequest.ContentType = "application/json";
            myRequest.Accept = "application/json";
            if (myRequest.RequestUri.Scheme.ToLower() == "https")
            {
                //挂接验证服务端证书的回调
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(RemoteCertificateValidationCallback);
                //打开本地计算机下的个人证书存储区
                X509Store certStore = new System.Security.Cryptography.X509Certificates.X509Store(StoreName.My, StoreLocation.LocalMachine);
                certStore.Open(OpenFlags.ReadOnly);
                //获取本地主机名称作为证书查找的参数
                string findValue = Dns.GetHostName();
                //根据名称查找匹配的证书集合，这里注意最后一个参数，传true的话会找不到
                X509Certificate2Collection certCollection = certStore.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindByIssuerName, findValue, false);
                //将证书添加至客户端证书集合
                if (certCollection.Count > 0)
                {
                    myRequest.ClientCertificates.Add(certCollection[0]);
                }
                else
                {
                    throw new Exception("无效的证书，请确保证书存在且有效！");
                }
            }
            if (postParams != null)
            {
                string postData = null;
                if (contentType.ToLower() == "application/json")
                    postData = GetPostDataJsonString(postParams);
                else
                    postData = GetPostDataString(postParams);
                byte[] rawdata = encoding.GetBytes(postData);
                myRequest.ContentLength = rawdata.Length;
                try
                {
                    Stream newStream = myRequest.GetRequestStream();
                    newStream.Write(rawdata, 0, rawdata.Length);
                    newStream.Close();
                }
                catch (Exception e)
                {
                }
            }
            HttpWebResponse myResponse;
            try
            {
                myResponse = (HttpWebResponse)myRequest.GetResponse();
            }
            catch (WebException ex)
            {
                myResponse = (HttpWebResponse)ex.Response;
            }
            if (myResponse != null && myResponse.StatusCode == HttpStatusCode.OK)
            {
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                content = reader.ReadToEnd();
            }
            return content;
        }

        #region 下面的方法由RemoteCertificateValidationDelegate调用
        /// <summary>
        /// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation</param>
        /// <param name="certificate">The certificate used to authenticate the remote party</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate</param>
        /// <returns></returns>
        public static bool RemoteCertificateValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;

            #region Validated Message
            ////如果没有错就表示验证成功
            //if (sslPolicyErrors == SslPolicyErrors.None)
            //    return true;
            //else
            //{
            //    if ((SslPolicyErrors.RemoteCertificateNameMismatch & sslPolicyErrors) == SslPolicyErrors.RemoteCertificateNameMismatch)
            //    {
            //        string errMsg = "证书名称不匹配{0}" + sslPolicyErrors;
            //        Console.WriteLine(errMsg);
            //        throw new AuthenticationException(errMsg);
            //    }

            //    if ((SslPolicyErrors.RemoteCertificateChainErrors & sslPolicyErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
            //    {
            //        string msg = "";
            //        foreach (X509ChainStatus status in chain.ChainStatus)
            //        {
            //            msg += "status code ={0} " + status.Status;
            //            msg += "Status info = " + status.StatusInformation + " ";
            //        }
            //        string errMsg = "证书链错误{0}" + msg;
            //        Console.WriteLine(errMsg);
            //        throw new AuthenticationException(errMsg);
            //    }
            //    string errorMsg = "证书验证失败{0}" + sslPolicyErrors;
            //    Console.WriteLine(errorMsg);
            //    throw new AuthenticationException(errorMsg);
            //}
            #endregion
        }
        #endregion

        private static string GetPostDataString(Dictionary<string, string> postParams)
        {
            StringBuilder sb = new StringBuilder();
            if (postParams != null)
            {
                foreach (string key in postParams.Keys)
                {
                    if (sb.Length == 0)
                        sb.Append(key + "=" + postParams[key]);
                    else
                        sb.Append("&" + key + "=" + postParams[key]);
                }
            }
            return sb.ToString();
        }

        private static string GetPostDataJsonString(Dictionary<string, string> postParams)
        {
            StringBuilder sb = new StringBuilder();
            if (postParams != null)
            {
                sb.Append("{");
                foreach (string key in postParams.Keys)
                {
                    if (sb.Length == 1)
                        sb.Append("\"" + key + "\":\"" + postParams[key] + "\"");
                    else
                        sb.Append(",\"" + key + "\":\"" + postParams[key] + "\"");
                }
                sb.Append("}");
            }
            return sb.ToString();
        }

        public bool Turn(string kid, bool onOrOff = true)
        {
            string userid = UserID;
            if (userid != "")
            {
                return DoSwitchK(_accesstoken, userid, kid, onOrOff ? 1 : 0);
            }
            return false;
        }

        public Controler(string clientId, string clientSecret, string username, string password)
        {
            //Thread th = AuthCode(clientId, clientSecret, username, password, authcode, accesstoken);
            //th.Join();
            AuthCode(clientId, clientSecret, username, password);
            while (!get)
            {
                Application.DoEvents();
            }
            remoters = new List<Remoter>();
            acremoters = new List<ACRemoter>();
        }

        ~Controler()
        {
            try
            {
                if (browser != null && !browser.IsDisposed)
                    browser.Dispose();
            }
            catch (Exception e)
            {
                //MessageBox.Show("释放网页控件时出错：" + e.Message);
            }
        }

        public List<ElectricityInfoMonth> GetElectricityByMonth(string kid)
        {
            List<ElectricityInfoMonth> electricities = new List<ElectricityInfoMonth>();
            string userid = UserID;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getKElectricityByMonth";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        JToken ds = token.SelectToken("datalist");
                        List<JToken> es = GetChildren(ds);
                        if (es.Count > 0)
                        {
                            foreach (JToken e in es)
                            {
                                string date = GetJsonValue(e, "month");
                                string[] _month = date.Split('-');
                                if (_month.Length == 2)
                                {
                                    ElectricityInfoMonth info = new ElectricityInfoMonth();
                                    info.year = Convert.ToInt32(_month[0]);
                                    info.month = Convert.ToInt32(_month[1]);
                                    info.electricity = Convert.ToSingle(GetJsonValue(e, "electricity"));
                                    electricities.Add(info);
                                }
                            }
                        }
                    }
                }
            }
            return electricities;
        }

        public List<ElectricityInfoDay> GetElectricityByDay(string kid)
        {
            List<ElectricityInfoDay> electricities = new List<ElectricityInfoDay>();
            string userid = UserID;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getKElectricityByDay";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        JToken ds = token.SelectToken("datalist");
                        List<JToken> es = GetChildren(ds);
                        if (es.Count > 0)
                        {
                            foreach (JToken e in es)
                            {
                                string date = GetJsonValue(e, "day");
                                string[] _day = date.Split('-');
                                if (_day.Length == 3)
                                {
                                    ElectricityInfoDay info = new ElectricityInfoDay();
                                    info.year = Convert.ToInt32(_day[0]);
                                    info.month = Convert.ToInt32(_day[1]);
                                    info.day = Convert.ToInt32(_day[2]);
                                    info.electricity = Convert.ToSingle(GetJsonValue(e, "electricity"));
                                    electricities.Add(info);
                                }
                            }
                        }
                    }
                }
            }
            return electricities;
        }

        public List<ElectricityInfoHour> GetElectricityByHour(string kid)
        {
            List<ElectricityInfoHour> electricities = new List<ElectricityInfoHour>();
            string userid = UserID;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getKElectricityByHour";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        JToken ds = token.SelectToken("datalist");
                        List<JToken> es = GetChildren(ds);
                        if (es.Count > 0)
                        {
                            foreach (JToken e in es)
                            {
                                string date = GetJsonValue(e, "hour");
                                string[] month = date.Split('-');
                                if (month.Length == 4)
                                {
                                    ElectricityInfoHour info = new ElectricityInfoHour();
                                    info.year = Convert.ToInt32(month[0]);
                                    info.month = Convert.ToInt32(month[1]);
                                    info.day = Convert.ToInt32(month[2]);
                                    info.hour = Convert.ToInt32(month[3]);
                                    info.electricity = Convert.ToSingle(GetJsonValue(e, "electricity"));
                                    electricities.Add(info);
                                }
                            }
                        }
                    }
                }
            }
            return electricities;
        }

        public List<EnviromentInfo> GetEnviromentInfo(string kid)
        {
            List<EnviromentInfo> infos = new List<EnviromentInfo>();
            string userid = UserID;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getEnvironmentInfo";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        JToken ds = token.SelectToken("datalist");
                        List<JToken> es = GetChildren(ds);
                        if (es.Count > 0)
                        {
                            foreach (JToken e in es)
                            {
                                string _hour = GetJsonValue(e, "hour");
                                string[] date = _hour.Split('-');
                                if (date.Length == 4)
                                {
                                    string day = date[0] + "-" + date[1] + "-" + date[2];
                                    string _illumination = GetJsonValue(e, "illumination");
                                    string _temperature = GetJsonValue(e, "temperature");
                                    string _humidity = GetJsonValue(e, "humidity");
                                    EnviromentInfo info = new EnviromentInfo() { hour = DateTime.Parse(day + " " + date[3] + ":00:00"), illumination = Convert.ToByte(_illumination), temperature = Convert.ToSingle(_temperature), humidity = Convert.ToSingle(_humidity) };
                                    infos.Add(info);
                                }
                            }
                        }
                    }
                }
            }
            return infos;
        }

        public List<IRemoter> GetIRemoters(string userid)
        {
            List<IRemoter> irs = new List<IRemoter>();
            List<Remoter> rs = GetRemoters(userid);
            irs.AddRange(rs);
            List<ACRemoter> ars = GetACRemoters(userid);
            irs.AddRange(ars);
            return irs;
        }

        public List<PluginInfo> GetPlugins(string kid)
        {
            List<PluginInfo> infos = new List<PluginInfo>();
            string userid = UserID;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KInfo/getSingleKStatus";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        JToken ds = token.SelectToken("datalist.module");
                        List<JToken> es = GetChildren(ds);
                        if (es.Count > 0)
                        {
                            foreach (JToken e in es)
                            {
                                if (e != null)
                                {
                                    string t = e.ToString();
                                    PluginType pt = (PluginType)Enum.Parse(typeof(PluginType), t);
                                    PluginInfo info = new PluginInfo();
                                    info.kid = kid;
                                    info.key = token.SelectToken("datalist.key").ToString();
                                    info.module = pt;
                                    infos.Add(info);
                                }
                            }
                        }
                    }
                }
            }
            return infos;
        }

        private string GetJsonPropertyValue(JToken token, string name)
        {
            if (token != null)
            {
                foreach (JToken t in token.Children())
                {
                    JProperty p = t as JProperty;
                    if (p != null && p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (p.Value != null)
                            return p.Value.ToString();
                        else
                            return "";
                    }
                }
            }
            return "";
        }

        public List<Remoter> GetRemoters(string userid)
        {
            if (remoters == null)
                remoters = new List<Remoter>();
            else
                remoters.Clear();
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/getGeneralRemoteList";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                //string ss = GetJsonValue(s, "datalist");
                JToken token = o as JToken;
                if (token != null)
                {
                    JToken ds = token.SelectToken("datalist");
                    List<JToken> es = GetChildren(ds);
                    if (es.Count > 0)
                    {
                        int index = 0;
                        foreach (JToken e in es)
                        {
                            string remoteType = GetJsonValue(e, "remoteType");
                            string userId = GetJsonValue(e, "userId");
                            List<Order> _orders = new List<Order>();
                            List<JToken> ess = GetChildren(e);
                            JToken orders = ess.Find(a=> a.Path=="datalist["+index+"].orders");
                            foreach (JToken r in orders.Children())
                            {
                                foreach (JToken i in r.Children())
                                {
                                    string _action = GetJsonValue(i, "action");
                                    string _order = GetJsonValue(i, "order");
                                    Order order = new Order() { action = _action, order = _order };
                                    _orders.Add(order);
                                }
                            }
                            string _kname = GetJsonValue(e, "kname");
                            string _kid = GetJsonValue(e, "kid");
                            RemoteType t = RemoteType.红外;
                            if (remoteType == "1")
                            {
                                t = RemoteType.红外;
                            }
                            else if (remoteType == "2")
                            {
                                t = RemoteType.射频;
                            }
                            remoters.Add(new Remoter() { rt = t, userid = userId, kname = _kname, orders = _orders, kid = _kid });
                            index++;
                        }
                    }
                }
            }
            return remoters;
        }

        public List<ACRemoter> GetACRemoters(string userid)
        {
            if (acremoters == null)
                acremoters = new List<ACRemoter>();
            else
                acremoters.Clear();
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/getAirConditionerRemoteList";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                //string ss = GetJsonValue(s, "datalist");
                JToken token = o as JToken;
                if (token != null)
                {
                    JToken ds = token.SelectToken("datalist");
                    List<JToken> es = GetChildren(ds);
                    if (es.Count > 0)
                    {
                        foreach (JToken e in es)
                        {
                            string remoteType = GetJsonValue(e, "remoteType");
                            string userId = GetJsonValue(e, "userId");
                            string ran = GetJsonValue(e, "range");
                            string[] rans = ran.Split('-');
                            Range _range = Range.Empty;
                            if (rans.Length == 2)
                            {
                                string _from = rans[0];
                                string _to = rans[1];
                                _range = new Range() { from = ACState.FromString(_from), to = ACState.FromString(_to) };
                            }
                            string _baseOrder = GetJsonValue(e, "baseOrder");
                            string _kname = GetJsonValue(e, "kname");
                            string _kid = GetJsonValue(e, "kid");
                            RemoteType t = RemoteType.红外;
                            if (remoteType == "1")
                            {
                                t = RemoteType.红外;
                            }
                            else if (remoteType == "2")
                            {
                                t = RemoteType.射频;
                            }
                            acremoters.Add(new ACRemoter() { rt = t, userid = userId, kname = _kname, kid = _kid, range = _range, baseOrder = _baseOrder });
                        }
                    }
                }
            }
            return acremoters;
        }

        public bool Remote(string userid, string kid, RemoteType remoteType, string order)
        {
            string rt = Convert.ToString((int)remoteType);
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid) && !string.IsNullOrEmpty(rt) && !string.IsNullOrEmpty(order))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KControl/sendGeneralRemoteOrder";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                postParams.Add("remoteType", rt);
                postParams.Add("order", order);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    return result == "0";
                }
            }
            return false;
        }

        public bool ACRemote(string userid, string kid, RemoteType remoteType, string baseOrder, ACState state)
        {
            string rt = Convert.ToString((int)remoteType);
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid) && !string.IsNullOrEmpty(rt) && !string.IsNullOrEmpty(baseOrder) && state != null)
            {
                //GetACRemoters(userid);
                //ACRemoter or = acremoters.Find(a => a.kid.Equals(r.kid, StringComparison.InvariantCultureIgnoreCase) && a.kname.Equals(r.kname, StringComparison.InvariantCultureIgnoreCase));
                //if (or.state != state)
                //{
                    string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KControl/sendAirConditionerOrder";
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("Authorization", "Bearer " + _accesstoken);
                    Dictionary<string, string> postParams = new Dictionary<string, string>();
                    postParams.Add("userid", userid);
                    postParams.Add("kid", kid);
                    postParams.Add("remoteType", rt);
                    postParams.Add("baseOrder", baseOrder);
                    postParams.Add("extraOrder", state.ToString());
                    string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                    object o = JsonConvert.DeserializeObject(s);
                    JToken token = o as JToken;
                    if (token != null)
                    {
                        string result = token.SelectToken("result").ToString();
                        return result == "0";
                    }
                //}
            }
            return false;
        }

        public bool DoSwitchKLight(string kid, int openOrClose)
        {
            string accesstoken = this.Accesstoken;
            string userid = this.UserID;
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/switchKLight";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                postParams.Add("key", openOrClose == 1 ? "open" : "close");
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    return result == "0";
                }
            }
            return false;
        }

        public bool GetKLightInfo(string kid, out string state)
        {
            string accesstoken = this.Accesstoken;
            string userid = this.UserID;
            state = "close";
            if (!string.IsNullOrEmpty(accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/getKLightInfo";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    if (result == "0")
                    {
                        state = token.SelectToken("data").ToString();
                        return true;
                    }
                }
            }
            return false;
        }

        [Obsolete("禁用，请改用Remote")]
        public bool TurnRemoter(Remoter r)
        {
            string userid = r.userid;
            string kid = r.kid;
            int openOrClose = 0;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KControl/sendGeneralRemoteOrder";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                postParams.Add("key", openOrClose == 1 ? "open" : "close");
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    return result == "0";
                }
            }
            return false;
        }

        [Obsolete("禁用，请改用ACRemote")]
        public bool TurnACRemoter(ACRemoter r)
        {
            string userid = r.userid;
            string kid = r.kid;
            int openOrClose = 0;
            if (!string.IsNullOrEmpty(_accesstoken) && !string.IsNullOrEmpty(userid) && !string.IsNullOrEmpty(kid))
            {
                string url = "http://kk.bigk2.com:8080/KOAuthDemeter/KControl/sendAirConditionerRemoteOrder";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + _accesstoken);
                Dictionary<string, string> postParams = new Dictionary<string, string>();
                postParams.Add("userid", userid);
                postParams.Add("kid", kid);
                postParams.Add("key", openOrClose == 1 ? "open" : "close");
                string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
                object o = JsonConvert.DeserializeObject(s);
                JToken token = o as JToken;
                if (token != null)
                {
                    string result = token.SelectToken("result").ToString();
                    return result == "0";
                }
            }
            return false;
        }
    }
}
