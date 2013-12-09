using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;

namespace EveDataEncoder
{
    class Program
    {
        static SqlConnection conn;

        static void Main(string[] args)
        {

            //////////////////////////////////////////////////////////////////////////////////////////
            // Don't modify below this line if you don't know what you're doing.                    //
            //////////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine("Fetching data from MSSQL...");
            Console.WriteLine();
            Console.WriteLine();

            if(Directory.Exists("output"))
                Directory.Delete("output", true);
            Directory.CreateDirectory("output");

            Program.conn = new SqlConnection(@"Server=localhost\SQLExpress;Database=ebs_DATADUMP;User Id=eveData;Password=test;");
            try
            {
                Program.conn.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown! (conn.open) " + e.ToString());
                Console.ReadLine();
                return;
            }

            try
            {
                Console.WriteLine("Building region table...");

                
                // new table, fetch data, then create file
                List<Region> regions = new List<Region>();

                SqlCommand cmd = new SqlCommand(String.Format("SELECT {0} FROM {1}", "regionID, regionName", "dbo.mapRegions"), Program.conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while(reader.Read())
                {
                    regions.Add(new Region()
                                    {
                                        id = (int)reader["regionID"]
                                        ,name = (string)reader["regionName"]
                                    });
                }

                reader.Close();

                Console.WriteLine("Building tables for "+regions.Count+" regions...");

                foreach (var region in regions)
                {
                    List<System> systems = new List<System>();
                    List<Jump> jumps = new List<Jump>();
                    Console.WriteLine("[" + region.name + "] Building systems table...");

                    cmd =
                        new SqlCommand(
                            String.Format("SELECT {0} FROM {1} WHERE {2}", "solarSystemID, solarSystemName, security",
                                          "dbo.mapSolarSystems", "regionID="+region.id), Program.conn);
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var id = ((int) reader["solarSystemID"]);
                        var name = ((string) reader["solarSystemName"]).Replace("\"", "\\\"");
                        var security = (double) reader["security"];
                        systems.Add(new System()
                                        {
                                            id = id,
                                            name = name,
                                            security = security
                                        });
                    }

                    Console.WriteLine("Building jumps table...");

                    reader.Close();
                    cmd =
                        new SqlCommand(
                            String.Format("SELECT {0} FROM {1} WHERE {2}",
                                          "fromSolarSystemID, toSolarSystemID, fromRegionID, toRegionID",
                                          "dbo.mapSolarSystemJumps", "fromRegionID="+region.id), Program.conn);
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int from = (int) reader["fromSolarSystemID"];
                        int to = (int) reader["toSolarSystemID"];
                        var target = systems.TakeWhile(c => c.id != to).Count();
                        if (target == systems.Count)
                        {
                            //Add new system for this one
                            systems.Add(new System(){id=to, name = "Region: "+regions.First(e=>e.id==(int)reader["toRegionID"]).name, security = -1, region = true});
                        }
                        jumps.Add(new Jump()
                                           {
                                               source = systems.TakeWhile(c => c.id != from).Count(),
                                               target = target
                                           });
                    }

                    Hashtable jsondata = new Hashtable();
                    List<Hashtable> jsonSystems =
                        systems.Select(
                            system =>
                            new Hashtable() {{"id", system.id}, {"name", system.name}, {"security", system.security}}).
                            ToList();
                    List<Hashtable> jsonJumps =
                        jumps.Select(jump => new Hashtable() {{"source", jump.source}, {"target", jump.target}}).ToList();
                    jsondata["systems"] = jsonSystems;
                    jsondata["jumps"] = jsonJumps;

                    Console.WriteLine("Systems: "+systems.Count);
                    Console.WriteLine("Jumps: "+jumps.Count);

                    string json = APIcheck.JSON.JsonEncode(jsondata);
                    File.WriteAllText("output/"+region.name.Replace(" ", "_")+".json", json);

                    reader.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown! (reader) " + e.ToString());
                Console.ReadLine();
                return;
            }


            Console.WriteLine("Application complete.");

            Program.conn.Close();

            Console.ReadLine();
        }
    }
}
