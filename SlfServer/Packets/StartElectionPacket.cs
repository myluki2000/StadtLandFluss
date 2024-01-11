﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Packets
{
    internal class StartElectionPacket : SlfPacketBase
    {
        public static byte PacketTypeId => 100;

        public StartElectionPacket(Guid senderId) : base(senderId)
        {
        }

        public static SlfPacketBase FromBytesInternal(IEnumerator<byte> e)
        {
            return new StartElectionPacket(e.TakeGuid());
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}