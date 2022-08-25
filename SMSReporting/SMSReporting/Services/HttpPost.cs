using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace BrotecsLateSMSReporting
{
    class HttpPost
    {
        public HttpPost()
        {

        }

        public string HttpPostToURL(string uri, string parameters)
        {
            // parameters: name1=value1&name2=value2
            Trace.WriteLine(string.Format("URL: {0}", uri));
            Trace.WriteLine(string.Format("Parameters: {0}", parameters));
            WebRequest webRequest = WebRequest.Create(uri);
            //string ProxyString = 
            //   System.Configuration.ConfigurationManager.AppSettings
            //   [GetConfigKey("proxy")];
            //webRequest.Proxy = new WebProxy (ProxyString, true);
            //Commenting out above required change to App.Config
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(parameters);
            Stream os = null;
            try
            { // send the Post
                webRequest.ContentLength = bytes.Length;   //Count bytes to send
                os = webRequest.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);         //Send it
            }
            catch (WebException ex)
            {
                Trace.WriteLine(string.Format("HttpPost: Request error: {0}", ex.Message));
            }
            finally
            {
                if (os != null)
                {
                    os.Close();
                }
            }

            try
            { // get the response
                WebResponse webResponse = webRequest.GetResponse();
                if (webResponse == null)
                { return null; }
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                Trace.WriteLine(string.Format("HttpPost: Response: {0}", sr.ToString()));
                return sr.ReadToEnd().Trim();
            }
            catch (WebException ex)
            {
                Trace.WriteLine(string.Format("HttpPost: Response error: {0}", ex.Message));
            }
            return null;
        } // end HttpPost 
    }
}
