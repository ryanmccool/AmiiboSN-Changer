﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiiboSN_Changer
{
    public class Program
    {
        private const string amiitool = "amiitool.exe";
        private const string retailKey = "key_retail.bin";

        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Use asnc.exe \"FILE\" AMOUNT");
                return;
            }

            var fileOrg = args[0];
            if (!File.Exists(fileOrg))
            {
                Console.WriteLine($"Couldn't find '{fileOrg}'");
                return;
            }

            var amount = 1;
            if (args.Length == 2)
            {
                var amountStr = args[1];
                int.TryParse(amountStr, out amount);
            }

            if (!File.Exists(amiitool))
            {
                Console.WriteLine($"Couldn't find '{amiitool}' near asnc.exe");
                return;
            }

            if (!File.Exists(retailKey))
            {
                Console.WriteLine($"Couldn't find '{retailKey}' near asnc.exe");
                return;
            }

            Console.WriteLine("This tool is a C# copy of AnalogMan's Python script. Props to him");
            Console.WriteLine("_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_");
            for (int nr = 0; nr < amount; nr++)
            {
                var fileDecrypted = DecrytAmiibo(fileOrg);

                var serialParts = GenerateSerial();
                var chk1 = serialParts[0] ^ serialParts[1] ^ serialParts[2];
                var chk2 = serialParts[3] ^ serialParts[4] ^ serialParts[5] ^ serialParts[6];

                var modResult = ModifyFile(fileDecrypted, serialParts, chk1, chk2);
                if (!modResult)
                {
                    Console.Error.WriteLine($"Failed modifing '{fileDecrypted}'");
                }
                else
                {
                    EncryptAmiibo(fileDecrypted, $"New_{Path.GetFileNameWithoutExtension(fileOrg)}_{nr+1}.bin");
                }

                Console.WriteLine("Cleaning up");
                File.Delete(fileDecrypted);
                Console.WriteLine();
                Console.WriteLine();
            }
            Console.WriteLine("Done");
        }

        private static bool ModifyFile(string orgFilePath, int[] serialParts, int chk1, int chk2)
        {
            Console.WriteLine($"Modifing '{Path.GetFileName(orgFilePath)}'");
            try
            {
                using (Stream stream = File.Open(orgFilePath, FileMode.Open))
                {
                    stream.Position = 0;
                    stream.Write(new[] { (byte)chk2 }, 0, 1);

                    stream.Position = 0x1D4;
                    var serialBytes = new byte[8];
                    serialBytes[0] = (byte)chk1;
                    for (int i = 1; i < serialBytes.Length; i++)
                    {
                        serialBytes[i] = (byte)serialParts[i - 1];
                    }
                    stream.Write(serialBytes, 0, serialBytes.Length);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int[] GenerateSerial()
        {
            Console.WriteLine("Generating new \"unique\" serial");
            var serialParts = new int[7];
            var r = new Random();
            for (int i = 0; i < serialParts.Length; i++)
            {
                serialParts[i] = r.Next(0, 255);
            }

            return serialParts;
        }

        private static string DecrytAmiibo(string orgFilePath)
        {
            Console.WriteLine($"Decrypting '{Path.GetFileName(orgFilePath)}'");
            var fileDecrypted = $"{Path.GetFileName(orgFilePath)}.decrypted";

            if (File.Exists(fileDecrypted))
            {
                Console.WriteLine("Found old decrypted file.");
                Console.WriteLine("Press ENTER to delete it or close this tool now.");
                Console.ReadLine();

                File.Delete(fileDecrypted);
            }

            CasllAmiitool($"-d -k {retailKey} -i \"{orgFilePath}\" -o \"{fileDecrypted}\"");

            return fileDecrypted;
        }

        private static void EncryptAmiibo(string name, string saveName)
        {
            Console.WriteLine($"Encrypting '{name}' as '{saveName}'");
            CasllAmiitool($"-e -k {retailKey} -i \"{name}\" -o \"{saveName}\"");
        }

        private static void CasllAmiitool(string args)
        {
            var amiitoolProc = new Process();
            amiitoolProc.StartInfo = new ProcessStartInfo()
            {
                FileName = amiitool,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            amiitoolProc.Start();
            amiitoolProc.WaitForExit();
            string error = amiitoolProc.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                AmiitoolErrorDataReceived(error);
            }
        }

        private static void AmiitoolErrorDataReceived(string error)
        {
            Console.Error.WriteLine("Got error calling amiitool.exe:");
            Console.Error.WriteLine(error);
            Environment.Exit(1);
        }
    }
}
