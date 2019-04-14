using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Common;
using NLog;

namespace MessageSender
{
    public static class MailMessageSender
    {

        static Logger Logger;
        public static  void SendMessage(string message,string subject,ServiceMonitorConfiguration config)
        {
            Logger = LogManager.GetCurrentClassLogger();
            SmtpClient client = new SmtpClient(config.SmtpAddress, config.SmtpPort);
            for (int i = 0; i < config.ToMailAddress.Length; i++)
            {
                MailMessage msg = new MailMessage(config.FromMailAddress, config.ToMailAddress[i]);

                client.Credentials = new NetworkCredential("moshekri@gmail.com", "Moshe!Admin007");
                client.Host = "smtp.gmail.com";
                client.Port = 587;
                client.EnableSsl = true;


                msg.Subject = subject;
                msg.Body = message;

                try
                {
                    client.Send(msg);
                    Logger.Debug("Mail message Sent successfully!");
                }
                catch (Exception ex)
                {

                    Logger.Error($"Error Sendig mail message , error was  : {ex.Message}");
                }

            }



        }
    }
}
