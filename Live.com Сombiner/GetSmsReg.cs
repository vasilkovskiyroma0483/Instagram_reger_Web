using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leaf.xNet;

namespace Live.com_Сombiner
{
    class GetSmsReg
    {
        public static string Country, Service, APIKey;
        public static object locker = new object();

        #region Метод заполнения данных для Sms-Get.com
        public static bool FillInSettings(string country, string service, string apiKey)
        {
            if (string.IsNullOrEmpty(country))
            {
                MessageBox.Show("Выберите страну телефонного номера.");
                return false;
            }
            if (string.IsNullOrEmpty(country))
            {
                MessageBox.Show("Выберите сервис, для которого нужен номер.");
                return false;
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Введите ключ Api Для Sms-Reg");
                return false;
            }

            Country = country;
            Service = service;
            APIKey = apiKey;

            return true;
        }
        #endregion

        #region Метод запрашивает номер, и отдает его
        public static (string tzid, string number) GetNumber()
        {
            string Response;
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    using (var request = new HttpRequest())
                    {
                        request.UserAgent = Http.ChromeUserAgent();
                        request.EnableEncodingContent = true;

                        var UrlParams = new RequestParams
                        {
                            ["country"] = Country,
                            ["service"] = Service,
                            ["apikey"] = APIKey
                        };
                        lock (locker)
                        {
                            Response = request.Get("api.sms-reg.com/getNum.php", UrlParams).ToString();

                            if (Response.Contains("WARNING_WAIT15MIN"))
                            {
                                SaveData.WriteToLog("Sms-Reg", "Ждем 15 минут, на получение номера");
                                Thread.Sleep(901000);
                            }
                        }

                        if (Response.Contains("response\":\"1\""))
                        {
                            string tzid = Response.BetweenOrEmpty("tzid\":\"", "\"");
                            return (tzid, GetResult(tzid, "number\":\""));
                        }
                        else
                            Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return (null, null);
        }
        #endregion

        #region Метод возвращает номер или код из смс
        public static string GetResult(string tzid, string option)
        {
            for (int i = 0; i < 85; i++)
            {
                try
                {
                    using (var request = new HttpRequest())
                    {
                        request.UserAgent = Http.ChromeUserAgent();
                        request.EnableEncodingContent = true;

                        string Response = request.Get("api.sms-reg.com/getState.php", new RequestParams() { ["tzid"] = tzid, ["apikey"] = APIKey }).ToString();

                        // Ожидаем выдачу номера/получение смс.
                        if (Response.Contains("TZ_INPOOL") || Response.Contains("TZ_NUM_WAIT"))
                        {
                            Thread.Sleep(10000);
                            continue;
                        }
                        // Нету подходящих номеров
                        if (Response.Contains("WARNING_NO_NUMS"))
                        {
                            SaveData.WriteToLog("Sms-Reg", "Не нашелся подходящий номер");
                            return null;
                        }
                        // Номер успешно выдан / Пришел код
                        if (Response.Contains("TZ_NUM_PREPARE") || Response.Contains("TZ_NUM_ANSWER"))
                        {
                            return Response.BetweenOrEmpty(option, "\"");
                        }
                        if (Response.Contains("TZ_OVER_OK") || Response.Contains("TZ_OVER_EMPTY") || Response.Contains("TZ_OVER_NR") || Response.Contains("TZ_DELETED"))
                            return null;

                        if (Response.Contains("WARNING_TOO_MANY_REQUESTS"))
                        {
                            SaveData.WriteToLog("Sms-Reg", "WARNING_TOO_MANY_REQUESTS");
                            Thread.Sleep(60000);
                        }
                    }
                }
                catch (Exception exception) { MessageBox.Show(exception.Message); }
            }
            return null;
        }
        #endregion

        #region Метод сообщает что готовы получить смс, и возвращает код.
        public static string GetCode(string tzid)
        {
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    using (var request = new HttpRequest())
                    {
                        request.UserAgent = Http.ChromeUserAgent();
                        request.EnableEncodingContent = true;

                        string Response = request.Get("api.sms-reg.com/setReady.php", new RequestParams() { ["tzid"] = tzid, ["apikey"] = APIKey }).ToString();

                        if (Response.Contains("response\":\"1\""))
                            return GetResult(tzid, "msg\":\"");
                        else
                            Thread.Sleep(5000);
                    }
                }
                catch (Exception exception) { MessageBox.Show(exception.Message); }
            }
            return null;
        }
        #endregion
    }
}
