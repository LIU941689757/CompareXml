using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace XmlTackMatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var xml1Path = args[0];
            var xml2Path = args[1];
            var csvPath = args[2];

            var xml1Doc = new XmlDocument();
            xml1Doc.Load(xml1Path);
            var tacks = xml1Doc.SelectNodes("/root/tack").Cast<XmlNode>().ToList();

            var xml2Doc = new XmlDocument();
            xml2Doc.Load(xml2Path);

            var csvLines = new List<string> { "tack_id,tag_name,text,匹配说明" };

            foreach (var tack in tacks)
            {
                ProcessTack(tack, xml2Doc, csvLines);
            }

            File.WriteAllLines(csvPath, csvLines);
            Console.WriteLine($"匹配完成，CSV已生成: {csvPath}");
        }

        // 扁平化处理每个 tack
        static void ProcessTack(XmlNode tack, XmlDocument xml2Doc, List<string> csvLines)
        {
            var tackId = tack.Attributes["id"].Value;
            var leafNodes = GetLeafNodes(tack);

            foreach (var leaf in leafNodes)
            {
                AddMatchesToCsv(tackId, leaf, xml2Doc, csvLines);
            }
        }

        // 获取所有叶子节点（返回列表而不是传引用，避免嵌套）
        static List<XmlNode> GetLeafNodes(XmlNode node)
        {
            if (node.NodeType != XmlNodeType.Element) return new List<XmlNode>();

            var elementChildren = node.ChildNodes.Cast<XmlNode>().Where(c => c.NodeType == XmlNodeType.Element).ToList();
            if (!elementChildren.Any()) return new List<XmlNode> { node };

            return elementChildren.SelectMany(c => GetLeafNodes(c)).ToList();
        }

        // 扁平化处理每个叶子节点匹配
        static void AddMatchesToCsv(string tackId, XmlNode leaf, XmlDocument xml2Doc, List<string> csvLines)
        {
            var tagName = leaf.Name;
            var textContent = leaf.InnerText.Trim();

            var matches = xml2Doc.SelectNodes($"//*[local-name()='{tagName}']").Cast<XmlNode>()
                .Where(m => m.InnerText.Trim() == textContent)
                .ToList();

            int index = 1;
            foreach (var m in matches)
            {
                var info = m as IXmlLineInfo;
                var lineNum = (info != null && info.HasLineInfo()) ? info.LineNumber.ToString() : "?";
                var matchDesc = $"{GetChineseOrdinal(index)}：xml2第{lineNum}行";

                csvLines.Add($"tack_id={tackId},{tagName},{textContent},{matchDesc}");
                index++;
            }
        }

        static string GetChineseOrdinal(int n)
        {
            var map = new[] { "第一次", "第二次", "第三次", "第四次", "第五次", "第六次", "第七次", "第八次", "第九次", "第十次" };
            return n <= map.Length ? map[n - 1] : $"第{n}次";
        }
    }
}