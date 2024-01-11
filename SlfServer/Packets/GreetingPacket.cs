﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Packets
{
    internal class GreetingPacket : SlfPacketBase
    {
        public static byte PacketTypeId => 50;

        public GreetingPacket(Guid senderId) : base(senderId)
        {
        }

        public static SlfPacketBase FromBytesInternal(IEnumerator<byte> bytes)
        {
            return new GreetingPacket(bytes.TakeGuid());
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}