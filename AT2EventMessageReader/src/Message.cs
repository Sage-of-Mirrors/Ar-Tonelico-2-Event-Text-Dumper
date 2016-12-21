using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AT2EventMessageReader.src.enums;
using GameFormatReader.Common;

namespace AT2EventMessageReader.src
{
    public class Message
    {
        TextboxType boxType;
        NameID characterName;
        int characterID;
        int index;
        PortraitPosition pos;
        short unknown1;
        string message;

        public Message(EndianBinaryReader reader)
        {
            boxType = (TextboxType)reader.ReadInt32();
            characterName = (NameID)reader.ReadInt32();
            characterID = reader.ReadInt32();
            index = reader.ReadInt32();
            pos = (PortraitPosition)reader.ReadInt16();
            unknown1 = reader.ReadInt16();

            int messageSize  = reader.ReadInt32();
            List<byte> chars = new List<byte>();

            // Switch endianness because text is stored in big endian
            reader.CurrentEndian = Endian.Big;

            for (int i = 0; i < messageSize; i++)
            {
                if (reader.PeekReadByte() == (byte)'C' || reader.PeekReadByte() == (byte)'#' || reader.PeekReadByte() == (byte)'I')
                {
                    long curPos = reader.BaseStream.Position;
                    GetControlCode(reader, chars);
                    i--;
                    i += (int)(reader.BaseStream.Position - curPos);
                }
                else
                    chars.Add(reader.ReadByte());
            }

            if (chars[chars.Count - 1] != 0)
                chars[chars.Count - 1] = 0;

            reader.CurrentEndian = Endian.Little;

            Encoding shiftJis = Encoding.GetEncoding(932);

            char[] test = shiftJis.GetChars(chars.ToArray());

            string testString = new string(test);
            message = testString.Normalize(NormalizationForm.FormKD).Trim('\0');

            PadStream(reader, 4);

            while (reader.PeekReadUInt32() > 12)
                reader.SkipByte();
        }

        public void PrintMessage(EndianBinaryWriter writer)
        {
            writer.Write(string.Format("{0}", index).ToCharArray());
            writer.Write("\n".ToCharArray());

            writer.Write(string.Format("Textbox: {0}", boxType.ToString()).ToCharArray());
            writer.Write("\n".ToCharArray());

            writer.Write(string.Format("Charater Name: {0}", characterName.ToString().Replace('_', ' ')).ToCharArray());
            writer.Write("\n".ToCharArray());

            writer.Write(message.ToCharArray());
            writer.Write("\n\n".ToCharArray());
        }

        public void Write(EndianBinaryWriter writer)
        {
            writer.Write((int)boxType);
            writer.Write((int)characterName);
            writer.Write((int)characterID);
            writer.Write((int)index);
            writer.Write((short)pos);
            writer.Write((short)0x0001);
        }

        private void GetControlCode(EndianBinaryReader reader, List<byte> charList)
        {
            char firstCtrl = reader.ReadChar();

            if (firstCtrl == 'C')
            {
                char secCtrl = reader.ReadChar();

                if (secCtrl == 'R')
                {
                    charList.Add(0xA);
                }
                else if (secCtrl == 'L')
                {
                    char thirdCtrl = reader.ReadChar();
                    char fourCtrl = reader.ReadChar();

                    char[] ctrlCode = new char[] { firstCtrl, secCtrl, thirdCtrl, fourCtrl };

                    string fullCode = new string(ctrlCode);

                    switch (fullCode)
                    {
                        case "CLYL":
                            charList.AddRange(Encoding.ASCII.GetBytes("<yellow>"));
                            break;
                        case "CLR1":
                            charList.AddRange(Encoding.ASCII.GetBytes("<red1>"));
                            break;
                        case "CLRE":
                            charList.AddRange(Encoding.ASCII.GetBytes("<red2>"));
                            break;
                        case "CLEG":
                            charList.AddRange(Encoding.ASCII.GetBytes("<green>"));
                            break;
                        case "CLBR":
                            charList.AddRange(Encoding.ASCII.GetBytes("<brown>"));
                            break;
                        case "CLBL":
                            charList.AddRange(Encoding.ASCII.GetBytes("<blue>"));
                            break;
                        case "CLNR":
                            charList.AddRange(Encoding.ASCII.GetBytes("<normal>"));
                            break;
                    }
                }
                else
                {
                    charList.Add((byte)'C');
                    reader.BaseStream.Position -= 1;
                }
            }
            else if (firstCtrl == '#')
            {
                char secHym = reader.ReadChar();

                if (secHym == '0')
                {
                    charList.AddRange(Encoding.ASCII.GetBytes("<format:hymmnos>"));
                }
                else if (secHym == '#')
                {
                    charList.AddRange(Encoding.ASCII.GetBytes("<format:normal>"));
                }
                else if (secHym == '1')
                {
                    charList.AddRange(Encoding.ASCII.GetBytes("<format:small>"));
                }
            }
            else if (firstCtrl == 'I')
            {
                char secImg = reader.ReadChar();

                if (secImg == 'M')
                {
                    char[] ctrl = new char[] { firstCtrl, secImg, reader.ReadChar(), reader.ReadChar() };

                    string imgCode = new string(ctrl);

                    switch (imgCode)
                    {
                        case "IM00":
                            charList.AddRange(Encoding.ASCII.GetBytes("<X button>"));
                            break;
                        case "IM01":
                            charList.AddRange(Encoding.ASCII.GetBytes("<Circle button>"));
                            break;
                        case "IM02":
                            charList.AddRange(Encoding.ASCII.GetBytes("<Square button>"));
                            break;
                        case "IM03":
                            charList.AddRange(Encoding.ASCII.GetBytes("<Triangle button>"));
                            break;
                        case "IM04":
                            charList.AddRange(Encoding.ASCII.GetBytes("<D-Pad U/D>"));
                            break;
                        case "IM05":
                            charList.AddRange(Encoding.ASCII.GetBytes("<D-Pad L/R>"));
                            break;
                        case "IM06":
                            charList.AddRange(Encoding.ASCII.GetBytes("<D-Pad>"));
                            break;
                        case "IM07":
                            charList.AddRange(Encoding.ASCII.GetBytes("<L1 button>"));
                            break;
                        case "IM08":
                            charList.AddRange(Encoding.ASCII.GetBytes("<R1 button>"));
                            break;
                        case "IM09":
                            charList.AddRange(Encoding.ASCII.GetBytes("<Select button>"));
                            break;
                        case "IM0B":
                            charList.AddRange(Encoding.ASCII.GetBytes("<X2 button>"));
                            break;
                    }
                }
                else
                {
                    charList.Add((byte)'I');
                    reader.BaseStream.Position -= 1;
                }
            }
            else if (firstCtrl == 'B')
            {

            }
        }

        private void PadStream(EndianBinaryReader reader, int padValue)
        {
            // Pad up to a 32 byte alignment
            // Formula: (x + (n-1)) & ~(n-1)
            long nextAligned = (reader.BaseStream.Position + (padValue - 1)) & ~(padValue - 1);

            long delta = nextAligned - reader.BaseStream.Position;
            //writer.BaseStream.Position = writer.BaseStream.Length;
            for (int i = 0; i < delta; i++)
            {
                reader.SkipByte();
            }
        }
    }
}
