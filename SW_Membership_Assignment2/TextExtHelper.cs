using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordTextExt.Office;

namespace SW_Membership_Assignment2
{
    // 텍스트 추출
    public class TextExtHelper
    {
        private IWordFile wordFile = null;  //워드 파일 열었을때의 파일 포인터;
        private IOfficeFile _file;          //텍스트 추출 객체

        public void OpenFile(String filePath)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = System.IO.Path.GetExtension(filePath);
            string addName = fileName + fileExtension;
            try
            {
                this._file = OfficeFileFactory.CreateOfficeFile(filePath);
            }
            catch (Exception e)
            {
                this.CloseFile();
            }
        }

        public void CloseFile()
        {
            this._file = null;
            Console.WriteLine("close");
        }


        public void ShowSummary(Dictionary<String, String> dictionary)
        {
            if (dictionary == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<String, String> pair in dictionary)
            {
                sb.AppendFormat("[{0}]={1}", pair.Key, pair.Value);
                sb.AppendLine();
            }
        }

        public void ShowContentsFunc(IOfficeFile file)
        {
            if (file is IWordFile)
            {
                wordFile = file as IWordFile;
            }
            else if (file is IPowerPointFile)
            {
                IPowerPointFile pptFile = file as IPowerPointFile;
            }
        }

        public IOfficeFile get_file()
        {
            return _file;
        }

        public IWordFile getwordFile()
        {
            return wordFile;
        }
    }
}





