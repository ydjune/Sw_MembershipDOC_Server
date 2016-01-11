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
    class CorePart
    {
        // ###########################################################################################
        // ######### 텍스트 추출 ### 형태소 분석 ### 주어 목적어 ### TF-IDF ### 코사인 유사도 ########
        // ###########################################################################################
        
        public static void ProgramStart(int returnValue, String path)
        {
            // return value는 현재 누른 문서번호
            Dictionary<string, int> wordCountList = new Dictionary<string, int>();      
            List<Tuple<string, string, double>> _vocabularyTFIDF = new List<Tuple<string, string, double>>();
            List<Tuple<string, string, int>>[] documents = new List<Tuple<string, string, int>>[100];       // 단어와 빈도수 저장(모든 문서)
            List<string> AllDocumentName = new List<string>(); // 현재 모든 문서들의 이름 다 저장해놓기
            Dictionary<string, int> Subject_Object = new Dictionary<string, int>(); // 주어 목적어
            List<string> word = new List<string>(); // 형태소 분석 후 빈도수 저장 안된 결과값

            string dirCurrent = @"c:\SSM_Assignment2\result.current.txt";
            string dirTotal = @"c:\SSM_Assignment2\result.total.txt";
            string dirDictionary = @"c:\SSM_Assignment2\dictionary";
            string dirSTART = @"c:\SSM_Assignment2";
            System.IO.DirectoryInfo currentSTART = new System.IO.DirectoryInfo(dirSTART);
            System.IO.DirectoryInfo currentDir = new System.IO.DirectoryInfo(dirCurrent);

            TextExtHelper txtExtHelper = new TextExtHelper();
            StreamReader txtReader;

            int total_word = new int();             // 문서 Dj에서 모든 단어가 출현한 횟수
            int documentCount = new int();
            int documentTerm = new int();
            int FileCount = -1;

            // ############################ DATA BASE OPEN ###############################################
            MainServer.DataBaseOpen();

            // ############################ 사전 텍스트 파일 데이터 베이스에 저장 ############################
            /*
            StreamReader readfile = new StreamReader(dirDictionary + @"\" + "dictionary.output.txt");
            String readLineWord;
            String englishWord = null;
            while ((readLineWord = readfile.ReadLine()) != null)
            {
                String[] splitStr = Regex.Split(readLineWord, " ");
                for (int i = 1; i < splitStr.Length; i++)
                {
                        englishWord += splitStr[i] + " ";
                }
                // 쿼리문 날려서 디비에 저장
                String strSql = "INSERT INTO dbo.secmem_dictionary VALUES (N'" + splitStr[0] + "', N'" + englishWord + "')";
                SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                scom.Connection = MainServer.scon;
                scom.ExecuteNonQuery();
                englishWord = null;
            }
            readfile.Close();
            */


            // ############################ 텍스트 추출 하기 ###############################################
            if (currentSTART.GetFiles().Length != 0)
            {
                txtExtHelper.OpenFile(path);
                Console.WriteLine("[시작] File NAME: " + path);

                IOfficeFile _file = txtExtHelper.get_file();
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string fileExtension = System.IO.Path.GetExtension(path);
                txtExtHelper.ShowContentsFunc(_file);
                IWordFile wordfile = txtExtHelper.getwordFile();         //텍스트 추출 후 나온 word파일 내용을 읽어와야 여기에 저장이 된다.
                string currentSTARTDocument = wordfile.ParagraphText;
                currentSTARTDocument = currentSTARTDocument.Replace("\r\n", " ");

                // ############################ 형태소분석 ###############################################
                Console.WriteLine("형태소 분석");
                try
                {
                    string textDocument = currentSTARTDocument;
                    FileTransfer.workflow.analyze(textDocument);
                    LinkedList<Sentence> resultList = FileTransfer.workflow.getResultOfDocument(new Sentence(0, 0, false));

                    foreach (Sentence s in resultList)
                    {
                        Eojeol[] eojeolArray = s.Eojeols;
                        for (int i = 0; i < eojeolArray.Length; i++)
                        {
                            if (eojeolArray[i].length > 0)
                            {
                                // eojeolArray[i] : 개인/ncn, 국한/ncpa 명사형태인 단어 뽑혀나옴
                                // string morphemes[] : 개인, 국한   (태그 제거되고 단어만 나옴)
                                String[] morphemes = eojeolArray[i].Morphemes;
                                // ############################ 형태소 분석 돌린 단어 저장 ##################################
                                for (int j = 0; j < morphemes.Length; j++)
                                {
                                    morphemes[j] = morphemes[j].Replace("'", "");
                                    word.Add(morphemes[j]);
                                }
                                // ############################ 주어 목적어 ##################################
                                // jco: 역수를,변형을         jxc: 이것은, TF IDF는       jcs: 단어가   
                                /*
                                if (Tags[Tags.Length - 1].CompareTo("jco") == 0 || Tags[Tags.Length - 1].CompareTo("jxc") == 0 || Tags[Tags.Length - 1].CompareTo("jcs") == 0)
                                {
                                    for (int k = 0; k < Tags.Length - 1; k++)
                                    {
                                        // 주어 목적어중에서 명사만 저장하기 위해
                                        if (System.Text.RegularExpressions.Regex.IsMatch(Tags[k], "nc", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                        {
                                            if (Subject_Object.ContainsKey(morphemes[k]))
                                            {
                                                Subject_Object[morphemes[k]]++;
                                            }
                                            else
                                            {
                                                Subject_Object.Add(morphemes[k], 1);
                                            }
                                        }
                                    }

                                }
                                 */
                            }
                        }
                    }

                    // ##################################### 빈도수 저장 ###############################################
                    Console.WriteLine("빈도수 저장");
                    for (int i = 0; i < word.Count; i++)
                    {
                        // 현재 문서 형태소 분석
                        string value = word[i];
                        if (wordCountList.ContainsKey(value)) wordCountList[value]++;
                        else wordCountList.Add(value, 1);
                    }

                    foreach (var key in wordCountList.Keys.ToList())
                    {
                        if (wordCountList[key] == 1)
                        {
                            wordCountList.Remove(key);
                        }
                    }

                    // 정렬하기
                    var rank = wordCountList.OrderByDescending(num => num.Value);
                    StringBuilder sb = new StringBuilder();
                    foreach (var val in rank)
                    {

                        sb.AppendLine(" " + val.Key + " " + val.Value);
                    }
                    File.WriteAllText(dirCurrent + @"\" + fileName + ".txt", sb.ToString());
                    wordCountList.Clear();
                }
                catch (Exception ie)
                {
                    Console.WriteLine(ie.ToString());
                    return;
                }
                File.Delete(dirSTART + @"\" + fileName);
                txtExtHelper.CloseFile();
            }

            // ################################### TF-IDF ########################################################
            // 현재 문서 고치고, 현재 문서 있을 때 모든 문서에 가중치 결과 값 업데이트까지 고려 해줘야 한다

            Console.WriteLine("TF-IDF시작");

            documentCount = 0;
            if (currentDir.GetFiles().Length != 0)
            {
                // ############################ TF 모든문서 들고오기 ###############################################
                System.IO.DirectoryInfo totalDir = new System.IO.DirectoryInfo(dirTotal);
                foreach (var item in totalDir.GetFiles())
                {
                    ++FileCount;
                    documents[FileCount] = new List<Tuple<string, string, int>>();
                    documentCount++;

                    Console.WriteLine(dirTotal + @"\" + item.Name);
                    txtReader = new StreamReader(dirTotal + @"\" + item.Name);
                    AllDocumentName.Add(item.Name);

                    string line;
                    String Korean = null;
                    String English = null;
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
                            documents[FileCount].Add(new Tuple<string, string, int>(Korean, English, System.Convert.ToInt32(words[2])));
                        }

                        reader.Close();
                        Korean = null;
                        English = null;
                    }
                    txtReader.Close();
                } // 다른 문서 없으면 FileCount = -1


                // ############################ TF 현재 문서 들고오기 ###############################################
                // 들고 올때, 블루투스 <-> bluetooth 같은거니깐 빈도수 같이 더하기
                Console.WriteLine("TF 현재 문서 들고오기");
                foreach (var item in currentDir.GetFiles())
                {
                    ++FileCount;
                    documents[FileCount] = new List<Tuple<string, string, int>>();

                    txtReader = new StreamReader(dirCurrent + @"\" + item.Name);
                    AllDocumentName.Add(item.Name);
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
                            if (documents[FileCount].Any(t => t.Item1 == Korean || t.Item1 == English))
                            {
                                int edit = documents[FileCount].Find(t => t.Item1 == Korean || t.Item1 == English).Item3;
                                String temp1 = documents[FileCount].Find(t => t.Item1 == Korean || t.Item1 == English).Item1;
                                String temp2 = documents[FileCount].Find(t => t.Item1 == Korean || t.Item1 == English).Item2;
                                documents[FileCount].RemoveAll(it => it.Item1 == temp1 && it.Item2 == temp2);
                                documents[FileCount].Add(new Tuple<string, string, int>(temp1, temp2, System.Convert.ToInt32(words[2]) + edit));
                            }
                            else
                            {
                                documents[FileCount].Add(new Tuple<string, string, int>(Korean, English, System.Convert.ToInt32(words[2])));
                            }
                        }
                        Korean = null;
                        English = null;
                        reader.Close();
                    }
                    txtReader.Close();
                    System.IO.File.Move(dirCurrent + @"\" + item.Name, dirTotal + @"\" + item.Name);
                    // 무조건 문서 한개만 들고오기
                    break;  
                }

                _vocabularyTFIDF.Clear();
                // ####################### 현재 문서에 대해서 IDF 계산하기 ########################################
                double IDFcalculate = new double();
                double TF_IDFcalculate = new double();
                int currentFileCount = FileCount;
                double TOTAL_TF_IDF = new double();

                total_word = 0;
                foreach (var item in documents[currentFileCount])
                {
                    total_word += item.Item3;
                    documentTerm = 0;

                    // 다른문서만 비교랑 내꺼 비교
                    for (int current = 0; current < currentFileCount; current++)
                    {
                        if (documents[current].Any(t => t.Item1 == item.Item1 || t.Item1 == item.Item2))
                        {
                            documentTerm++;
                        }
                    }

                    // 각 단어당 문서 개수 찾기 완료 IDF 계산하기 =: documentTerm이 0개 나오면 무한대 되기 때문에 documentTerm에 +1을 해야된다. 그래야 log1은 0됨
                    if (currentFileCount == 0) // 다른문서 하나도 없다.
                    {
                        _vocabularyTFIDF.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 0));
                        TOTAL_TF_IDF = 0;
                    }
                    else
                    {
                        IDFcalculate = Math.Log10((double)(currentFileCount + 1) / (1 + documentTerm));
                        //TF_IDFcalculate = ((double)(item.Item3) / total_word) * IDFcalculate;
                        TF_IDFcalculate = (0.5 + 0.5 * ((double)(item.Item3) / total_word)) * IDFcalculate;
                        _vocabularyTFIDF.Add(new Tuple<string, string, double>(item.Item1, item.Item2, TF_IDFcalculate));
                        TOTAL_TF_IDF += TF_IDFcalculate;
                    }
                    TF_IDFcalculate = 0;
                    IDFcalculate = 0;
                } // _vocabularyTFIDF(단어, TF-IDF)
                Console.WriteLine("현재문서 가중치 다 계산함");

                String updateSql = "UPDATE dbo.secmem_board_extra_ssm_project SET ex_total_tfidf=" + TOTAL_TF_IDF + " WHERE wr_id=" + returnValue;
                SqlCommand updateScom = new SqlCommand(updateSql, MainServer.scon);
                updateScom.Connection = MainServer.scon;
                updateScom.ExecuteNonQuery();
                TOTAL_TF_IDF = 0;


                // ############################# 주어목적어 같이 추가 해주기 ########################################
                /*
                var rank_desc = _vocabularyTFIDF.OrderByDescending(num => num.Item3);
                foreach (var item in rank_desc)
                {
                    if (Subject_Object.ContainsKey(item.Item1) || Subject_Object.ContainsKey(item.Item2))
                    {
                        int val = documents[FileCount].Find(t => t.Item1 == item.Item1).Item3;
                        if (val > 0)
                        {
                            String strSql = "INSERT INTO dbo.secmem_technology_db_ssm_project VALUES (N'" + item.Item1 + "', N'" + item.Item2 + "', N'" + returnValue + "', N'" + item.Item3 + "', N'" + 1 + "', N'" + val + "')";
                            SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                            scom.Connection = MainServer.scon;
                            scom.ExecuteNonQuery();
                        }
                        else
                        {
                            val = documents[FileCount].Find(t => t.Item2 == item.Item1).Item3;
                            String strSql = "INSERT INTO dbo.secmem_technology_db_ssm_project VALUES (N'" + item.Item2 + "', N'" + item.Item1 + "', N'" + returnValue + "', N'" + item.Item3 + "', N'" + 1 + "', N'" + val + "')";
                            SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                            scom.Connection = MainServer.scon;
                            scom.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        int val = documents[FileCount].Find(t => t.Item1 == item.Item1).Item3;
                        if (val > 0)
                        {
                            String strSql = "INSERT INTO dbo.secmem_technology_db_ssm_project VALUES (N'" + item.Item1 + "', N'" + item.Item2 + "', N'" + returnValue + "', N'" + item.Item3 + "', N'" + 0 + "', N'" + val + "')";
                            SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                            scom.Connection = MainServer.scon;
                            scom.ExecuteNonQuery();

                        }
                        else
                        {
                            val = documents[FileCount].Find(t => t.Item2 == item.Item1).Item3;
                            String strSql = "INSERT INTO dbo.secmem_technology_db_ssm_project VALUES (N'" + item.Item2 + "', N'" + item.Item1 + "', N'" + returnValue + "', N'" + item.Item3 + "', N'" + 0 + "', N'" + val + "')";
                            SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                            scom.Connection = MainServer.scon;
                            scom.ExecuteNonQuery();
                        }
                    }
                }
                */


                // ####################################################################################################
                // ################################# 현재 등록된 문서 업데이트 해주기 #################################
                // ####################################################################################################
                if (FileCount == 0)
                {
                    Console.WriteLine("업데이트 필요 없음");
                }
                else
                {
                    int currentValue;
                    // 모든 문서에서 하나씩 읽어와서 TF 구하기 (현재문서)
                    for (int i = 0; i < FileCount; i++)
                    {
                        _vocabularyTFIDF.Clear();
                        // 현재문서에 관한 과제번호 값 알아오기
                        String returnID = "SELECT wr_id from dbo.secmem_board_extra_ssm_project WHERE ex_pr_name = '" + System.IO.Path.GetFileNameWithoutExtension(AllDocumentName[i]) + "'";
                        SqlCommand returnScom = new SqlCommand(returnID, MainServer.scon);
                        returnScom.Connection = MainServer.scon;
                        currentValue = (int)returnScom.ExecuteScalar();
                        Console.WriteLine("");
                        Console.WriteLine(System.IO.Path.GetFileNameWithoutExtension(AllDocumentName[i]) + ", " + currentValue);

                        documentTerm = 0;
                        double IDFtemp;
                        double TF_IDFtemp;
                        total_word = 0;

                        // documents[i]는 현재문서
                        foreach (var currentitem in documents[i])   
                        {
                            total_word += currentitem.Item3;
                            for (int index = 0; index <= FileCount; index++)
                            {
                                if (i == index) continue;
                                if (documents[index].Any(t => t.Item1 == currentitem.Item1 || t.Item1 == currentitem.Item2))
                                {
                                    documentTerm++;
                                    int val = documents[index].Find(t => t.Item1 == currentitem.Item1 || t.Item1 == currentitem.Item2).Item3;
                                }
                            }

                            // 현재 문서 안에 있는 단어 중에서, 한 단어당 모든문서 찾아서 TF IDF 값 계산하기
                            IDFtemp = Math.Log10((double)(FileCount + 1) / (documentTerm + 1));
                            TF_IDFtemp = (0.5 + 0.5 * ((double)(currentitem.Item3) / total_word)) * IDFtemp;
                            
                            _vocabularyTFIDF.Add(new Tuple<string, string, double>(currentitem.Item1, currentitem.Item2, TF_IDFtemp));
                            TOTAL_TF_IDF += TF_IDFtemp;
                            documentTerm = 0;
                        }

                        // 각 문서 가중치값 업데이트, currentValue 현재 문서 번호 이다.
                        foreach (var temptemp in _vocabularyTFIDF)
                        {
                            // update 고치기, 여기서 단어값이 얼마나 해당되는지 한글인지 영어 인지 고치기!!!!
                            updateSql = "UPDATE dbo.secmem_technology_db_ssm_project SET technology_TFIDF=" + temptemp.Item3 + " WHERE technology_word=" + "'" + temptemp.Item1 + "'" + " and technology_english=" + "'" + temptemp.Item2 + "'" + " and technology_document=" + currentValue;
                            SqlCommand scom = new SqlCommand(updateSql, MainServer.scon);
                            scom.Connection = MainServer.scon;
                            scom.ExecuteNonQuery();
                        }

                        updateSql = "UPDATE dbo.secmem_board_extra_ssm_project SET ex_total_tfidf=" + TOTAL_TF_IDF + " WHERE wr_id=" + currentValue;
                        SqlCommand updateS = new SqlCommand(updateSql, MainServer.scon);
                        updateS.Connection = MainServer.scon;
                        updateS.ExecuteNonQuery();
                        TOTAL_TF_IDF = 0;
                    } // for문 (i)
                }
            } // #### Current File 조건문 ###
            else
            {
                Console.WriteLine("현재 업데이트 된 새로운 텍스트 파일이 없습니다.");
            }
            Console.WriteLine("완료");
        }
    }
}
