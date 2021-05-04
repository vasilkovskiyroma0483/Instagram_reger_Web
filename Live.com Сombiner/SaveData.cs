using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Live.com_Сombiner
{
    class SaveData
    {
        #region Свойства класса
        #region List для сохранения лога и аккаунтов
        /// <summary>
        /// Лист с Логом работы
        /// </summary>
        public static List<string> Log = new List<string>();
        /// <summary>
        /// Лист с успешно зарегестрированными аккаунтами
        /// </summary>
        public static List<string> GoodRegistrationList = new List<string>();
        /// <summary>
        /// Лист с отработанными аккаунтами
        /// </summary>
        public static List<string> ProcessedRegistrationList = new List<string>();
        /// <summary>
        /// Лист с не успешно зарегестрированными аккаунтами
        /// </summary>
        public static List<string> InvalidRegistrationList = new List<string>();
        /// <summary>
        /// Лист с аккаунтами, на которых чекпоинт с номером
        /// </summary>
        public static List<string> NumberErrorList = new List<string>();
        /// <summary>
        /// Лист не валидных Mail почтовиков.
        /// </summary>
        public static List<string> InvalidEmailList = new List<string>();
        /// <summary>
        /// Лист аккаунтов с грязным соксом.
        /// </summary>
        public static List<string> BadProxyList = new List<string>();
        #endregion

        /// <summary>
        /// Счетчики успешно выполненных операций по регистрации
        /// </summary>
        public static int GoodRegistration, InvalidRegistration, UsedRegistration, NumberError, InvalidEmail, BadProxy;
        #endregion

        #region Метод записи данных в лог
        public static void WriteToLog(string account, string text)
        {
            try
            {
                lock (WorkWithAccount.LogOBJ)
                {
                    Log.Add($"[{Form1.stopwatch.Elapsed.ToString("hh\\:mm\\:ss")}] [{Thread.CurrentThread.Name}] {account} - {text}");
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Метод записи аккаунтов в файлы
        public static void SaveAccount(string account, List<string> file)
        {
            try
            {
                lock (WorkWithAccount.LogOBJ)
                {
                    file.Add(account);
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion
    }
}
