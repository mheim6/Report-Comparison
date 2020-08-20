using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoMobiusCompare
{
    class Program
    {
        static string MOBIUS_PATH = @"\\dorcsswinstarf\dorwinstar\Project_Management\Testing\SIT\Cycle 10\Missing Reports\Cycle 10C Reports Mobius";
        static string LINUX_PATH = @"\\dorcsswinstarf\dorwinstar\Project_Management\Testing\SIT\Cycle 10\Missing Reports\Linux_WINSTARD_Cycle10C";
        static string REPORT_OUTPUT_PATH = @"\\dorcsswinstarf\dorwinstar\Project_Management\Testing\SIT\Cycle 10\Missing Reports\ReportCycle10C.csv";

        // just takes a minute
        static void Main(string[] args)
        {
            List<string> summaryReport = new List<string>();
            summaryReport.Add("Job,Report,Summary,Notes");
            var mobiusFiles = Directory.EnumerateFiles(MOBIUS_PATH, "*.TXT", SearchOption.AllDirectories).ToLookup(GenReportKey);
            var linuxFiles = Directory.EnumerateFiles(LINUX_PATH, "*.txt", SearchOption.AllDirectories).ToLookup(GenReportKey);
            List<string> combinedKeys = mobiusFiles.Select(x => x.Key).ToList();
            foreach (var key in linuxFiles.Select(x => x.Key))
            {
                if (!combinedKeys.Contains(key)) combinedKeys.Add(key);
            }
            combinedKeys.Sort();
            foreach (var key in combinedKeys)
            {
                bool mobiusExists = mobiusFiles.Contains(key);
                bool linuxExists = linuxFiles.Contains(key);
                if (!mobiusExists && !linuxExists) throw new NotImplementedException();
                if (mobiusExists && !linuxExists) summaryReport.Add(key + ",Missing From Linux");
                if (!mobiusExists && linuxExists) summaryReport.Add(key + ",Missing From Mobius");
                if (mobiusExists && linuxExists) summaryReport.Add(key + "," + SummaryOfDiffs(ChooseReport(mobiusFiles[key]), ChooseReport(linuxFiles[key])));
            }
            File.WriteAllLines(REPORT_OUTPUT_PATH, summaryReport);
        }

        private static string ChooseReport(IEnumerable<string> list)
        {
            return list.OrderBy(x => x).Last();
        }

        static int wrong = 0;
        private static string SummaryOfDiffs(string mobiusFilePath, string linuxFilePath)
        {
            string mobiusText = File.ReadAllText(mobiusFilePath);
            string linuxText = File.ReadAllText(linuxFilePath);
            mobiusText = Normalize(mobiusText);
            linuxText = Normalize(linuxText);
            bool matches = mobiusText == linuxText;
            string summary = matches ? "Matches" : "Doesn't Match";
            if (!matches)
            {
                // mainframe suppresses 0.00 and 0
                // overkill on removing zeroes, but simple
                mobiusText = Regex.Replace(mobiusText, "[0\\.]", "");
                linuxText = Regex.Replace(linuxText, "[0\\.]", "");
                if (linuxText == mobiusText)
                {
                    summary += ",Mainframe suppresses 0.00";
                }
                else
                {
                    int[] mobiusCharCounts = GetCharCount(mobiusText);
                    int[] linuxCharCounts = GetCharCount(linuxText);
                    bool allMatch = true;
                    for (int i = 0; i < 256; i++)
                    {
                        if (mobiusCharCounts[i] != linuxCharCounts[i])
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch)
                    {
                        summary += ",Sort Issues";
                    }
                }
            }
            if (summary == "Doesn't Match") // too vague
            {
                // at least ten of these look the same
                //Process.Start(@"C:\Program Files\Notepad++\notepad++.exe", "\"" + Path.GetFullPath(mobiusFilePath) + "\"");
                //Process.Start(@"C:\Program Files\Notepad++\notepad++.exe", "\"" + Path.GetFullPath(linuxFilePath) + "\"");
                //throw new NotImplementedException();
            }
            return summary;
        }

        private static int[] GetCharCount(string text)
        {
            int[] charCounts = new int[256];
            foreach (var c in text)
            {
                if ((int)c < 256) charCounts[c]++;
            }
            return charCounts;
        }

        private static string Normalize(string text)
        {
            text = text.Replace((char)12 + "", ""); // remove FF
            text = Regex.Replace(text, "JOBNAME: .{17}", ""); // ignore job/step name
            text = Regex.Replace(text, "CREATED: .{17}", ""); // ignore time created
            text = Regex.Replace(text, "NSTAR BATCH DATE: .{8}", ""); // ignore NSTAR batch date created
            text = Regex.Replace(text, "[0-9]{2}[/\\-][0-9]{2}[/\\-][0-9]{2}  [0-9]{2}:[0-9]{2}:[0-9]{2}", ""); // ignore timestamps
            text = Regex.Replace(text, "[ \r\n]", ""); ; // ignore whitespace
            return text;
        }

        // ex: RCSB105,CSTI01B1-01
        private static string GenReportKey(string path)
        {
            string[] split = path.Split('\\');
            string reportName = split.Last().Split('.').First();
            if (reportName == "NON-NEGOTIABLE") reportName = "CSFE03B2-02";
            string jobName = split.Where(x => !x.Contains(".") && (x.StartsWith("RCS") || x.StartsWith("ESP"))).Single();
            if (jobName.EndsWith("L")) jobName = jobName.Substring(0, jobName.Length - 1) + "P";
            if (!Regex.IsMatch(reportName, "[0-9A-Z]{7,8}-[0-9]{2}"))
            {
                String text = File.ReadAllText(path);
                //reportName = " REPORT: CSFD01BF-02";
                reportName = Regex.Match(text, "REPORT: ([0-9A-Z]{7,8}-[0-9]{2})").Groups[1].Value;
                if (!Regex.IsMatch(reportName, "[0-9A-Z]{7,8}-[0-9]{2}"))
                {
                    // throw new Exception();
                    return split.Last().Split('.').First();
                }
            }


            return jobName + "," + reportName;
        }
    }
}
