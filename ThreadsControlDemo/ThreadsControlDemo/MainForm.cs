using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ThreadsControlDemo
{
    public partial class MainForm : Form
    {
        private int ThreadMax = 5;//开启子线程最多个数
        private int Seed = 1000;//数量到达基数 超此数则开启子线程进行任务操作

        Thread threadMain;//主管分配子线程的主线程
        private object ThreadCountOperateLock = new object(); //子线程个数操作锁
        private int ThreadRun = 0;//正在运行的子线程个数
        ManualResetEvent MainManualResetEvent = new ManualResetEvent(true);// 主管分配子线程的主线程暂停
        ManualResetEvent manualResetEvent = new ManualResetEvent(true);// 子线程数量达到ThreadMax分配子线程暂停

        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Text = "运行中...";
            btnStart.Enabled = false;

            //确认订单
            threadMain = new Thread(new ThreadStart(threadMainWork));
            threadMain.IsBackground = true;
            threadMain.Start();
        }

        #region 线程操作
        private void threadMainWork()
        {
            IList<string> orderList = null;
            orderList = new List<string>();

            for (int i = 0; i < 19000; i++)
            {
                orderList.Add(i.ToString());
            }
            while (true)
            {
                if (null != orderList && orderList.Count > 0)
                {
                    int threads = orderList.Count % Seed == 0 ? orderList.Count / Seed : orderList.Count / Seed + 1;

                    ShowMessage(orderList.Count + "分" + threads + "批");
                    for (int t = 0; t < threads; t++)
                    {
                        manualResetEvent.WaitOne();//主线程暂停 直到所有任务完成
                        if (ThreadRun <= ThreadMax)
                        {
                            //ShowMessage("申请新线程:" + ThreadRun + " i【" + t + "】");
                            //ShowMessage("申请新线程:第【" + t + "】批");
                            BackgroundWorkerParam backgroundWorkerParam = new BackgroundWorkerParam();
                            backgroundWorkerParam.BatSerial = t;
                            backgroundWorkerParam.StartIndex = Seed * t;
                            if (t == threads - 1)
                            {
                                backgroundWorkerParam.EndIndex = orderList.Count - 1;
                            }
                            else
                            {
                                backgroundWorkerParam.EndIndex = Seed * (t + 1) - 1;
                            }
                            backgroundWorkerParam.Orders = new List<string>();
                            for (int index = backgroundWorkerParam.StartIndex; index <= backgroundWorkerParam.EndIndex; index++)
                            {
                                backgroundWorkerParam.Orders.Add(orderList[index]);
                            }
                            ShowMessage("申请新线程:第【" + t + "】批列表序号【" + backgroundWorkerParam.StartIndex + "】-【" + backgroundWorkerParam.EndIndex + "】");
                            //新建进进程
                            BackgroundWorker backgroundWorker = new BackgroundWorker();
                            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_RunWork);
                            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackGroundWorker_RunWorkerCompleted);
                            backgroundWorker.RunWorkerAsync(backgroundWorkerParam);

                            Thread.Sleep(1000);
                            ThreadCountOprate("+");
                        }
                    }

                    //分配完之后主线程要设置成等待状态
                    ShowMessage("主线程开始等待所有子线程完成工作任务");
                    MainManualResetEvent.Reset();
                }
                else
                {
                    ShowMessage("没有数据");
                }

                MainManualResetEvent.WaitOne();//主线程暂停 直到所有任务完成
                ShowMessage("所有子线程完成工作任务 主线程继续");

                Thread.Sleep(5 * 60 * 1000);//5分钟
            }
        }

        private void BackgroundWorker_RunWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorkerParam backgroundWorkerParam = (BackgroundWorkerParam)e.Argument;
            ShowMessage("子线程【" + backgroundWorkerParam.BatSerial + "】工作中...");
            try
            {
                SynData(backgroundWorkerParam);
            }
            catch (Exception ex)
            {
                ShowMessage("子线程【" + backgroundWorkerParam.BatSerial + "】异常：" + ex.Message);
            }
            ShowMessage("子线程【" + backgroundWorkerParam.BatSerial + "】工作完成");
        }

        private void BackGroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ThreadCountOprate("-");
            if (ThreadRun <= 0)
            {
                MainManualResetEvent.Set();
            }
        }

        /// <summary>
        /// 数据处理操作
        /// </summary>
        /// <param name="backgroundWorkerParam"></param>
        private void SynData(BackgroundWorkerParam backgroundWorkerParam)
        {
            ShowMessage("开始处理第【" + backgroundWorkerParam.BatSerial + "】批数据...");
            try
            {
                if (null != backgroundWorkerParam.Orders && 0 < backgroundWorkerParam.Orders.Count)
                {
                    for (int o = 0; o < backgroundWorkerParam.Orders.Count; o++)
                    {
                        //ShowMessage(backgroundWorkerParam.Orders[o]);
                        Thread.Sleep(5);
                    }
                }
                else
                {
                    ShowMessage("第【" + backgroundWorkerParam.BatSerial + "】批数据个数为0");
                }
            }
            catch (Exception ex)
            {
                ShowMessage("处理第【" + backgroundWorkerParam.BatSerial + "】批数据异常" + ex.Message);
            }
            ShowMessage("第【" + backgroundWorkerParam.BatSerial + "】批数据处理完成.");
        }

        /// <summary>
        /// 对正在运行的线程个数进行增减
        /// "“+”进行加1操作 “-”进行减1操作
        /// </summary>
        /// <param name="num"></param>
        private void ThreadCountOprate(string operate)
        {
            lock (ThreadCountOperateLock)
            {
                if (operate == "+")
                {
                    ThreadRun = ThreadRun + 1;
                    if (ThreadRun == ThreadMax)
                    {
                        ShowMessage("分配线程开始等待");
                        //线程数达到最大值 设置主线程等待
                        manualResetEvent.Reset();
                    }
                }
                else if (operate == "-")
                {
                    ThreadRun = ThreadRun - 1;
                    if (ThreadRun == ThreadMax - 1)
                    {
                        //允许暂停的地方继续
                        ShowMessage("分配线程继续");
                        manualResetEvent.Set();
                    }
                }
            }
        }
        #endregion

        private void ShowMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    this.txtMessage.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + "\r\n");
                }));
            }
            else
            {
                this.txtMessage.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + "\r\n");
            }
        }
    }

    /// <summary>
    /// BackgroundWorker参数
    /// </summary>
    public class BackgroundWorkerParam
    {
        /// <summary>
        /// 批次号
        /// </summary>
        public int BatSerial { get; set; }

        /// <summary>
        /// 总订单列表中截取的开始序号
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 总订单列表中截取的结束序号
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// 总订单列表中截取订单列表
        /// </summary>
        public List<string> Orders { get; set; }
    }
}
