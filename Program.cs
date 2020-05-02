using System;
using System.Collections.Specialized;
using System.Configuration;

namespace stipsToDataTable
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                //Pass api name as program argument
                var dataTableCreator = new DataTableCreator("stips_data_table", args[0]);
                dataTableCreator.PageToDataTable(1);
                var table = dataTableCreator.Table;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} | {e.InnerException}");
            }
            Console.ReadKey();
        }
    }
}
