﻿using Discord;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VergilBot.Modules
{
    public class elephantSql
    {
        
        public elephantSql() {}

        public decimal transact(string discord_id, string typeOfTransaction, decimal balance) 
        {
            var connectionString = Get();
            
            using (var connection = new NpgsqlConnection(connectionString))
            {

                connection.Open();
                
                using (var command = new NpgsqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT balance FROM user_accounts WHERE discord_account_id = @id";
                    command.Parameters.AddWithValue("@id", discord_id);

                    // Execute the query and get the results in a DataTable
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // Use the data in the DataTable here
                        if (dataTable.Rows.Count > 0)
                        {
                            if (typeOfTransaction.Equals("deposit"))
                            {
                                var balanceOnDB = (decimal)dataTable.Rows[0]["balance"];
                                Console.WriteLine("Account balance: {0:C}", balanceOnDB);
                                Console.WriteLine("Requested balance to add: {0:C}\n", balance);

                                var newBalance = balanceOnDB + balance;

                                var updateCommand = new NpgsqlCommand("UPDATE user_accounts SET balance = @balance WHERE discord_account_id = @id", connection);
                                updateCommand.Parameters.AddWithValue("@balance", newBalance);
                                updateCommand.Parameters.AddWithValue("@id", discord_id);

                                var rowsUpdated = updateCommand.ExecuteNonQuery();
                                return newBalance;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Account not found.");
                        }

                        
                        // Close the adapter and dispose of the DataTable
                        adapter.Dispose();
                        dataTable.Dispose();
                    }
                }


                connection.Close();
            }
            return 0;
            
        }

        public string Register(IUser user) {
            var connectionString = Get();

            try
            {
                using var conn = new NpgsqlConnection(connectionString);

                conn.Open();

                var sql = "INSERT INTO user_accounts (username, balance, discord_account_id) VALUES (@username, @balance, @discord_account_id)";

                using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("username", user.Username + "#" + user.Discriminator);
                cmd.Parameters.AddWithValue("balance", 1000.50);
                cmd.Parameters.AddWithValue("discord_account_id", user.Id.ToString());

                var rowsAffected = cmd.ExecuteNonQuery();

                conn.Close();
                return "Successfully registered!";
            }
            catch (PostgresException e)
            {
                if (e.Code.Equals("23505"))
                    return "You are already registered!";
                else
                    return e.MessageText;
            }
            return "Unknown error";
        }

        private void Deposit()
        { 
        
        }

        private static string Get() 
        {
            IConfigurationRoot config = Program.GetConfiguration();
            var uriString = config.GetSection("ELEPHANTSQL_CON").Value; //moved connection string to user secrets
            var uri = new Uri(uriString);
            var db = uri.AbsolutePath.Trim('/');
            var user = uri.UserInfo.Split(':')[0];
            var passwd = uri.UserInfo.Split(':')[1];
            var port = uri.Port > 0 ? uri.Port : 5432;
            var connStr = string.Format("Server={0};Database={1};User Id={2};Password={3};Port={4}",
                uri.Host, db, user, passwd, port);
            return connStr;
        }
    }
}