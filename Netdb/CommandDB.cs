using System;
using System.Collections.Generic;
using System.Text;

namespace Netdb
{
    /// <summary>
    /// Manages all the data related to commands
    /// </summary>
    class CommandDB
    {
        /// <summary>
        /// Setup for Database Command table
        /// </summary>
        public static void Setup()
        {
            var cmd = Program._con.CreateCommand();
            string command = "CREATE TABLE IF NOT EXISTS `sys`.`commands` (`id` INT NOT NULL AUTO_INCREMENT,`command` VARCHAR(45) NULL,`alias` VARCHAR(10) NULL,`short_description` VARCHAR(100) NULL,`syntax` VARCHAR(100) NULL,`mod_required` TINYINT NULL,`uses` INT NULL,PRIMARY KEY(`id`)); ALTER TABLE `sys`.`commands` CHANGE COLUMN `uses` `uses` INT NULL DEFAULT 0;";
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        /// <summary>
        /// Get All data for a specific command
        /// </summary>
        /// <param name="command">Command or Alias to search for</param>
        /// <param name="alias">Returns alias</param>
        /// <param name="description">Returns description</param>
        /// <param name="short_description">Returns short description</param>
        /// <param name="mod_required">Returns if mod is required</param>
        /// <param name="uses">Returns how often that command was executed</param>
        /// <returns>Returns if command was found</returns>
        public static bool GetCommandData(string command, out string commandA, out string alias, out string syntax, out string short_description, out bool mod_required, out int uses)
        {
            alias = "";
            syntax = "";
            short_description = "";
            mod_required = false;
            uses = 0;
            commandA = command;

            Tools.ValidateSQLValues(ref command);

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from commands where command = '{command}';";
            var r = cmd.ExecuteReader();
            if (r.Read())
            {
                alias = r[2].ToString();
                short_description = r[3].ToString();
                syntax = r[4].ToString();
                mod_required = r[5].ToString() == "1" ? true : false;
                uses = int.Parse(r[6].ToString());

                r.Close();
            }
            else
            {
                r.Close();

                cmd = Program._con.CreateCommand();
                cmd.CommandText = $"select * from commands where alias = '{command}';";
                r = cmd.ExecuteReader();
                if (r.Read())
                {
                    commandA = r[1].ToString();
                    alias = r[2].ToString();
                    short_description = r[3].ToString();
                    syntax = r[4].ToString();
                    mod_required = r[5].ToString() == "1" ? true : false;
                    uses = int.Parse(r[6].ToString());

                    r.Close();
                }
                else
                {
                    r.Close();
                    return false;
                }
            }

            r.Dispose();
            cmd.Dispose();
            return true;
        }

        /// <summary>
        /// Gets Data of all commands in db
        /// </summary>
        /// <param name="alias">Array of all alias'e</param>
        /// <param name="description">Array of all descriptions</param>
        /// <param name="short_description">Array of all short descriptions</param>
        /// <param name="mod_required">Array of all mod_required property</param>
        /// <param name="uses">Array of all uses states</param>
        public static void GetCommandDataOfAllCommands(out string[] commands, out string[] alias, out string[] description, out string[] short_description, out bool[] mod_required, out int[] uses)
        {
            List<string> cmdN = new List<string>();
            List<string> aliasN = new List<string>();
            List<string> short_DN = new List<string>();
            List<string> DN = new List<string>();
            List<bool> modReqN = new List<bool>();
            List<int> usesN = new List<int>();

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"select * from commands;";
            var r = cmd.ExecuteReader();
            while (r.Read())
            {
                cmdN.Add(r[1].ToString());
                aliasN.Add(r[2].ToString());
                short_DN.Add(r[3].ToString());
                DN.Add(r[4].ToString());
                modReqN.Add(r[5].ToString() == "1" ? true : false);
                usesN.Add(int.Parse(r[6].ToString()));
            }

            commands = cmdN.ToArray();
            alias = aliasN.ToArray();
            description = DN.ToArray();
            short_description = short_DN.ToArray();
            mod_required = modReqN.ToArray();
            uses = usesN.ToArray();

            r.Dispose();
            cmd.Dispose();
        }

        /// <summary>
        /// Highers the count of one command
        /// </summary>
        /// <param name="command">Command to update count from</param>
        public static void CommandUsed(string command)
        {
            Tools.ValidateSQLValues(ref command);

            var cmd = Program._con.CreateCommand();
            cmd.CommandText = $"update sys.commands set uses = uses + 1 where command = '{command}';";
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }
    }
}
