using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Leaf.xNet;

namespace Live.com_Сombiner
{
    class GetProxy
    {
        #region Описание свойств
        /// <summary>
        /// List с Прокси
        /// </summary>
        public static List<string> Proxy = new List<string>();
        /// <summary>
        /// Тип прокси
        /// </summary>
        public static string TypeOfProxy;
        /// <summary>
        /// Режим работы прокси
        /// </summary>
        public static string ProxyMode;
        /// <summary>
        /// Позиция прокси в List
        /// </summary>
        public static int Position;
        /// <summary>
        /// Ссылка для проверки прокси.
        /// </summary>
        public static string ProxyCheckLink;
        #endregion

        #region Метод заполнения Прокси
        public static bool FillInProxy(string proxyFilePath, string proxySource, string proxyMode, string typeOfProxy, string proxyCheckLink)
        {
            try
            {
                if (string.IsNullOrEmpty(proxyMode))
                {
                    MessageBox.Show("Выберите режим работы прокси");
                    return false;
                }

                if (proxyMode == "Не использовать")
                {
                    ProxyMode = proxyMode;
                    return true;
                }

                Proxy.Clear();
                Position = -1;
                if (String.IsNullOrEmpty(proxySource))
                {
                    MessageBox.Show("Выберите источник прокси!");
                    return false;
                }
                if (proxySource == "Файл")
                {
                    if (String.IsNullOrEmpty(proxyFilePath))
                    {
                        MessageBox.Show("Введите путь к файлу с прокси!");
                        return false;
                    }
                    if (!File.Exists(proxyFilePath))
                    {
                        MessageBox.Show("Файла с прокси не существует!");
                        return false;
                    }

                    Proxy.AddRange(File.ReadAllLines(proxyFilePath));

                    if (Proxy.Count <= 0)
                    {
                        MessageBox.Show("Файл с прокси пустой!");
                        return false;
                    }
                }
                else
                {
                    using (HttpRequest request = new HttpRequest())
                    {
                        Proxy.AddRange(request.Get(proxyFilePath).ToString().Split('\n'));
                    }
                }
                if (String.IsNullOrEmpty(proxyMode))
                {
                    MessageBox.Show("Выберите режим прокси!");
                    return false;
                }
                if (String.IsNullOrEmpty(typeOfProxy))
                {
                    MessageBox.Show("Выберите тип прокси!");
                    return false;
                }
                if (proxyMode == "Проверять" && String.IsNullOrEmpty(proxyCheckLink))
                {
                    MessageBox.Show("Введите ссылку на проверку прокси!");
                    return false;
                }

                TypeOfProxy = typeOfProxy;
                ProxyCheckLink = proxyCheckLink;
                ProxyMode = proxyMode;

                return true;
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return false;
        }
        #endregion

        #region Метод проверки Прокси
        public static bool CheckProxy(ProxyClient proxyClient)
        {
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    request.UserAgent = GetUserAgent.get();
                    request.EnableEncodingContent = true;
                    request.Proxy = proxyClient;
                    request.Get(ProxyCheckLink);
                    if (request.Response != null)
                        return true;
                }
            }
            catch {};
            return false;
        }
        #endregion

        #region Метод выдачи Прокси
        public static ProxyClient get()
        {
            try
            {
                // Если не использовать прокси - возвращаем null
                if (ProxyMode == "Не использовать")
                    return null;

                // Если проверять прокси, проверяем прокси и возвращаем прокси если Прокси Живой.
                if (ProxyMode == "Проверять")
                {
                    while (true)
                    {
                        Position++;
                        if (Position >= Proxy.Count)
                        {
                            Position = 0;
                        }
                        if (!String.IsNullOrEmpty(Proxy[Position]))
                        {
                            switch (TypeOfProxy)
                            {
                                case "HTTP":
                                    if (CheckProxy(ProxyClient.Parse(ProxyType.HTTP, Proxy[Position])))
                                        return ProxyClient.Parse(ProxyType.HTTP, Proxy[Position]);
                                    else
                                        continue;
                                case "Socks4":
                                    if (CheckProxy(ProxyClient.Parse(ProxyType.Socks4, Proxy[Position])))
                                        return ProxyClient.Parse(ProxyType.Socks4, Proxy[Position]);
                                    else
                                        continue;
                                case "Socks5":
                                    if (CheckProxy(ProxyClient.Parse(ProxyType.Socks5, Proxy[Position])))
                                        return ProxyClient.Parse(ProxyType.Socks5, Proxy[Position]);
                                    else
                                        continue;
                                default:
                                    return null;
                            }
                        }
                    }
                }

                // Если не проверять прокси, просто возвращаем.
                while (true)
                {
                    Position++;
                    if (Position >= Proxy.Count)
                    {
                        Position = 0;
                    }
                    if (!String.IsNullOrEmpty(Proxy[Position]))
                    {
                        switch (TypeOfProxy)
                        {
                            case "HTTP":
                                return ProxyClient.Parse(ProxyType.HTTP, Proxy[Position]);
                            case "Socks4":
                                return ProxyClient.Parse(ProxyType.Socks4, Proxy[Position]);
                            case "Socks5":
                                return ProxyClient.Parse(ProxyType.Socks5, Proxy[Position]);
                            default:
                                return null;
                        }
                    }
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return null;
        }
        #endregion
    }
}
