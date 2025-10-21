using System;                      // 引入基本系统功能（控制台、字符串等）
using System.Collections.Generic;  // 引入泛型集合类（List<T> 等）
using System.IO;                   // 文件操作（File.Exists、File.Read 等）
using System.Linq;                 // LINQ 查询支持（OrderBy、FirstOrDefault 等）
using System.Xml.Linq;             // LINQ to XML 的支持类（XDocument、XElement）

namespace CompareXml
{
    class Program
    {
        static int Main(string[] args)
        {
            // 这里直接指定两个要比较的文件路径
            // 可以改成命令行参数 args[0], args[1]
            string file1 = @"D:\gpresult.xml";
            string file2 = @"D:\gpresult - 副本.xml";

            // 检查文件是否存在
            if (!File.Exists(file1) || !File.Exists(file2))
            {
                Console.WriteLine("文件不存在。");
                return 1; // 返回非零表示失败
            }

            // ======== 语法糖：var ========
            // var 是 C# 的类型推断语法糖。
            // 编译器会自动推断出 XDocument 类型。
            // 等价写法：
            // XDocument xml1 = XDocument.Load(file1);
            // XDocument xml2 = XDocument.Load(file2);
            var xml1 = XDocument.Load(file1);
            var xml2 = XDocument.Load(file2);

            // 建一个字符串列表，用来存放所有差异描述
            var diffs = new List<string>();

            // 从根节点开始递归比较
            CompareElements(xml1.Root, xml2.Root, "/", diffs);

            // 如果没有差异
            if (diffs.Count == 0)
            {
                Console.WriteLine("XML 内容完全相同。");
            }
            else
            {
                Console.WriteLine("差异如下：");
                // foreach 也是语法糖，等价于普通 for 循环
                // foreach (string diff in diffs) Console.WriteLine(diff);
                foreach (var diff in diffs)
                    Console.WriteLine(diff);
            }

            return 0; // 正常结束
        }

        // 递归比较两个 XML 元素
        static void CompareElements(XElement e1, XElement e2, string path, List<string> diffs)
        {
            // currentPath 用来记录当前比较到哪个节点层级，例如 /root/item/
            string currentPath = path + e1.Name.LocalName + "/";

            // ========= 比较属性部分 =========
            // e1.Attributes() 取当前节点的所有属性。
            // OrderBy 是 LINQ 方法，用于按属性名排序，防止顺序影响比较结果。
            // ToList() 把 LINQ 结果转成 List<XAttribute>。
            // 这是 LINQ 的链式语法糖（方法链调用），相当于手动循环排序。
            var attrs1 = e1.Attributes().OrderBy(a => a.Name.LocalName).ToList();
            var attrs2 = e2.Attributes().OrderBy(a => a.Name.LocalName).ToList();

            if (attrs1.Count != attrs2.Count)
                // 字符串插值语法糖：$"{变量}"
                // 等价于 "路径: 属性数量不同 (" + attrs1.Count + " VS " + attrs2.Count + ")"
                diffs.Add($"{currentPath}: 属性数量不同 ({attrs1.Count} VS {attrs2.Count})");

            // 遍历第一个 XML 的属性列表
            foreach (var a1 in attrs1)
            {
                // LINQ 方法 FirstOrDefault：返回第一个符合条件的元素，若找不到返回 null
                // 等价于手动循环查找。
                var a2 = attrs2.FirstOrDefault(a => a.Name == a1.Name);

                if (a2 == null)
                {
                    diffs.Add($"{currentPath}@{a1.Name}: 只在第一个XML中存在");
                }
                else if (a1.Value != a2.Value)
                {
                    diffs.Add($"{currentPath}@{a1.Name}: 不同值 -> '{a1.Value}' vs '{a2.Value}'");
                }
            }

            // ========= 比较文本内容 =========
            // 只有当两个节点都没有子元素时才比较文本值
            if (!e1.HasElements && !e2.HasElements)
            {
                // ?. 是空条件访问符（语法糖）
                // e1.Value ?? "" 表示如果 e1.Value 是 null 就用空字符串
                string text1 = (e1.Value ?? "").Trim();
                string text2 = (e2.Value ?? "").Trim();

                if (text1 != text2)
                    diffs.Add($"{currentPath}: 值不同 -> '{text1}' vs '{text2}'");
            }

            // ========= 比较子节点 =========
            // Elements() 返回子元素集合（忽略文本/注释）
            var children1 = e1.Elements().ToList();
            var children2 = e2.Elements().ToList();

            if (children1.Count != children2.Count)
                diffs.Add($"{currentPath}: 子节点数量不同 ({children1.Count} vs {children2.Count})");

            // 递归比较每对对应位置的子节点
            for (int i = 0; i < Math.Min(children1.Count, children2.Count); i++)
                CompareElements(children1[i], children2[i], currentPath, diffs);
        }
    }
}