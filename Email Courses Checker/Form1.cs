using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Exchange.WebServices.Data;
using HtmlAgilityPack;
using System.IO;

namespace Email_Courses_Checker
{
    public partial class Form1 : Form
    {
        ExchangeService exchange = null;

        public Form1()
        {
            InitializeComponent();
            tbEmail.Text = "ID@ua.edu.lb";
            tbMaxEmails.Text = "12";
            StreamReader sr = new StreamReader(@".\CourseNumbers.txt");
            tbClassNumbers.Text = sr.ReadLine();
            sr.Close();
        }

        static bool RedirectionCallback(string url)
        {
            // Return true if the URL is an HTTPS URL.
            return url.ToLower().StartsWith("https://");
        }

        private void btnGetCourses_Click(object sender, EventArgs e)
        {
            try
            {
                //Initializing Service, Message
                exchange = new ExchangeService(ExchangeVersion.Exchange2013);

                //Defining Service Credentials
                exchange.Credentials = new WebCredentials(tbEmail.Text, tbPassword.Text, "AutodiscoverUrl");
                exchange.AutodiscoverUrl(tbEmail.Text, RedirectionCallback);

                System.Threading.Tasks.Task.Run(() =>
                {
                    MessageBox.Show("Connected!\nFetching Email...");
                });

                GetEmails();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The Autodiscover service couldn't be located."))
                {
                    MessageBox.Show("Wrong Email or Password");
                    return;
                }
                MessageBox.Show(ex.Message + "\n\n" + ex);
            }
        }

        private void GetEmails()
        {
            //Get Emails, specify the folder (Inbox) and the number of emails to get (100)
            FindItemsResults<Item> findResults = exchange.FindItems(WellKnownFolderName.Inbox, new ItemView(Int32.Parse(tbMaxEmails.Text)));

            bool foundCoursesEmail = false;

            //Check for bounced emails
            foreach (Item email in findResults.Items)
            {
                if (email.Subject.Contains("Online courses"))
                {
                    email.Load();
                    string body = email.Body.Text;
                    int Start = body.IndexOf("<table", 0);
                    int End = body.IndexOf("</table>", Start) + "</table>".Length;
                    string htmlTableString = body.Substring(Start, End - Start);
                    DataTable TableCourses = ConvertHTMLTableToDataTable(htmlTableString);
                    DataRow[] selectedRows = TableCourses.Select("[Class Nbr] IN (" + tbClassNumbers.Text + ")" , "Course ASC");

                    tbResponse.Clear();
                    tbResponse.Text = email.Subject;
                    tbResponse.AppendText(Environment.NewLine);
                    tbResponse.AppendText(Environment.NewLine);
                    tbResponse.Text += "Your courses for this week are:";

                    foreach (var row in selectedRows)
                    {
                        tbResponse.AppendText(Environment.NewLine);
                        tbResponse.Text += row["Course"].ToString() + " - Instructor: " + row["First Name"].ToString() + " " + row["Last Name"].ToString();
                    }
                    foundCoursesEmail = true;
                    break;
                }
            }
            StreamWriter sw = new StreamWriter(@".\CourseNumbers.txt");
            sw.WriteLine(tbClassNumbers.Text);
            sw.Close();

            //If online courses email was not found
            if (!foundCoursesEmail)
            {
                tbResponse.Text = "The online courses email was not found, try putting a higher Max Emails number and try again.";
            }
        }

        private DataTable ConvertHTMLTableToDataTable(string table)
        {
            DataTable output = new DataTable();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(table);

            var nodes = doc.DocumentNode.SelectNodes("//table//tr");

            string textToGetHeaders = nodes[0].InnerText.Trim();

            string[] stringSeparators = new string[] { "\r\n" };
            var headers = textToGetHeaders.Split(stringSeparators, StringSplitOptions.None);
            headers = headers.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            foreach (var header in headers)
            {
                output.Columns.Add(header);
            }

            var rows = nodes.Skip(1).Select(tr => tr
                .Elements("td")
                .Select(td => td.InnerText.Trim())
                .ToArray());
            foreach (var row in rows)
            {
                output.Rows.Add(row);
            }
            return output;
        }

        private void label5_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.kwalka.com");
        }
    }
}
