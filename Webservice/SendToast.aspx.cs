using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Webservice
{
    public partial class SendToast : System.Web.UI.Page
    {
        private string packageSID = "ms-app://s-1-15-2-2049356196-473840743-1755438742-557431145-1749481629-2531920207-3842639452";
        private string clinetSecret = "TpuqfXpPbySrXWxiewgYAJIn2MkuSdN3";
        private string channelUri = "https://hk2.notify.windows.com/?token=AwYAAACP3LBleNqyaAfMxCMshRp%2bP8mnlbx9j78i%2fbRVURcBP%2fRWHF%2f6ydocL0ih9MkKmNMKCC7zLRHPUtwpAldx0vDqOwJndMrUnXcBHoHTDfzGoGrN8rvvP7Lw9PIjqDIhlKs%3d";
        private string accessToken = string.Empty;

        [DataContract]
        public class OAuthToken
        {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
        }

        OAuthToken GetOAuthTokenFromJson(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(OAuthToken));
                var oAuthToken = (OAuthToken)ser.ReadObject(ms);
                return oAuthToken;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Application["channelUri"] != null)
            {
                Application["channelUri"] = channelUri;
            }
            else
            {
                Application.Add("channelUri", channelUri);
            }

            if (Application["channelUri"] != null)
            {
                string aStrReq = Application["channelUri"] as string;
                string toast1 = "<?xml version=\"1.0\" encoding=\"utf-8\"?> ";
                string toast2 = @"<toast>
                        <visual>
                            <binding template=""ToastText01"">
                                <text id=""1"">Hello Push Notification Service!!</text>
                            </binding>
                        </visual>
                    </toast>";
                string xml = toast1 + toast2;

                Response.Write("Result: " + PostToCloud(aStrReq, xml));
            }
            else
            {
                Response.Write("Application 'channelUri=' has not been set yet");
            }
            Response.End();
        }
        protected string PostToCloud(string uri, string xml, string type = "wns/toast")
        {
            try
            {
                if (accessToken == "")
                {
                    GetAccessToken();
                }
                byte[] contentInBytes = Encoding.UTF8.GetBytes(xml);

                WebRequest webRequest = HttpWebRequest.Create(uri);
                HttpWebRequest request = webRequest as HttpWebRequest;
                webRequest.Method = "POST";

                webRequest.Headers.Add("X-WNS-Type", type);
                webRequest.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken));

                Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(contentInBytes, 0, contentInBytes.Length);
                requestStream.Close();

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                return webResponse.StatusCode.ToString();
            }
            catch (WebException webException)
            {
                string exceptionDetails = webException.Response.Headers["WWW-Authenticate"];
                if ((exceptionDetails != null) && exceptionDetails.Contains("Token expired"))
                {
                    GetAccessToken();
                    return PostToCloud(uri, xml, type);
                }
                else
                {
                    return "EXCEPTION: " + webException.Message;
                }
            }
            catch (Exception ex)
            {
                return "EXCEPTION: " + ex.Message;
            }
        }
        public void GetAccessToken()
        {
            var urlEncodedSid = HttpUtility.UrlEncode(String.Format("{0}", this.packageSID));
            var urlEncodedSecret = HttpUtility.UrlEncode(this.clinetSecret);

            var body =
              String.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com", urlEncodedSid, urlEncodedSecret);

            var client = new WebClient();
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            string response = client.UploadString("https://login.live.com/accesstoken.srf", body);
            var oAuthToken = GetOAuthTokenFromJson(response);
            this.accessToken = oAuthToken.AccessToken;
        }
    }
}