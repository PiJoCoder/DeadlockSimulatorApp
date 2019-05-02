using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Threading;


namespace DeadlockSimulator
{
    class Program
    {
        static string sqlConnectionString = "Server=(local);Database=tempdb;Trusted_Connection=True;Application Name=DeadlockSimulator";
        public static int attemps1 = 0;
        public static int attemps2 = 0;
        static void Main(string[] args)
        {
            //create a couple of tables and insert some rows
            CreateObjects();


            Thread t1 = new Thread(new ThreadStart(Routine1));
            Thread t2 = new Thread(new ThreadStart(Routine2));

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            //drop the objects at the end
            DropObjects();

            Console.WriteLine("Press Enter to quit...");
            Console.Read();
        }

        static void CreateObjects()
        {
            string cmdStr = @"use tempdb
                                
                                if OBJECT_ID('dl_tab1') is not null
	                                drop table dl_tab1;
                                
                                create table dl_tab1 (col1 int);
                                
                                if OBJECT_ID('dl_tab2') is not null
	                                drop table dl_tab2;
                                
                                create table dl_tab2 (col1 int);
                                
                                insert into dl_tab1 values (10);
                                insert into dl_tab2 values (101);";




            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                SqlCommand command = new SqlCommand(cmdStr, connection);
                connection.Open();
                command.ExecuteNonQuery();
            }

            Console.WriteLine("CreateObjects completed");
        }

        static void DropObjects()
        {
            string cmdStr = @"use tempdb
                                if OBJECT_ID('dl_tab1') is not null
	                                drop table dl_tab1;
                                if OBJECT_ID('dl_tab2') is not null
	                                drop table dl_tab2;";

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                SqlCommand command = new SqlCommand(cmdStr, connection);
                connection.Open();
                command.ExecuteNonQuery();
            }

            Console.WriteLine("DropObjects completed");

        }

        static void Routine1()
        {
            int retries = 3;


            while (!DoRoutine1())
            {
                attemps1++;

                if (attemps1 >= retries)
                {
                    break;
                }
            }
        }
        public static bool DoRoutine1()
        {
            bool retval = false;

            Console.WriteLine("Entered Routine1(). Please be patient there is a 10 sec forced delay");

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                SqlTransaction transaction;
                SqlCommand command = connection.CreateCommand();

                connection.Open();
                transaction = connection.BeginTransaction("UpdateTran_Routine1");

                command.Connection = connection;
                command.Transaction = transaction;

                try
                {
                    //update one of the tables 
                    command.CommandText = "update dl_tab1 set col1 = 987";
                    command.ExecuteNonQuery();

                    //sleep some so we create a conditions
                    Thread.Sleep(10000);

                    //update the other table
                    command.CommandText = "update dl_tab2 set col1 = 123";
                    command.ExecuteNonQuery();


                    transaction.Commit();
                    Console.WriteLine("Routine1: Both updates were written to database.");
                    retval = true;
                }

                catch (SqlException ex)
                {
                    if (ex.Number == 1205)
                    {
                        Console.WriteLine("Routine1: Commit Exception Type: {0}", ex.GetType());
                        Console.WriteLine("  Message: {0}", ex.Message);

                        // Attempt to roll back the transaction.
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception ex2)
                        {
                            // This catch block will handle any errors that may have occurred
                            // on the server that would cause the rollback to fail, such as
                            // a closed connection.
                            Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                            Console.WriteLine("  Message: {0}", ex2.Message);
                        }
                    }

                }
                catch (Exception ex2)//any other .net exception
                {
                    throw;
                }

            }

            Console.WriteLine("Completed Routine1()");
            return retval;

        }

        static void Routine2()
        {
            int retries = 3;


            while (!DoRoutine2())
            {
                attemps2++;

                if (attemps2 >= retries)
                {
                    break;
                }
            }
        }

        public static bool DoRoutine2()
        {
            bool retval = false;
            Console.WriteLine("Entered Routine2(). Please be patient there is a 10 sec forced delay");

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                SqlTransaction transaction;
                SqlCommand command = connection.CreateCommand();

                connection.Open();
                transaction = connection.BeginTransaction("UpdateTran_Routine2");

                command.Connection = connection;
                command.Transaction = transaction;

                try
                {
                    //update one of the tables 
                    command.CommandText = "update dl_tab2 set col1 = 123";
                    command.ExecuteNonQuery();

                    //sleep some so we create a conditions
                    Thread.Sleep(10000);

                    //update the other table
                    command.CommandText = "update dl_tab1 set col1 = 987";
                    command.ExecuteNonQuery();

                    transaction.Commit();
                    Console.WriteLine("Routine2: Both updates were written to database.");
                    retval = true;
                }

                catch (SqlException ex)
                {
                    if (ex.Number == 1205) 
                    {
                        Console.WriteLine("Routine2: Commit Exception Type: {0}", ex.GetType());
                        Console.WriteLine("  Message: {0}", ex.Message);

                        // Attempt to roll back the transaction.
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception ex2)
                        {
                            // This catch block will handle any errors that may have occurred
                            // on the server that would cause the rollback to fail, such as
                            // a closed connection.
                            Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                            Console.WriteLine("  Message: {0}", ex2.Message);
                        }
                    }
                }
                catch (Exception ex2)//any other .net exception
                {
                    throw;
                }
            }
            Console.WriteLine("Completed Routine2");
            return retval;
        }
    }
}
