using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;
using Guid = WowPacketParser.Misc.Guid;

namespace WowPacketParserModule.V5_4_0_17359.Parsers
{
    public static class NpcHandler
    {
        [Parser(Opcode.CMSG_GOSSIP_HELLO)]
        public static void HandleGossipHello(Packet packet)
        {
            var guid = new byte[8];
            guid[2] = packet.ReadBit();
            guid[3] = packet.ReadBit();
            guid[0] = packet.ReadBit();
            guid[7] = packet.ReadBit();
            guid[5] = packet.ReadBit();
            guid[4] = packet.ReadBit();
            guid[6] = packet.ReadBit();
            guid[1] = packet.ReadBit();

            packet.ReadXORByte(guid, 2);
            packet.ReadXORByte(guid, 6);
            packet.ReadXORByte(guid, 0);
            packet.ReadXORByte(guid, 3);
            packet.ReadXORByte(guid, 1);
            packet.ReadXORByte(guid, 5);
            packet.ReadXORByte(guid, 7);
            packet.ReadXORByte(guid, 4);
            packet.WriteGuid("GUID", guid);
        }

        [Parser(Opcode.CMSG_GOSSIP_SELECT_OPTION)]
        public static void HandleNpcGossipSelectOption(Packet packet)
        {
            var guid = new byte[8];
            var gossipId = packet.ReadUInt32("Gossip Id");
            var menuEntry = packet.ReadUInt32("Menu Id");
            guid[7] = packet.ReadBit();
            guid[6] = packet.ReadBit();
            guid[1] = packet.ReadBit();
            var bits8 = packet.ReadBits(8);
            guid[5] = packet.ReadBit();
            guid[2] = packet.ReadBit();
            guid[4] = packet.ReadBit();
            guid[3] = packet.ReadBit();
            guid[0] = packet.ReadBit();

            packet.ReadXORBytes(guid, 1, 0, 6, 3, 7, 5, 2);
            packet.ReadWoWString("Box Text", bits8);
            packet.ReadXORByte(guid, 4);

            Storage.GossipSelects.Add(Tuple.Create(menuEntry, gossipId), null, packet.TimeSpan);
            packet.WriteGuid("GUID", guid);
        }

        [HasSniffData]
        [Parser(Opcode.SMSG_GOSSIP_MESSAGE)]
        public static void HandleNpcGossip(Packet packet)
        {
            var guid = new byte[8];
            uint[] titleLen;
            uint[] OptionTextLen;
            uint[] BoxTextLen;

            var menuId = packet.ReadUInt32("Menu Id");
            packet.ReadUInt32("Friendship Faction");
            var textId = packet.ReadUInt32("Text Id");
            packet.StartBitStream(guid, 0, 1);         
            var AmountOfOptions = packet.ReadBits("Amount of Options", 20);
            packet.StartBitStream(guid, 6, 7);
            
            OptionTextLen = new uint[AmountOfOptions];
            BoxTextLen = new uint[AmountOfOptions];
            for (var i = 0; i < AmountOfOptions; ++i)
            {
                OptionTextLen[i] = packet.ReadBits(12);
                BoxTextLen[i] = packet.ReadBits(12);
            }
            packet.StartBitStream(guid, 4, 3, 2);
                      
            var questgossips = packet.ReadBits("Amount of Quest gossips", 19);
            
            titleLen = new uint[questgossips];
            for (var i = 0; i < questgossips; ++i)
            {
                titleLen[i] = packet.ReadBits(9);
                packet.ReadBit("Change Icon", i);
            }
            guid[5] = packet.ReadBit();
            packet.ResetBitReader();
            
            for (var i = 0; i < questgossips; i++)
            {
                packet.ReadEnum<QuestFlags2>("Flags 2", TypeCode.UInt32, i);
                packet.ReadUInt32("Icon", i);    
                packet.ReadWoWString("Title", titleLen[i], i);
                packet.ReadEnum<QuestFlags>("Flags", TypeCode.UInt32, i);
                packet.ReadInt32("Level", i);
                packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID", i);                      
            }
            
            var gossip = new Gossip();

            gossip.GossipOptions = new List<GossipOption>((int)AmountOfOptions);
            for (var i = 0; i < AmountOfOptions; ++i)
            {
                var gossipOption = new GossipOption
                {
                    RequiredMoney = packet.ReadUInt32("Required money", i),
                    Index = packet.ReadUInt32("Index", i),
                    BoxText = packet.ReadWoWString("Box Text", BoxTextLen[i], i),
                    Box = packet.ReadBoolean("Box", i),
                    OptionText = packet.ReadWoWString("Text", OptionTextLen[i], i),
                    OptionIcon = packet.ReadEnum<GossipOptionIcon>("Icon", TypeCode.Byte, i),
                };

                gossip.GossipOptions.Add(gossipOption);
            }
            
            packet.ParseBitStream(guid, 3, 4, 7, 2, 1, 6, 0, 5);
            packet.WriteGuid("GUID", guid);

            var GUID = new Guid(BitConverter.ToUInt64(guid, 0));
            gossip.ObjectType = GUID.GetObjectType();
            gossip.ObjectEntry = GUID.GetEntry();

            Storage.Gossips.Add(Tuple.Create(menuId, textId), gossip, packet.TimeSpan);
            packet.AddSniffData(StoreNameType.Gossip, (int)menuId, GUID.GetEntry().ToString(CultureInfo.InvariantCulture));
        }

