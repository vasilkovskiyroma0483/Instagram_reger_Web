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
    class Controller
    {
        #region Свойства класса
        /// <summary>
        /// List с потоками
        /// </summary>
        public static List<Thread> Threads = new List<Thread>();
        /// <summary>
        /// Переменная для запуска и прекращения работы Таймера
        /// </summary>
        public static bool isAliveTimer;
        /// <summary>
        /// Количество потоков
        /// </summary>
        public static int countThread;
        #endregion
        public static void StartThread()
        {
            try
            {
                SaveData.WriteToLog("SYSTEM", $"Начало работы программы. Режим {WorkWithAccount.OperatingMode}");
                Threads.Clear();
                SaveData.WriteToLog("SYSTEM", $"Создаем потоки");
                for (int i = 0; i < countThread; i++)
                {
                    Thread thread = new Thread(() => WorkWithAccount.StartWork()) { IsBackground = true };
                    thread.Name = $"Поток {i}";
                    Threads.Add(thread);
                    Threads[i].Start();
                }
                SaveData.WriteToLog("SYSTEM", $"Создано {Threads.Count.ToString()} потоков");
                new Thread(() => CheckCompletion()) { IsBackground = true }.Start();

            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }

        #region Проверка состояния потоков
        public static void CheckCompletion()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(500);
                    if (!IsAlive())
                    {
                        MessageBox.Show("Программа закончила свою работу");
                        SaveData.WriteToLog("SYSTEM", $"Программа закончила свою работу");
                        isAliveTimer = false;
                        return;
                    }
                }
            }
            catch (Exception exception)
            { MessageBox.Show(exception.Message); }
        }
        public static bool IsAlive()
        {
            try
            {
                for (int i = 0; i < Threads.Count; i++)
                {
                    if (Threads[i] != null && Threads[i].IsAlive)
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception)
            { MessageBox.Show(exception.Message); }
            return false;
        }
        #endregion
    }
}
