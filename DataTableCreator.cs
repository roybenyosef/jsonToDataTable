using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace stipsToDataTable
{


    public class DataTableCreator
    {
        public string ApiName { get; set; }
        public DataTable Table { get; }
        readonly NameValueCollection dataMapping;
        HashSet<string> UnmappedDataHeaders { get; set; }

        public DataTableCreator(string tableName, string apiName)
        {
            ApiName = apiName;
            Table = new DataTable(tableName);
            this.dataMapping = ReadDataMappingConfig();
        }

        private static NameValueCollection ReadDataMappingConfig()
        {
            var PostSetting = ConfigurationManager.GetSection("DataMapping/StipsToDataTableColumn") as NameValueCollection;
            if (PostSetting.Count == 0)
            {
                throw new Exception(("DataMapping/StipsToDataTableColumn Settings are not defined in app.cofig"));
            }
            else
            {
                return PostSetting;
            }
        }

        public void PageToDataTable(int page)
        {
            var resultJson = ApiGetOrdersOnline(page);
            writeDataTableFromJson(resultJson);
        }

        private string ApiGetOrdersOnline(int page)
        {
            try
            {
                string webAddr = $"http://stips.co.il/api?name={ApiName}&page={page}";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
                httpWebRequest.Method = "GET";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string orders = streamReader.ReadToEnd();
                    return orders;
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private void writeDataTableFromJson(string result)
        {

            dynamic jsonObj = JsonConvert.DeserializeObject(result);
            JObject inputJson = JObject.FromObject(jsonObj);
            var properties = inputJson.Properties();
            var dataArray = properties.Where(x => x.Name == "data");

            UnmappedDataHeaders = new HashSet<string>();
            foreach (var dataRecord in dataArray.First().Value)
            {
                DataRow row = Table.NewRow();
                WriteDataTableRow(dataRecord, row);
                Table.Rows.Add(row);
            }

            PrintResults();
        }

        private void PrintResults()
        {
            if (UnmappedDataHeaders.Any())
            {
                Console.WriteLine("****************************************");

                Console.WriteLine("Unmapped item names found:");
                foreach(var item in UnmappedDataHeaders)
                {
                    Console.WriteLine(item);
                }
                Console.WriteLine("****************************************");
            }
        }

        private void WriteDataTableRow(JToken dataRecord, DataRow row)
        {
            foreach (var item in dataRecord.Values())
            {
                try
                {
                    if (item.Type == JTokenType.Object)
                    {
                        JObject childJson = JObject.FromObject(item);
                        WriteDataTableRow(childJson, row);
                    }
                    else
                    {
                        CreateColumnIfMissing(item);
                        row[GetColumnName(item)] = GetItemValue(item);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error processing item with column: {GetColumnName(item)}, type: {item.Type}, value: {item.Path}, error: {e.Message}");
                }
                //Console.WriteLine($"type: {item.Type}, value: {item.Path}");
            }

        }

        private string GetItemValue(JToken item)
        {
            if (item.Type == JTokenType.Array)
            {
                return item.ToString();
            }
            else
            {
                return item.Value<string>();
            }
        }

        private void CreateColumnIfMissing(JToken item)
        {
            if (!Table.Columns.Contains(GetColumnName(item)))
            {
                DataColumn column = new DataColumn
                {
                    DataType = Type.GetType("System.String"),
                    ColumnName = GetColumnName(item)
                };
                Table.Columns.Add(column);
            }
        }

        private string GetColumnName(JToken item)
        {
            string path = Regex.Replace(item.Path, @"\[\d+\]", "[]");
            if (dataMapping.AllKeys.Contains(path))
            {
                return dataMapping.GetValues(path).First();
            }
            else
            {
                path = $"**{{{path}}}**";
                UnmappedDataHeaders.Add(path);
                return path;
            }
        }
    }
}
