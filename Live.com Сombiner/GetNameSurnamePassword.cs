using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Live.com_Сombiner
{
    class GetNameSurnamePassword
    {
        #region Описание свойств класса
        /// <summary>
        /// List с логинами
        /// </summary>
        public static List<string> ListNameSurname = new List<string>();
        /// <summary>
        /// List с паролями
        /// </summary>
        public static List<string> ListPassword = new List<string>();
        /// <summary>
        /// Чекбоксы на использование кастомных Почт/паролей или пользовательских
        /// </summary>
        public static bool GeneratePasswordCheck;
        public static Random rand = new Random((int)DateTime.Now.Ticks);
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
        public static bool FillInData(string NameSurnameBox, bool PasswordGenerateCheckBox, string PasswordFileBox)
        {
            try
            {
                ListNameSurname.Clear();
                ListPassword.Clear();
                #region Загружаем в List имена/фамилии
                if (String.IsNullOrEmpty(NameSurnameBox))
                {
                    MessageBox.Show("Введите путь к файлу с именами/фамилиями!");
                    return false;
                }
                if (!File.Exists(NameSurnameBox))
                {
                    MessageBox.Show("Файла с именами/фамилиями не существует!");
                    return false;
                }
                ListNameSurname.AddRange(File.ReadAllLines(NameSurnameBox));
                if (ListNameSurname.Count <= 0)
                {
                    MessageBox.Show("Файл с именами/фамилиями пустой!");
                    return false;
                }
                #endregion

                #region Проверка на генерацию пароля или использовать пользовательские
                GeneratePasswordCheck = PasswordGenerateCheckBox;
                if (!GeneratePasswordCheck)
                {
                    #region Загружаем в List Пароли
                    if (String.IsNullOrEmpty(PasswordFileBox))
                    {
                        MessageBox.Show("Введите путь к файлу с паролями!");
                        return false;
                    }
                    if (!File.Exists(PasswordFileBox))
                    {
                        MessageBox.Show("Файла с паролями не существует!");
                        return false;
                    }
                    ListPassword.AddRange(File.ReadAllLines(PasswordFileBox));
                    if (ListPassword.Count <= 0)
                    {
                        MessageBox.Show("Файл с паролями пустой!");
                        return false;
                    }
                    #endregion
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
        public static (string nameSurname, string password) Get()
        {
            try
            {
                string Password = "";

                #region Подготовка к возврату пароля
                if (GeneratePasswordCheck)
                {
                    Password = GenerateRandomSymbol(8, 13, SourceLine);
                }
                else
                {
                    Password = ListPassword[rand.Next(ListPassword.Count)];
                }
                #endregion

                return (ListNameSurname[rand.Next(ListNameSurname.Count)], Password);
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return (null, null);
        }
        #endregion

        #region Метод генерации рандомных символов либо пароля
        public static string Numbers = "0123456789";
        public static string SourceLine = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string Symbol = "!@#$%^&*()_+-={[]}|'<,.>/?";
        /// <summary>
        /// Метод генерации рандомных символов либо пароля
        /// </summary>
        /// <param name="min">Минимальная длина строки</param>
        /// <param name="max">Максимальная длина строки</param>
        /// <param name="sourceLine">С какой строки берем символы</param>
        /// <param name="password">Если True - генерируем пароль</param>
        /// <returns></returns>
        public static string GenerateRandomSymbol(int min, int max, string sourceLine)
        {
            string result = "";
            try
            {
                int count = rand.Next(min, max);
                for (int i = 0; i < count; i++)
                    result += sourceLine[rand.Next(sourceLine.Length)];

                int symbol = rand.Next(0, result.Length);
                result = result.Replace(result[symbol], Char.ToUpper(result[symbol]));
                result = result.Insert(rand.Next(0, result.Length), (Symbol[rand.Next(Symbol.Length)]).ToString());
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return result;
        }
        #endregion
    }
}
