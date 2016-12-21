using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using AT2EventMessageReader.src;
using System.IO;

namespace AT2EventMessageReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] fileNames = Directory.GetFiles(@"D:\Dropbox\Ar Tonelico Docs\Event");

            List<Message> messages = new List<Message>();

            foreach (string st in fileNames)
            {
                using (FileStream stream = new FileStream(st, FileMode.Open, FileAccess.Read))
                {
                    EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Little);

                    int messageCount = reader.ReadInt32();

                    for (int i = 0; i < messageCount; i++)
                    {
                        Message mes = new Message(reader);
                        messages.Add(mes);
                    }
                }

                using (FileStream outStream = new FileStream(string.Format(@"D:\Event\{0}.txt", Path.GetFileNameWithoutExtension(st)), FileMode.Create, FileAccess.Write))
                {
                    EndianBinaryWriter writer = new EndianBinaryWriter(outStream, Endian.Big);
                    foreach (Message mes in messages)
                    {
                        mes.PrintMessage(writer);
                    }
                }

                messages.Clear();
            }
        }
    }
}
