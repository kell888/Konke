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

namespace Konke
{
    public static class Common
    {
        static TextBox auth_code;
        static string this_username;
        static string this_password;
        static bool auth;
        public static void AuthCode(string clientId, string callbackurl, WebBrowser browser, TextBox authcode, string username, string password)
        {
            auth_code = authcode;
            this_username = username;
            this_password = password;
            //string url = "https://kk.bigk2.com:8443/KOAuthDemeter/Alley/authorize?client_id=" + clientId + "&response_type=code&user=" + username + "&pwd=" + password;
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/authorize?client_id=" + clientId + "&response_type=code&redirect_uri=" + callbackurl;
            browser.Navigated += new WebBrowserNavigatedEventHandler(browser_Navigated);
            browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(browser_DocumentCompleted);
            auth = true;
            browser.Navigate(url);
        }

        static void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser browser = sender as WebBrowser;
            if (browser != null && auth)
            {
                IHTMLDocument2 doc = browser.Document.DomDocument as IHTMLDocument2;
                IHTMLElement user = doc.all.item("username", 0);
                IHTMLElement pwd = doc.all.item("password", 0);
                if (user != null && pwd != null)
                {
                    user.setAttribute("value", this_username);
                    pwd.setAttribute("value", this_password);
                    auth = false;
                    IHTMLElementCollection fs = doc.forms;
                    foreach (IHTMLFormElement f in fs)
                    {
                        f.submit();
                    }
                    MessageBox.Show("登录成功！");
                }
                else
                {
                    MessageBox.Show("登录失败，请重新登录！");
                }
            }
        }

        static void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            WebBrowser browser = sender as WebBrowser;
            if (browser != null)
            {
                string[] ss = browser.Url.Query.Split('=');
                if (ss.Length == 2 && ss[0].Equals("?code", StringComparison.InvariantCultureIgnoreCase))
                    auth_code.Text = ss[1];
            }
        }

        public static string AccessToken(string clientId, string clientsecret, string auth_code, string callbackurl)
        {
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/accessToken";
            Dictionary<string, string> postParams = new Dictionary<string,string>();
            postParams.Add("grant_type", "authorization_code");
            postParams.Add("client_id", clientId);
            postParams.Add("client_secret", clientsecret);
            postParams.Add("redirect_uri", callbackurl);
            postParams.Add("code", auth_code);
            string s = RequestUrl(url, Encoding.UTF8, "POST", "application/x-www-form-urlencoded", postParams);
            return GetJsonValue(s, "access_token");
        }

        public static string AccessToken2(string clientId, string clientsecret, string refreshtoken, string callbackurl)
        {
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/token";
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            postParams.Add("grant_type", "authorization_code");
            postParams.Add("client_id", clientId);
            postParams.Add("client_secret", clientsecret);
            postParams.Add("redirect_uri", callbackurl);
            postParams.Add("refresh_token", refreshtoken);
            string s = RequestUrl(url, Encoding.UTF8, "POST", "application/x-www-form-urlencoded", postParams);
            return GetJsonValue(s, "access_token");
        }

        [Obsolete("不再建议使用这个老api", true)]
        public static string UserId(string accesstoken, out string username)
        {
            username = "";
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/UserInfo";
            Dictionary<string, string> headers = new Dictionary<string,string>();
            headers.Add("Authorization", "Bearer " + accesstoken);
            string s = RequestUrl(url, Encoding.UTF8, "POST", "application/x-www-form-urlencoded", null, headers);
            //username = GetJsonValue(s, "username");
            //return GetJsonValue(s, "userid");
            object u = JsonConvert.DeserializeObject(s);
            JToken user = u as JToken;
            if (user != null)
            {
                username = user.SelectToken("username").ToString();
                return user.SelectToken("userid").ToString();
            }
            return "";
        }
        public static string UserId(string accesstoken, string username)
        {
            string url = "http://kk.bigk2.com:8080/KOAuthDemeter/User/queryUserId";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + accesstoken);
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            postParams.Add("username", username);
            string s = RequestUrl(url, Encoding.UTF8, "POST", "application/json", postParams, headers);
            object u = JsonConvert.DeserializeObject(s);
            JToken user = u as JToken;
            if (user != null)
            {
                return user.SelectToken("userid").ToString();
            }
            return "";
        }

        public static bool CheckAccessToken(string accesstoken, string username)
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
            return false;
        }

        public static List<MiniK> GetKList(string accesstoken, string userid)
        {
            List<MiniK> ks = new List<MiniK>();
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
                        ks.Add(new MiniK() { device_name = _device_name, device_mac = _device_mac, device_type = _device_type, user_id = _user_id, kid = _kid });
                    }
                }
            }
            //foreach (string e in js)
            //{
            //    string _device_name = GetJsonValue(e, "device_name");
            //    string _device_type = GetJsonValue(e, "device_type");
            //    string _user_id = GetJsonValue(e, "user_id");
            //    string _kid = GetJsonValue(e, "kid");
            //    ks.Add(new MiniK() { device_name = _device_name, device_type = _device_type, user_id = _user_id, kid = _kid });
            //}
            return ks;
        }

        public static bool DoSwitchK(string accesstoken, string userid, string kid, int openOrClose)
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
                //string result = GetJsonValue(s, "result");
                return result == "0";
            }
            return false;
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
        #region 弃用部分
        private static List<string> GetJsonValues(string json)
        {
            json = json.Trim();
            json = json.Trim(Environment.NewLine.ToCharArray());
            List<string> s=new List<string>();
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
                Stream newStream = myRequest.GetRequestStream();
                newStream.Write(rawdata, 0, rawdata.Length);
                newStream.Close();
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
            if (myResponse.StatusCode == HttpStatusCode.OK)
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
    }
}
