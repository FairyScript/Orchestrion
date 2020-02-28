using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Orchestrion
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            //UI线程未捕获异常处理事件（UI主线程）
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        //UI线程未捕获异常处理事件（UI主线程）
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ExceptionMessageBox(e.Exception, "UI线程异常");

            e.Handled = true;//表示异常已处理，可以继续运行
        }

        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
        //如果UI线程异常DispatcherUnhandledException未注册，则如果发生了UI线程未处理异常也会触发此异常事件
        //此机制的异常捕获后应用程序会直接终止。没有像DispatcherUnhandledException事件中的Handler=true的处理方式，可以通过比如Dispatcher.Invoke将子线程异常丢在UI主线程异常处理机制中处理
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionMessageBox((Exception)e.ExceptionObject);
        }

        //Task线程内未捕获异常处理事件
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ExceptionMessageBox(e.Exception);
        }

        public void ExceptionMessageBox(Exception e, string title = "出错啦")
        {

            string str =
                $"软件发生了意料之外的错误,请截图发给我!\n" +
                $"==================================\n" +
                $"软件版本: {Assembly.GetExecutingAssembly().GetName().Version}\n" +
                $"异常类型: {e.GetType()}\n" +
                $"异常描述: {e.Message}\n" +
                $"调用栈:\n{e.StackTrace}\n" +
                $"=================================="
                ;
            MessageBox.Show(str, title);
        }
        //异常处理 封装
        private void OnExceptionHandler(Exception ex)
        {
            if (ex != null)
            {
                string errorMsg = "";
                if (ex.InnerException != null)
                {
                    errorMsg += String.Format("【InnerException】{0}\n{1}\n", ex.InnerException.Message, ex.InnerException.StackTrace);
                }
                errorMsg += String.Format("{0}\n{1}", ex.Message, ex.StackTrace);

            }
        }
    }
}
