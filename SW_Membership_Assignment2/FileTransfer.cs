using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordTextExt.Office;
using kr.ac.kaist.swrc.jhannanum.comm;
using kr.ac.kaist.swrc.jhannanum.hannanum;
using System.Data;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;


namespace SW_Membership_Assignment2
{
    public class FileTransfer
    {
        public static Workflow workflow = WorkflowFactory.getPredefinedWorkflow(WorkflowFactory.WORKFLOW_NOUN_EXTRACTOR);//단어 추출 객체.
        string BASICPATH = "C:\\SSM_Assignment2\\";
        TextExtHelper txtExtHelper = null; 
        private Socket server = null;
        private Socket client = null;
        private Thread th = null;
        private string client_ip = null;
        public FileInfo file_info = null;
        private const int BUFFER = 4096;

        /* 생성자 */
        public FileTransfer(MainServer _wnd)
        {
            txtExtHelper = new TextExtHelper();
            workflow.activateWorkflow(true);
        }

        /* 서버 시작 */
        public void ServerStart()
        {
            try
            {
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 7500);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(ipep);
                server.Listen(10);
                Console.WriteLine("File Server Start...");
                while (true)
                {
                    this.client = server.Accept();
                    IPEndPoint ip = (IPEndPoint)this.client.RemoteEndPoint;
                    Console.WriteLine(ip.Address + "Accept.");
                    this.client_ip = ip.Address.ToString();
                    th = new Thread(new ThreadStart(Receive));
                    th.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /* 서버 종료 */
        public void ServerStop()
        {
            try
            {
                if (client != null)
                {
                    // 파일 전송 클라이언트와 연결되어 있다면
                    if (client.Connected)  
                    {
                        client.Close(); 					
                        th.Abort();
                    }
                } server.Close();
                Console.WriteLine("파일 서버 종료...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <param name="ip">연결할 서버 아이피 주소</param>
        public bool Connect(string ip)
        {
            try
            {
                // 접속할 파일 서버 아이피 주소와 포트번호 설정
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), 7500);
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ipep);
                Console.WriteLine(ip + "서버에 접속 성공...");
                this.client_ip = ip;   // 파일 서버 아이피 주소 기록
                th = new Thread(new ThreadStart(Receive));  // 파일 서버가 보내는 데이터 수신
                th.Start();
                return true;   // 파일 서버 접속에 성공하면 true 값 반환
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;   // 파일 서버 접속에 실패하면 false 값 반환
            }
        }

        /* 파일 서버 연결 종료 */
        public void Disconnect()
        {
            try
            {
                if (client != null)  // 파일 서버에 접속되어 있다면
                {
                    if (client.Connected) client.Close();	
                    if (th.IsAlive) th.Abort();
                }
                Console.WriteLine("파일 서버 연결 종료!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        // ###################################################################################################
        // ############################## 접속된 상대방으로부터 데이터 얻기 ##################################
        // ###################################################################################################   
        public void Receive()
        {
            try
            {
                // 상대방과 연결되어 있다면 
                MainServer.DataBaseOpen();

                while (client != null && client.Connected)
                {
                    // 상대방이 보낸 데이터 읽어오기
                    byte[] data = this.ReceiveData();
                    string msg = Encoding.Default.GetString(data);
                    string[] token = msg.Split('\a');

                    switch (token[0])
                    {
                        // 전송할 파일 정보
                        case "CTOC_FILE_TRANS_INFO":  
                            FileInfo(token[1], Convert.ToInt64(token[2].Trim()));
                            break;

                        // 파일 전송 수락
                        case "CTOC_FILE_TRANS_YES":	 
                            long current_size = Convert.ToInt64(token[1].Trim());
                            this.SendFileData(this.file_info, current_size);
                            break;

                        // 파일 전송 거부
                        case "CTOC_FILE_TRANS_NO":  
                            Console.WriteLine("상대방이 파일 전송을 거부했습니다.");
                            break;

                        //파일전송을 요청
                        case "CTOC_FILE_DOWN_LOAD": 
                            FileInfo send_file = new FileInfo(token[1]);//token[1] : BASICPATH + filename;
                            Console.WriteLine("상대방이 파일다운로드를 요청하였습니다.");
                            this.Send("CTOC_FILE_TRANS_INFO" + "\a" + send_file.Name + "\a" + send_file.Length.ToString());
                            file_info = send_file;
                            break;

                        //쿼리문 실행과 파일 전송
                        case "CTOC_SEND_SQL_AND_File_Trans": 
                            string strSql = "INSERT INTO dbo.secmem_board_extra_ssm_project VALUES (N'" +
                            token[1] + "', N'" + token[2] + "', N'" + token[3] + "',N'" +  //name , type, des
                            token[4] + "',N'" + token[5] + "',N'" + token[6] + "',N'" + token[7] + "',N'" + //sData, eData, status, startIdx
                            token[8] + "',N'" + token[9] + "',N'" + token[10] + "',N'" + BASICPATH + token[11] + "',N'" + token[12] + "', null, null)"; //endIdx, plat, is_used, path
                            SqlCommand scom = new SqlCommand(strSql, MainServer.scon);
                            scom.Connection = MainServer.scon;
                            scom.ExecuteNonQuery();
                            // send_file.Name, send_file.Length.ToString()
                            FileInfo(token[13], Convert.ToInt64(token[14].Trim())); 
                            break;

                        case "CTOC_TEXT_EXT_PROC":
                            int returnValue;
                            Console.WriteLine("확장자명확인" + token[2]);
                            String strSelect = "SELECT wr_id from dbo.secmem_board_extra_ssm_project WHERE ex_pr_name = '" + token[2] + "';";
                            SqlCommand scomSelect = new SqlCommand(strSelect, MainServer.scon);
                            scomSelect.Connection = MainServer.scon;
                            returnValue = (int)scomSelect.ExecuteScalar();

                            // 승인 버튼 눌렀을 때 (여기서 ex_is_used 바꾸기)
                            CorePart.ProgramStart(returnValue, token[1]);
                            String strUpdate = "UPDATE dbo.secmem_board_extra_ssm_project SET ex_is_used=" + "'진행중'" + " WHERE wr_id=" + returnValue;
                            SqlCommand scomUpdate = new SqlCommand(strUpdate, MainServer.scon);
                            scomUpdate.Connection = MainServer.scon;
                            scomUpdate.ExecuteNonQuery();
                            Send("CLIENT_WAITING");
                            break;

                        // token[1] 과제요약문장
                        case "CTOC_HOVER_MSG":
                            Console.WriteLine(token[1]);
                            String returnData = "SELECT wr_id from dbo.secmem_board_extra_ssm_project WHERE ex_pr_name = '" + token[1] + "';";
                            SqlCommand returnSelect = new SqlCommand(returnData, MainServer.scon);
                            returnSelect.Connection = MainServer.scon;
                            returnData = (String)returnSelect.ExecuteScalar();
                            Send(returnData);
                            break;

                        // Client에게 4개의 코사인 유사도 값 보여주기
                        case "CLIENT_TECHNOLOGY":
                            String clientReturn = "SELECT technology_word, technology_tfidf from dbo.secmem_technology_db_ssm_project where technology_document=" + token[1];
                            SqlCommand returnClient = new SqlCommand(clientReturn, MainServer.scon);
                            returnClient.Connection = MainServer.scon;
                            SqlDataReader readerDoc = returnClient.ExecuteReader();
                            List<Tuple<string, double>> currentTFIDF = new List<Tuple<string, double>>();
                            if (readerDoc.HasRows)
                            {
                                while (readerDoc.Read())
                                {
                                    currentTFIDF.Add(new Tuple<string, double>(readerDoc.GetString(0), readerDoc.GetDouble(1)));
                                }
                            }
                            readerDoc.Close();

                            var rankTFIDF = currentTFIDF.OrderByDescending(num => num.Item2);
                            int counting = 0;

                            // 4개의 결과값 클라이언트에게 전송
                            String clientTechnologyTag = "SERVER_TECHNOLOGY\a";
                            foreach(var item in rankTFIDF){
                                counting++;
                                clientTechnologyTag += item.Item1 + "\a";
                                if (counting > 4) break;
                            }
                            Send(clientTechnologyTag);
                            break;

                        // 과제 대기중인지 승인중인지 표시 하기
                        case "SEVER_RELATION":
                            String returnState = "SELECT ex_is_used from dbo.secmem_board_extra_ssm_project WHERE wr_id=" + token[1];
                            SqlCommand returnStat = new SqlCommand(returnState, MainServer.scon);
                            returnStat.Connection = MainServer.scon;
                            String stateData = (String)returnStat.ExecuteScalar();

                            // 연관과제 해주기 (token[1]에 현재 과제번호)
                            if (stateData.Equals("진행중"))
                            {
                                Console.WriteLine("SEVER_RELATION");
                                List<Tuple<string, double>> result_cosin = CosinSimilarity.cosin(System.Convert.ToInt32(token[1]));
                                var rank = result_cosin.OrderByDescending(num => num.Item2);
                                int count = 0;

                                // 클라이언트에게 과제 이름 보내주기
                                String strName = "CLIENT_RELATION\a";
                                foreach (var val in rank)
                                {
                                    count++;
                                    strName += val.Item1 + "\a";
                                    Console.WriteLine("[Relation]  " + val.Item1);
                                    if (count > 3) break;
                                }
                                Console.WriteLine("현재count : " + count);
                                Send(strName);
                              
                            }
                            break;

                        // 질의어 검색 모드
                        case "CTOC_QUESTION_MSG": 
                            List<string> word = new List<string>();
                            word = FindWord(token[1]);      
                            int cnt = 0;
                            String strWhere;
                            String total;

                            // where 쿼리문 완성
                            strWhere = "where ";
                            foreach (var item in word)
                            {
                                Console.WriteLine(item);
                                cnt++;
                                if (word.Count == cnt)
                                {
                                    strWhere += "(technology_word = '" + item + "' or technology_english= '" + item + "')";
                                }
                                else
                                {
                                    strWhere += "(technology_word = '" + item + "' or technology_english= '" + item + "') or ";
                                }
                            }
                            
                            // 질의어 쿼리문
                            total = "declare @technology_document int declare @technology_tfidf float ";
                            total += "create table temptable (temp_document int, temp_tfidf float) ";
                            total += "declare mycur Cursor local for select technology_document, technology_tfidf from secmem_technology_db_ssm_project " + strWhere;
                            total += "open mycur ";
                            total += "fetch next from mycur into @technology_document, @technology_tfidf ";
                            total += "while(@@FETCH_STATUS=0) ";
                            total += "begin ";
                            total += "if (select count (*) from temptable where temp_document=@technology_document) = 0 ";
                            total += "begin ";
                            total += "insert into temptable (temp_document, temp_tfidf) values (@technology_document, @technology_tfidf) ";
                            total += "end ";
                            total += "else begin ";
                            total += "update temptable set temp_tfidf = @technology_tfidf + temp_tfidf where temp_document=@technology_document ";
                            total += "end ";
                            total += "fetch next from mycur into @technology_document, @technology_tfidf ";
                            total += "end ";
                            total += "close mycur ";
                            total += "select * from temptable order by temp_tfidf desc ";

                            SqlCommand searchCmd = new SqlCommand(total, MainServer.scon);
                            SqlDataReader reader = searchCmd.ExecuteReader();
                            Console.WriteLine("결과 출력");

                            // 문서 번호랑 더해진 TFIDF 값 얻어오기
                            List<Tuple<int, double>> orderDocument = new List<Tuple<int, double>>();
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    orderDocument.Add(new Tuple<int, double>(reader.GetInt32(0), reader.GetDouble(1)));
                                }
                            }
                            reader.Close();

                            
                            // 토탈 TF-IDF 값을 가져와서, 나눈다.
                            foreach (var item in orderDocument)
                            {
                                String returnTF = "SELECT ex_total_tfidf from dbo.secmem_board_extra_ssm_project WHERE wr_id = " + item.Item1;
                                SqlCommand returnScom = new SqlCommand(returnTF, MainServer.scon);
                                double returnTFIDF = (double)returnScom.ExecuteScalar();

                                // ( 100 * 더해진 TF-IDF ) / 총 질의어 단어수
                                Console.WriteLine("[질의어]" + cnt);
                                String updateSql = "UPDATE dbo.secmem_board_extra_ssm_project SET ex_query_tfidf=" + ((item.Item2 * 100) / word.Count) + " WHERE wr_id=" + item.Item1;
                                SqlCommand updateScom = new SqlCommand(updateSql, MainServer.scon);
                                updateScom.Connection = MainServer.scon;
                                updateScom.ExecuteNonQuery();
                            }
                            Console.WriteLine("결과 출력 끝");

                            // 임시 테이블 삭제 하기
                            SqlCommand endCmd = new SqlCommand("drop table temptable ", MainServer.scon);
                            endCmd.ExecuteNonQuery();
                            Console.WriteLine("테이블 삭제 성공");

                            // 테이블 하나하나 들고 와서 뿌려줘야되는데, 결과 문서 번호가지고 쿼리문에 더하기
                            String strPrintDocument = null;
                            cnt = 0;
                            strPrintDocument = "SELECT wr_id, ex_pr_name, ex_pr_type, ex_description, ex_start_date, ex_end_date, ex_pr_status, ex_start_file_idx, ex_end_file_idx, ex_platform, ex_is_used, ex_path, ex_data, ex_query_tfidf FROM dbo.secmem_board_extra_ssm_project where wr_id IN (";

                            if (orderDocument.Count != 0)
                            {
                                foreach (var itemdoc in orderDocument)
                                {
                                    cnt++;
                                    if (cnt == orderDocument.Count)
                                    {
                                        strPrintDocument += itemdoc.Item1;
                                    }
                                    else
                                    {
                                        strPrintDocument += itemdoc.Item1 + ", ";
                                    }
                                }

                                // MS SQL 쿼리 응답문 정렬 안하기
                                strPrintDocument += ") order by CHARINDEX(CONVERT(varchar, wr_id), '";
                                cnt = 0;
                                foreach (var itemdoc in orderDocument)
                                {
                                    cnt++;
                                    if (cnt == orderDocument.Count)
                                    {
                                        strPrintDocument += itemdoc.Item1 + "')";
                                    }
                                    else
                                    {
                                        strPrintDocument += itemdoc.Item1 + ", ";
                                    }
                                }

                                // 데이터 테이블 클라이언트에게 보내기
                                SqlDataAdapter Selectadapter = new SqlDataAdapter();
                                Selectadapter.SelectCommand = new SqlCommand(strPrintDocument, MainServer.scon);
                                SqlCommandBuilder builder = new SqlCommandBuilder(Selectadapter);
                                DataTable dataTable = new DataTable();
                                Selectadapter.Fill(dataTable);

                                DataRow rwItem = dataTable.Rows[0];
                                string strColum = rwItem["ex_pr_name"].ToString();
                                Console.WriteLine("가져온 값: " + strColum);

                                // 클라이언트에서 이거 받고 나서, 그리드뷰에 나타내주기
                                // DataTable -> DataSet -> Byte 변환해서 보내주기
                                DataSet dummyDs = new DataSet();
                                dummyDs.Tables.Add(dataTable);

                                byte[] start = new byte[1];
                                start[0] = 0x02;
                                if (start[0] == 0x02) Console.WriteLine("STX.");
                                byte[] DataSetData = CompressDataSet(dummyDs);
                                byte[] result = new byte[start.Length + DataSetData.Length];
                                System.Buffer.BlockCopy(start, 0, result, 0, start.Length);
                                System.Buffer.BlockCopy(DataSetData, 0, result, start.Length, DataSetData.Length);
                                SendData(result);
                            }
                            else
                            {
                                Send("SERVER_QUESTION_MSG\a");
                                Console.WriteLine("검색결과없음");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // ###################################################################################################
        // ###################### 상대방이 보낸 데이터를 바이너리 형태로 읽어오기 ############################
        // ################################################################################################### 
        private byte[] ReceiveData()
        {
            try
            {
                int total = 0;
                int size = 0;
                int left_data = 0;
                int recv_data = 0;

                // 수신할 데이터 크기 알아내기   
                byte[] data_size = new byte[8];
                recv_data = this.client.Receive(data_size, 0, 8, SocketFlags.None);
                size = BitConverter.ToInt32(data_size, 0);
                left_data = size;
                byte[] data = new byte[size];
                // 서버에서 전송한 실제 데이터 수신
                while (total < size)
                {
                    recv_data = this.client.Receive(data, total, left_data, SocketFlags.None);
                    if (recv_data == 0) break;
                    total += recv_data;
                    left_data -= recv_data;
                }
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        public byte[] CompressDataSet(DataSet ds)
        {
            // 데이터셋 Serialize
            ds.RemotingFormat = SerializationFormat.Binary;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, ds);
            byte[] inbyt = ms.ToArray();

            // 데이터 압축
            System.IO.MemoryStream objStream = new MemoryStream();
            DeflateStream objZS = new DeflateStream(objStream, CompressionMode.Compress);
            objZS.Write(inbyt, 0, inbyt.Length);
            objZS.Flush();
            objZS.Close();

            // 데이터 리턴
            return objStream.ToArray();
        }


        // 질의어에 관한 형태소 분석 돌리기
        public List<string> FindWord(String queryMsg)
        {
            List<string> word = new List<string>();
            try
            {
                workflow.analyze(queryMsg);
                LinkedList<Sentence> resultList = workflow.getResultOfDocument(new Sentence(0, 0, false));
                foreach (Sentence s in resultList)
                {
                    Eojeol[] eojeolArray = s.Eojeols;
                    for (int i = 0; i < eojeolArray.Length; i++)
                    {
                        if (eojeolArray[i].length > 0)
                        {
                            String[] morphemes = eojeolArray[i].Morphemes;
                            for (int j = 0; j < morphemes.Length; j++)
                            {
                                // ' 안빼면 MS SQL 쿼리문의 문자로 인식하기 때문에 빼주기
                                morphemes[j] = morphemes[j].Replace("'", "");
                                word.Add((morphemes[j]));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                workflow.close();
            }
            return word;
        }

        

       /* 문자열 전송 */
        /// <param name="msg">전송할 문자열</param>
        public void Send(string msg)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            this.SendData(data);
        }



        /* 바이너리 전송 */
        private void SendData(byte[] data)
        {
            try
            {
                int total = 0;
                int size = data.Length;
                int left_data = size;
                int send_data = 0;

                // 전송할 실제 데이터의 크기 전달
                byte[] data_size = new byte[4];
                data_size = BitConverter.GetBytes(size);
                send_data = this.client.Send(data_size);

                // 실제 데이터 전송
                while (total < size)
                {
                    send_data = this.client.Send(data, total, left_data, SocketFlags.None);
                    total += send_data;
                    left_data -= send_data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /* 파일 데이터 전송 */
        /// <param name="file">상대방에게 보낼 파일 정보</param>
        /// <param name="currentl_size">상대방에게 보낼 파일 포인터 위치</param>
        private void SendFileData(FileInfo file, long current_size)
        {
            long total_size = file.Length;              // 파일 크기
            long size = file.Length - current_size;     // 보낼 파일 위치 지정
            long count = size / BUFFER;                 // 전송할 횟수
            long remain_byte = size % BUFFER;

            long index = 0;
            long prg_value = 0;
            long time = 0;

            FileStream fs = null;
            BinaryReader br = null;

            try
            {  
                // 전송할 실제 파일 데이터 크기 전달
                fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

                // 파일이 상대방에게 있을 경우 파일 포인터 이동
                if (current_size > 0)   
                {
                    fs.Seek(current_size, SeekOrigin.Begin);
                    prg_value += current_size;
                }

                // 파일 읽어오기
                br = new BinaryReader(fs);   
                Byte[] data = new byte[BUFFER];
                while (index < count)
                {
                    // 1초마다 프로그래스바 갱신
                    if (DateTime.Now.Ticks - time > 10E7)   
                    {
                        time = DateTime.Now.Ticks;
                    }
                    br.Read(data, 0, BUFFER);
                    client.Send(data, 0, BUFFER, SocketFlags.None);
                    index++;
                }

                // 남아있는 데이터가 있다면
                if (remain_byte > 0)   
                {
                    br.Read(data, 0, (int)remain_byte);
                    client.Send(data, 0, (int)remain_byte, SocketFlags.None);
                }
                Console.WriteLine("파일 전송 완료!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (br != null) br.Close();
                if (fs != null) fs.Close();
            }
        }

        /* 파일 데이터 수신  */
        /// <param name="fs">수신할 파일 스트림</param>
        /// <param name="total_size">파일 전체 크기</param>
        /// <param name="remain_size">수신할 파일 크기</param>
        private void ReceiveFileData(FileStream fs, long total_size, long remain_size)
        {
            long total = 0;
            long left_size = remain_size;
            int recv_size = 0;
            long prg_value = 0;
            long time = 0;

            BinaryWriter bw = null;
            Byte[] data = new byte[BUFFER];  // 4096 단위로 데이터 수신

            try
            {
                bw = new BinaryWriter(fs);
                if (total_size > remain_size)   // 이어받기 기능일 경우 해당 위치로 파일 포인터이동
                {
                    bw.Seek((int)(total_size - remain_size), SeekOrigin.Begin);
                    prg_value += total_size - remain_size;
                }

                while (total < remain_size)
                {
                    if (DateTime.Now.Ticks - time > 10E7) // 1초마다 프로그래스바 갱신
                    {
                        time = DateTime.Now.Ticks;
                    }

                    if (left_size > BUFFER)
                        recv_size = this.client.Receive(data, BUFFER, SocketFlags.None);
                    else
                        recv_size = this.client.Receive(data, (int)left_size, SocketFlags.None);

                    if (recv_size == 0) break;
                    bw.Write(data, 0, recv_size);
                    total += recv_size;
                    left_size -= recv_size;
                }
                Console.WriteLine("File send success.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (bw != null) bw.Close();
            }
        }

        /* 상대방이 전송하려는 파일이름과 크기 출력, 파일 수신기능 포함 */
        public void FileInfo(string filename, long filesize)
        {	
            // 파일이름 + 파일크기
            string message = this.client_ip + " 님이 보내는 파일 : " + filename +
                                    " (" + filesize + " byte)을 ";
            Console.WriteLine(message);

            FileStream fs = new FileStream(BASICPATH + filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            this.Send("CTOC_FILE_TRANS_YES\a" + fs.Length.ToString());
            this.ReceiveFileData(fs, filesize, filesize - fs.Length); // 파일 수신 시작
            fs.Close();
        }
    }
}