        [Parser(Opcode.SMSG_THREAT_UPDATE)]
        public static void HandleThreatlistUpdate(Packet packet)
        {            
            var guid1 = new byte[8];
            var guid2 = new byte[8];

            guid1[5] = packet.ReadBit();
            guid2[0] = packet.ReadBit();
            guid1[4] = packet.ReadBit();
            packet.StartBitStream(guid2, 5, 3);
            guid1[1] = packet.ReadBit();
            guid2[1] = packet.ReadBit();
            guid1[7] = packet.ReadBit();
            guid2[6] = packet.ReadBit();
            packet.StartBitStream(guid1, 2, 0, 6);
            guid2[7] = packet.ReadBit();

            var count = packet.ReadBits(21);

            var guid = new byte[count][];

            for (var i = 0; i < count; i++)
            {
                guid[i] = new byte[8];

                packet.StartBitStream(guid[i], 5, 0, 6, 2, 7, 3, 4, 1);
            }

            packet.StartBitStream(guid2, 4, 2);

            guid1[3] = packet.ReadBit();

            packet.ReadXORByte(guid2, 5);
            packet.ReadXORByte(guid2, 3);
            packet.ReadXORByte(guid2, 4);
            packet.ReadXORByte(guid2, 7);
            packet.ReadXORByte(guid1, 0);
            packet.ReadXORByte(guid1, 4);

            for (var i = 0; i < count; i++)
            {
                packet.ReadXORByte(guid[i], 6);
                packet.ReadXORByte(guid[i], 3);
                packet.ReadXORByte(guid[i], 2);
                packet.ReadXORByte(guid[i], 0);
                packet.ReadXORByte(guid[i], 5);

                packet.ReadInt32("Threat", i);

                packet.ReadXORByte(guid[i], 1);
                packet.ReadXORByte(guid[i], 7);
                packet.ReadXORByte(guid[i], 4);

                packet.WriteGuid("Hostile", guid[i], i);
            }

            packet.ReadXORByte(guid1, 5);
            packet.ReadXORByte(guid1, 1);
            packet.ReadXORByte(guid1, 7);
            packet.ReadXORByte(guid1, 2);
            packet.ReadXORByte(guid1, 6);
            packet.ReadXORByte(guid2, 0);
            packet.ReadXORByte(guid2, 2);
            packet.ReadXORByte(guid1, 3);
            packet.ReadXORByte(guid2, 6);
            packet.ReadXORByte(guid2, 1);

            packet.WriteGuid("Guid1", guid1);
            packet.WriteGuid("GUID2", guid2);

        }

        [Parser(Opcode.SMSG_HIGHEST_THREAT_UPDATE)]
        public static void HandleHighestThreatlistUpdate(Packet packet)
        {
            var guid1 = new byte[8];

            var count = packet.ReadBits("Size", 21);

            var guid2 = new byte[count][];
            for (var i = 0; i < count; ++i)
            {
                guid2[i] = new byte[8];
                packet.StartBitStream(guid2[i], 7, 4, 3, 2, 6, 1, 0, 5);
            }


            packet.StartBitStream(guid1, 2, 7, 4, 0, 1, 6, 3, 5);

            for (var i = 0; i < count; ++i)
            {
                packet.ParseBitStream(guid2[i], 2, 5, 6, 0, 1, 4);
                packet.ReadInt32("IntED", i);
                packet.ParseBitStream(guid2[i], 7, 3);
                packet.WriteGuid("Guid1D", guid2[i], i);

            }

            packet.ParseBitStream(guid1, 1, 0, 6, 3, 2, 7, 5, 4);

            packet.WriteGuid("Guid1", guid1);
        }
    }
}
