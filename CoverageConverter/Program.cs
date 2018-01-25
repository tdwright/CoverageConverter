using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoverageConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputFile = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(inputFile)) throw new ArgumentException("Please provide a filename");
            if (!File.Exists(inputFile)) throw new FileNotFoundException("Invalid filename");

            var inputReport = XDocument.Load(inputFile);

            var modules = inputReport
                .Descendants("Module")
                .Where(m=>!m.Attributes().Any(a=>a.Name== "skippedDueTo"))
                .Select(ProcessModule);

            var root = new XElement("coverage",
                    new XAttribute("profilerVersion", "4.0.0.0"),
                    modules
                );

            var outputReport = new XDocument(root);

            using (var sw = new StreamWriter(File.Open("output.xml", FileMode.Create)))
            {
                outputReport.Save(sw);
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static XElement ProcessModule(XElement input)
        {
            var classes = input
                .Descendants("Class")
                .Select(ProcessClass);

            var fullname = GetFirstDescVal(input, "ModuleName");
            var shortname = Path.GetFileName(fullname);

            return new XElement("module",
                    new XAttribute("name", shortname),
                    classes
                );
        }

        private static XElement ProcessClass(XElement input)
        {
            var methods = input
                .Descendants("Method")
                .Select(ProcessMethod);

            var fullname = GetFirstDescVal(input, "FullName");

            return new XElement("class",
                    new XAttribute("name", fullname),
                    methods
                );
        }

        private static XElement ProcessMethod(XElement input)
        {
            var seqpnts = input
                .Descendants("SequencePoint")
                .Select(ProcessSeqPnts);

            var rawSig = GetFirstDescVal(input, "Name");
            var sigMatches = new Regex(@"^(?'return'[A-Za-z\.]+)\s.+::(?'name'[a-zA-Z_]*)(?'params'\(.*\))$").Match(rawSig);
            
            var name = sigMatches.Groups["name"].Value;
            var param = sigMatches.Groups["params"].Value;
            var returnRaw = sigMatches.Groups["return"].Value;
            var returnType = (returnRaw == "System.Void") ? "void" : returnRaw;
            var sig = $"{name}{param} : {returnType}";

            return new XElement("method",
                    new XAttribute("name", name),
                    new XAttribute("signature", sig),
                    seqpnts
                );
        }

        private static XElement ProcessSeqPnts(XElement input)
        {

            return new XElement("seqpnt",
                    new XAttribute("vc", GetAttrVal(input,"vc")),
                    new XAttribute("o", GetAttrVal(input,"offset"))
                );
        }

        private static string GetFirstDescVal(XElement input, string name)
        {
            return input.Descendants(name).First().Value;
        }

        private static string GetAttrVal(XElement input, string name)
        {
            return input.Attributes(name).First().Value;
        }
    }
}