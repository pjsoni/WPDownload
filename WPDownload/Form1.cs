using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace WPDownload
{
    public partial class Form1 : Form
    {
        private delegate void SetTextCallback(string text);
        private string DOWNLOAD_PATH = string.Empty;
        private string logFile = "wpdownload.txt";
        private Stopwatch sw = new Stopwatch();
        int imgIndex = 0;
        int imgCount = 10;

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the calling thread to the thread ID
            // of the creating thread. If these threads are different, it returns true.
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox1.AppendText(text);
            }
        }

        private void SetDownloadPath() {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["downloadLocation"]) && ConfigurationManager.AppSettings["downloadLocation"].ToString().ToUpper() == "USERPROFILE")
            {
                DOWNLOAD_PATH = String.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigurationManager.AppSettings["downloadFolder"]);  

            }
            else {
                DOWNLOAD_PATH =  ConfigurationManager.AppSettings["downloadFolder"];

            }


        }

    public Form1()
        {
            InitializeComponent();
            numericUpDownIndex.Value = imgIndex;
            numericUpDownCount.Value = imgCount;
            SetDownloadPath();
           

        }

        

        private void SaveImage(string url, string datesuffix)
        {
            string fileName = GetServerImageName(url);
            string localFileName = GetLocalImageName(datesuffix);
            try
            {
                WebRequest req = WebRequest.Create(@"http://bing.com" + url.Replace("1366x768", "1920x1200"));
                using (var stream = req.GetResponse().GetResponseStream())
                {
                    Image img = Image.FromStream(stream);
                    img.Save(localFileName);
                    SetText(string.Format("Saved image - {0}", localFileName) + Environment.NewLine);
                }
                using (StreamWriter sw = File.AppendText(logFile))
                {
                    sw.WriteLine(url);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    SetText(string.Format("File not found on server - {0}", fileName) + Environment.NewLine);

                    // do nothing
                }
                else
                {
                    HandleException(ex);
                }
            }
        }

        private string GetLocalImageName(string datesuffix)
        {
            DateTime dt = DateTime.ParseExact(datesuffix, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(-1);
            return string.Format(@"{0}\BingWallpaper-{1}.jpg", DOWNLOAD_PATH, dt.ToString("yyyy-MM-dd"));
        }

        private string GetServerImageName(string url)
        {
            Regex reg = new Regex(@"[^/]*(?=\.jpg|JPG.*$)");
            Match m = reg.Match(url);
            var retVal = string.Format("{0}.jpg", m.Value);
            return retVal.Replace("1366x768", "1920x1200");
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            #region Download from BING Commented Code

            sw.Start();

            XDocument doc = null;
            HttpWebRequest request = null;

           
            string url = string.Format(ConfigurationManager.AppSettings["bingUrl"], imgIndex, imgCount);
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (var stream = request.GetResponse().GetResponseStream())
                {
                    doc = XDocument.Load(stream);
                }

               
                SetText(String.Format("Total images found - {0}", doc.Descendants("image").Count().ToString()) + Environment.NewLine);

                foreach (var image in doc.Descendants("image"))
                {
                    string fileName = GetLocalImageName(image.Element("enddate").Value);
                    if (!File.Exists(fileName))
                    {
                        SaveImage(image.Element("url").Value, image.Element("enddate").Value);
                    }
                    else
                    {
                        SetText(string.Format("Local file exist - {0}", fileName) + Environment.NewLine);
                    }
                }

               
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            
        }

        private void HandleException(Exception ex)
        {
            //SetText("-------------------------" + Environment.NewLine);
            SetText(ex.Message + Environment.NewLine);

            //SetText("-------------------------" + Environment.NewLine);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            sw.Stop();

            SetText(String.Format("Elapsed={0}", sw.Elapsed) + Environment.NewLine);
            SetText("Task completed" + Environment.NewLine);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void numericUpDownCount_ValueChanged(object sender, EventArgs e)
        {
            imgCount = Convert.ToInt32(numericUpDownCount.Value);
        }

        private void numericUpDownIndex_ValueChanged(object sender, EventArgs e)
        {
            imgIndex = Convert.ToInt32(numericUpDownIndex.Value);
        }
    }
}