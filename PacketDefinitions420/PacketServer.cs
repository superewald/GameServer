﻿using ENet;
using GameServerCore;
using GameServerCore.Packets.Enums;
using GameServerCore.Packets.Handlers;
using GameServerCore.Packets.Interfaces;
using GameServerCore.Packets.PacketDefinitions;
using PacketDefinitions420.Exceptions;
using System;
using System.Collections.Generic;

namespace PacketDefinitions420
{
    public class PacketServer
    {
        private Host _server;
        public BlowFish Blowfish { get; private set; }
        private uint _serverHost = Address.IPv4HostAny;

        public IPacketHandlerManager PacketHandlerManager { get; private set; }
        

        private IGame _game;

        protected const int PEER_MTU = 996;


        public void InitServer(ushort port, List<string> blowfishKeys, IGame game, NetworkHandler<ICoreRequest> netReq, NetworkHandler<ICoreResponse> netResp)
        {
            _game = game;
            _server = new Host();
            _server.Create(new Address(_serverHost,port), 32, 32, 0, 0);

            List<BlowFish> keys = new List<BlowFish>();
            foreach(var rawKey in blowfishKeys)
            {
                var key = Convert.FromBase64String(rawKey);
                if (key.Length <= 0)
                {
                    throw new InvalidKeyException($"Invalid blowfish key supplied ({key})");
                }

                keys.Add(new BlowFish(key));
            }

            PacketHandlerManager = new PacketHandlerManager(keys, _server, game, netReq, netResp);
            
        }
        public void NetLoop()
        {
            while (_server.Service(0, out var enetEvent) > 0)
            {
                switch (enetEvent.Type)
                {
                    case EventType.Connect:
                        {
                            // Set some defaults
                            enetEvent.Peer.Mtu = PEER_MTU;
                            enetEvent.Data = 0;
                        }
                        break;
                    case EventType.Receive:
                        {
                            var channel = (Channel)enetEvent.ChannelID;
                            PacketHandlerManager.HandlePacket(enetEvent.Peer, enetEvent.Packet, channel);
                            // Clean up the packet now that we're done using it.
                            enetEvent.Packet.Dispose();
                        }
                        break;
                    case EventType.Disconnect:
                        {
                            PacketHandlerManager.HandleDisconnect(enetEvent.Peer);
                        }
                        break;
                }
            }
        }
    }
}
