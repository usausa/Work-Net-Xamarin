namespace DataMapperWork
{
    using System;
    using System.Linq;

    using Microsoft.Data.Sqlite;

    using Smart.Data.Mapper;

    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var con = new SqliteConnection("Data Source=:memory:"))
            {
                con.Open();
                con.Execute("CREATE TABLE IF NOT EXISTS Table1 (Id int PRIMARY KEY, Data text)");

                con.Execute("INSERT INTO Table1 (Id, Data) VALUES (@Id, @Data)", new { Id = 1, Data = "test" });

                var count = con.ExecuteScalar<int>("SELECT COUNT(*) FROM Table1");
                Console.WriteLine(count);

                var list = con.Query<Table>("SELECT * FROM Table1").ToList();
                foreach (var entity in list)
                {
                    Console.WriteLine($"{entity.Id} : {entity.Data}");
                }
            }
        }
    }

    public class Table
    {
        public int Id { get; set; }

        public string Data { get; set; }
    }
}
