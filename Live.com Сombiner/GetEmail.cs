using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Live.com_Сombiner
{
    class GetEmail
    {
        #region Описание свойств класса
        /// <summary>
        /// List с логинами
        /// </summary>
        public static List<string> EmailPassword = new List<string>();
        #endregion

        #region Метод заполнения данных для регистрации
        /// <summary>
        /// Метод заполнения данных для регистрации
        /// </summary>
        /// <param name="LoginGenerateCheckBox">True - генерировать логины</param>
        /// <param name="LoginFileBox">Путь к файлу с логинами</param>
        /// <param name="PasswordGenerateCheckBox">True - генерировать пароли</param>
        /// <param name="PasswordFileBox">Путь к файлу с паролями</param>
        /// <returns></returns>
        public static bool FillInData(string EmailBox)
        {
            try
            {
                EmailPassword.Clear();

                #region Загружаем в List Email:Password
                if (String.IsNullOrEmpty(EmailBox))
                {
                    MessageBox.Show("Введите путь к файлу с почтами!");
                    return false;
                }
                if (!File.Exists(EmailBox))
                {
                    MessageBox.Show("Файла с почтами не существует!");
                    return false;
                }
                EmailPassword.AddRange(File.ReadAllLines(EmailBox));

                if (EmailPassword.Count <= 0)
                {
                    MessageBox.Show("Файл с Почтами пустой!");
                    return false;
                }
                #endregion

                return true;
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return false;
        }
        #endregion

        #region Метод возврата данных для регистрации
        /// <summary>
        /// Метод возврата данных для регистрации
        /// </summary>
        /// <returns>Возвращаем кортеж с: Именем, Фамилией, Почтой, Паролем</returns>
        public static (string Email, string Password) Get()
        {
            try
            {
                string Email = EmailPassword[0].Substring(0, EmailPassword[0].IndexOf(":"));
                string Password = EmailPassword[0].Substring(EmailPassword[0].IndexOf(":") + 1);
                EmailPassword.RemoveAt(0);
                return (Email, Password);
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return (null, null);
        }
        #endregion
    }
}
