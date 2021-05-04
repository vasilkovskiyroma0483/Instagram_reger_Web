using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using xNet;

namespace Live.com_Сombiner
{
    class GetCaptcha
    {
        #region Свойства класса
        /// <summary>
        /// Ключ клиента Антикапчи
        /// </summary>
        public static string ClientKey;
        #endregion

        #region Метод проверки ClientKey и Баланса счета
        /// <summary>
        /// Метод проверки ClientKey и Баланса счета
        /// </summary>
        /// <param name="clientKey">Ключ клиента Антикапчи</param>
        /// <returns></returns>
        public static string CheckBalance(string clientKey)
        {
            try
            {
                ClientKey = clientKey;
                using (HttpRequest request = new HttpRequest())
                {
                    string result = request.Post("https://api.anti-captcha.com/getBalance", "{\"clientKey\":\"" + ClientKey + "\"}", "application/json").ToString();
                    if (result.Contains("\"errorId\":0,"))
                    {
                        return result.Substring("\"balance\":", "}");
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return "";
        }
        #endregion

        #region Метод решения ReCaptcha
        /// <summary>
        /// Метод решения ReCaptcha
        /// </summary>
        /// <param name="websiteURL">Ссылка на сайт, на котором решаем ReCaptcha</param>
        /// <param name="websiteKey">SiteKey</param>
        /// <param name="account">Аккаунт</param>
        /// <returns></returns>
        public static string GetRecaptcha(string websiteURL, string websiteKey, string account)
        {
            try
            {
                SaveData.WriteToLog(account, "Решаем ReCaptcha");
                using (HttpRequest request = new HttpRequest())
                {
                    string json = $"{{\"clientKey\":\"{ClientKey}\",\"task\":{{\"type\":\"RecaptchaV2TaskProxyless\",\"websiteURL\":\"{websiteURL}\",\"websiteKey\":\"{websiteKey}\"}},\"softId\":0,\"languagePool\":\"en\"}}";
                    string taskID = request.Post("https://api.anti-captcha.com/createTask ", json, "application/json").ToString();
                    taskID = taskID.Substring("\"taskId\":", "}");
                    for (int i = 0; i < 50; i++)
                    {
                        Thread.Sleep(5000);
                        string result = request.Post("https://api.anti-captcha.com/getTaskResult", $"{{\"clientKey\": \"{ClientKey}\",\"taskId\": {taskID}}}", "application/json").ToString();
                        if (result.Contains("\"errorId\":0,\"status\":\"ready\""))
                        {
                            SaveData.WriteToLog(account, "ReCaptcha решена.");
                            return result.Substring("\"gRecaptchaResponse\":\"", "\"");
                        }
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            SaveData.WriteToLog("Captcha", "Не смогли решить ReCaptcha");
            return "";
        }
        #endregion

        #region Метод решения ImagesCaptcha
        /// <summary>
        /// Метод решения ImagesCaptcha
        /// </summary>
        /// <param name="URLToCaptcha">Ссылка на Captcha</param>
        /// <param name="account">Аккаунт</param>
        /// <returns></returns>
        public static string GetImagesCaptcha(string URLToCaptcha, CookieDictionary cookieDictionary, string account)
        {
            try
            {
                SaveData.WriteToLog(account, "Решаем ImagesCaptcha");
                using (HttpRequest request = new HttpRequest())
                {
                    request.Cookies = cookieDictionary;
                    var base64 = Convert.ToBase64String(request.Get(URLToCaptcha).ToBytes());
                    string json = $"{{\"clientKey\":\"{ClientKey}\",\"task\":{{\"type\":\"ImageToTextTask\",\"body\":\"{base64}\",\"phrase\":false,\"case\":false,\"numeric\":false,\"math\":0,\"minLength\":0,\"maxLength\":0}}}}";
                    string taskID = request.Post("https://api.anti-captcha.com/createTask ", json, "application/json").ToString();
                    taskID = taskID.Substring("\"taskId\":", "}");
                    for (int i = 0; i < 50; i++)
                    {
                        Thread.Sleep(5000);
                        string result = request.Post("https://api.anti-captcha.com/getTaskResult", $"{{\"clientKey\": \"{ClientKey}\",\"taskId\": {taskID}}}", "application/json").ToString();
                        if (result.Contains("\"errorId\":0,\"status\":\"ready\""))
                        {
                            SaveData.WriteToLog(account, "ImagesCaptcha решена.");
                            return result.Substring("\"text\":\"", "\"");
                        }
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            SaveData.WriteToLog("Captcha", "Не смогли решить ImagesCaptcha");
            return "";
        }
        #endregion
    }
}
