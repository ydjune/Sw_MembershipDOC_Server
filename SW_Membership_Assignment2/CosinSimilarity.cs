using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordTextExt.Office;
using kr.ac.kaist.swrc.jhannanum.comm;
using kr.ac.kaist.swrc.jhannanum.hannanum;
using System.Data;

namespace SW_Membership_Assignment2
{
    class CosinSimilarity
    {

        // ###################################################################################################################
        // ############################################### 코사인 유사도 계산 ################################################
        // ###################################################################################################################
        public static double CosSim(List<Tuple<string, string, double>> currentDoc, List<Tuple<string, string, double>> otherDoc)
        {
            double numerator = 0.0;
            double denominator_A = 0.0;
            double denominator_B = 0.0;
            double denominator = 0.0;

            if (currentDoc.Count != otherDoc.Count)
            {
                Console.WriteLine("not match feature Dictionary size");
                return -1;
            }

            // 내적 계산
            foreach (var item in currentDoc)
            {
                double otherVectorValue = otherDoc.Find(t => t.Item1 == item.Item1 || t.Item2 == item.Item1).Item3;
                numerator += otherVectorValue * item.Item3;
            }

            // 외적 계산
            foreach (var item in currentDoc)
            {
                denominator_A += Math.Pow((double)currentDoc.Find(t => t.Item1 == item.Item1 || t.Item2 == item.Item1).Item3, 2.0);
                denominator_B += Math.Pow((double)otherDoc.Find(t => t.Item1 == item.Item1 || t.Item2 == item.Item1).Item3, 2.0);
            }
            denominator = Math.Sqrt(denominator_A) * Math.Sqrt(denominator_B);
            return numerator / denominator;
        }


        // #########################################################################################################
        // ################################## 코사인 유사도 계산하기 ###############################################
        // #########################################################################################################
        public static List<Tuple<string, double>> cosin(int currentNumber)
        {
            List<Tuple<string, string, int>> currentPage = getDocument.getCurrent(currentNumber);
            List<Tuple<string, string, int>>[] everyPage = getDocument.getEveryDocument(currentNumber);
            List<string> AllDocumentName = getDocument.getAllDocumentName();

            List<Tuple<string, string, double>> current_cosin = new List<Tuple<string, string, double>>();
            List<Tuple<string, string, double>> other_cosin = new List<Tuple<string, string, double>>();
            List<Tuple<string, double>> result_cosin = new List<Tuple<string, double>>();

            int otherIndex = getDocument.FileCount;
            for (int i = 0; i <= otherIndex; i++)
            {
                foreach (var item in currentPage)
                {
                    // 현재 문서에 있는 키가, 모든 문서에 있다면
                    if (everyPage[i].Any(t => t.Item1 == item.Item1 || t.Item2 == item.Item1))
                    {
                        int maxValue = everyPage[i].Find(t => t.Item1 == item.Item1 || t.Item2 == item.Item1).Item3;
                        if (maxValue >= item.Item3)
                        {
                            // 그 값이 있다면 (빈도수/빈도수)
                            current_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, (double)(item.Item3) / maxValue));
                            other_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 1));
                        }
                        else
                        {
                            current_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 1));
                            other_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, (double)(maxValue) / item.Item3));
                        }
                    }
                    else
                    {
                        current_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 1));
                        other_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 0));
                    }
                }

                // 모든 문서 단어
                foreach (var item in everyPage[i])
                {
                    if (currentPage.Any(t => t.Item1 == item.Item1 || t.Item2 == item.Item1))
                    {
                        // 현재 코사인 유사도 벡터에 KEY값이 있는지 검사하기
                        if (current_cosin.Any(t => t.Item1 == item.Item1 || t.Item2 == item.Item1))
                        {
                            continue;
                        }
                        else
                        {
                            int maxCurrentValue = currentPage.Find(t => t.Item1 == item.Item1 || t.Item2 == item.Item1).Item3;
                            if (maxCurrentValue >= item.Item3)
                            {
                                current_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 1));
                                other_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, (double)(item.Item3) / maxCurrentValue));
                            }
                            else
                            {
                                current_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, (double)(maxCurrentValue) / item.Item3));
                                other_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 1));
                            }
                        }
                    }
                    else
                    {
                        current_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 0));
                        other_cosin.Add(new Tuple<string, string, double>(item.Item1, item.Item2, 1));
                    }
                }

                // 한 문서 벡터 구한 후 코사인 유사도 값 계산하기
                result_cosin.Add(new Tuple<string, double>(AllDocumentName[i], CosSim(current_cosin, other_cosin)));
                current_cosin.Clear();
                other_cosin.Clear();
            }
            return result_cosin;
        }
    }
}
