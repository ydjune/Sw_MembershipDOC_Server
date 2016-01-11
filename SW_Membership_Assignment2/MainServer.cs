using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using kr.ac.kaist.swrc.jhannanum.comm;
using kr.ac.kaist.swrc.jhannanum.hannanum;
using System.Text.RegularExpressions;
using System.Data.SqlClient;    // MSSQL 연동
using WordTextExt.Office;
using System.Threading;

namespace SW_Membership_Assignment2
{
    public partial class MainServer : Form
    {
        // DB연결하기
        public static SqlConnection scon;
        private static FileTransfer ft = null;             // 파일 전송 클래스 객체 변수
        private Thread server_th = null;	        // 스레드 	
        private TextExtHelper txtExtHelper = null;


        public MainServer()
        {
            InitializeComponent();
            ft = new FileTransfer(this);	  // FileTransfer 객체 변수 추가	
            txtExtHelper = new TextExtHelper(); //텍스트 추출 클래스 객체 변수
        }

        private void MainServer_Load(object sender, EventArgs e)
        {

        }

        // ################################ 서버 프로그램 가동 #######################################
        private void ServerStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (ServerStart.Text == "ServerStart")
                {
                    server_th = new Thread(new ThreadStart(ft.ServerStart));
                    server_th.Start();
                    ServerStart.Text = "ServerStop";

                }
                else
                {
                    ft.ServerStop();
                    if (server_th.IsAlive) server_th.Abort();
                    ServerStart.Text = "ServerStart";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        

        private static string[] Tokenize(string text)
        {
            text = Regex.Replace(text, "<[^<>]+>", "");
            text = Regex.Replace(text, "[0-9]+", "");
            text = Regex.Replace(text, @"(http|https)://[^\s]*", "");
            text = Regex.Replace(text, @"[^\s]+@[^\s]+", "");
            text = Regex.Replace(text, "[$]+", "");
            text = Regex.Replace(text, @"@[^\s]+", "");
            text = Regex.Replace(text, "_", " ");
            return text.Split(" @$/#.-:&*+=[]?!(){},''\">_<;%\\".ToCharArray());
        }

        // DATABASE CONNECTION
        public static void DataBaseOpen()
        {
            // DB Open
            string connectionString = "server = 210.118.69.165; uid = sa; pwd = tnwls; database = secmem_ver3; ";
            scon = new SqlConnection(connectionString);
            try
            {
                scon.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Data base open error. " + ex.Message);
            }
        }

        public static void DataBaseClose()
        {
            scon.Close();
        }

        private void MainServer_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                
                if (FileTransfer.workflow != null)
                {
                    FileTransfer.workflow.close();
                    Console.WriteLine("Workflow is closed");
                }

                ft.ServerStop();  // 파일 서버 작동 중지

                if ((server_th != null) && (server_th.IsAlive))
                    server_th.Abort();	// 수신 스레드 종료	
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }		
        }
      
    }
}