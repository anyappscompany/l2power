using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Net;
using System.IO;

namespace l2power
{
    class Program
    {
        //очередь адресов для закачки
        static Queue<string> URLs = new Queue<string>();
        //список скачанных страниц
        static List<string> HTMLs = new List<string>();
        //локер для очереди адресов
        static object URLlocker = new object();
        //локер для списка скачанных страниц
        static object HTMLlocker = new object();
        //очередь ошибок
        static Queue<Exception> exceptions = new Queue<Exception>();
        static void Main(string[] args)
        {
            string line;
            StreamReader file = new System.IO.StreamReader("power.txt");
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                URLs.Enqueue(line);
            }

            ManualResetEvent[] handles = new ManualResetEvent[1];
            //создаем и запускаем 3 потока
            for (int i = 0; i < 1; i++)
            {
                handles[i] = new ManualResetEvent(false);
                (new Thread(new ParameterizedThreadStart(Download))).Start(handles[i]);
            }
            //ожидаем, пока все потоки отработают
            WaitHandle.WaitAll(handles);
            //проверяем ошибки, если были - выводим
            foreach (Exception ex in exceptions)
                Console.WriteLine(ex.Message);
            //сохраняем закачанные страницы в файлы
            try
            {
                for (int i = 0; i < HTMLs.Count; i++)
                    File.WriteAllText("c:\\" + i + ".html", HTMLs[i]);
                Console.WriteLine(HTMLs.Count + " files saved");
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            //
            Console.WriteLine("Download completed");
            Console.ReadLine();
        }
        public static void Download(object handle)
        {
            //будем крутить цикл, пока не закончатся ULR в очереди
            while (true)
            try
            {
                string URL;
                string login;
                string passwd;
                string mail;
                //блокируем очередь URL и достаем оттуда один адрес
                lock (URLlocker)
                {
                    if (URLs.Count == 0)
                        break;//адресов больше нет, выходим из метода, завершаем поток
                    else
                    {
                        URL = URLs.Dequeue();
                        string[] logins = URL.Split('@');
                        login = logins[0];

                        string[] passwds = URL.Split(':');
                        passwd = passwds[1];
                        mail = passwds[0];
                    }
                }
                Console.WriteLine(URL + " - start downloading ...");
                //скачиваем страницу

                string HTML = POST("http://l2power.ru/xCp/?do=module&id=reg", "login=" + login + "&pwd=" + passwd + "&repwd=" + passwd + "&mail=" + mail + "&serverId=1&capcha=");
                Console.WriteLine("login=" + login + "&pwd=" + passwd + "&repwd=" + passwd + "&mail=" + mail + "&serverId=1&capcha=");
                //Console.ReadKey();
                //блокируем список скачанных страниц, и заносим туда свою страницу
                lock (HTMLlocker)
                {
                    HTMLs.Add(HTML);

                    
                    //Console.ReadKey();
                    if(HTML.IndexOf("существует") != -1)
                    {
                        using (var writer = new StreamWriter("result.txt", true))
                        {                           
                            //Добавляем к старому содержимому файла
                            writer.WriteLine(login + ":" + passwd + ":" + mail);
                        }
                    }
                }
                //
                Console.WriteLine(URL + " - downloaded (" + HTML.Length + " bytes)");
            }
            catch (ThreadAbortException)
            {
                //это исключение возникает если главный поток хочет завершить приложение
                //просто выходим из цикла, и завершаем выполнение
                break;
            }
            catch (Exception ex)
            {
                //в процессе работы возникло исключение
                //заносим ошибку в очередь ошибок, предварительно залочив ее
                lock (exceptions)
                    exceptions.Enqueue(ex);
                //берем следующий URL
                continue;
            }
            //устанавливаем флажок хендла, что бы сообщить главному потоку о том, что мы отработали
            ((ManualResetEvent)handle).Set();
        }
        private static string POST(string Url, string Data)
{
  WebRequest req = WebRequest.Create(Url);
  req.Method = "POST";
  req.Timeout = 100000;
  req.ContentType = "application/x-www-form-urlencoded";
  byte[] sentData = Encoding.GetEncoding(1251).GetBytes(Data);
  req.ContentLength = sentData.Length;
  Stream sendStream = req.GetRequestStream();
  sendStream.Write(sentData, 0, sentData.Length);
  sendStream.Close();
  WebResponse res = req.GetResponse();
  Stream ReceiveStream = res.GetResponseStream();
  StreamReader sr = new StreamReader(ReceiveStream, Encoding.GetEncoding("windows-1251"));
  //Кодировка указывается в зависимости от кодировки ответа сервера
  Char[] read = new Char[256];
  int count = sr.Read(read, 0, 256);
  string Out = String.Empty;
  while (count > 0)
  {
    String str = new String(read, 0, count);
    Out += str;
    count = sr.Read(read, 0, 256);
  }
  return Out;
}
    }
}
