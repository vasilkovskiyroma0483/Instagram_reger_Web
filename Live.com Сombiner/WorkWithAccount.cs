using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Leaf.xNet;

namespace Live.com_Сombiner
{
    class WorkWithAccount
    {
        #region Свойства класса
        /// <summary>
        /// Паузы, и количество попыток запроса
        /// </summary>
        public static int minPause, maxPause, countRequest, minPauseRegistration, maxPauseRegistration;
        /// <summary>
        /// Режим работы
        /// </summary>
        public static string OperatingMode;
        /// <summary>
        /// Перечисление статусов
        /// </summary>
        public enum Status
        {
            True,
            False,
            UnknownError,
            BlockedAccount,
            NumberError,
            InvalidEmail,
            BadProxy
        }
        /// <summary>
        /// Количество аккаунтов для регистрации
        /// </summary>
        public static int CountAccountForRegistration;

        public static Random rand = new Random((int)DateTime.Now.Ticks);
        public static object locker = new object();
        public static object LogOBJ = new object();
        #endregion

        #region Выбор режима работы
        public static void StartWork()
        {
            try
            {
                SaveData.WriteToLog(null, "Начал свою работу");
                if (OperatingMode == "Регистратор")
                    RegistrationAccount();
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Запуск метода регистрации и проверка результата
        public static void RegistrationAccount()
        {
            try
            {
                string Password, UserAgent, NameSurname;
                string proxyLog = "";
                (string Email, string PasswordEmail) Email;
                ProxyClient proxyClient;
                while (true)
                {
                    #region Выдача аккаунтов
                    lock (locker)
                    {
                        if (SaveData.UsedRegistration < CountAccountForRegistration)
                        {
                            (string NameSurname, string Password) DataForRegistration = GetNameSurnamePassword.Get();
                            Email = GetEmail.Get();
                            if (String.IsNullOrEmpty(DataForRegistration.NameSurname) || String.IsNullOrEmpty(DataForRegistration.Password) || string.IsNullOrEmpty(Email.Email))
                                continue;

                            NameSurname = DataForRegistration.NameSurname;
                            Password = DataForRegistration.Password;
                            SaveData.UsedRegistration++;
                            SaveData.SaveAccount($"{Email.Email}:{Password}", SaveData.ProcessedRegistrationList);
                        }
                        else
                        {
                            break;
                        }
                        UserAgent = GetUserAgent.get();
                        proxyClient = GetProxy.get();
                        proxyLog = proxyClient == null ? "" : $";{proxyClient.ToString()}";
                    }
                    #endregion

                    #region Вызов метода регистрации, и проверка результата
                    SaveData.WriteToLog($"{Email.Email}:{Password}", "Попытка зарегестрировать аккаунт");

                    (Status status, CookieStorage cookie) Data;
                    for (int i = 0; i < countRequest; i++)
                    {
                        Data = GoRegistrationAccount(NameSurname, Email, Password, UserAgent, proxyClient);
                        switch (Data.status)
                        {
                            case Status.True:
                                SaveData.GoodRegistration++;
                                SaveData.WriteToLog($"{Email.Email}:{Password}", "Аккаунт успешно зарегестрирован");
                                SaveData.SaveAccount($"{Email.Email}:{Password}{proxyLog}|{UserAgent}", SaveData.GoodRegistrationList);
                                Data.cookie.SaveToFile($"out/cookies/{Email.Email}.jar", true);
                                break;
                            case Status.False:
                                SaveData.InvalidRegistration++;
                                SaveData.WriteToLog($"{Email.Email}:{Password}", "Аккаунт не зарегестрирован");
                                SaveData.SaveAccount($"{Email.Email}:{Password}{proxyLog}|{UserAgent}", SaveData.InvalidRegistrationList);
                                break;
                            case Status.NumberError:
                                SaveData.NumberError++;
                                SaveData.WriteToLog($"{Email.Email}:{Password}", "Аккаунт не зарегестрирован, попросили номер после капчи.");
                                SaveData.SaveAccount($"{Email.Email}:{Password}{proxyLog}|{UserAgent}", SaveData.NumberErrorList);
                                break;
                            case Status.InvalidEmail:
                                SaveData.InvalidEmail++;
                                SaveData.WriteToLog($"{Email.Email}:{Password}", "Аккаунт не зарегестрирован, не смогли подключится к почте.");
                                SaveData.SaveAccount($"{Email.Email}:{Password}{proxyLog}|{UserAgent}", SaveData.InvalidEmailList);
                                break;
                            case Status.BadProxy:
                                SaveData.BadProxy++;
                                SaveData.WriteToLog($"{Email.Email}:{Password}", "Аккаунт не зарегестрирован, грязный прокси.");
                                SaveData.SaveAccount($"{Email.Email}:{Password}{proxyLog}|{UserAgent}", SaveData.BadProxyList);
                                break;
                            default:
                                SaveData.WriteToLog($"{Email.Email}:{Password}", "Неизвестная ошибка, повторяем.");
                                UserAgent = GetUserAgent.get();
                                proxyClient = GetProxy.get();
                                continue;
                        }
                        break;
                    }
                    int sleep = rand.Next(minPauseRegistration, maxPauseRegistration);
                    SaveData.WriteToLog($"System", $"Засыпаем на {sleep / 60000} минут");
                    Thread.Sleep(sleep);
                    #endregion
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Метод регистрации аккаунта
        /// <summary>
        /// Метод регистрации аккаунта
        /// </summary>
        /// <param name="Login">Логин для регистрации</param>
        /// <param name="Password">Пароль для регистрации</param>
        /// <param name="UserAgent">UserAgent</param>
        /// <param name="proxyClient">Прокси</param>
        /// <returns></returns>
        public static (Status status, CookieStorage cookie) GoRegistrationAccount(string nameSurname, (string Email, string Password) Email, string password, string userAgent, ProxyClient proxyClient)
        {
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    request.Cookies = new CookieStorage();
                    request.UseCookies = true;
                    request.Proxy = proxyClient;
                    request.UserAgent = userAgent;
                    var UrlParams = new RequestParams();
                    request["Accept-Language"] = "en-US";

                    string day = rand.Next(1, 28).ToString();
                    string month = rand.Next(1, 13).ToString();
                    string year = rand.Next(1985, 2003).ToString();

                    #region Читаем все сообщения на почте
                    if (!GetMailKit.ReadMessages(Email, password, request))
                        return (Status.InvalidEmail, null);
                    #endregion

                    #region Делаем Get запрос на главную страницу сайта. Парсим: Headers XInstagramAJAX, Headers csrf_token, Библиотека ConsumerLibCommons.
                    request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Upgrade-Insecure-Requests", "1");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "DNT",
                    "Connection",
                    "Upgrade-Insecure-Requests",
                    "Accept-Encoding"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    string Response = request.Get("Https://www.instagram.com/").ToString();

                    string device_id = Response.BetweenOrEmpty("device_id\":\"", "\"");
                    string XInstagramAJAX = Response.BetweenOrEmpty("rollout_hash\":\"", "\"");
                    string csrf_token = Response.BetweenOrEmpty("csrf_token\":\"", "\"");
                    string xmid = request.Cookies.GetCookieHeader("Https://www.instagram.com/").BetweenOrEmpty("mid=", ";");

                    string[] librarys = Response.BetweenOrEmpty("<link rel=\"manifest\" href=\"/data/manifest.json\">", "<script type=\"text/javascript\">").Split('>');
                    string ConsumerLibCommons = ParseCurrentLibrary(librarys, "ConsumerLibCommons.js");
                    string ConsumerUICommons = ParseCurrentLibrary(librarys, "ConsumerUICommons.js");
                    string Consumer = ParseCurrentLibrary(librarys, "Consumer.js");
                    #endregion

                    #region Делаем Get запрос для парсинга Headers FbAppID. Post INTERSTITIAL, PAGE_TOP, TOOLTIP
                    request.AddHeader("Referer", "https://www.instagram.com/");
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("DNT", "1");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "DNT",
                    "Connection",
                    "Upgrade-Insecure-Requests",
                    "Accept-Encoding"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Get(ConsumerLibCommons).ToString();
                    string PWAAppId = Response.BetweenOrEmpty("e.instagramWindowsPWAAppId='", "'");
                    string INTERSTITIAL = Response.BetweenOrEmpty("INTERSTITIAL:'", "'");
                    string PAGE_TOP = Response.BetweenOrEmpty("PAGE_TOP:'", "'");
                    string TOOLTIP = Response.BetweenOrEmpty("TOOLTIP:'", "'");

                    string viewer = Response.BetweenOrEmpty("m.exports=\"", "\"") + "\"";
                    string surfaces_to_queries = $"{{\"{PAGE_TOP}\":\"{viewer},\"{INTERSTITIAL}\":\"{viewer},\"{TOOLTIP}\":\"{viewer}}}";
                    string doc_id = Response.BetweenOrEmpty("{BANNER:'1',MODAL:'2'}", ";").BetweenOrEmpty("n='", "'");
                    #endregion

                    #region Делаем Get запрос для парсинга Params bloks_versioning_id
                    request.AddHeader("Referer", "https://www.instagram.com/");
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("DNT", "1");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "DNT",
                    "Connection",
                    "Upgrade-Insecure-Requests",
                    "Accept-Encoding"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    string bloks_versioning_id = request.Get(ConsumerUICommons).ToString().BetweenOrEmpty("e.VERSIONING_ID=\"", "\"");
                    #endregion

                    #region Парсим MID данные для encrypt и куки.
                    if (xmid.Length <= 0)
                    {
                        xmid = GetXmid();

                        #region Делаем Post запрос на проверку браузера
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("X-Mid", xmid);
                        request.AddHeader("X-CSRFToken", csrf_token);
                        request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                        request.AddHeader("X-IG-App-ID", PWAAppId);
                        request.AddHeader("X-IG-WWW-Claim", "0");
                        request.AddHeader("X-Requested-With", "XMLHttpRequest");
                        request.AddHeader("Origin", "https://www.instagram.com");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", "https://www.instagram.com/");

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-Mid",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                        #endregion

                        UrlParams.Clear();
                        UrlParams["bloks_versioning_id"] = bloks_versioning_id;
                        UrlParams["surfaces_to_queries"] = surfaces_to_queries;
                        UrlParams["vc_policy"] = "default";
                        UrlParams["version"] = "1";

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        request.Post("https://www.instagram.com/qp/batch_fetch_web/", UrlParams);
                        #endregion

                        #region Соглашаемся с считыванием куков Post запрос
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("X-Mid", xmid);
                        request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                        request.AddHeader("X-IG-App-ID", PWAAppId);
                        request.AddHeader("Origin", "https://www.instagram.com");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", "https://www.instagram.com/");

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-Mid",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "Content-Type",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                        #endregion

                        UrlParams.Clear();
                        UrlParams["doc_id"] = doc_id;
                        UrlParams["variables"] = $"{{\"ig_did\":\"{device_id}\",\"first_party_tracking_opt_in\":true,\"third_party_tracking_opt_in\":true,\"input\":{{\"client_mutation_id\":0}}}}";

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        Response = request.Post("https://graphql.instagram.com/graphql/", UrlParams).ToString();
                        #endregion

                        #region Делаем запрос на shared_data
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("X-Web-Device-Id", device_id);
                        request.AddHeader("X-Mid", xmid);
                        request.AddHeader("X-IG-App-ID", PWAAppId);
                        request.AddHeader("X-IG-WWW-Claim", "0");
                        request.AddHeader("X-Requested-With", "XMLHttpRequest");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", "https://www.instagram.com/accounts/login/");

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-Web-Device-Id",
                    "X-Mid",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "X-Requested-With",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Accept-Encoding"
                    });
                        #endregion

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        Response = request.Get("https://www.instagram.com/data/shared_data/").ToString();
                        #endregion

                        xmid = request.Cookies.GetCookieHeader("https://www.instagram.com/data/shared_data/").BetweenOrEmpty("mid=", ";");
                        csrf_token = request.Cookies.GetCookieHeader("https://www.instagram.com/data/shared_data/").BetweenOrEmpty("csrftoken=", ";");
                    }
                    else
                    {
                        #region Делаем Post запрос на проверку браузера
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("X-CSRFToken", csrf_token);
                        request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                        request.AddHeader("X-IG-App-ID", PWAAppId);
                        request.AddHeader("X-IG-WWW-Claim", "0");
                        request.AddHeader("X-Requested-With", "XMLHttpRequest");
                        request.AddHeader("Origin", "https://www.instagram.com");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", "https://www.instagram.com/");

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                        #endregion

                        UrlParams.Clear();
                        UrlParams["bloks_versioning_id"] = bloks_versioning_id;
                        UrlParams["surfaces_to_queries"] = surfaces_to_queries;
                        UrlParams["vc_policy"] = "default";
                        UrlParams["version"] = "1";

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        request.Post("https://www.instagram.com/qp/batch_fetch_web/", UrlParams);
                        #endregion

                        #region Делаем запрос на shared_data в случае если есть куки
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("X-CSRFToken", csrf_token);
                        request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                        request.AddHeader("X-IG-App-ID", PWAAppId);
                        request.AddHeader("X-IG-WWW-Claim", "0");
                        request.AddHeader("X-Requested-With", "XMLHttpRequest");
                        request.AddHeader("Origin", "https://www.instagram.com");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                        #endregion

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        Response = request.Get("https://www.instagram.com/data/shared_data/").ToString();
                        #endregion
                    }

                    string key_id = Response.BetweenOrEmpty("key_id\":\"", "\"");
                    string public_key = Response.BetweenOrEmpty("public_key\":\"", "\"");
                    string version = Response.BetweenOrEmpty("version\":\"", "\"");
                    #endregion

                    #region Делаем Get запрос на страницу регистрации
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "X-Requested-With",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Get("Https://www.instagram.com/accounts/emailsignup/?__a=1");
                    #endregion

                    #region Начинаем отправлять Ajax Запросы на ввод данных для регистрации (Ввели Почту и парсим логин)
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["email"] = Email.Email;
                    UrlParams["username"] = "";
                    UrlParams["first_name"] = "";
                    UrlParams["opt_into_one_tap"] = "false";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("Https://www.instagram.com/accounts/web_create_ajax/attempt/", UrlParams).ToString();

                    string Login = GetLogin(Response.BetweenOrEmpty("username_suggestions\": [", "]"));
                    #endregion

                    #region Ajax Запросы на ввод данных для регистрации (Ввели Имя и логин)
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion
                    UrlParams.Clear();
                    UrlParams["email"] = Email.Email;
                    UrlParams["username"] = Login;
                    UrlParams["first_name"] = nameSurname;
                    UrlParams["opt_into_one_tap"] = "false";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("Https://www.instagram.com/accounts/web_create_ajax/attempt/", UrlParams);
                    #endregion

                    #region Ajax Запросы на ввод данных для регистрации (Ввели пароль)
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["email"] = Email.Email;
                    UrlParams["enc_password"] = EncryptionService.GetEncryptPassword(password, public_key, key_id, version);
                    UrlParams["username"] = Login;
                    UrlParams["first_name"] = nameSurname;
                    UrlParams["client_id"] = xmid;
                    UrlParams["seamless_login_enabled"] = "1";
                    UrlParams["opt_into_one_tap"] = "false";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("Https://www.instagram.com/accounts/web_create_ajax/attempt/", UrlParams);
                    #endregion

                    #region Делаем Post запрос на проверку даты
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["day"] = day;
                    UrlParams["month"] = month;
                    UrlParams["year"] = year;

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("Https://www.instagram.com/web/consent/check_age_eligibility/", UrlParams);
                    #endregion

                    #region Ajax Запросы на ввод данных для регистрации (Ввели дату рождения)
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["email"] = Email.Email;
                    UrlParams["enc_password"] = EncryptionService.GetEncryptPassword(password, public_key, key_id, version);
                    UrlParams["username"] = Login;
                    UrlParams["first_name"] = nameSurname;
                    UrlParams["month"] = month;
                    UrlParams["day"] = day;
                    UrlParams["year"] = year;
                    UrlParams["client_id"] = xmid;
                    UrlParams["seamless_login_enabled"] = "1";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("Https://www.instagram.com/accounts/web_create_ajax/attempt/", UrlParams);
                    #endregion

                    #region Отправлям Post запрос на отправку кода верификации на телефон
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["device_id"] = xmid;
                    UrlParams["email"] = Email.Email;

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("https://i.instagram.com/api/v1/accounts/send_verify_email/", UrlParams);
                    #endregion

                    #region Принимаем сообщение с почты
                    string code = GetMailKit.GetCode(Email, password, request);
                    if (code == null)
                        return (Status.UnknownError, null);
                    #endregion

                    #region Отправлям Post запрос с кодом из почты, получаем signup_code
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["code"] = code;
                    UrlParams["device_id"] = xmid;
                    UrlParams["email"] = Email.Email;

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/accounts/check_confirmation_code/", UrlParams).ToString();

                    string signup_code = Response.BetweenOrEmpty("signup_code\":\"", "\"");
                    #endregion

                    #region Ajax Запросы на ввод данных для регистрации (Конечный запрос)
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("X-CSRFToken", csrf_token);
                    request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                    request.AddHeader("X-IG-App-ID", PWAAppId);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Requested-With", "XMLHttpRequest");
                    request.AddHeader("Origin", "https://www.instagram.com");
                    request.AddHeader("DNT", "1");
                    request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                    #endregion

                    UrlParams.Clear();
                    UrlParams["email"] = Email.Email;
                    UrlParams["enc_password"] = EncryptionService.GetEncryptPassword(password, public_key, key_id, version);
                    UrlParams["username"] = Login;
                    UrlParams["first_name"] = nameSurname;
                    UrlParams["month"] = month;
                    UrlParams["day"] = day;
                    UrlParams["year"] = year;
                    UrlParams["client_id"] = xmid;
                    UrlParams["seamless_login_enabled"] = "1";
                    UrlParams["tos_version"] = "row";
                    UrlParams["force_sign_up_code"] = signup_code;

                    request.IgnoreProtocolErrors = true;
                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("Https://www.instagram.com/accounts/web_create_ajax/", UrlParams).ToString();
                    #endregion
                    
                    if (Response.Contains("account_created\":true"))    
                        return (Status.True, request.Cookies);      // Валидный аккаунт
                    if (Response.Contains("\"errors\": {\"ip\":"))
                        return (Status.BadProxy, request.Cookies);      // Грязный прокси
                    if (Response.Contains("checkpoint_required"))   // Капча
                    {
                        SaveData.WriteToLog($"{Email.Email}:{password}", "Попали на капчу");
                        string checkpoint_url = Response.BetweenOrEmpty("checkpoint_url\":\"", "\"");

                        #region Делаем Get запрос на страницу checkpoint_required
                        request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", "https://www.instagram.com/accounts/emailsignup/");
                        request.AddHeader("Upgrade-Insecure-Requests", "1");

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Upgrade-Insecure-Requests",
                    "Accept-Encoding"
                    });
                        #endregion

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        Response = request.Get(checkpoint_url).ToString();

                        string checkpoint_url2 = Response.BetweenOrEmpty("<link rel=\"alternate\" href=\"", "?challenge_context");
                        string step_name = Response.BetweenOrEmpty("step_name\\\": \\\"", "\\\"");
                        string nonce_code = Response.BetweenOrEmpty("nonce_code\\\": \\\"", "\\\"");
                        string user_id = Response.BetweenOrEmpty("user_id\\\": ", ",");
                        string is_stateless = Response.BetweenOrEmpty("is_stateless\\\": ", ",");
                        string challenge_type_enum = Response.BetweenOrEmpty("challenge_type_enum\\\": \\\"", "\\\"");
                        #endregion

                        #region Делаем Post запрос с решенной капчей
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("X-CSRFToken", csrf_token);
                        request.AddHeader("X-Instagram-AJAX", XInstagramAJAX);
                        request.AddHeader("X-IG-App-ID", PWAAppId);
                        request.AddHeader("X-IG-WWW-Claim", "0");
                        request.AddHeader("X-Requested-With", "XMLHttpRequest");
                        request.AddHeader("Origin", "https://www.instagram.com");
                        request.AddHeader("DNT", "1");
                        request.AddHeader("Referer", checkpoint_url.Replace("%7B", "{").Replace("%7D", "}"));

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "User-Agent",
                    "Accept",
                    "Accept-Language",
                    "X-CSRFToken",
                    "X-Instagram-AJAX",
                    "X-IG-App-ID",
                    "X-IG-WWW-Claim",
                    "Content-Type",
                    "X-Requested-With",
                    "Origin",
                    "DNT",
                    "Connection",
                    "Referer",
                    "Cookie",
                    "Accept-Encoding",
                    "Content-Length"
                    });
                        #endregion

                        UrlParams.Clear();
                        UrlParams["g-recaptcha-response"] = GetCaptcha.GetRecaptcha("https://www.fbsbx.com/", "6Lc9qjcUAAAAADTnJq5kJMjN9aD1lxpRLMnCS2TR", $"{Email.Email}:{password}");
                        UrlParams["challenge_context"] = $"{{\"step_name\": \"{step_name}\", \"nonce_code\": \"{nonce_code}\", \"user_id\": {user_id}, \"is_stateless\": {is_stateless}, \"challenge_type_enum\": \"{challenge_type_enum}\"}}";

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        Response = request.Post(checkpoint_url2, UrlParams).ToString();
                        #endregion

                        if (Response.Contains("SubmitPhoneNumberForm"))
                            return (Status.NumberError, null);      // Требует ввода номера после решения капчи
                        if (Response.Contains("account_created\":true"))
                            return (Status.True, request.Cookies);      // Валидный аккаунт
                        else
                            return (Status.False, null);    // Не удалось зарегестрировать
                    }
                    else
                        return (Status.False, null);    // Не удалось зарегестрировать
                }
            }
            catch (Exception exception){ SaveData.WriteToLog($"{Email.Email}:{password}", $"Ошибка: {exception.Message}"); };
            return (Status.UnknownError, null);     // Неизвестная ошибка
        }
        #endregion

        #region Парсим Логин
        public static string GetLogin(string logins)
        {
            List<string> Logins = new List<string>();
            try
            {
                while (logins.Contains("\""))
                {
                    Logins.Add(logins.BetweenOrEmpty("\"", "\""));
                    logins = logins.Replace($"\"{logins.BetweenOrEmpty("\"", "\"")}\"", "");
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return Logins[rand.Next(Logins.Count)];
        }
        #endregion

        #region UnixTime
        public static string JSTime(bool cut = false)
        {
            try
            {
                string t = DateTime.UtcNow
                   .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalMilliseconds.ToString();

                if (t.Contains(","))
                    t = t.Substring(0, t.IndexOf(','));

                if (cut && t.Length > 10) t = t.Remove(t.Length - 3, 3);

                return t;
            }
            catch { }

            return "";
        }
        #endregion

        #region Метод парсинга нужной библиотеки JS
        /// <summary>
        /// Метод парсинга нужной библиотеки JS
        /// </summary>
        /// <param name="librarys">Массив строк с библиотеками JS</param>
        /// <param name="currentLibrarys">Какую библиотеку JS будем искать</param>
        /// <returns></returns>
        public static string ParseCurrentLibrary(string[] librarys, string currentLibrarys)
        {
            try
            {
                foreach (var Librarys in librarys)
                    if (Librarys.Contains(currentLibrarys))
                        return "https://www.instagram.com" + Librarys.BetweenOrEmpty("href=\"", "\"");
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return null;
        }
        #endregion

        #region Из Hex в Массив байтов
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        #endregion

        #region Генерация фейк xMid
        public static string GetXmid()
        {
            int randLength = rand.Next(52, 55);
            string sourceline = "abcdefghijklmnopqrstuvwxyz0123456789", xmid = "";
            for (int i = 0; i < randLength; i++)
                xmid += sourceline[rand.Next(sourceline.Length)];
            return xmid;
        }
        #endregion
    }
}