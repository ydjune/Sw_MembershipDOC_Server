using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordTextExt.Office;
using kr.ac.kaist.swrc.jhannanum.comm;
using kr.ac.kaist.swrc.jhannanum.hannanum;
using System.Data;
using System.IO;
using System.Data.SqlClient;
namespace SW_Membership_Assignment2
{
    public static class getDocument
    {
        public static string dirTotal = @"c:\SSM_Assignment2\result.total.txt";
        public static System.IO.DirectoryInfo totalDir = new System.IO.DirectoryInfo(dirTotal);
        public static List<string> AllDocumentName = new List<string>();
        public static int FileCount = -1;

        public static List<string> getAllDocumentName()
        {
            return AllDocumentName;
        }

        public static int getFileCount()
        {
            return FileCount;
        }


        // 모든 문서 가져오기
        public static List<Tuple<string, string, int>>[] getEveryDocument(int currentDocumentNumber)
        {
            List<Tuple<string, string, int>>[] everyPage = new List<Tuple<string, string, int>>[100];
            StreamReader txtReader;
            AllDocumentName.Clear();

            String returnID = "SELECT ex_pr_name from dbo.secmem_board_extra_ssm_project WHERE wr_id=" + currentDocumentNumber;
            SqlCommand returnScom = new SqlCommand(returnID, MainServer.scon);
            returnScom.Connection = MainServer.scon;
            String currentName = (String)returnScom.ExecuteScalar();

            Console.WriteLine("현재 클릭 " + ", " + currentDocumentNumber + ", " + currentName);
            
            FileCount = -1;
            foreach (var item in totalDir.GetFiles())
            {
                
                if (Path.GetFileNameWithoutExtension(item.Name).Equals(currentName) )
                {
                    Console.WriteLine("continue " + ", " + currentDocumentNumber + ", " +item.Name);
                    continue;
                }
                ++FileCount;
                everyPage[FileCount] = new List<Tuple<string, string, int>>();
                AllDocumentName.Add(Path.GetFileNameWithoutExtension(item.Name));

                string line;
                String Korean = null;
                String English = null;
                txtReader = new StreamReader(dirTotal + @"\" + item.Name);
                while ((line = txtReader.ReadLine()) != null)
                {
                    if (line == null) { continue; }
                    string[] words = line.Split(' ');

                    // words[0] 공백       words[1] 단어         words[2] 빈도수
                    String strSql = "SELECT dic_korean, dic_english FROM dbo.secmem_dictionary where dic_korean='" + words[1] + "' or dic_english='" + words[1] + "'";
                    SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                    scom.Connection = MainServer.scon;
                    SqlDataReader reader = scom.ExecuteReader();
                    while (reader.Read())
                    {
                        Korean = reader.GetString(0);
                        English = reader.GetString(1);
                        if (Korean != null && English != null)
                        {
                            break;
                        }
                    }

                    if (Korean != null && English != null)
                    {
                        everyPage[FileCount].Add(new Tuple<string, string, int>(Korean, English, System.Convert.ToInt32(words[2])));
                    }

                    reader.Close();
                    Korean = null;
                    English = null;
                }
                txtReader.Close();
            } // 모든 문서 들고오기: 문서 없으면 FileCount = -1

            return everyPage;
        }


        // 현재 문서 가져오기
        public static List<Tuple<string, string, int>> getCurrent(int documentNumber)
        {
            List<Tuple<string, string, int>> currentPage = new List<Tuple<string, string, int>>();

            String returnID = "SELECT ex_pr_name from dbo.secmem_board_extra_ssm_project WHERE wr_id=" + documentNumber;
            SqlCommand returnScom = new SqlCommand(returnID, MainServer.scon);
            returnScom.Connection = MainServer.scon;
            String currentName = (String)returnScom.ExecuteScalar();

            //Console.WriteLine(dirTotal + @"\" + currentName + ".txt");
            StreamReader txtReader = new StreamReader(dirTotal + @"\" + currentName + ".txt");
            string line;
            String Korean = null;
            String English = null;

            while ((line = txtReader.ReadLine()) != null)
            {
                if (line == null) { continue; }
                string[] words = line.Split(' ');
                // words[0] 공백       words[1] 단어         words[2] 빈도수
                // 사전 쿼리
                String strSql = "SELECT dic_korean, dic_english FROM dbo.secmem_dictionary where dic_korean='" + words[1] + "' or dic_english='" + words[1] + "'";
                SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                scom.Connection = MainServer.scon;
                SqlDataReader reader = scom.ExecuteReader();
                while (reader.Read())
                {
                    Korean = reader.GetString(0);
                    English = reader.GetString(1);
                    if (Korean != null && English != null)
                    {
                        break;
                    }
                }

                if (Korean != null && English != null)
                {
                    // 한국어인지 영어인지 구분해서 item1, item2 선택하기 -> 아직 안함
                    // 값을 꺼내고, 지우고, 그다음에 더한다음에 다시 삽입시키는 방법밖에 없는것 같다.
                    if (currentPage.Any(t => t.Item1 == Korean || t.Item1 == English))
                    {
                        int edit = currentPage.Find(t => t.Item1 == Korean || t.Item1 == English).Item3;
                        String temp1 = currentPage.Find(t => t.Item1 == Korean || t.Item1 == English).Item1;
                        String temp2 = currentPage.Find(t => t.Item1 == Korean || t.Item1 == English).Item2;
                        currentPage.RemoveAll(it => it.Item1 == temp1 && it.Item2 == temp2);
                        currentPage.Add(new Tuple<string, string, int>(temp1, temp2, System.Convert.ToInt32(words[2]) + edit));
                    }
                    else
                    {
                        currentPage.Add(new Tuple<string, string, int>(Korean, English, System.Convert.ToInt32(words[2])));
                    }
                }
                Korean = null;
                English = null;
                reader.Close();
            }
            txtReader.Close();
            return currentPage;
        }
    }
}

