using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using xNet;

namespace Live.com_Сombiner
{
    class GetUserAgent
    {
        #region Свойства класса
        /// <summary>
        /// Использовать внутренние UI или пользователя. True - внутренние, False - пользовательские
        /// </summary>
        public static bool CustomUserAgent = true;
        /// <summary>
        /// Список UserAgent
        /// </summary>
        public static List<string> UserAgent = new List<string>();
        public static Random rand = new Random((int)DateTime.Now.Ticks);
        #endregion

        #region Метод заполнения UserAgent-ов
        /// <summary>
        /// Метод заполнения UserAgent-ов
        /// </summary>
        /// <param name="PathtoUserAgent">Путь к UserAgent (Файл/Ссылка)</param>
        /// <param name="BuiltInUserAgents">Использовать личные или пользовательские UserAgent</param>
        /// <returns></returns>
        public static bool FillInUserAgents(string PathtoUserAgent, bool BuiltInUserAgents)
        {
            try
            {
                if (BuiltInUserAgents)
                {
                    CustomUserAgent = true;
                    return true;
                }
                else
                {
                    if (String.IsNullOrEmpty(PathtoUserAgent))
                    {
                        MessageBox.Show("Путь к файлу не указан!");
                        return false;
                    }
                    if (!File.Exists(PathtoUserAgent))
                    {
                        MessageBox.Show("Файла с UserAgent не существует!");
                        return false;
                    }
                    CustomUserAgent = false;
                    UserAgent.Clear();
                    UserAgent.AddRange(File.ReadAllLines(PathtoUserAgent));
                    if (UserAgent.Count <= 0)
                    {
                        MessageBox.Show("Файла с UserAgent пустой!");
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return false;
        }
        #endregion

        #region Метод выдачи UserAgent
        public static string get()
        {
            try
            {
                if (CustomUserAgent)
                {
                    return Http.ChromeUserAgent();
                }
                else
                {
                    return UserAgent[rand.Next(0, UserAgent.Count)];
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return null;
        }
        #endregion
    }
}
