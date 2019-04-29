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
        public static void SendMessage(string message, string subject, ServiceMonitorConfiguration config)
        {
            Logger = LogManager.GetCurrentClassLogger();
            //set up mail client
            SmtpClient client = new SmtpClient(config.SmtpAddress, config.SmtpPort);
            //setup ssl
            if (config.UseSSL)
            {
                client.Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword);
                client.EnableSsl = true;
                client.Port = config.SslPort;
            }
            //for every mail recipiant in the list send message
            for (int i = 0; i < config.ToMailAddress.Length; i++)
            {
                MailMessage msg = new MailMessage(config.FromMailAddress, config.ToMailAddress[i]);
                msg.Subject = subject;
                msg.Body = message;

                try
                {
                    Logger.Info($"sending email to {config.ToMailAddress[i]} : {msg.Body} ");
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
